/*
 * AccessControl — Card Reader (standalone)
 * Poprawiona inicjalizacja NFC (bez skanera I2C)
 */

#include <Wire.h>
#include <Adafruit_PN532.h>
#include <ESP8266WiFi.h>
#include <ESP8266mDNS.h>
#include <PubSubClient.h>
#include <WiFiManager.h>
#include <ArduinoJson.h>
#include <EEPROM.h>

// ═══════════════════════════════════════════════════════════
//  CONFIGURATION
// ═══════════════════════════════════════════════════════════

#define LED_R_PIN    D5
#define LED_G_PIN    D6
#define LED_B_PIN    D7
#define BUZZER_PIN   D8

#define MQTT_BROKER_IP   "192.168.0.3"
#define MQTT_PORT        1883
#define MQTT_USERNAME    "dev_mqtt_user"
#define MQTT_PASSWORD    "dev_mqtt_pass123"
#define MQTT_TOPIC_PREFIX "accesscontrol"

#define DEVICE_MODEL     "D1Mini-CardReader"
#define DEVICE_FEATURES  "1"
#define FIRMWARE_VERSION "1.0.0"

#define EEPROM_SIZE              128
#define EEPROM_DEVID_MAGIC       0xAC
#define EEPROM_DEVID_MAGIC_ADDR  0
#define EEPROM_DEVID_HWID_ADDR   1
#define EEPROM_DEVID_HWID_LEN    36
#define EEPROM_CFG_MAGIC         0xCF
#define EEPROM_CFG_MAGIC_ADDR    64
#define EEPROM_CFG_DATA_ADDR     65

#define DEFAULT_HEARTBEAT_INTERVAL  30000
#define DEFAULT_ENROLLMENT_TIMEOUT  30000
#define DEFAULT_BUZZER_ENABLED      true
#define DEFAULT_LED_BRIGHTNESS      128
#define CARD_READ_COOLDOWN          1500

// ═══════════════════════════════════════════════════════════
//  NFC 
// ═══════════════════════════════════════════════════════════
Adafruit_PN532 nfc(2, 3);

// ═══════════════════════════════════════════════════════════
//  GLOBALS
// ═══════════════════════════════════════════════════════════
WiFiClient espClient;
PubSubClient mqtt(espClient);

char hwid[37] = {0};
bool nfcAvailable = false;

struct DeviceConfig {
  uint32_t heartbeatInterval;
  uint32_t enrollmentTimeout;
  bool     buzzerEnabled;
  uint8_t  ledBrightness;
};
DeviceConfig cfg;

enum DeviceMode { MODE_NORMAL, MODE_ENROLLMENT };
DeviceMode currentMode = MODE_NORMAL;
unsigned long enrollmentStartTime = 0;
unsigned long enrollmentTimeoutMs = 0;
unsigned long lastHeartbeat = 0;
unsigned long lastCardRead = 0;
unsigned long lastMqttReconnect = 0;
unsigned long mqttReconnectDelay = 2000;

uint8_t lastUid[7] = {0};
uint8_t lastUidLen = 0;

char topicBuf[128];
char payloadBuf[256];

// ═══════════════════════════════════════════════════════════
//  HELPERS
// ═══════════════════════════════════════════════════════════
void uidToHex(const uint8_t* uid, uint8_t len, char* out) {
  for (uint8_t i = 0; i < len; i++) {
    sprintf(out + (i * 2), "%02X", uid[i]);
  }
  out[len * 2] = '\0';
}

void setStatusLed(uint8_t r, uint8_t g, uint8_t b) {
  float scale = cfg.ledBrightness / 255.0f;
  analogWrite(LED_R_PIN, (int)(r * scale));
  analogWrite(LED_G_PIN, (int)(g * scale));
  analogWrite(LED_B_PIN, (int)(b * scale));
}

void beepOk() {
  digitalWrite(BUZZER_PIN, HIGH);
  delay(80);
  digitalWrite(BUZZER_PIN, LOW);
}

void beepError() {
  for (int i = 0; i < 3; i++) {
    digitalWrite(BUZZER_PIN, HIGH);
    delay(60);
    digitalWrite(BUZZER_PIN, LOW);
    delay(60);
  }
}

// ═══════════════════════════════════════════════════════════
//  EEPROM
// ═══════════════════════════════════════════════════════════
void eepromPutU32(int addr, uint32_t val) {
  EEPROM.write(addr,     (val)       & 0xFF);
  EEPROM.write(addr + 1, (val >> 8)  & 0xFF);
  EEPROM.write(addr + 2, (val >> 16) & 0xFF);
  EEPROM.write(addr + 3, (val >> 24) & 0xFF);
}

uint32_t eepromGetU32(int addr) {
  return (uint32_t)EEPROM.read(addr)
       | ((uint32_t)EEPROM.read(addr + 1) << 8)
       | ((uint32_t)EEPROM.read(addr + 2) << 16)
       | ((uint32_t)EEPROM.read(addr + 3) << 24);
}

void deviceIdInit() {
  EEPROM.begin(EEPROM_SIZE);

  if (EEPROM.read(EEPROM_DEVID_MAGIC_ADDR) == EEPROM_DEVID_MAGIC) {
    for (int i = 0; i < EEPROM_DEVID_HWID_LEN; i++) {
      hwid[i] = (char)EEPROM.read(EEPROM_DEVID_HWID_ADDR + i);
    }
    hwid[EEPROM_DEVID_HWID_LEN] = '\0';
    EEPROM.end();
    return;
  }

  uint8_t mac[6];
  WiFi.macAddress(mac);
  uint8_t bytes[16] = {
    0xAC, 0xCE, 0x55, 0x10,
    0x00, 0x00,
    mac[0], mac[1], mac[2], mac[3], mac[4], mac[5],
    0x00, 0x00, 0x00, 0x00
  };
  bytes[6] = (bytes[6] & 0x0F) | 0x50;
  bytes[8] = (bytes[8] & 0x3F) | 0x80;

  snprintf(hwid, 37,
    "%02x%02x%02x%02x-%02x%02x-%02x%02x-%02x%02x-%02x%02x%02x%02x%02x%02x",
    bytes[0], bytes[1], bytes[2], bytes[3],
    bytes[4], bytes[5], bytes[6], bytes[7],
    bytes[8], bytes[9], bytes[10], bytes[11],
    bytes[12], bytes[13], bytes[14], bytes[15]);

  EEPROM.write(EEPROM_DEVID_MAGIC_ADDR, EEPROM_DEVID_MAGIC);
  for (int i = 0; i < EEPROM_DEVID_HWID_LEN; i++) {
    EEPROM.write(EEPROM_DEVID_HWID_ADDR + i, (uint8_t)hwid[i]);
  }
  EEPROM.commit();
  EEPROM.end();
  Serial.printf("[DeviceId] Generated: %s\n", hwid);
}

void configLoad() {
  EEPROM.begin(EEPROM_SIZE);
  if (EEPROM.read(EEPROM_CFG_MAGIC_ADDR) == EEPROM_CFG_MAGIC) {
    int a = EEPROM_CFG_DATA_ADDR;
    cfg.heartbeatInterval = eepromGetU32(a); a += 4;
    cfg.enrollmentTimeout = eepromGetU32(a); a += 4;
    a += 4; // reserved
    cfg.buzzerEnabled     = EEPROM.read(a++) != 0;
    cfg.ledBrightness     = EEPROM.read(a++);
  } else {
    cfg.heartbeatInterval = DEFAULT_HEARTBEAT_INTERVAL;
    cfg.enrollmentTimeout = DEFAULT_ENROLLMENT_TIMEOUT;
    cfg.buzzerEnabled     = DEFAULT_BUZZER_ENABLED;
    cfg.ledBrightness     = DEFAULT_LED_BRIGHTNESS;
  }
  EEPROM.end();
}

void configSave() {
  EEPROM.begin(EEPROM_SIZE);
  EEPROM.write(EEPROM_CFG_MAGIC_ADDR, EEPROM_CFG_MAGIC);
  int a = EEPROM_CFG_DATA_ADDR;
  eepromPutU32(a, cfg.heartbeatInterval); a += 4;
  eepromPutU32(a, cfg.enrollmentTimeout); a += 4;
  a += 4; // reserved
  EEPROM.write(a++, cfg.buzzerEnabled ? 1 : 0);
  EEPROM.write(a++, cfg.ledBrightness);
  EEPROM.commit();
  EEPROM.end();
}

// ═══════════════════════════════════════════════════════════
//  MQTT
// ═══════════════════════════════════════════════════════════
void mqttPublishCard(const char* action, const uint8_t* uid, uint8_t uidLen) {
  if (!mqtt.connected()) return;
  char uidHex[15];
  uidToHex(uid, uidLen, uidHex);
  snprintf(topicBuf, sizeof(topicBuf), "%s/%s/card/%s", MQTT_TOPIC_PREFIX, hwid, action);
  snprintf(payloadBuf, sizeof(payloadBuf), "{\"uid\":\"%s\",\"uidLen\":%d}", uidHex, uidLen);
  mqtt.publish(topicBuf, payloadBuf);
  Serial.printf("[Card] %s: %s\n", action, uidHex);
}

void sendHeartbeat() {
  if (!mqtt.connected()) return;
  snprintf(topicBuf, sizeof(topicBuf), "%s/%s/heartbeat", MQTT_TOPIC_PREFIX, hwid);
  snprintf(payloadBuf, sizeof(payloadBuf),
    "{\"status\":\"online\",\"uptime\":%lu,\"rssi\":%d,\"freeHeap\":%u,\"nfc\":%s}",
    millis() / 1000, WiFi.RSSI(), ESP.getFreeHeap(),
    nfcAvailable ? "true" : "false");
  mqtt.publish(topicBuf, payloadBuf, true);
}

void mqttCallback(char* topic, byte* payload, unsigned int length) {
  Serial.printf("[MQTT] %s (%u bytes)\n", topic, length);

  char json[256];
  unsigned int copyLen = (length < sizeof(json) - 1) ? length : sizeof(json) - 1;
  memcpy(json, payload, copyLen);
  json[copyLen] = '\0';

  // Config update
  char configTopic[128];
  snprintf(configTopic, sizeof(configTopic), "%s/%s/config/set", MQTT_TOPIC_PREFIX, hwid);
  if (strcmp(topic, configTopic) == 0) {
    JsonDocument doc;
    if (deserializeJson(doc, json)) return;

    bool changed = false;
    if (doc["heartbeatInterval"].is<uint32_t>()) { cfg.heartbeatInterval = doc["heartbeatInterval"]; changed = true; }
    if (doc["enrollmentTimeout"].is<uint32_t>()) { cfg.enrollmentTimeout = doc["enrollmentTimeout"]; changed = true; }
    if (doc["buzzerEnabled"].is<bool>())         { cfg.buzzerEnabled = doc["buzzerEnabled"];         changed = true; }
    if (doc["ledBrightness"].is<uint8_t>())      { cfg.ledBrightness = doc["ledBrightness"];         changed = true; }

    if (changed) configSave();

    char ackTopic[128];
    snprintf(ackTopic, sizeof(ackTopic), "%s/%s/config/ack", MQTT_TOPIC_PREFIX, hwid);
    mqtt.publish(ackTopic, "{\"applied\":true}");
    Serial.println("[Config] Updated");
    return;
  }

  // Enrollment
  char enrollTopic[128];
  snprintf(enrollTopic, sizeof(enrollTopic), "%s/%s/card/enroll", MQTT_TOPIC_PREFIX, hwid);
  if (strcmp(topic, enrollTopic) == 0) {
    JsonDocument doc;
    if (deserializeJson(doc, json)) return;

    const char* action = doc["action"] | "start";
    if (strcmp(action, "cancel") == 0) {
      if (currentMode == MODE_ENROLLMENT) {
        currentMode = MODE_NORMAL;
        setStatusLed(0, 255, 0);
        Serial.println("[Enroll] Cancelled");
      }
      return;
    }

    uint32_t timeoutSec = doc["timeout"] | (cfg.enrollmentTimeout / 1000);
    enrollmentTimeoutMs = timeoutSec * 1000UL;
    enrollmentStartTime = millis();
    currentMode = MODE_ENROLLMENT;
    setStatusLed(255, 165, 0);
    Serial.printf("[Enroll] Active (timeout %us)\n", timeoutSec);
    return;
  }
}

void mqttConnect() {
  char lwtTopic[128];
  snprintf(lwtTopic, sizeof(lwtTopic), "%s/%s/heartbeat", MQTT_TOPIC_PREFIX, hwid);
  bool ok = mqtt.connect(hwid, MQTT_USERNAME, MQTT_PASSWORD, lwtTopic, 1, true, "{\"status\":\"offline\"}");
  if (ok) {
    Serial.println("[MQTT] Connected!");
    mqttReconnectDelay = 2000;
    snprintf(topicBuf, sizeof(topicBuf), "%s/%s/config/set", MQTT_TOPIC_PREFIX, hwid);
    mqtt.subscribe(topicBuf, 1);
    snprintf(topicBuf, sizeof(topicBuf), "%s/%s/card/enroll", MQTT_TOPIC_PREFIX, hwid);
    mqtt.subscribe(topicBuf, 1);
  } else {
    Serial.printf("[MQTT] Failed rc=%d\n", mqtt.state());
  }
}

void mqttReconnectLoop() {
  if (mqtt.connected()) { mqtt.loop(); return; }
  unsigned long now = millis();
  if (now - lastMqttReconnect < mqttReconnectDelay) return;
  lastMqttReconnect = now;
  Serial.printf("[MQTT] Reconnecting (delay %lums)...\n", mqttReconnectDelay);
  mqttConnect();
  if (!mqtt.connected()) {
    mqttReconnectDelay = min(mqttReconnectDelay * 2, (unsigned long)30000);
  }
}

// ═══════════════════════════════════════════════════════════
//  mDNS
// ═══════════════════════════════════════════════════════════
void mdnsInit() {
  char hostname[32];
  snprintf(hostname, sizeof(hostname), "ac-%.8s", hwid);
  WiFi.hostname(hostname);

  if (!MDNS.begin(hostname)) {
    Serial.println("[mDNS] Failed!");
    return;
  }

  MDNS.addService("_accesscontrol", "_tcp", 80);
  MDNS.addServiceTxt("_accesscontrol", "_tcp", "hwid", hwid);
  MDNS.addServiceTxt("_accesscontrol", "_tcp", "model", DEVICE_MODEL);

  uint8_t mac[6];
  WiFi.macAddress(mac);
  char macStr[18];
  snprintf(macStr, sizeof(macStr), "%02X:%02X:%02X:%02X:%02X:%02X",
           mac[0], mac[1], mac[2], mac[3], mac[4], mac[5]);
  MDNS.addServiceTxt("_accesscontrol", "_tcp", "mac", macStr);
  MDNS.addServiceTxt("_accesscontrol", "_tcp", "features", DEVICE_FEATURES);
  MDNS.addServiceTxt("_accesscontrol", "_tcp", "fw", FIRMWARE_VERSION);

  Serial.printf("[mDNS] %s advertising\n", hostname);
}

// ═══════════════════════════════════════════════════════════
//  I2C BUS RECOVERY
// ═══════════════════════════════════════════════════════════
void i2cBusRecovery() {
  Serial.println("[I2C] Bus recovery...");
  pinMode(D1, OUTPUT);     // SCL
  pinMode(D2, INPUT_PULLUP); // SDA
  for (int i = 0; i < 18; i++) {
    digitalWrite(D1, LOW);
    delayMicroseconds(5);
    digitalWrite(D1, HIGH);
    delayMicroseconds(5);
  }
  // STOP condition
  pinMode(D2, OUTPUT);
  digitalWrite(D2, LOW);
  delayMicroseconds(5);
  digitalWrite(D1, HIGH);
  delayMicroseconds(5);
  digitalWrite(D2, HIGH);
  delayMicroseconds(5);
}

// ═══════════════════════════════════════════════════════════
//  SETUP
// ═══════════════════════════════════════════════════════════
void setup() {
  Serial.begin(115200);
  delay(500);
  Serial.println("\n\n========================================");
  Serial.println("  AccessControl Card Reader (standalone)");
  Serial.println("========================================");

  // ---- NFC ABSOLUTELY FIRST — identical to working test sketch ----
  // Do NOT call Wire.begin(), bus recovery, WiFi.mode, or ANYTHING before this.
  // nfc.begin() internally calls Wire.begin() with correct default pins.
  nfc.begin();
  uint32_t versiondata = nfc.getFirmwareVersion();

  if (!versiondata) {
    // First attempt failed — wait and retry (no bus recovery — it makes things worse)
    Serial.println("[NFC] Attempt 1 failed, retrying...");
    delay(500);
    nfc.begin();
    delay(100);
    versiondata = nfc.getFirmwareVersion();
  }

  if (!versiondata) {
    Serial.println("[NFC] PN532 NOT FOUND! Sprawdz kable.");
    nfcAvailable = false;
  } else {
    nfcAvailable = true;
    Serial.printf("[NFC] PN5%02X fw %d.%d — OK!\n",
      (versiondata >> 24) & 0xFF,
      (versiondata >> 16) & 0xFF,
      (versiondata >> 8) & 0xFF);
    nfc.SAMConfig();
    nfc.setPassiveActivationRetries(0x01);
  }

  // ---- LED & Buzzer (after NFC) ----
  pinMode(LED_R_PIN, OUTPUT);
  pinMode(LED_G_PIN, OUTPUT);
  pinMode(LED_B_PIN, OUTPUT);
  pinMode(BUZZER_PIN, OUTPUT);
  digitalWrite(BUZZER_PIN, LOW);

  configLoad();
  setStatusLed(0, 0, 255);

  // ---- Device ID ----
  deviceIdInit();
  Serial.printf("HWID: %s\n", hwid);

  // ---- WiFi ----
  setStatusLed(0, 0, 128);
  {
    WiFiManager wm;
    wm.setConnectTimeout(30);
    wm.setConfigPortalTimeout(180);
    uint8_t mac[6];
    WiFi.macAddress(mac);
    char apName[24];
    snprintf(apName, sizeof(apName), "AccessControl-%02X%02X", mac[4], mac[5]);
    Serial.printf("[WiFi] AP: %s\n", apName);
    if (!wm.autoConnect(apName)) {
      Serial.println("[WiFi] Failed — restarting");
      ESP.restart();
    }
  }
  Serial.printf("[WiFi] IP: %s\n", WiFi.localIP().toString().c_str());

  // ---- Reinit I2C after WiFi (WiFi can mess with pin muxing) ----
  Wire.begin(D2, D1);
  Wire.setClock(100000);
  Wire.setClockStretchLimit(230000);

  // ---- mDNS ----
  mdnsInit();

  // ---- MQTT ----
  mqtt.setBufferSize(512);
  mqtt.setCallback(mqttCallback);
  Serial.printf("[MQTT] Broker: %s:%d\n", MQTT_BROKER_IP, MQTT_PORT);
  mqtt.setServer(MQTT_BROKER_IP, MQTT_PORT);
  mqttConnect();

  // ---- Ready ----
  if (nfcAvailable) setStatusLed(0, 255, 0);
  else              setStatusLed(255, 165, 0);
  beepOk();
  sendHeartbeat();
  Serial.println("[Ready] Waiting for cards...");
}

// ═══════════════════════════════════════════════════════════
//  LOOP
// ═══════════════════════════════════════════════════════════
void loop() {
  mqttReconnectLoop();
  MDNS.update();

  unsigned long now = millis();

  if (now - lastHeartbeat >= cfg.heartbeatInterval) {
    sendHeartbeat();
    lastHeartbeat = now;
  }

  if (currentMode == MODE_ENROLLMENT && (now - enrollmentStartTime >= enrollmentTimeoutMs)) {
    Serial.println("[Enroll] Timeout");
    currentMode = MODE_NORMAL;
    setStatusLed(0, 255, 0);
  }

  if (!nfcAvailable) {
    delay(100);
    return;
  }

  uint8_t uid[7] = {0};
  uint8_t uidLen = 0;

  if (nfc.readPassiveTargetID(PN532_MIFARE_ISO14443A, uid, &uidLen, 100)) {
    bool sameCard = (uidLen == lastUidLen) && (memcmp(uid, lastUid, uidLen) == 0);
    if (!sameCard || (now - lastCardRead > CARD_READ_COOLDOWN)) {
      memcpy(lastUid, uid, uidLen);
      lastUidLen = uidLen;
      lastCardRead = now;

      if (currentMode == MODE_ENROLLMENT) {
        setStatusLed(0, 255, 255);
        if (cfg.buzzerEnabled) { beepOk(); delay(50); beepOk(); }
        mqttPublishCard("enrolled", uid, uidLen);
        currentMode = MODE_NORMAL;
        delay(200);
        setStatusLed(0, 255, 0);
      } else {
        setStatusLed(0, 0, 255);
        if (cfg.buzzerEnabled) beepOk();
        mqttPublishCard("scanned", uid, uidLen);
        delay(200);
        setStatusLed(0, 255, 0);
      }
    }
  }
}