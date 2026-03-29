/*
 * AccessControl — ESP32 Lock Controller Firmware
 *
 * Hardware: ESP32 DevKit + Relay Module + Buzzer + LED
 * Communication: WiFi → MQTT → AccessControl server
 * Discovery: mDNS (_accesscontrol._tcp)
 *
 * Libraries required:
 *   - PubSubClient   (Nick O'Leary)
 *   - WiFiManager    (tzapu)
 *   - ArduinoJson    (Benoît Blanchon)
 *   - ESPmDNS        (built-in)
 *   - Preferences    (built-in)
 */

#include <ArduinoJson.h>

#include "config.h"
#include "../shared/config_common.h"
#include "../shared/device_id.h"
#include "../shared/wifi_manager.h"
#include "../shared/mqtt_client.h"
#include "../shared/mdns_advertiser.h"
#include "../shared/device_config.h"

// ── State ───────────────────────────────────────────────
static bool lockOpen = false;
static unsigned long lockOpenTime = 0;
static unsigned long lastHeartbeat = 0;

static char topicBuf[128];
static char payloadBuf[256];

// ── Forward declarations ────────────────────────────────
void mqttCallback(char* topic, byte* payload, unsigned int length);
void handleLockCommand(const char* payload, unsigned int len);
void handleConfigUpdate(const char* payload, unsigned int len);
void setRelay(bool open);
void publishLockStatus();
void sendHeartbeat();

// ────────────────────────────────────────────────────────
void setup() {
    Serial.begin(115200);
    delay(500);
    Serial.println("\n=== AccessControl Lock Controller ===");

    // Relay + LED + Buzzer
    pinMode(RELAY_PIN, OUTPUT);
    pinMode(LED_PIN, OUTPUT);
    pinMode(BUZZER_PIN, OUTPUT);
    setRelay(false); // Ensure locked on boot
    digitalWrite(LED_PIN, LOW);
    digitalWrite(BUZZER_PIN, LOW);

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
    snprintf(topicBuf, sizeof(topicBuf), "%s/%s/lock/command", MQTT_TOPIC_PREFIX, hwid);
    mqttSubscribe(topicBuf);

    snprintf(topicBuf, sizeof(topicBuf), "%s/%s/config/update", MQTT_TOPIC_PREFIX, hwid);
    mqttSubscribe(topicBuf);

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

    // Auto-lock timer
    if (lockOpen && (now - lockOpenTime >= cfg.lockOpenDuration)) {
        Serial.println("[Lock] Auto-lock timer expired");
        setRelay(false);
        lockOpen = false;
        publishLockStatus();

        if (cfg.buzzerEnabled) {
            tone(BUZZER_PIN, 800, 100); // short beep on lock
        }
    }
}

// ── MQTT Callback ───────────────────────────────────────
void mqttCallback(char* topic, byte* payload, unsigned int length) {
    char msg[512];
    unsigned int len = min(length, (unsigned int)(sizeof(msg) - 1));
    memcpy(msg, payload, len);
    msg[len] = '\0';

    const char* hwid = deviceIdGet();
    String t(topic);

    // lock/command
    snprintf(topicBuf, sizeof(topicBuf), "%s/%s/lock/command", MQTT_TOPIC_PREFIX, hwid);
    if (t == topicBuf) {
        handleLockCommand(msg, len);
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
void handleLockCommand(const char* payload, unsigned int len) {
    JsonDocument doc;
    if (deserializeJson(doc, payload, len)) {
        Serial.println("[Lock] Invalid JSON");
        return;
    }

    const char* action = doc["action"] | "";
    const DeviceConfig& cfg = deviceConfigGet();

    if (strcmp(action, "open") == 0) {
        Serial.println("[Lock] Opening...");
        setRelay(true);
        lockOpen = true;
        lockOpenTime = millis();
        publishLockStatus();

        // Feedback
        digitalWrite(LED_PIN, HIGH);
        if (cfg.buzzerEnabled) {
            tone(BUZZER_PIN, 1000, 200);
        }
    } else if (strcmp(action, "close") == 0) {
        Serial.println("[Lock] Closing...");
        setRelay(false);
        lockOpen = false;
        publishLockStatus();

        digitalWrite(LED_PIN, LOW);
        if (cfg.buzzerEnabled) {
            tone(BUZZER_PIN, 800, 100);
        }
    } else {
        Serial.printf("[Lock] Unknown action: %s\n", action);
    }
}

void handleConfigUpdate(const char* payload, unsigned int len) {
    deviceConfigOnUpdate(payload, deviceIdGet());
    Serial.println("[Config] Configuration updated from server");
}

// ── Helpers ─────────────────────────────────────────────
void setRelay(bool open) {
    bool pinState = open;
    if (RELAY_ACTIVE_LOW) pinState = !pinState;

    digitalWrite(RELAY_PIN, pinState ? HIGH : LOW);
    Serial.printf("[Relay] %s\n", open ? "OPEN" : "CLOSED");
}

void publishLockStatus() {
    if (!mqttIsConnected()) return;

    const char* hwid = deviceIdGet();
    snprintf(topicBuf, sizeof(topicBuf), "%s/%s/lock/status", MQTT_TOPIC_PREFIX, hwid);
    snprintf(payloadBuf, sizeof(payloadBuf), "{\"state\":\"%s\"}", lockOpen ? "open" : "closed");
    mqttPublish(topicBuf, payloadBuf);
}

void sendHeartbeat() {
    if (!mqttIsConnected()) return;

    const char* hwid = deviceIdGet();
    snprintf(topicBuf, sizeof(topicBuf), "%s/%s/heartbeat", MQTT_TOPIC_PREFIX, hwid);

    int rssi = WiFi.RSSI();
    unsigned long uptimeSec = millis() / 1000;

    snprintf(payloadBuf, sizeof(payloadBuf),
        "{\"status\":\"online\",\"uptime\":%lu,\"rssi\":%d,\"lockState\":\"%s\"}",
        uptimeSec, rssi,
        lockOpen ? "open" : "closed");

    mqttPublish(topicBuf, payloadBuf, true);
}
