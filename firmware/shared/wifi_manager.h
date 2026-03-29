#pragma once

#include <Arduino.h>

/// Initialize WiFi via WiFiManager captive portal.
/// AP name will be "AccessControl-XXXX" where XXXX = last 4 hex of MAC.
/// Returns true when connected to WiFi.
bool wifiManagerInit();

/// Erase saved WiFi credentials and restart the device.
/// Call this when the user holds the reset button at boot.
void wifiResetCredentials();
