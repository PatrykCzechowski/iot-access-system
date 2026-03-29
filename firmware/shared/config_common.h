#pragma once

// ── mDNS ────────────────────────────────────────────────
#define MDNS_SERVICE_TYPE "_accesscontrol"
#define MDNS_SERVICE_PROTO "_tcp"

// ── MQTT ────────────────────────────────────────────────
#define MQTT_TOPIC_PREFIX "accesscontrol"
#define MQTT_DEFAULT_PORT 1883

// ── Default configuration values ────────────────────────
#define DEFAULT_LOCK_OPEN_DURATION   5000   // ms
#define DEFAULT_HEARTBEAT_INTERVAL   30000  // ms
#define DEFAULT_ENROLLMENT_TIMEOUT   30000  // ms
#define DEFAULT_BUZZER_ENABLED       true
#define DEFAULT_LED_BRIGHTNESS       128    // 0-255

// ── Firmware version ────────────────────────────────────
#define FIRMWARE_VERSION "1.0.0"
