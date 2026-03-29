# ESP32 Lock Controller — AccessControl

Firmware for an ESP32-based electromagnetic lock controller that receives commands from the AccessControl server over MQTT.

## Hardware

| Component   | Pin      |
|-------------|----------|
| Relay       | GPIO 26  |
| Status LED  | GPIO 2   |
| Buzzer      | GPIO 15  |

## Libraries (Arduino IDE / PlatformIO)

- **PubSubClient** by Nick O'Leary
- **WiFiManager** by tzapu (ESP32 branch)
- **ArduinoJson** by Benoît Blanchon (v7+)
- **ESPmDNS** — built-in with ESP32 Arduino core
- **Preferences** — built-in with ESP32 Arduino core

## First Boot

1. Power on the ESP32.
2. Connect to WiFi AP **AccessControl-XXXX** (shown in Serial monitor).
3. Open captive portal → enter WiFi SSID + password.
4. Device connects to WiFi, starts mDNS advertisement, and connects to MQTT broker.

## Configuration

Edit `config.h` to change:
- `RELAY_PIN` — GPIO pin connected to relay module
- `RELAY_ACTIVE_LOW` — set `true` if relay is active-low
- `MQTT_BROKER_IP` — IP address of the MQTT broker

Dynamic config (lockOpenDuration, heartbeatInterval, etc.) is managed from the server and persisted to NVS.

## MQTT Topics

| Direction | Topic | Description |
|-----------|-------|-------------|
| Publish   | `accesscontrol/{hwid}/heartbeat` | Periodic status |
| Publish   | `accesscontrol/{hwid}/lock/status` | Lock state after command |
| Publish   | `accesscontrol/{hwid}/config/ack` | Config update acknowledgment |
| Subscribe | `accesscontrol/{hwid}/lock/command` | Open/close commands |
| Subscribe | `accesscontrol/{hwid}/config/update` | Dynamic config from server |

## Lock Behavior

- On `lock/command` with `"action": "open"`: relay opens, auto-lock timer starts (duration from `lockOpenDuration` config)
- On `lock/command` with `"action": "close"`: relay closes immediately
- Auto-lock: relay closes automatically after `lockOpenDuration` ms (default: 5000ms)
