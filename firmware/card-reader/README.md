# ESP32 Card Reader — AccessControl

Firmware for an ESP32-based RFID card reader that communicates with the AccessControl server over MQTT.

## Hardware

| Component      | Pin(s)           |
|----------------|------------------|
| RC522 SCK      | GPIO 18          |
| RC522 MISO     | GPIO 19          |
| RC522 MOSI     | GPIO 23          |
| RC522 SS/SDA   | GPIO 5           |
| RC522 RST      | GPIO 22          |
| Status LED     | GPIO 2 (built-in)|
| Buzzer         | GPIO 15          |

## Narzędzie do budowania: PlatformIO

Projekt używa **PlatformIO CLI** — narzędzia budowania obsługującego strukture wieloplikową z folderem `../shared/`.  
Arduino IDE **nie jest obsługiwane** (nie wspiera ścieżek `../shared/`).

### Instalacja PlatformIO

```bash
pip install platformio
```

> Po instalacji upewnij się, że katalog skryptów pip jest w PATH, np.:
> - Windows: `C:\Users\<user>\AppData\Roaming\Python\Python3xx\Scripts`
> - Linux/macOS: `~/.local/bin`

### Wgrywanie firmware

```bash
# Wejdź do folderu szkicu
cd firmware/card-reader

# Kompiluj i wgraj (automatyczne wykrycie portu)
pio run --target upload

# Lub wskaż port ręcznie
pio run --target upload --upload-port COM9       # Windows
pio run --target upload --upload-port /dev/ttyUSB0  # Linux
```

### Monitor szeregowy

```bash
pio device monitor --port COM9 --baud 115200
```

### Tylko kompilacja (bez wgrywania)

```bash
pio run
```

### Wylistowanie podłączonych urządzeń

```bash
pio device list
```

## Libraries

Biblioteki są instalowane automatycznie przez PlatformIO przy pierwszej kompilacji na podstawie `platformio.ini`.

| Biblioteka | Autor | Wersja |
|---|---|---|
| **MFRC522** | miguelbalboa | ≥ 1.4 |
| **PubSubClient** | Nick O'Leary | ≥ 2.8 |
| **WiFiManager** | tzapu | ≥ 2.0 |
| **ArduinoJson** | Benoît Blanchon | v7+ |
| **ESPmDNS** | wbudowana w ESP32 Arduino core | — |
| **Preferences** | wbudowana w ESP32 Arduino core | — |
| **WiFi** | wbudowana w ESP32 Arduino core | — |

## First Boot

1. Power on the ESP32.
2. Connect to WiFi AP **AccessControl-XXXX** (shown in Serial monitor).
3. Open captive portal → enter WiFi SSID + password.
4. Device connects to WiFi, starts mDNS advertisement, and connects to MQTT broker.

## Configuration

Edit `config.h` to change:
- SPI pin definitions
- `MQTT_BROKER_IP` — fallback IP used only if mDNS broker auto-discovery fails

Dynamic config (lockOpenDuration, heartbeatInterval, etc.) is managed from the server and persisted to NVS.

## MQTT Topics

| Direction | Topic | Description |
|-----------|-------|-------------|
| Publish   | `accesscontrol/{hwid}/heartbeat` | Periodic status |
| Publish   | `accesscontrol/{hwid}/card/read` | Card UID for validation |
| Publish   | `accesscontrol/{hwid}/card/enrolled` | Enrolled card UID |
| Publish   | `accesscontrol/{hwid}/config/ack` | Config update acknowledgment |
| Subscribe | `accesscontrol/{hwid}/card/result` | Validation result |
| Subscribe | `accesscontrol/{hwid}/config/update` | Dynamic config from server |
| Subscribe | `accesscontrol/{hwid}/enrollment/start` | Enter enrollment mode |
| Subscribe | `accesscontrol/{hwid}/enrollment/cancel` | Cancel enrollment |
