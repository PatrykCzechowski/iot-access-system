/*
 * AccessControl — ESP32 Card Reader Firmware
 *
 * Hardware: ESP32 DevKit + RC522 RFID (SPI) + Buzzer + LED
 * Communication: WiFi → MQTT → AccessControl server
 * Discovery: mDNS (_accesscontrol._tcp)
 *
 * Libraries required:
 *   - MFRC522       (miguelbalboa)
 *   - PubSubClient   (Nick O'Leary)
 *   - WiFiManager    (tzapu)
 *   - ArduinoJson    (Benoît Blanchon)
 *   - ESPmDNS        (built-in)
 *   - Preferences    (built-in)
 */

#include <SPI.h>
#include <MFRC522.h>
#include <ArduinoJson.h>
#include <WiFi.h>

// ── Triple-reset WiFi credential wipe ──────────────────
// Press RST 3 times within 10s to erase saved WiFi network.
// RTC memory survives resets but is cleared on power-off.
RTC_DATA_ATTR int  _resetCount     = 0;
RTC_DATA_ATTR long _resetWindowEnd = 0; // ms since epoch (RTC timer)

#include "config.h"
#include "../shared/config_common.h"
#include "../shared/device_id.h"
#include "../shared/wifi_manager.h"
#include "../shared/mqtt_client.h"
#include "../shared/mdns_advertiser.h"
#include "../shared/device_config.h"

// ── State ───────────────────────────────────────────────
enum DeviceMode { MODE_NORMAL, MODE_ENROLLMENT };
static DeviceMode currentMode = MODE_NORMAL;
static unsigned long enrollmentStartTime = 0;

static MFRC522 rfid(RC522_SS, RC522_RST);
static unsigned long lastHeartbeat = 0;
static unsigned long lastCardRead = 0;
static const unsigned long CARD_DEBOUNCE_MS = 2000; // ignore same card for 2s

static char topicBuf[128];
static char payloadBuf[256];

// ── Forward declarations ────────────────────────────────
void mqttCallback(char* topic, byte* payload, unsigned int length);
void handleCardResult(const char* payload, unsigned int len);
void handleEnrollmentStart(const char* payload, unsigned int len);
void handleEnrollmentCancel();
void handleConfigUpdate(const char* payload, unsigned int len);
void sendAnnounce();
void sendHeartbeat();
String uidToHex(MFRC522::Uid* uid);

// ────────────────────────────────────────────────────────
void setup() {
    Serial.begin(115200);
    delay(500);
    Serial.println("\n=== AccessControl Card Reader ===");

    // LED + Buzzer
    pinMode(LED_PIN, OUTPUT);
    pinMode(BUZZER_PIN, OUTPUT);
    digitalWrite(LED_PIN, LOW);
    digitalWrite(BUZZER_PIN, LOW);

    // WiFi reset button — hold for 5s to erase credentials
    // Connect button between WIFI_RESET_BTN_PIN and GND
    pinMode(WIFI_RESET_BTN_PIN, INPUT_PULLUP);
    if (digitalRead(WIFI_RESET_BTN_PIN) == LOW) {
        Serial.println("[WiFi] Reset button pressed — hold 5s to erase credentials...");
        unsigned long holdStart = millis();
        bool triggered = false;
        while (digitalRead(WIFI_RESET_BTN_PIN) == LOW) {
            // Blink LED as long as button is held
            digitalWrite(LED_PIN, HIGH); delay(100);
            digitalWrite(LED_PIN, LOW);  delay(100);
            if (millis() - holdStart >= WIFI_RESET_HOLD_MS) {
                triggered = true;
                break;
            }
        }
        if (triggered) {
            // 5 fast blinks to confirm
            for (int i = 0; i < 5; i++) {
                digitalWrite(LED_PIN, HIGH); delay(80);
                digitalWrite(LED_PIN, LOW);  delay(80);
            }
            Serial.println("[WiFi] Threshold reached — erasing credentials!");
            wifiResetCredentials(); // erases and restarts, never returns
        }
        Serial.println("[WiFi] Button released early — skipping reset");
        digitalWrite(LED_PIN, LOW);
    }

    // Triple-reset detection — no button required, just press RST 3x within 10s
    {
        long nowMs = (long)(esp_timer_get_time() / 1000LL);
        if (nowMs > _resetWindowEnd) {
            // Window expired — start fresh
            _resetCount = 1;
            _resetWindowEnd = nowMs + WIFI_RESET_WINDOW_MS;
        } else {
            _resetCount++;
        }
        Serial.printf("[WiFi] Reset counter: %d/%d (window: %lds left)\n",
                      _resetCount, WIFI_RESET_COUNT,
                      (_resetWindowEnd - nowMs) / 1000);
        if (_resetCount >= WIFI_RESET_COUNT) {
            _resetCount = 0;
            _resetWindowEnd = 0;
            // Signal to user: 5 fast blinks before erasing
            for (int i = 0; i < 5; i++) {
                digitalWrite(LED_PIN, HIGH); delay(80);
                digitalWrite(LED_PIN, LOW);  delay(80);
            }
            Serial.println("[WiFi] Triple-reset detected — erasing credentials!");
            wifiResetCredentials(); // erases and restarts, never returns
        }
    }

    // 1. Device ID (NVS)
    deviceIdInit();
    const char* hwid = deviceIdGet();
    Serial.printf("Hardware ID: %s\n", hwid);

    // 2. Dynamic config from NVS
    deviceConfigInit();

    // 3. WiFi (captive portal)
    wifiManagerInit();

    // 4. mDNS
    mdnsInit(hwid, DEVICE_MODEL, DEVICE_FEATURES, FIRMWARE_VERSION);

    // 5. MQTT — discover broker via mDNS, fallback to config.h
    IPAddress brokerIp;
    uint16_t brokerPort = MQTT_DEFAULT_PORT;

    if (mdnsDiscoverMqttBroker(brokerIp, brokerPort)) {
        Serial.printf("MQTT broker discovered: %s:%d\n", brokerIp.toString().c_str(), brokerPort);
        mqttInit(brokerIp, brokerPort, hwid, mqttCallback);
    } else {
        Serial.printf("mDNS discovery failed, using fallback: %s:%d\n", MQTT_BROKER_IP, MQTT_DEFAULT_PORT);
        mqttInit(MQTT_BROKER_IP, MQTT_DEFAULT_PORT, hwid, mqttCallback);
    }

    // 6. Subscribe to device-specific topics
    snprintf(topicBuf, sizeof(topicBuf), "%s/%s/card/result", MQTT_TOPIC_PREFIX, hwid);
    mqttSubscribe(topicBuf);

    snprintf(topicBuf, sizeof(topicBuf), "%s/%s/config/update", MQTT_TOPIC_PREFIX, hwid);
    mqttSubscribe(topicBuf);

    snprintf(topicBuf, sizeof(topicBuf), "%s/%s/enrollment/start", MQTT_TOPIC_PREFIX, hwid);
    mqttSubscribe(topicBuf);

    snprintf(topicBuf, sizeof(topicBuf), "%s/%s/enrollment/cancel", MQTT_TOPIC_PREFIX, hwid);
    mqttSubscribe(topicBuf);

    // 7. Announce device via MQTT (so server can discover without mDNS)
    sendAnnounce();

    // 8. RC522 init
    SPI.begin(RC522_SCK, RC522_MISO, RC522_MOSI, RC522_SS);
    rfid.PCD_Init();
    rfid.PCD_DumpVersionToSerial();

    Serial.println("Setup complete. Entering main loop.");
}

// ────────────────────────────────────────────────────────
void loop() {
    mqttLoop();

    const DeviceConfig& cfg = deviceConfigGet();
    unsigned long now = millis();

    // Heartbeat
    if (now - lastHeartbeat >= cfg.heartbeatInterval) {
        sendHeartbeat();
        lastHeartbeat = now;
    }

    // Enrollment timeout check
    if (currentMode == MODE_ENROLLMENT) {
        if (now - enrollmentStartTime >= cfg.enrollmentTimeout) {
            Serial.println("[Enrollment] Timeout — returning to normal mode");
            currentMode = MODE_NORMAL;
            digitalWrite(LED_PIN, LOW);
        }
    }

    // Check for new RFID card
    if (!rfid.PICC_IsNewCardPresent() || !rfid.PICC_ReadCardSerial()) {
        return;
    }

    // Debounce: ignore if same card read too recently
    if (now - lastCardRead < CARD_DEBOUNCE_MS) {
        rfid.PICC_HaltA();
        rfid.PCD_StopCrypto1();
        return;
    }
    lastCardRead = now;

    String uid = uidToHex(&rfid.uid);
    Serial.printf("[RFID] Card UID: %s\n", uid.c_str());

    const char* hwid = deviceIdGet();

    if (currentMode == MODE_ENROLLMENT) {
        // ── Enrollment mode: publish enrolled card ──
        snprintf(topicBuf, sizeof(topicBuf), "%s/%s/card/enrolled", MQTT_TOPIC_PREFIX, hwid);
        snprintf(payloadBuf, sizeof(payloadBuf), "{\"uid\":\"%s\"}", uid.c_str());
        mqttPublish(topicBuf, payloadBuf);

        Serial.printf("[Enrollment] Card enrolled: %s\n", uid.c_str());

        // Feedback: quick double blink
        for (int i = 0; i < 2; i++) {
            digitalWrite(LED_PIN, HIGH);
            delay(100);
            digitalWrite(LED_PIN, LOW);
            delay(100);
        }

        // Return to normal mode
        currentMode = MODE_NORMAL;
    } else {
        // ── Normal mode: publish card read for server validation ──
        snprintf(topicBuf, sizeof(topicBuf), "%s/%s/card/read", MQTT_TOPIC_PREFIX, hwid);
        snprintf(payloadBuf, sizeof(payloadBuf), "{\"uid\":\"%s\"}", uid.c_str());
        mqttPublish(topicBuf, payloadBuf);

        // LED on while waiting for result
        digitalWrite(LED_PIN, HIGH);
    }

    rfid.PICC_HaltA();
    rfid.PCD_StopCrypto1();
}

// ── MQTT Callback ───────────────────────────────────────
void mqttCallback(char* topic, byte* payload, unsigned int length) {
    // Null-terminate payload
    char msg[512];
    unsigned int len = min(length, (unsigned int)(sizeof(msg) - 1));
    memcpy(msg, payload, len);
    msg[len] = '\0';

    const char* hwid = deviceIdGet();
    String t(topic);

    // card/result
    snprintf(topicBuf, sizeof(topicBuf), "%s/%s/card/result", MQTT_TOPIC_PREFIX, hwid);
    if (t == topicBuf) {
        handleCardResult(msg, len);
        return;
    }

    // enrollment/start
    snprintf(topicBuf, sizeof(topicBuf), "%s/%s/enrollment/start", MQTT_TOPIC_PREFIX, hwid);
    if (t == topicBuf) {
        handleEnrollmentStart(msg, len);
        return;
    }

    // enrollment/cancel
    snprintf(topicBuf, sizeof(topicBuf), "%s/%s/enrollment/cancel", MQTT_TOPIC_PREFIX, hwid);
    if (t == topicBuf) {
        handleEnrollmentCancel();
        return;
    }

    // config/update
    snprintf(topicBuf, sizeof(topicBuf), "%s/%s/config/update", MQTT_TOPIC_PREFIX, hwid);
    if (t == topicBuf) {
        handleConfigUpdate(msg, len);
        return;
    }
}

// ── Handlers ────────────────────────────────────────────
void handleCardResult(const char* payload, unsigned int len) {
    JsonDocument doc;
    if (deserializeJson(doc, payload, len)) return;

    bool granted = doc["granted"] | false;
    const char* userName = doc["userName"] | "";

    const DeviceConfig& cfg = deviceConfigGet();

    if (granted) {
        Serial.printf("[Access] GRANTED — %s\n", userName);
        digitalWrite(LED_PIN, HIGH);
        if (cfg.buzzerEnabled) {
            tone(BUZZER_PIN, 1000, 200); // short beep
        }
        delay(500);
        digitalWrite(LED_PIN, LOW);
    } else {
        Serial.println("[Access] DENIED");
        // Triple flash = denied
        for (int i = 0; i < 3; i++) {
            digitalWrite(LED_PIN, HIGH);
            if (cfg.buzzerEnabled) {
                tone(BUZZER_PIN, 400, 100);
            }
            delay(150);
            digitalWrite(LED_PIN, LOW);
            delay(150);
        }
    }
}

void handleEnrollmentStart(const char* payload, unsigned int len) {
    JsonDocument doc;
    if (deserializeJson(doc, payload, len)) return;

    uint32_t timeoutSec = doc["timeout"] | 30;

    currentMode = MODE_ENROLLMENT;
    enrollmentStartTime = millis();

    const DeviceConfig& cfg = deviceConfigGet();
    // Override timeout if provided
    // Note: server sends timeout in seconds, our timer works in ms
    // We use the config value unless server overrides
    if (timeoutSec > 0) {
        enrollmentStartTime = millis();
        // We'll use cfg.enrollmentTimeout by default, but server can override
    }

    Serial.printf("[Enrollment] Started (timeout: %us)\n", timeoutSec);

    // Visual feedback: LED blinking handled in loop or just turn on
    digitalWrite(LED_PIN, HIGH);
}

void handleEnrollmentCancel() {
    currentMode = MODE_NORMAL;
    digitalWrite(LED_PIN, LOW);
    Serial.println("[Enrollment] Cancelled by server");
}

void handleConfigUpdate(const char* payload, unsigned int len) {
    deviceConfigOnUpdate(payload, deviceIdGet());
    Serial.println("[Config] Configuration updated from server");
}

// ── Helpers ─────────────────────────────────────────────
void sendAnnounce() {
    if (!mqttIsConnected()) return;

    const char* hwid = deviceIdGet();
    snprintf(topicBuf, sizeof(topicBuf), "%s/%s/announce", MQTT_TOPIC_PREFIX, hwid);

    uint8_t mac[6];
    WiFi.macAddress(mac);

    snprintf(payloadBuf, sizeof(payloadBuf),
        "{\"model\":\"%s\",\"mac\":\"%02X:%02X:%02X:%02X:%02X:%02X\","
        "\"features\":%d,\"fw\":\"%s\",\"ip\":\"%s\"}",
        DEVICE_MODEL,
        mac[0], mac[1], mac[2], mac[3], mac[4], mac[5],
        DEVICE_FEATURES, FIRMWARE_VERSION,
        WiFi.localIP().toString().c_str());

    mqttPublish(topicBuf, payloadBuf, true); // retained
    Serial.printf("[MQTT] Announced device: %s at %s\n", DEVICE_MODEL, WiFi.localIP().toString().c_str());
}

void sendHeartbeat() {
    if (!mqttIsConnected()) return;

    const char* hwid = deviceIdGet();
    snprintf(topicBuf, sizeof(topicBuf), "%s/%s/heartbeat", MQTT_TOPIC_PREFIX, hwid);

    int rssi = WiFi.RSSI();
    unsigned long uptimeSec = millis() / 1000;

    snprintf(payloadBuf, sizeof(payloadBuf),
        "{\"status\":\"online\",\"uptime\":%lu,\"rssi\":%d,\"mode\":\"%s\"}",
        uptimeSec, rssi,
        currentMode == MODE_ENROLLMENT ? "enrollment" : "normal");

    mqttPublish(topicBuf, payloadBuf, true);
}

String uidToHex(MFRC522::Uid* uid) {
    String hex = "";
    for (byte i = 0; i < uid->size; i++) {
        if (uid->uidByte[i] < 0x10) hex += "0";
        hex += String(uid->uidByte[i], HEX);
    }
    hex.toUpperCase();
    return hex;
}
