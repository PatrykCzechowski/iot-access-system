#include "device_id.h"
#include <Preferences.h>
#include <WiFi.h>

static char _hwid[37]; // UUID string "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx\0"
static Preferences _prefs;

static void macToUuid(const uint8_t mac[6], char* out) {
    // Deterministic UUID v5-like: namespace bytes + MAC → formatted as UUID
    // Simple approach: pad MAC into 16 bytes with fixed namespace prefix
    uint8_t bytes[16] = {
        0xAC, 0xCE, 0x55, 0x10, // "ACCESS" prefix (fixed namespace)
        0x00, 0x00,              // padding
        mac[0], mac[1], mac[2], mac[3], mac[4], mac[5],
        0x00, 0x00, 0x00, 0x00
    };
    // Set version 5 bits
    bytes[6] = (bytes[6] & 0x0F) | 0x50;
    bytes[8] = (bytes[8] & 0x3F) | 0x80;

    snprintf(out, 37,
        "%02x%02x%02x%02x-%02x%02x-%02x%02x-%02x%02x-%02x%02x%02x%02x%02x%02x",
        bytes[0], bytes[1], bytes[2], bytes[3],
        bytes[4], bytes[5],
        bytes[6], bytes[7],
        bytes[8], bytes[9],
        bytes[10], bytes[11], bytes[12], bytes[13], bytes[14], bytes[15]);
}

void deviceIdInit() {
    _prefs.begin("device", true); // read-only
    String stored = _prefs.getString("hwid", "");
    _prefs.end();

    if (stored.length() == 36) {
        stored.toCharArray(_hwid, 37);
        return;
    }

    // Generate from MAC
    uint8_t mac[6];
    WiFi.macAddress(mac);
    macToUuid(mac, _hwid);

    // Persist
    _prefs.begin("device", false);
    _prefs.putString("hwid", _hwid);
    _prefs.end();

    Serial.printf("[DeviceId] Generated HWID: %s\n", _hwid);
}

const char* deviceIdGet() {
    return _hwid;
}
