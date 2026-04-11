# Specyfikacja Projektu — Elektronika

## AccessControl Card Reader + Lock Controller v2.0

**Model:** NanoESP32-CardReader  
**Firmware:** 2.0.0  
**Data:** 2026-04-11

---

## 1. BOM — Spis elementów elektronicznych

| # | Element | Oznaczenie | Ilość | Opis |
|---|---------|------------|-------|------|
| 1 | Arduino Nano ESP32 | U1 | 1 | Mikrokontroler ESP32-S3, moduł deweloperski, USB-C |
| 2 | PN532 NFC/RFID Module | U2 | 1 | Moduł czytnika NFC/RFID 13.56 MHz, interfejs I2C |
| 3 | HW-482 Relay Module | K1 | 1 | Moduł przekaźnika 1-kanałowy, sterowanie active LOW, 5V |
| 4 | RGB LED (Common Cathode) | LED1 | 1 | Dioda LED RGB ze wspólną katodą, 5mm |
| 5 | TMB12A05 Active Buzzer | BZ1 | 1 | Buzzer aktywny 5V, ⌀12mm |
| 6 | Przycisk chwilowy (tact switch) | SW1 | 1 | Przycisk Reset/Factory Reset, NO |
| 7 | Przewody połączeniowe | — | ~15 | Dupont male-female / male-male |
| 8 | Zasilacz USB 5V | — | 1 | Zasilanie przez USB-C mikrokontrolera |

---

## 2. Schemat elektryczny — Pinout

### 2.1 Tabela połączeń

| Komponent | Pin komponentu | Pin ESP32 (Arduino) | Pin ESP32 (GPIO) | Uwagi |
|-----------|---------------|---------------------|-------------------|-------|
| **PN532** | SDA | A4 | GPIO11 | Domyślna linia I2C SDA |
| **PN532** | SCL | A5 | GPIO12 | Domyślna linia I2C SCL |
| **PN532** | VCC | 3V3 | — | Zasilanie 3.3V |
| **PN532** | GND | GND | — | Masa |
| **RGB LED** | R (Red) | D2 | GPIO5 | Czerwony — wyjście cyfrowe |
| **RGB LED** | G (Green) | D3 | GPIO6 | Zielony — wyjście cyfrowe |
| **RGB LED** | B (Blue) | D4 | GPIO7 | Niebieski — wyjście cyfrowe |
| **RGB LED** | Katoda (–) | GND | — | Wspólna katoda → masa |
| **Buzzer** | + (sygnał) | D5 | GPIO8 | Sterowanie HIGH/LOW |
| **Buzzer** | – | GND | — | Masa |
| **HW-482 Relay** | IN1 | D6 | GPIO9 | **Active LOW** — LOW = otwarty |
| **HW-482 Relay** | VCC | 5V (VUSB) | — | Zasilanie 5V z USB |
| **HW-482 Relay** | GND | GND | — | Masa |
| **Reset Button** | Pin 1 | D7 | GPIO10 | INPUT_PULLUP, przytrzymać 3s przy starcie |
| **Reset Button** | Pin 2 | GND | — | Masa |

### 2.2 Konfiguracja DIP Switch modułu PN532

| Switch | Pozycja | Tryb |
|--------|---------|------|
| SW1 | **ON** | I2C |
| SW2 | **OFF** | I2C |

> Adres I2C modułu PN532: **0x24** (domyślny)

---

## 3. Opis użytych elementów

### 3.1 Arduino Nano ESP32 (ESP32-S3)

| Parametr | Wartość |
|----------|---------|
| Chipset | Espressif ESP32-S3 |
| Rdzeń | Dual-core Xtensa LX7, do 240 MHz |
| RAM | 512 KB SRAM + 8 MB PSRAM |
| Flash | 16 MB |
| WiFi | 802.11 b/g/n, 2.4 GHz |
| Bluetooth | BLE 5.0 (nieużywany w projekcie) |
| USB | USB-C (natywne USB OTG) |
| Napięcie logiki | 3.3V |
| Wyjścia zasilania | 3V3 (3.3V), 5V (VUSB — bezpośrednio z USB) |
| Wymiary | 45 × 18 mm |
| Interfejsy | I2C, SPI, UART, GPIO, ADC, DAC |
| Obudowa | Moduł DIP, zgodny z pinoutem Arduino Nano |
| Pamięć nieulotna | EEPROM (emulowana we Flash), Preferences API (NVS) |

**Uwagi:**
- Domyślne piny I2C (`Wire.begin()`) na Nano ESP32 to GPIO21/GPIO22, **nie** A4/A5. Firmware ręcznie inicjalizuje `Wire.begin(A4, A5)`.
- Timeout I2C ustawiony na 3000 ms (clock stretching dla operacji RF na PN532).
- Upload firmware przez protokół DFU (USB).

### 3.2 PN532 NFC/RFID Module

| Parametr | Wartość |
|----------|---------|
| Chipset | NXP PN532 |
| Standardy NFC | ISO/IEC 14443A/B, FeliCa, ISO/IEC 18092 |
| Częstotliwość | 13.56 MHz |
| Odczyt kart | MIFARE Classic 1K/4K, MIFARE Ultralight, NTAG21x |
| Interfejsy | I2C, SPI, UART (wybór switch DIP) |
| Napięcie zasilania | 3.3V – 5.5V |
| Prąd pracy | ~100 mA (podczas skanowania RF) |
| Zasięg odczytu | ~5 cm (zależy od anteny i karty) |
| Moduł | Breakout board z wbudowaną anteną PCB |
| Adres I2C | 0x24 |
| Biblioteka | Adafruit PN532 v1.3.4 |

**Uwagi:**
- Tryb pracy: polling I2C (bez pinu IRQ — parametr `-1` w konstruktorze).
- `setPassiveActivationRetries(1)` — 1 retry = 2 próby RF na odczyt.
- Przed `Wire.begin()` firmware wykonuje **I2C bus recovery** (18 impulsów zegara + STOP) — zapobiega zawieszeniu magistrali po restarcie ESP32 w trakcie transmisji.

### 3.3 HW-482 Relay Module (1-kanałowy)

| Parametr | Wartość |
|----------|---------|
| Kanały | 1 |
| Napięcie sterowania | 5V |
| Wejście sterujące | Active LOW (LOW = przekaźnik załączony) |
| Obciążalność styku | AC 250V/10A, DC 30V/10A |
| Izolacja | Optoizolacja (optocoupler) |
| Zasilanie | 5V DC (z pinu VUSB na ESP32) |
| Wskaźnik | LED na płytce modułu |
| Wymiary | ~50 × 26 × 18 mm |

**Logika sterowania:**
- `digitalWrite(RELAY_PIN, LOW)` → przekaźnik **otwarty** (zamek odblokowany)
- `digitalWrite(RELAY_PIN, HIGH)` → przekaźnik **zamknięty** (zamek zablokowany) — stan domyślny

### 3.4 RGB LED (Common Cathode)

| Parametr | Wartość |
|----------|---------|
| Typ | LED RGB 5mm, wspólna katoda |
| Kolory | Czerwony, Zielony, Niebieski |
| Napięcie przewodzenia | R: ~2.0V, G: ~3.0V, B: ~3.0V |
| Prąd roboczy | 20 mA na kolor |
| Sterowanie | Cyfrowe (HIGH/LOW), bez PWM |

**Sygnalizacja kolorami:**
| Kolor | Znaczenie |
|-------|-----------|
| Niebieski | Gotowy — normalny tryb, NFC dostępne |
| Pomarańczowy (R+G) | NFC niedostępne |
| Zielony | Dostęp przyznany |
| Czerwony | Dostęp odmówiony |
| Żółty (R+G) | Oczekiwanie na provisioning MQTT |
| Fioletowy (R+B) | Tryb enrollment (mruganie) |
| Biały (R+G+B) | Factory reset potwierdzony |

### 3.5 TMB12A05 Active Buzzer

| Parametr | Wartość |
|----------|---------|
| Typ | Buzzer aktywny (wbudowany generator) |
| Napięcie pracy | 5V DC |
| Częstotliwość dźwięku | ~2300 Hz (stała) |
| Prąd | ~30 mA |
| Średnica | 12 mm |
| Sterowanie | Proste HIGH/LOW |

**Wzorce dźwiękowe:**
| Wzorzec | Znaczenie |
|---------|-----------|
| 1× krótki beep (80 ms) | OK / potwierdzenie |
| 2× beep (80+80 ms) | Dostęp przyznany / zamek otwarty |
| 3× szybki beep (60 ms) | Błąd / dostęp odmówiony |

### 3.6 Przycisk chwilowy (Tact Switch)

| Parametr | Wartość |
|----------|---------|
| Typ | Momentary push button (NO) |
| Podciąganie | Wewnętrzne INPUT_PULLUP (ESP32) |
| Logika | Active LOW (wciśnięty = LOW) |
| Funkcja | Factory reset (3s hold przy starcie) |

---

## 4. Magistrale i protokoły komunikacyjne

### 4.1 I2C (Inter-Integrated Circuit)

| Parametr | Wartość |
|----------|---------|
| Zastosowanie | Komunikacja ESP32 ↔ PN532 |
| Linie | SDA (A4/GPIO11), SCL (A5/GPIO12) |
| Napięcie logiki | 3.3V |
| Tryb | Master (ESP32) — Slave (PN532) |
| Adres slave | 0x24 (PN532) |
| Timeout | 3000 ms (clock stretching) |
| Prędkość | Standard Mode (100 kHz) |
| Pull-upy | Na module PN532 (wbudowane) |

**Odzyskiwanie magistrali (Bus Recovery):**
Firmware implementuje procedurę odzyskiwania I2C przy starcie — 18 impulsów zegara + sygnał STOP — aby zwolnić linię SDA, jeśli PN532 trzyma ją nisko po niespodziewanym resecie.

### 4.2 WiFi (IEEE 802.11)

| Parametr | Wartość |
|----------|---------|
| Standard | 802.11 b/g/n |
| Pasmo | 2.4 GHz |
| Tryb | Station (STA) |
| Provisioning | Captive Portal (WiFiManager) |
| Timeout AP | 180 sekund |
| Nazwa AP | `AccessControl-XXYY` (XX/YY = ostatnie 2 bajty MAC) |
| Hostname | `ac-XXXXXXXX` (8 znaków HWID) |

**Przepływ provisioning WiFi:**
1. Przy pierwszym uruchomieniu (lub po factory reset) ESP32 tworzy Access Point.
2. Użytkownik łączy się z AP i konfiguruje WiFi przez captive portal.
3. Dane WiFi zapisywane w NVS (Flash) — przetrwają restart.

### 4.3 MQTT (Message Queuing Telemetry Transport)

| Parametr | Wartość |
|----------|---------|
| Zastosowanie | Komunikacja urządzenie ↔ serwer backend |
| Broker | Mosquitto (docker) lub dowolny MQTT 3.1.1 |
| Port | Konfigurowalny (domyślny: 1883) |
| QoS subskrypcji | 1 (at least once) |
| Keep-alive | 60 sekund |
| LWT (Last Will) | `accesscontrol/{hwid}/heartbeat` → `{"status":"offline"}` (retain) |
| Bufor wiadomości | 512 bajtów |
| Biblioteka | PubSubClient v2.8 |
| Reconnect | Exponential backoff: 2s → 4s → 8s → ... → 30s (max) |

**Przestrzeń nazw topiców:**

```
accesscontrol/{hwid}/
├── announce          (PUB, retain) — ogłoszenie urządzenia
├── heartbeat         (PUB, retain) — status online/offline + uptime, RSSI, heap
├── card/
│   ├── scanned       (PUB) — karta odczytana w trybie normalnym
│   ├── enrolled      (PUB) — karta odczytana w trybie enrollment
│   ├── enroll        (SUB) — rozpoczęcie/anulowanie enrollment
│   └── result        (SUB) — wynik weryfikacji karty (granted/denied)
├── config/
│   ├── set           (SUB) — nowa konfiguracja urządzenia
│   └── ack           (PUB) — potwierdzenie zastosowania konfiguracji
└── lock/
    ├── command        (SUB) — komenda open/close zamka
    └── status         (PUB) — status zamka (open/closed)
```

**Provisioning MQTT:**
- Przez HTTP POST na `http://<ip>/api/provision` z JSON `{"broker":"...", "port":..., "username":"...", "password":"..."}`
- Przez komendę serialową: `mqtt_set <ip> [port]`
- Zapis w EEPROM (adres 96–227)

### 4.4 mDNS (Multicast DNS)

| Parametr | Wartość |
|----------|---------|
| Zastosowanie | Automatyczne odkrywanie urządzeń w sieci lokalnej |
| Typ usługi | `_accesscontrol._tcp` |
| Port | 80 |
| Hostname | `ac-XXXXXXXX.local` |

**Rekordy TXT:**

| Klucz | Wartość | Opis |
|-------|---------|------|
| `hwid` | UUID urządzenia | Unikalny identyfikator (UUID v5-like z MAC) |
| `model` | `NanoESP32-CardReader` | Model urządzenia |
| `mac` | `XX:XX:XX:XX:XX:XX` | Adres MAC |
| `features` | `17` | Maska bitowa funkcji (card reader + lock) |
| `fw` | `2.0.0` | Wersja firmware |

### 4.5 HTTP (Provisioning Server)

| Parametr | Wartość |
|----------|---------|
| Port | 80 |
| Endpoint | `POST /api/provision` |
| Content-Type | `application/json` |
| Aktywny | Tylko gdy MQTT nie jest skonfigurowany |
| Biblioteka | WebServer (ESP32 wbudowany) |

### 4.6 UART (Serial)

| Parametr | Wartość |
|----------|---------|
| Zastosowanie | Debug / komendy serwisowe |
| Prędkość | 115200 baud |
| Komendy | `help`, `mqtt_set <ip> [port]`, `mqtt_reset` |

---

## 5. Mapa pamięci EEPROM

| Adres | Rozmiar | Magic Byte | Dane |
|-------|---------|------------|------|
| 0 | 1 B | `0xAC` | Device ID magic |
| 1–36 | 36 B | — | HWID (UUID, string ASCII) |
| 64 | 1 B | `0xD0` | Config magic |
| 65–78 | 14 B | — | Konfiguracja urządzenia (heartbeat, enrollment timeout, buzzer, LED, lock duration) |
| 96 | 1 B | `0x4D` | MQTT config magic |
| 97–160 | 64 B | — | Broker address (string) |
| 161–162 | 2 B | — | Port (uint16, little-endian) |
| 163–194 | 32 B | — | Username (string) |
| 195–226 | 32 B | — | Password (string) |

**Łączny rozmiar EEPROM:** 256 bajtów

---

## 6. Wymagania zasilania

| Źródło | Napięcie | Odbiorcy |
|--------|----------|----------|
| USB-C | 5V | ESP32, Buzzer (TMB12A05), Relay (HW-482) |
| Regulator ESP32 | 3.3V | PN532, RGB LED, logika ESP32 |

**Szacunkowy pobór prądu:**
| Stan | Prąd (szacunkowo) |
|------|-------------------|
| Idle (WiFi + MQTT) | ~80 mA |
| Skanowanie NFC | ~180 mA |
| Przekaźnik aktywny | +70 mA |
| Buzzer aktywny | +30 mA |
| **Max (wszystko naraz)** | **~360 mA** |

---

## 7. Zależności — Biblioteki firmware

| Biblioteka | Wersja | Zastosowanie |
|------------|--------|-------------|
| Adafruit PN532 | ^1.3.4 | Komunikacja z czytnikiem NFC (I2C) |
| PubSubClient | ^2.8 | Klient MQTT |
| WiFiManager | ^2.0.17 | Konfiguracja WiFi przez Captive Portal |
| ArduinoJson | ^7.4.1 | Parsowanie / budowanie JSON |
| Wire (wbudowana) | — | Magistrala I2C |
| WiFi (wbudowana) | — | Obsługa WiFi ESP32 |
| ESPmDNS (wbudowana) | — | Rozgłaszanie usługi mDNS |
| WebServer (wbudowana) | — | Serwer HTTP do provisioningu |
| EEPROM (wbudowana) | — | Zapis konfiguracji w pamięci Flash |

**Platforma:** PlatformIO, `espressif32`, board `arduino_nano_esp32`, framework Arduino.
