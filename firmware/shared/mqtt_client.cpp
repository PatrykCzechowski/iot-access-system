#include "mqtt_client.h"
#include "config_common.h"
#include <WiFi.h>

static WiFiClient _wifiClient;
static PubSubClient _mqtt(_wifiClient);
static const char* _hwid = nullptr;
static unsigned long _lastReconnectAttempt = 0;
static unsigned long _reconnectDelay = 2000;
static const unsigned long _maxReconnectDelay = 30000;

static void mqttConnect(const char* hwid) {
    char lwtTopic[128];
    snprintf(lwtTopic, sizeof(lwtTopic), "%s/%s/heartbeat", MQTT_TOPIC_PREFIX, hwid);
    const char* lwtPayload = "{\"status\":\"offline\"}";

    if (_mqtt.connect(hwid, lwtTopic, 1, true, lwtPayload)) {
        Serial.println("[MQTT] Connected!");
        _reconnectDelay = 2000;
    } else {
        Serial.printf("[MQTT] Connection failed, rc=%d\n", _mqtt.state());
    }
}

void mqttInit(const char* brokerIp, uint16_t port, const char* hwid,
              void (*callback)(char* topic, byte* payload, unsigned int length)) {
    _hwid = hwid;
    _mqtt.setServer(brokerIp, port);
    _mqtt.setCallback(callback);
    _mqtt.setBufferSize(512);

    Serial.printf("[MQTT] Connecting to %s:%d as %s\n", brokerIp, port, hwid);
    mqttConnect(hwid);
}

void mqttInit(IPAddress brokerIp, uint16_t port, const char* hwid,
              void (*callback)(char* topic, byte* payload, unsigned int length)) {
    _hwid = hwid;
    _mqtt.setServer(brokerIp, port);
    _mqtt.setCallback(callback);
    _mqtt.setBufferSize(512);

    Serial.printf("[MQTT] Connecting to %s:%d as %s\n", brokerIp.toString().c_str(), port, hwid);
    mqttConnect(hwid);
}

void mqttLoop() {
    if (_mqtt.connected()) {
        _mqtt.loop();
        return;
    }

    // Reconnect with exponential backoff
    unsigned long now = millis();
    if (now - _lastReconnectAttempt < _reconnectDelay) return;
    _lastReconnectAttempt = now;

    char lwtTopic[128];
    snprintf(lwtTopic, sizeof(lwtTopic), "%s/%s/heartbeat", MQTT_TOPIC_PREFIX, _hwid);

    Serial.printf("[MQTT] Reconnecting (delay: %lums)...\n", _reconnectDelay);

    if (_mqtt.connect(_hwid, lwtTopic, 1, true, "{\"status\":\"offline\"}")) {
        Serial.println("[MQTT] Reconnected!");
        _reconnectDelay = 2000;
    } else {
        _reconnectDelay = min(_reconnectDelay * 2, _maxReconnectDelay);
    }
}

bool mqttPublish(const char* topic, const char* payload, bool retain) {
    return _mqtt.publish(topic, payload, retain);
}

bool mqttSubscribe(const char* topic) {
    return _mqtt.subscribe(topic, 1); // QoS 1
}

bool mqttIsConnected() {
    return _mqtt.connected();
}

PubSubClient& mqttGetClient() {
    return _mqtt;
}
