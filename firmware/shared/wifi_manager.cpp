#include "wifi_manager.h"
#include <WiFiManager.h>
#include <WiFi.h>

void wifiResetCredentials() {
    Serial.println("[WiFi] Erasing saved credentials...");
    WiFiManager wm;
    wm.resetSettings();
    Serial.println("[WiFi] Credentials erased. Restarting...");
    delay(500);
    ESP.restart();
}

bool wifiManagerInit() {
    WiFiManager wm;

    // Auto-connect timeout (seconds). If fails, starts AP.
    wm.setConnectTimeout(30);
    // Portal timeout – auto-close AP after 3 minutes of inactivity
    wm.setConfigPortalTimeout(180);

    // Build AP name from MAC
    uint8_t mac[6];
    WiFi.macAddress(mac);
    char apName[24];
    snprintf(apName, sizeof(apName), "AccessControl-%02X%02X", mac[4], mac[5]);

    Serial.printf("[WiFi] Starting WiFiManager, AP: %s\n", apName);

    bool connected = wm.autoConnect(apName);

    if (connected) {
        Serial.printf("[WiFi] Connected! IP: %s\n", WiFi.localIP().toString().c_str());
    } else {
        Serial.println("[WiFi] Failed to connect. Will restart.");
        ESP.restart();
    }

    return connected;
}
