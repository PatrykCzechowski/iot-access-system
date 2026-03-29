#pragma once

// ── Hardware pins ───────────────────────────────────────
#define RELAY_PIN       26
#define RELAY_ACTIVE_LOW false  // Set true if relay module is active-low
#define LED_PIN          2     // Built-in LED
#define BUZZER_PIN      15     // Optional buzzer

// ── Device identity ─────────────────────────────────────
#define DEVICE_MODEL    "ESP32-LockController"
#define DEVICE_FEATURES 16  // DeviceFeatures.LockControl

// ── MQTT Broker ─────────────────────────────────────────
// Fallback broker IP — used only if mDNS auto-discovery fails
#define MQTT_BROKER_IP  "192.168.1.100"
