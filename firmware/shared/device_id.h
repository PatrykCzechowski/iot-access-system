#pragma once

#include <Arduino.h>

/// Generate or load a persistent hardware ID from NVS.
/// Returns a deterministic UUID string based on the ESP32 MAC address.
void deviceIdInit();
const char* deviceIdGet();
