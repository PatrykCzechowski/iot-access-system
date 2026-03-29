#pragma once

#include <Arduino.h>

struct DeviceConfig {
    uint32_t lockOpenDuration;
    uint32_t heartbeatInterval;
    uint32_t enrollmentTimeout;
    bool     buzzerEnabled;
    uint8_t  ledBrightness;
};

/// Load configuration from NVS (or defaults if not saved yet).
void deviceConfigInit();

/// Get current configuration.
const DeviceConfig& deviceConfigGet();

/// Handle incoming config/update JSON from server.
/// Parses, saves changed keys to NVS, updates RAM, returns applied key names.
/// @param json   The JSON payload from MQTT
/// @param hwid   Hardware ID for publishing ACK
void deviceConfigOnUpdate(const char* json, const char* hwid);
