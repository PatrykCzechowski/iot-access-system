#pragma once

#include <Arduino.h>
#include <IPAddress.h>
#include <PubSubClient.h>

/// Initialize MQTT client with string IP, connect to broker, set up auto-reconnect and LWT.
void mqttInit(const char* brokerIp, uint16_t port, const char* hwid,
              void (*callback)(char* topic, byte* payload, unsigned int length));

/// Initialize MQTT client with IPAddress, connect to broker, set up auto-reconnect and LWT.
void mqttInit(IPAddress brokerIp, uint16_t port, const char* hwid,
              void (*callback)(char* topic, byte* payload, unsigned int length));

/// Call in loop() to maintain connection and process messages.
void mqttLoop();

/// Publish a message to a topic.
bool mqttPublish(const char* topic, const char* payload, bool retain = false);

/// Subscribe to a topic.
bool mqttSubscribe(const char* topic);

/// Check if connected.
bool mqttIsConnected();

/// Get the underlying PubSubClient (for advanced use).
PubSubClient& mqttGetClient();
