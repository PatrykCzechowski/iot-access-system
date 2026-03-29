#include "mdns_advertiser.h"
#include "config_common.h"
#include <ESPmDNS.h>
#include <WiFi.h>

void mdnsInit(const char* hwid, const char* model, int features, const char* fwVer) {
    // Use hwid prefix as hostname (mDNS requires valid hostname)
    char hostname[32];
    snprintf(hostname, sizeof(hostname), "ac-%.8s", hwid);

    if (!MDNS.begin(hostname)) {
        Serial.println("[mDNS] Failed to start mDNS responder!");
        return;
    }

    // Advertise _accesscontrol._tcp service
    MDNS.addService(MDNS_SERVICE_TYPE, MDNS_SERVICE_PROTO, 80);

    // TXT records — must match what the server's DeviceDiscoveryService expects
    MDNS.addServiceTxt(MDNS_SERVICE_TYPE, MDNS_SERVICE_PROTO, "hwid", hwid);
    MDNS.addServiceTxt(MDNS_SERVICE_TYPE, MDNS_SERVICE_PROTO, "model", model);

    uint8_t mac[6];
    WiFi.macAddress(mac);
    char macStr[18];
    snprintf(macStr, sizeof(macStr), "%02X:%02X:%02X:%02X:%02X:%02X",
             mac[0], mac[1], mac[2], mac[3], mac[4], mac[5]);
    MDNS.addServiceTxt(MDNS_SERVICE_TYPE, MDNS_SERVICE_PROTO, "mac", (const char*)macStr);

    char featStr[8];
    snprintf(featStr, sizeof(featStr), "%d", features);
    MDNS.addServiceTxt(MDNS_SERVICE_TYPE, MDNS_SERVICE_PROTO, "features", (const char*)featStr);

    MDNS.addServiceTxt(MDNS_SERVICE_TYPE, MDNS_SERVICE_PROTO, "fw", fwVer);

    Serial.printf("[mDNS] Advertising %s on %s._tcp, hwid=%s\n", model, MDNS_SERVICE_TYPE, hwid);
}

bool mdnsDiscoverMqttBroker(IPAddress& outIp, uint16_t& outPort,
                            unsigned long timeoutMs, int maxRetries) {
    Serial.println("[mDNS] Searching for MQTT broker (_mqtt._tcp)...");

    for (int attempt = 1; attempt <= maxRetries; attempt++) {
        Serial.printf("[mDNS] Discovery attempt %d/%d\n", attempt, maxRetries);

        int n = MDNS.queryService("mqtt", "tcp");

        if (n > 0) {
            // Use the first found service
            outIp   = MDNS.IP(0);
            outPort = MDNS.port(0);
            Serial.printf("[mDNS] MQTT broker found: %s:%d\n",
                          outIp.toString().c_str(), outPort);
            return true;
        }

        if (attempt < maxRetries) {
            Serial.printf("[mDNS] No broker found, retrying in %lums...\n", timeoutMs / maxRetries);
            delay(timeoutMs / maxRetries);
        }
    }

    Serial.println("[mDNS] MQTT broker NOT found via mDNS");
    return false;
}
