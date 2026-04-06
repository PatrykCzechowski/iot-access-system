/*
 * AccessControl — Card Reader + Lock Controller (all-in-one)
 *
 * Hardware: Arduino Nano ESP32 (ESP32-S3) + PN532 NFC/RFID (I2C)
 *           + HW-482 Relay Module + RGB LED (CC) + TMB12A05 Buzzer
 *
 * Pin connections:
 *   PN532 SDA  → A4 (GPIO11) — default I2C SDA
 *   PN532 SCL  → A5 (GPIO12) — default I2C SCL
 *   PN532 VCC  → 3V3
 *   PN532 GND  → GND
 *   PN532 DIP: SW1=ON, SW2=OFF (I2C mode)
 *
 *   LED_R      → D2 (GPIO5)
 *   LED_G      → D3 (GPIO6)
 *   LED_B      → D4 (GPIO7)
 *   BUZZER     → D5 (GPIO8)  — TMB12A05 active buzzer, 5V
 *   HW-482 IN1 → D6 (GPIO9)  — relay, active LOW
 *   HW-482 VCC → 5V (VUSB)
 *   HW-482 GND → GND
 *   RESET BTN  → D7 (GPIO10) ↔ GND (INPUT_PULLUP, hold 3s at boot)
 */

#include <Arduino.h>
#include <Wire.h>
#include <Adafruit_PN532.h>
#include <WiFi.h>
#include <ESPmDNS.h>
#include <PubSubClient.h>
#include <WiFiManager.h>
#include <ArduinoJson.h>
#include <WebServer.h>
#include <EEPROM.h>

// ═══════════════════════════════════════════════════════════
//  CONFIGURATION
// ═══════════════════════════════════════════════════════════

#define LED_R_PIN      D2   // GPIO5
#define LED_G_PIN      D3   // GPIO6
#define LED_B_PIN      D4   // GPIO7
#define BUZZER_PIN     D5   // GPIO8
#define RELAY_PIN      D6   // GPIO9  — HW-482 relay, active LOW
#define RESET_BTN_PIN  D7   // GPIO10 — factory reset button, active LOW

#define MQTT_TOPIC_PREFIX "accesscontrol"

#define DEVICE_MODEL     "NanoESP32-CardReader"
#define DEVICE_FEATURES  "17"
#define FIRMWARE_VERSION "2.0.0"

#define EEPROM_SIZE              256
#define EEPROM_DEVID_MAGIC       0xAC
#define EEPROM_DEVID_MAGIC_ADDR  0
#define EEPROM_DEVID_HWID_ADDR   1
#define EEPROM_DEVID_HWID_LEN    36
#define EEPROM_CFG_MAGIC         0xD0
#define EEPROM_CFG_MAGIC_ADDR    64
#define EEPROM_CFG_DATA_ADDR     65

// MQTT provisioning EEPROM layout (addr 96-227)
#define EEPROM_MQTT_MAGIC        0x4D
#define EEPROM_MQTT_MAGIC_ADDR   96
#define EEPROM_MQTT_DATA_ADDR    97
#define EEPROM_MQTT_BROKER_LEN   64
#define EEPROM_MQTT_USER_LEN     32
#define EEPROM_MQTT_PASS_LEN     32

#define DEFAULT_HEARTBEAT_INTERVAL  30000
#define DEFAULT_ENROLLMENT_TIMEOUT  30000
#define DEFAULT_BUZZER_ENABLED      true
#define DEFAULT_LED_BRIGHTNESS      128
#define DEFAULT_LOCK_DURATION       5000
#define CARD_READ_COOLDOWN          1500
#define FACTORY_RESET_HOLD_MS       3000
#define RESULT_TIMEOUT_MS           5000

// ═══════════════════════════════════════════════════════════
//  NFC
// ═══════════════════════════════════════════════════════════
// I2C polling mode: pass -1 for IRQ so library polls I2C ready byte
// instead of waiting on an unconnected IRQ pin
Adafruit_PN532 nfc(-1, -1, &Wire);

// ═══════════════════════════════════════════════════════════
//  GLOBALS
// ═══════════════════════════════════════════════════════════
WiFiClient espClient;
PubSubClient mqtt(espClient);

static char hwid[37] = {0};
static bool nfcAvailable = false;

struct MqttConfig {
    char     broker[64];
    uint16_t port;
    char     username[32];
    char     password[32];
};
static MqttConfig mqttCfg;

struct DeviceConfig {
    uint32_t heartbeatInterval;
    uint32_t enrollmentTimeout;
    bool     buzzerEnabled;
    uint8_t  ledBrightness;
    uint32_t lockOpenDuration;
};
static DeviceConfig cfg;

enum DeviceMode { MODE_NORMAL, MODE_ENROLLMENT };
static DeviceMode currentMode = MODE_NORMAL;
static unsigned long enrollmentStartTime = 0;
static unsigned long enrollmentTimeoutMs = 0;
static unsigned long lastHeartbeat = 0;
static unsigned long lastCardRead = 0;
static unsigned long lastMqttReconnect = 0;
static unsigned long mqttReconnectDelay = 2000;

// Relay / lock state
static unsigned long lockCloseTime = 0;

// LED feedback timer (non-blocking restore to default color)
static unsigned long ledFeedbackEndTime = 0;

// Enrollment blink state
static unsigned long enrollBlinkTimer = 0;
static bool enrollBlinkState = false;

// Card result waiting
static bool waitingForResult = false;
static unsigned long resultTimeoutTime = 0;

static bool mqttProvisioned = false;
static WebServer provisionServer(80);
static bool provisionServerActive = false;

static uint8_t lastUid[7] = {0};
static uint8_t lastUidLen = 0;

static char topicBuf[128];
static char payloadBuf[256];

// ═══════════════════════════════════════════════════════════
//  HELPERS
// ═══════════════════════════════════════════════════════════
void uidToHex(const uint8_t* uid, uint8_t len, char* out) {
    for (uint8_t i = 0; i < len; i++) {
        sprintf(out + (i * 2), "%02X", uid[i]);
    }
    out[len * 2] = '\0';
}

void setStatusLed(uint8_t r, uint8_t g, uint8_t b) {
    // Use digitalWrite only — analogWrite/PWM creates timer interrupts
    // that corrupt software I2C timing on ESP8266
    digitalWrite(LED_R_PIN, r > 127 ? HIGH : LOW);
    digitalWrite(LED_G_PIN, g > 127 ? HIGH : LOW);
    digitalWrite(LED_B_PIN, b > 127 ? HIGH : LOW);
}

void beepOk() {
    digitalWrite(BUZZER_PIN, HIGH);
    delay(80);
    digitalWrite(BUZZER_PIN, LOW);
}

void beepError() {
    for (int i = 0; i < 3; i++) {
        digitalWrite(BUZZER_PIN, HIGH);
        delay(60);
        digitalWrite(BUZZER_PIN, LOW);
        delay(60);
    }
}

void beepDouble() {
    digitalWrite(BUZZER_PIN, HIGH);
    delay(80);
    digitalWrite(BUZZER_PIN, LOW);
    delay(60);
    digitalWrite(BUZZER_PIN, HIGH);
    delay(80);
    digitalWrite(BUZZER_PIN, LOW);
}

void setDefaultLed() {
    if (currentMode == MODE_ENROLLMENT) return;
    if (nfcAvailable) setStatusLed(0, 0, 255);   // blue = ready
    else              setStatusLed(255, 165, 0);  // orange = NFC unavailable
}

// ═══════════════════════════════════════════════════════════
//  EEPROM
// ═══════════════════════════════════════════════════════════
void eepromPutU32(int addr, uint32_t val) {
    EEPROM.write(addr,     (val)       & 0xFF);
    EEPROM.write(addr + 1, (val >> 8)  & 0xFF);
    EEPROM.write(addr + 2, (val >> 16) & 0xFF);
    EEPROM.write(addr + 3, (val >> 24) & 0xFF);
}

uint32_t eepromGetU32(int addr) {
    return (uint32_t)EEPROM.read(addr)
         | ((uint32_t)EEPROM.read(addr + 1) << 8)
         | ((uint32_t)EEPROM.read(addr + 2) << 16)
         | ((uint32_t)EEPROM.read(addr + 3) << 24);
}

void deviceIdInit() {
    EEPROM.begin(EEPROM_SIZE);
    if (EEPROM.read(EEPROM_DEVID_MAGIC_ADDR) == EEPROM_DEVID_MAGIC) {
        for (int i = 0; i < EEPROM_DEVID_HWID_LEN; i++) {
            hwid[i] = (char)EEPROM.read(EEPROM_DEVID_HWID_ADDR + i);
        }
        hwid[EEPROM_DEVID_HWID_LEN] = '\0';
        EEPROM.end();
        return;
    }
    uint8_t mac[6];
    WiFi.macAddress(mac);
    uint8_t bytes[16] = {
        0xAC, 0xCE, 0x55, 0x10, 0x00, 0x00,
        mac[0], mac[1], mac[2], mac[3], mac[4], mac[5],
        0x00, 0x00, 0x00, 0x00
    };
    bytes[6] = (bytes[6] & 0x0F) | 0x50;
    bytes[8] = (bytes[8] & 0x3F) | 0x80;
    snprintf(hwid, 37,
        "%02x%02x%02x%02x-%02x%02x-%02x%02x-%02x%02x-%02x%02x%02x%02x%02x%02x",
        bytes[0], bytes[1], bytes[2], bytes[3],
        bytes[4], bytes[5], bytes[6], bytes[7],
        bytes[8], bytes[9], bytes[10], bytes[11],
        bytes[12], bytes[13], bytes[14], bytes[15]);
    EEPROM.write(EEPROM_DEVID_MAGIC_ADDR, EEPROM_DEVID_MAGIC);
    for (int i = 0; i < EEPROM_DEVID_HWID_LEN; i++) {
        EEPROM.write(EEPROM_DEVID_HWID_ADDR + i, (uint8_t)hwid[i]);
    }
    EEPROM.commit();
    EEPROM.end();
    Serial.printf("[DeviceId] Generated: %s\n", hwid);
}

void configLoad() {
    EEPROM.begin(EEPROM_SIZE);
    if (EEPROM.read(EEPROM_CFG_MAGIC_ADDR) == EEPROM_CFG_MAGIC) {
        int a = EEPROM_CFG_DATA_ADDR;
        cfg.heartbeatInterval = eepromGetU32(a); a += 4;
        cfg.enrollmentTimeout = eepromGetU32(a); a += 4;
        cfg.buzzerEnabled     = EEPROM.read(a++) != 0;
        cfg.ledBrightness     = EEPROM.read(a++);
        cfg.lockOpenDuration  = eepromGetU32(a); a += 4;
        if (cfg.lockOpenDuration == 0 || cfg.lockOpenDuration > 60000)
            cfg.lockOpenDuration = DEFAULT_LOCK_DURATION;
    } else {
        cfg.heartbeatInterval = DEFAULT_HEARTBEAT_INTERVAL;
        cfg.enrollmentTimeout = DEFAULT_ENROLLMENT_TIMEOUT;
        cfg.buzzerEnabled     = DEFAULT_BUZZER_ENABLED;
        cfg.ledBrightness     = DEFAULT_LED_BRIGHTNESS;
        cfg.lockOpenDuration  = DEFAULT_LOCK_DURATION;
    }
    EEPROM.end();
}

void configSave() {
    EEPROM.begin(EEPROM_SIZE);
    EEPROM.write(EEPROM_CFG_MAGIC_ADDR, EEPROM_CFG_MAGIC);
    int a = EEPROM_CFG_DATA_ADDR;
    eepromPutU32(a, cfg.heartbeatInterval); a += 4;
    eepromPutU32(a, cfg.enrollmentTimeout); a += 4;
    EEPROM.write(a++, cfg.buzzerEnabled ? 1 : 0);
    EEPROM.write(a++, cfg.ledBrightness);
    eepromPutU32(a, cfg.lockOpenDuration); a += 4;
    EEPROM.commit();
    EEPROM.end();
}

// ═══════════════════════════════════════════════════════════
//  EEPROM — MQTT provisioning
// ═══════════════════════════════════════════════════════════
bool mqttConfigLoad() {
    EEPROM.begin(EEPROM_SIZE);
    bool valid = EEPROM.read(EEPROM_MQTT_MAGIC_ADDR) == EEPROM_MQTT_MAGIC;
    if (valid) {
        int a = EEPROM_MQTT_DATA_ADDR;
        for (int i = 0; i < EEPROM_MQTT_BROKER_LEN; i++) mqttCfg.broker[i] = (char)EEPROM.read(a++);
        mqttCfg.broker[EEPROM_MQTT_BROKER_LEN - 1] = '\0';
        mqttCfg.port = (uint16_t)EEPROM.read(a) | ((uint16_t)EEPROM.read(a + 1) << 8); a += 2;
        for (int i = 0; i < EEPROM_MQTT_USER_LEN; i++) mqttCfg.username[i] = (char)EEPROM.read(a++);
        mqttCfg.username[EEPROM_MQTT_USER_LEN - 1] = '\0';
        for (int i = 0; i < EEPROM_MQTT_PASS_LEN; i++) mqttCfg.password[i] = (char)EEPROM.read(a++);
        mqttCfg.password[EEPROM_MQTT_PASS_LEN - 1] = '\0';
        Serial.printf("[MQTT Config] Loaded: %s:%d user=%s\n", mqttCfg.broker, mqttCfg.port, mqttCfg.username);
    }
    EEPROM.end();
    return valid;
}

void mqttConfigSave() {
    EEPROM.begin(EEPROM_SIZE);
    EEPROM.write(EEPROM_MQTT_MAGIC_ADDR, EEPROM_MQTT_MAGIC);
    int a = EEPROM_MQTT_DATA_ADDR;
    for (int i = 0; i < EEPROM_MQTT_BROKER_LEN; i++) EEPROM.write(a++, (uint8_t)mqttCfg.broker[i]);
    EEPROM.write(a, mqttCfg.port & 0xFF); EEPROM.write(a + 1, (mqttCfg.port >> 8) & 0xFF); a += 2;
    for (int i = 0; i < EEPROM_MQTT_USER_LEN; i++) EEPROM.write(a++, (uint8_t)mqttCfg.username[i]);
    for (int i = 0; i < EEPROM_MQTT_PASS_LEN; i++) EEPROM.write(a++, (uint8_t)mqttCfg.password[i]);
    EEPROM.commit();
    EEPROM.end();
    Serial.printf("[MQTT Config] Saved: %s:%d\n", mqttCfg.broker, mqttCfg.port);
}

// ═══════════════════════════════════════════════════════════
//  PROVISIONING — passive HTTP server (push-based)
// ═══════════════════════════════════════════════════════════
void handleProvisionPost() {
    if (mqttProvisioned) {
        provisionServer.send(409, "application/json", "{\"error\":\"already provisioned\"}");
        return;
    }

    if (!provisionServer.hasArg("plain")) {
        provisionServer.send(400, "application/json", "{\"error\":\"missing body\"}");
        return;
    }

    String body = provisionServer.arg("plain");
    JsonDocument doc;
    if (deserializeJson(doc, body)) {
        provisionServer.send(400, "application/json", "{\"error\":\"invalid JSON\"}");
        return;
    }

    const char* broker = doc["broker"];
    int port = doc["port"] | 0;
    if (!broker || strlen(broker) == 0 || port <= 0) {
        provisionServer.send(400, "application/json", "{\"error\":\"broker and port required\"}");
        return;
    }

    const char* username = doc["username"];
    const char* password = doc["password"];

    strlcpy(mqttCfg.broker, broker, sizeof(mqttCfg.broker));
    mqttCfg.port = (uint16_t)port;
    strlcpy(mqttCfg.username, username ? username : "", sizeof(mqttCfg.username));
    strlcpy(mqttCfg.password, password ? password : "", sizeof(mqttCfg.password));
    mqttConfigSave();

    Serial.printf("[Provision] Received: broker=%s:%d user=%s\n",
                  mqttCfg.broker, mqttCfg.port, mqttCfg.username);

    char response[128];
    snprintf(response, sizeof(response), "{\"status\":\"ok\",\"hwid\":\"%s\"}", hwid);
    provisionServer.send(200, "application/json", response);

    // Restart for clean setup with new MQTT config
    Serial.println("[Provision] Restarting...");
    delay(500);
    ESP.restart();
}

void provisionServerStart() {
    provisionServer.on("/api/provision", HTTP_POST, handleProvisionPost);
    provisionServer.onNotFound([]() {
        provisionServer.send(404, "application/json", "{\"error\":\"not found\"}");
    });
    provisionServer.begin();
    provisionServerActive = true;
    Serial.println("[Provision] HTTP server listening on port 80");
}

void mqttConfigClear() {
    EEPROM.begin(EEPROM_SIZE);
    EEPROM.write(EEPROM_MQTT_MAGIC_ADDR, 0xFF);
    EEPROM.commit();
    EEPROM.end();
    Serial.println("[MQTT Config] Cleared");
}

/// Handles serial debug commands (non-blocking, called from loop).
void handleSerialCommands() {
    if (!Serial.available()) return;
    String cmd = Serial.readStringUntil('\n');
    cmd.trim();
    if (cmd.length() == 0) return;

    if (cmd == "mqtt_reset") {
        Serial.println("[CMD] Clearing MQTT config and restarting...");
        mqttConfigClear();
        delay(500);
        ESP.restart();
    }
    else if (cmd.startsWith("mqtt_set ")) {
        String args = cmd.substring(9);
        args.trim();
        int spaceIdx = args.indexOf(' ');
        String ip = (spaceIdx > 0) ? args.substring(0, spaceIdx) : args;
        uint16_t port = (spaceIdx > 0) ? (uint16_t)args.substring(spaceIdx + 1).toInt() : 1883;
        if (ip.length() == 0 || port == 0) {
            Serial.println("[CMD] Usage: mqtt_set <broker_ip> [port]");
        } else {
            strlcpy(mqttCfg.broker, ip.c_str(), sizeof(mqttCfg.broker));
            mqttCfg.port = port;
            mqttCfg.username[0] = '\0';
            mqttCfg.password[0] = '\0';
            mqttConfigSave();
            Serial.printf("[CMD] Broker set to %s:%d — restarting...\n", mqttCfg.broker, mqttCfg.port);
            delay(500);
            ESP.restart();
        }
    }
    else if (cmd == "help") {
        Serial.println("Commands: mqtt_reset, mqtt_set <ip> [port], help");
    }
}

// ═══════════════════════════════════════════════════════════
//  MQTT
// ═══════════════════════════════════════════════════════════
void mqttPublishCard(const char* action, const uint8_t* uid, uint8_t uidLen) {
    if (!mqtt.connected()) return;
    char uidHex[15];
    uidToHex(uid, uidLen, uidHex);
    snprintf(topicBuf, sizeof(topicBuf), "%s/%s/card/%s", MQTT_TOPIC_PREFIX, hwid, action);
    snprintf(payloadBuf, sizeof(payloadBuf), "{\"uid\":\"%s\",\"uidLen\":%d}", uidHex, uidLen);
    mqtt.publish(topicBuf, payloadBuf);
    Serial.printf("[Card] %s: %s\n", action, uidHex);
}

void sendHeartbeat() {
    if (!mqtt.connected()) return;
    snprintf(topicBuf, sizeof(topicBuf), "%s/%s/heartbeat", MQTT_TOPIC_PREFIX, hwid);
    snprintf(payloadBuf, sizeof(payloadBuf),
        "{\"status\":\"online\",\"uptime\":%lu,\"rssi\":%d,\"freeHeap\":%u,\"nfc\":%s}",
        millis() / 1000, WiFi.RSSI(), ESP.getFreeHeap(),
        nfcAvailable ? "true" : "false");
    mqtt.publish(topicBuf, payloadBuf, true);
}

void sendAnnounce() {
    if (!mqtt.connected()) return;
    uint8_t mac[6];
    WiFi.macAddress(mac);
    char macStr[18];
    snprintf(macStr, sizeof(macStr), "%02X:%02X:%02X:%02X:%02X:%02X",
             mac[0], mac[1], mac[2], mac[3], mac[4], mac[5]);
    snprintf(topicBuf, sizeof(topicBuf), "%s/%s/announce", MQTT_TOPIC_PREFIX, hwid);
    snprintf(payloadBuf, sizeof(payloadBuf),
        "{\"model\":\"%s\",\"ip\":\"%s\",\"mac\":\"%s\",\"features\":%s,\"fw\":\"%s\"}",
        DEVICE_MODEL, WiFi.localIP().toString().c_str(), macStr, DEVICE_FEATURES, FIRMWARE_VERSION);
    mqtt.publish(topicBuf, payloadBuf, true);
    Serial.println("[MQTT] Announce sent");
}

void mqttCallback(char* topic, byte* payload, unsigned int length) {
    Serial.printf("[MQTT] %s (%u bytes)\n", topic, length);
    char json[256];
    unsigned int copyLen = (length < sizeof(json) - 1) ? length : sizeof(json) - 1;
    memcpy(json, payload, copyLen);
    json[copyLen] = '\0';

    // ---- config/set ----
    char configTopic[128];
    snprintf(configTopic, sizeof(configTopic), "%s/%s/config/set", MQTT_TOPIC_PREFIX, hwid);
    if (strcmp(topic, configTopic) == 0) {
        JsonDocument doc;
        if (deserializeJson(doc, json)) return;
        bool changed = false;
        if (doc["heartbeatInterval"].is<uint32_t>()) { cfg.heartbeatInterval = doc["heartbeatInterval"]; changed = true; }
        if (doc["enrollmentTimeout"].is<uint32_t>()) { cfg.enrollmentTimeout = doc["enrollmentTimeout"]; changed = true; }
        if (doc["buzzerEnabled"].is<bool>())         { cfg.buzzerEnabled = doc["buzzerEnabled"];         changed = true; }
        if (doc["ledBrightness"].is<uint8_t>())      { cfg.ledBrightness = doc["ledBrightness"];         changed = true; }
        if (doc["lockOpenDuration"].is<uint32_t>())  { cfg.lockOpenDuration = doc["lockOpenDuration"];   changed = true; }
        if (changed) configSave();
        char ackTopic[128];
        snprintf(ackTopic, sizeof(ackTopic), "%s/%s/config/ack", MQTT_TOPIC_PREFIX, hwid);
        mqtt.publish(ackTopic, "{\"applied\":true}");
        Serial.println("[Config] Updated");
        return;
    }

    // ---- card/enroll ----
    char enrollTopic[128];
    snprintf(enrollTopic, sizeof(enrollTopic), "%s/%s/card/enroll", MQTT_TOPIC_PREFIX, hwid);
    if (strcmp(topic, enrollTopic) == 0) {
        JsonDocument doc;
        if (deserializeJson(doc, json)) return;
        const char* action = doc["action"] | "start";
        if (strcmp(action, "cancel") == 0) {
            if (currentMode == MODE_ENROLLMENT) {
                currentMode = MODE_NORMAL;
                setDefaultLed();
                Serial.println("[Enroll] Cancelled");
            }
            return;
        }
        uint32_t timeoutSec = doc["timeout"] | (cfg.enrollmentTimeout / 1000);
        enrollmentTimeoutMs = timeoutSec * 1000UL;
        enrollmentStartTime = millis();
        enrollBlinkTimer = millis();
        enrollBlinkState = true;
        currentMode = MODE_ENROLLMENT;
        Serial.printf("[Enroll] Active (timeout %us)\n", timeoutSec);
        return;
    }

    // ---- lock/command ----
    char lockTopic[128];
    snprintf(lockTopic, sizeof(lockTopic), "%s/%s/lock/command", MQTT_TOPIC_PREFIX, hwid);
    if (strcmp(topic, lockTopic) == 0) {
        JsonDocument doc;
        if (deserializeJson(doc, json)) return;
        const char* action = doc["action"] | "";
        if (strcmp(action, "open") == 0) {
            uint32_t durationSec = doc["durationSec"] | (cfg.lockOpenDuration / 1000);
            digitalWrite(RELAY_PIN, LOW);  // HW-482 active LOW = open
            lockCloseTime = millis() + durationSec * 1000UL;
            if (cfg.buzzerEnabled) beepDouble();
            snprintf(topicBuf, sizeof(topicBuf), "%s/%s/lock/status", MQTT_TOPIC_PREFIX, hwid);
            mqtt.publish(topicBuf, "{\"state\":\"open\"}");
            Serial.printf("[Lock] OPEN for %lus\n", (unsigned long)durationSec);
        } else if (strcmp(action, "close") == 0) {
            digitalWrite(RELAY_PIN, HIGH);  // HIGH = closed/locked
            lockCloseTime = 0;
            snprintf(topicBuf, sizeof(topicBuf), "%s/%s/lock/status", MQTT_TOPIC_PREFIX, hwid);
            mqtt.publish(topicBuf, "{\"state\":\"closed\"}");
            Serial.println("[Lock] CLOSED");
        }
        return;
    }

    // ---- card/result ----
    char resultTopic[128];
    snprintf(resultTopic, sizeof(resultTopic), "%s/%s/card/result", MQTT_TOPIC_PREFIX, hwid);
    if (strcmp(topic, resultTopic) == 0) {
        waitingForResult = false;
        JsonDocument doc;
        if (deserializeJson(doc, json)) return;
        bool granted = doc["granted"] | false;
        if (granted) {
            setStatusLed(0, 255, 0);  // green
            if (cfg.buzzerEnabled) beepDouble();
            // All-in-one: open own relay immediately on granted result
            digitalWrite(RELAY_PIN, LOW);  // HW-482 active LOW = open
            uint32_t durationSec = doc["durationSec"] | (cfg.lockOpenDuration / 1000);
            lockCloseTime = millis() + durationSec * 1000UL;
            snprintf(topicBuf, sizeof(topicBuf), "%s/%s/lock/status", MQTT_TOPIC_PREFIX, hwid);
            mqtt.publish(topicBuf, "{\"state\":\"open\"}");
            Serial.printf("[Access] GRANTED — lock open for %lus\n", (unsigned long)durationSec);
        } else {
            setStatusLed(255, 0, 0);  // red
            if (cfg.buzzerEnabled) beepError();
            Serial.println("[Access] DENIED");
        }
        ledFeedbackEndTime = millis() + 2000;
        return;
    }
}

void mqttConnect() {
    char lwtTopic[128];
    snprintf(lwtTopic, sizeof(lwtTopic), "%s/%s/heartbeat", MQTT_TOPIC_PREFIX, hwid);
    const char* user = strlen(mqttCfg.username) > 0 ? mqttCfg.username : nullptr;
    const char* pass = strlen(mqttCfg.password) > 0 ? mqttCfg.password : nullptr;
    bool ok = mqtt.connect(hwid, user, pass, lwtTopic, 1, true, "{\"status\":\"offline\"}");
    if (ok) {
        Serial.println("[MQTT] Connected!");
        mqttReconnectDelay = 2000;
        snprintf(topicBuf, sizeof(topicBuf), "%s/%s/config/set", MQTT_TOPIC_PREFIX, hwid);
        mqtt.subscribe(topicBuf, 1);
        snprintf(topicBuf, sizeof(topicBuf), "%s/%s/card/enroll", MQTT_TOPIC_PREFIX, hwid);
        mqtt.subscribe(topicBuf, 1);
        snprintf(topicBuf, sizeof(topicBuf), "%s/%s/lock/command", MQTT_TOPIC_PREFIX, hwid);
        mqtt.subscribe(topicBuf, 1);
        snprintf(topicBuf, sizeof(topicBuf), "%s/%s/card/result", MQTT_TOPIC_PREFIX, hwid);
        mqtt.subscribe(topicBuf, 1);
        sendAnnounce();
    } else {
        Serial.printf("[MQTT] Failed rc=%d\n", mqtt.state());
    }
}

void mqttReconnectLoop() {
    if (mqtt.connected()) { mqtt.loop(); return; }
    unsigned long now = millis();
    if (now - lastMqttReconnect < mqttReconnectDelay) return;
    lastMqttReconnect = now;
    Serial.printf("[MQTT] Reconnecting (delay %lums)...\n", mqttReconnectDelay);
    mqttConnect();
    if (!mqtt.connected()) {
        mqttReconnectDelay = min(mqttReconnectDelay * 2, (unsigned long)30000);
    }
}

// ═══════════════════════════════════════════════════════════
//  mDNS
// ═══════════════════════════════════════════════════════════
void mdnsInit() {
    char hostname[32];
    snprintf(hostname, sizeof(hostname), "ac-%.8s", hwid);
    WiFi.setHostname(hostname);
    if (!MDNS.begin(hostname)) {
        Serial.println("[mDNS] Failed!");
        return;
    }
    MDNS.addService("_accesscontrol", "_tcp", 80);
    MDNS.addServiceTxt("_accesscontrol", "_tcp", "hwid", (const char*)hwid);
    MDNS.addServiceTxt("_accesscontrol", "_tcp", "model", DEVICE_MODEL);
    uint8_t mac[6];
    WiFi.macAddress(mac);
    char macStr[18];
    snprintf(macStr, sizeof(macStr), "%02X:%02X:%02X:%02X:%02X:%02X",
             mac[0], mac[1], mac[2], mac[3], mac[4], mac[5]);
    MDNS.addServiceTxt("_accesscontrol", "_tcp", "mac", (const char*)macStr);
    MDNS.addServiceTxt("_accesscontrol", "_tcp", "features", DEVICE_FEATURES);
    MDNS.addServiceTxt("_accesscontrol", "_tcp", "fw", FIRMWARE_VERSION);
    Serial.printf("[mDNS] %s advertising\n", hostname);
}

// ═══════════════════════════════════════════════════════════
//  SETUP
// ═══════════════════════════════════════════════════════════
void setup() {
    Serial.begin(115200);
    delay(500);

    // ---- I2C bus recovery ----
    // PN532 can hold SCL/SDA low if ESP32 reset during I2C transaction (e.g. DFU flash).
    // Send 18 clock pulses + STOP condition to release the bus before Wire.begin().
    pinMode(A5, OUTPUT);     // SCL
    pinMode(A4, INPUT_PULLUP); // SDA
    for (int i = 0; i < 18; i++) {
        digitalWrite(A5, LOW);
        delayMicroseconds(5);
        digitalWrite(A5, HIGH);
        delayMicroseconds(5);
    }
    // STOP condition: SDA LOW→HIGH while SCL is HIGH
    pinMode(A4, OUTPUT);
    digitalWrite(A4, LOW);
    delayMicroseconds(5);
    digitalWrite(A5, HIGH);
    delayMicroseconds(5);
    digitalWrite(A4, HIGH);
    delayMicroseconds(5);
    // Release pins for Wire library
    pinMode(A4, INPUT);
    pinMode(A5, INPUT);
    delay(50);

    // ---- I2C init ----
    // Nano ESP32 default Wire.begin() uses GPIO21/22, NOT A4/A5!
    // PN532 is wired to A4(SDA) and A5(SCL) physical pins
    Wire.begin(A4, A5);
    Wire.setTimeOut(3000);  // 3s — PN532 needs clock-stretching margin for RF ops
    Serial.printf("[I2C] Wire.begin(A4=GPIO%d, A5=GPIO%d) timeout=3000ms\n", (int)A4, (int)A5);

    // I2C scan — find connected devices
    Serial.print("[I2C] Scanning: ");
    int found = 0;
    for (uint8_t addr = 1; addr < 127; addr++) {
        Wire.beginTransmission(addr);
        uint8_t err = Wire.endTransmission();
        if (err == 0) {
            Serial.printf("0x%02X ", addr);
            found++;
        }
    }
    Serial.printf("(%d devices)\n", found);

    if (found == 0) {
        Serial.println("[I2C] No devices! Check wiring: SDA->A4, SCL->A5, VCC->3V3, GND->GND");
    }

    // ---- NFC init ----
    nfc.begin();
    delay(100);
    uint32_t versiondata = nfc.getFirmwareVersion();
    if (!versiondata) {
        Serial.println("[NFC] Attempt 1 failed, retrying...");
        delay(1000);
        nfc.begin();
        delay(200);
        versiondata = nfc.getFirmwareVersion();
    }
    if (!versiondata) {
        Serial.println("[NFC] PN532 NOT FOUND!");
        nfcAvailable = false;
    } else {
        nfcAvailable = true;
        Serial.printf("[NFC] PN5%02X fw %d.%d — OK!\n",
            (versiondata >> 24) & 0xFF,
            (versiondata >> 16) & 0xFF,
            (versiondata >> 8) & 0xFF);
        nfc.SAMConfig();
        nfc.setPassiveActivationRetries(1);  // 1 retry = 2 RF attempts
    }

    // ---- Banner (after NFC so we see result) ----
    Serial.println("\n========================================");
    Serial.println("  AccessControl Card Reader + Lock (v2)");
    Serial.println("========================================");
    Serial.printf("[NFC] Status: %s\n", nfcAvailable ? "OK" : "NOT FOUND");

    // ---- LED, Buzzer, Relay, Reset Button ----
    pinMode(LED_R_PIN, OUTPUT);
    pinMode(LED_G_PIN, OUTPUT);
    pinMode(LED_B_PIN, OUTPUT);
    pinMode(BUZZER_PIN, OUTPUT);
    digitalWrite(BUZZER_PIN, LOW);
    pinMode(RELAY_PIN, OUTPUT);
    digitalWrite(RELAY_PIN, HIGH);  // HW-482 active LOW → HIGH = closed/locked
    pinMode(RESET_BTN_PIN, INPUT_PULLUP);

    // ---- Config & Device ID ----
    configLoad();
    deviceIdInit();
    Serial.printf("HWID: %s\n", hwid);

    // ---- Factory Reset (hold button during boot) ----
    delay(100);  // debounce stabilization
    if (digitalRead(RESET_BTN_PIN) == LOW) {
        Serial.println("[Reset] Button pressed — hold for 3s to factory reset...");
        unsigned long resetStart = millis();
        bool held = true;
        while (millis() - resetStart < FACTORY_RESET_HOLD_MS) {
            // Blink red LED while waiting
            setStatusLed(255, 0, 0);
            delay(200);
            setStatusLed(0, 0, 0);
            delay(200);
            if (digitalRead(RESET_BTN_PIN) == HIGH) {
                held = false;
                break;
            }
        }
        if (held) {
            Serial.println("[Reset] Factory reset confirmed!");
            // Clear all EEPROM magic bytes
            EEPROM.begin(EEPROM_SIZE);
            EEPROM.write(EEPROM_DEVID_MAGIC_ADDR, 0xFF);
            EEPROM.write(EEPROM_CFG_MAGIC_ADDR, 0xFF);
            EEPROM.write(EEPROM_MQTT_MAGIC_ADDR, 0xFF);
            EEPROM.commit();
            EEPROM.end();
            // Clear WiFi credentials
            WiFiManager wm;
            wm.resetSettings();
            // Signal: white LED + 3 beeps
            setStatusLed(255, 255, 255);
            for (int i = 0; i < 3; i++) { beepOk(); delay(150); }
            Serial.println("[Reset] Done — restarting...");
            delay(1000);
            ESP.restart();
        }
        Serial.println("[Reset] Released early — normal boot");
    }

    // ---- WiFi ----
    setStatusLed(0, 0, 128);
    {
        WiFiManager wm;
        wm.setConnectTimeout(30);
        wm.setConfigPortalTimeout(180);
        uint8_t mac[6];
        WiFi.macAddress(mac);
        char apName[24];
        snprintf(apName, sizeof(apName), "AccessControl-%02X%02X", mac[4], mac[5]);
        Serial.printf("[WiFi] AP: %s\n", apName);
        if (!wm.autoConnect(apName)) {
            Serial.println("[WiFi] Failed — restarting");
            ESP.restart();
        }
    }
    Serial.printf("[WiFi] IP: %s\n", WiFi.localIP().toString().c_str());

    // ---- mDNS ----
    mdnsInit();

    // ---- MQTT provisioning ----
    mqtt.setBufferSize(512);
    mqtt.setKeepAlive(60);
    mqtt.setCallback(mqttCallback);

    if (!mqttConfigLoad()) {
        // Not provisioned — start HTTP server and wait for push from backend
        provisionServerStart();
        setStatusLed(255, 255, 0);  // yellow = waiting for provisioning
        Serial.println("[MQTT] Waiting for provisioning push on http://<ip>/api/provision");
        Serial.println("[MQTT] Tip: mqtt_set <ip> [port] also works via serial");
    } else {
        mqttProvisioned = true;
    }

    if (mqttProvisioned) {
        Serial.printf("[MQTT] Broker: %s:%d\n", mqttCfg.broker, mqttCfg.port);
        mqtt.setServer(mqttCfg.broker, mqttCfg.port);
        mqttConnect();
        beepOk();
        sendHeartbeat();
    }

    // ---- Ready ----
    if (provisionServerActive)      setStatusLed(255, 255, 0);   // yellow = waiting for provisioning
    else if (nfcAvailable)          setStatusLed(0, 255, 0);
    else                            setStatusLed(255, 165, 0);
    Serial.printf("[Ready] Lock duration: %lums\n", (unsigned long)cfg.lockOpenDuration);
    Serial.println("[Ready] Waiting for cards...");
}

// ═══════════════════════════════════════════════════════════
//  LOOP
// ═══════════════════════════════════════════════════════════
void loop() {
    handleSerialCommands();

    // Handle provisioning HTTP server (non-blocking, waits for push from backend)
    if (provisionServerActive) {
        provisionServer.handleClient();
        delay(10);
        return;
    }

    mqttReconnectLoop();

    unsigned long now = millis();

    // ---- Heartbeat ----
    if (now - lastHeartbeat >= cfg.heartbeatInterval) {
        sendHeartbeat();
        lastHeartbeat = now;
    }

    // ---- Relay auto-close ----
    if (lockCloseTime > 0 && now >= lockCloseTime) {
        digitalWrite(RELAY_PIN, HIGH);  // close relay
        lockCloseTime = 0;
        snprintf(topicBuf, sizeof(topicBuf), "%s/%s/lock/status", MQTT_TOPIC_PREFIX, hwid);
        if (mqtt.connected()) mqtt.publish(topicBuf, "{\"state\":\"closed\"}");
        Serial.println("[Lock] Auto-closed");
    }

    // ---- LED feedback restore ----
    if (ledFeedbackEndTime > 0 && now >= ledFeedbackEndTime) {
        ledFeedbackEndTime = 0;
        setDefaultLed();
    }

    // ---- Card result timeout ----
    if (waitingForResult && now >= resultTimeoutTime) {
        waitingForResult = false;
        setStatusLed(255, 165, 0);  // orange = timeout/no response
        if (cfg.buzzerEnabled) beepError();
        ledFeedbackEndTime = now + 2000;
        Serial.println("[Access] Server timeout");
    }

    // ---- Enrollment blink (yellow) ----
    if (currentMode == MODE_ENROLLMENT) {
        if (now - enrollmentStartTime >= enrollmentTimeoutMs) {
            Serial.println("[Enroll] Timeout");
            currentMode = MODE_NORMAL;
            setDefaultLed();
        } else if (now - enrollBlinkTimer >= 500) {
            enrollBlinkTimer = now;
            enrollBlinkState = !enrollBlinkState;
            if (enrollBlinkState) setStatusLed(255, 200, 0);  // yellow
            else                  setStatusLed(0, 0, 0);      // off
        }
    }

    if (!nfcAvailable) {
        delay(10);
        return;
    }

    // ---- NFC card scan ----
    // mqtt.loop() before blocking I2C read to keep MQTT keepalive alive
    if (mqtt.connected()) mqtt.loop();
    uint8_t uid[7] = {0};
    uint8_t uidLen = 0;
    bool ok = nfc.readPassiveTargetID(PN532_MIFARE_ISO14443A, uid, &uidLen, 200);

    if (ok && uidLen > 0) {
        bool sameCard = (uidLen == lastUidLen) && (memcmp(uid, lastUid, uidLen) == 0);
        if (!sameCard || (now - lastCardRead > CARD_READ_COOLDOWN)) {
            memcpy(lastUid, uid, uidLen);
            lastUidLen = uidLen;
            lastCardRead = now;

            if (currentMode == MODE_ENROLLMENT) {
                setStatusLed(0, 255, 255);  // cyan
                if (cfg.buzzerEnabled) beepDouble();
                mqttPublishCard("enrolled", uid, uidLen);
                currentMode = MODE_NORMAL;
                ledFeedbackEndTime = millis() + 1500;
            } else {
                setStatusLed(0, 0, 255);  // blue = processing
                if (cfg.buzzerEnabled) beepOk();
                mqttPublishCard("scanned", uid, uidLen);
                waitingForResult = true;
                resultTimeoutTime = millis() + RESULT_TIMEOUT_MS;
            }
        }
    }
}
