#include "device_config.h"
#include "config_common.h"
#include "mqtt_client.h"
#include <Preferences.h>
#include <ArduinoJson.h>

static DeviceConfig _config;
static Preferences _cfgPrefs;

void deviceConfigInit() {
    _cfgPrefs.begin("devconfig", true); // read-only

    _config.lockOpenDuration  = _cfgPrefs.getUInt("lockDur", DEFAULT_LOCK_OPEN_DURATION);
    _config.heartbeatInterval = _cfgPrefs.getUInt("hbInt",   DEFAULT_HEARTBEAT_INTERVAL);
    _config.enrollmentTimeout = _cfgPrefs.getUInt("enrollTo", DEFAULT_ENROLLMENT_TIMEOUT);
    _config.buzzerEnabled     = _cfgPrefs.getBool("buzzer",  DEFAULT_BUZZER_ENABLED);
    _config.ledBrightness     = _cfgPrefs.getUChar("ledBri", DEFAULT_LED_BRIGHTNESS);

    _cfgPrefs.end();

    Serial.printf("[Config] Loaded: lockDur=%u hbInt=%u enrollTo=%u buzzer=%d led=%d\n",
        _config.lockOpenDuration, _config.heartbeatInterval,
        _config.enrollmentTimeout, _config.buzzerEnabled, _config.ledBrightness);
}

const DeviceConfig& deviceConfigGet() {
    return _config;
}

void deviceConfigOnUpdate(const char* json, const char* hwid) {
    JsonDocument doc;
    DeserializationError err = deserializeJson(doc, json);
    if (err) {
        Serial.printf("[Config] JSON parse error: %s\n", err.c_str());
        return;
    }

    _cfgPrefs.begin("devconfig", false); // read-write

    // Track which keys were actually updated
    JsonDocument ackDoc;
    JsonArray keys = ackDoc["keys"].to<JsonArray>();

    if (doc.containsKey("lockOpenDuration")) {
        uint32_t v = doc["lockOpenDuration"].as<uint32_t>();
        if (v != _config.lockOpenDuration) {
            _config.lockOpenDuration = v;
            _cfgPrefs.putUInt("lockDur", v);
            keys.add("lockOpenDuration");
        }
    }

    if (doc.containsKey("heartbeatInterval")) {
        uint32_t v = doc["heartbeatInterval"].as<uint32_t>();
        if (v != _config.heartbeatInterval) {
            _config.heartbeatInterval = v;
            _cfgPrefs.putUInt("hbInt", v);
            keys.add("heartbeatInterval");
        }
    }

    if (doc.containsKey("enrollmentTimeout")) {
        uint32_t v = doc["enrollmentTimeout"].as<uint32_t>();
        if (v != _config.enrollmentTimeout) {
            _config.enrollmentTimeout = v;
            _cfgPrefs.putUInt("enrollTo", v);
            keys.add("enrollmentTimeout");
        }
    }

    if (doc.containsKey("buzzerEnabled")) {
        bool v = doc["buzzerEnabled"].as<bool>();
        if (v != _config.buzzerEnabled) {
            _config.buzzerEnabled = v;
            _cfgPrefs.putBool("buzzer", v);
            keys.add("buzzerEnabled");
        }
    }

    if (doc.containsKey("ledBrightness")) {
        uint8_t v = doc["ledBrightness"].as<uint8_t>();
        if (v != _config.ledBrightness) {
            _config.ledBrightness = v;
            _cfgPrefs.putUChar("ledBri", v);
            keys.add("ledBrightness");
        }
    }

    _cfgPrefs.end();

    // Publish ACK
    ackDoc["applied"] = true;
    char ackPayload[256];
    serializeJson(ackDoc, ackPayload, sizeof(ackPayload));

    char ackTopic[128];
    snprintf(ackTopic, sizeof(ackTopic), "%s/%s/config/ack", MQTT_TOPIC_PREFIX, hwid);
    mqttPublish(ackTopic, ackPayload);

    Serial.printf("[Config] Updated %d keys, ACK sent\n", keys.size());
}
