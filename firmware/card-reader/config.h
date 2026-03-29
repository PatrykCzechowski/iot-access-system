#pragma once

// ── Hardware pins (ESP32 DevKit) ────────────────────────
// RC522 SPI pins
#define RC522_SCK   18
#define RC522_MISO  19
#define RC522_MOSI  23
#define RC522_SS     5
#define RC522_RST   22

// Status LED (optional, built-in or external)
#define LED_PIN      2   // Built-in LED on most ESP32 DevKit boards

// Buzzer (optional)
#define BUZZER_PIN  15

// WiFi reset button — connect between this pin and GND
// Hold button for WIFI_RESET_HOLD_MS to erase credentials
#define WIFI_RESET_BTN_PIN    D2     // D2 on Arduino Nano ESP32 board (= GPIO5)
#define WIFI_RESET_HOLD_MS    5000   // Hold time in milliseconds

// WiFi reset via triple-reset: press RST button 3 times within 10 seconds
// No external button required — uses built-in RST and RTC memory
#define WIFI_RESET_WINDOW_MS  10000  // Time window for counting resets (ms)
#define WIFI_RESET_COUNT      3      // Number of resets required to trigger

// ── Device identity ─────────────────────────────────────
#define DEVICE_MODEL    "ESP32-CardReader"
#define DEVICE_FEATURES 1   // DeviceFeatures.CardReader

// ── MQTT Broker ─────────────────────────────────────────
// Fallback broker IP — used only if mDNS auto-discovery fails
#define MQTT_BROKER_IP  "192.168.0.3"
