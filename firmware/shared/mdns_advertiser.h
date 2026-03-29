#pragma once

#include <Arduino.h>
#include <IPAddress.h>

/// Start mDNS advertisement for this device.
/// @param hwid     Hardware ID (UUID string)
/// @param model    Device model string (e.g. "ESP32-CardReader")
/// @param features DeviceFeatures enum value (int)
/// @param fwVer    Firmware version string
void mdnsInit(const char* hwid, const char* model, int features, const char* fwVer);

/// Discover MQTT broker via mDNS (_mqtt._tcp).
/// @param outIp       Resolved broker IP address
/// @param outPort     Resolved broker port
/// @param timeoutMs   How long to wait for results (default 5000ms)
/// @param maxRetries  Number of scan attempts (default 3)
/// @return true if broker was found
bool mdnsDiscoverMqttBroker(IPAddress& outIp, uint16_t& outPort,
                            unsigned long timeoutMs = 5000, int maxRetries = 3);
