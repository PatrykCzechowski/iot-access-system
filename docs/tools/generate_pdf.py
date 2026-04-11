"""
Generate a professionally formatted PDF from the electronics specification.
Uses fpdf2 for full layout control — no LaTeX installation needed.
"""

from fpdf import FPDF, XPos, YPos
from pathlib import Path
import pymupdf
import re

DOCS_DIR = Path(__file__).parent.parent
PDF_FILE_PL = DOCS_DIR / "elektronika.pdf"
PDF_FILE_EN = DOCS_DIR / "electronics.pdf"

# ── Colors ──────────────────────────────────────────────────
C_PRIMARY = (26, 60, 110)
C_SECONDARY = (42, 90, 158)
C_ACCENT = (58, 106, 174)
C_TH_BG = (42, 90, 158)
C_TH_FG = (255, 255, 255)
C_ROW_ALT = (240, 244, 250)
C_ROW_NORM = (255, 255, 255)
C_TEXT = (30, 30, 30)
C_MUTED = (100, 100, 100)
C_CODE_BG = (245, 245, 245)
C_BORDER = (200, 200, 200)
C_QUOTE_BAR = (42, 90, 158)
C_QUOTE_BG = (240, 244, 250)


FONT_DIR = "C:/Windows/Fonts"


class SpecPDF(FPDF):
    def __init__(self, header_text="Specyfikacja Projektu \u2014 Elektronika | AccessControl v2.0",
                 footer_format="Strona {page}/{nb}"):
        super().__init__(orientation="P", unit="mm", format="A4")
        self.set_auto_page_break(auto=True, margin=20)
        self.set_margins(18, 18, 18)
        self._header_text = header_text
        self._footer_format = footer_format
        # Load Unicode TTF fonts for Polish character support
        self.add_font("Arial", "", f"{FONT_DIR}/arial.ttf")
        self.add_font("Arial", "B", f"{FONT_DIR}/arialbd.ttf")
        self.add_font("Arial", "I", f"{FONT_DIR}/ariali.ttf")
        self.add_font("Arial", "BI", f"{FONT_DIR}/arialbi.ttf")
        self.add_font("Cour", "", f"{FONT_DIR}/cour.ttf")
        self.add_font("Cour", "B", f"{FONT_DIR}/courbd.ttf")

    def header(self):
        if self.page_no() == 1:
            return
        self.set_font("Arial", "I", 8)
        self.set_text_color(*C_MUTED)
        self.cell(0, 6, self._header_text,
                  new_x=XPos.LEFT, new_y=YPos.NEXT, align="C")
        self.set_draw_color(*C_BORDER)
        self.line(18, self.get_y(), 192, self.get_y())
        self.ln(4)

    def footer(self):
        self.set_y(-15)
        self.set_font("Arial", "I", 8)
        self.set_text_color(*C_MUTED)
        self.cell(0, 10, self._footer_format.format(page=self.page_no(), nb="{nb}"),
                  new_x=XPos.RIGHT, new_y=YPos.TOP, align="C")

    # ── Building blocks ─────────────────────────────────────

    def title_page(self, title, subtitle, meta_lines, author=None):
        self.add_page()
        self.ln(50)
        self.set_font("Arial", "B", 28)
        self.set_text_color(*C_PRIMARY)
        self.cell(0, 14, title, new_x=XPos.LEFT, new_y=YPos.NEXT, align="C")
        self.ln(4)
        self.set_font("Arial", "", 16)
        self.set_text_color(*C_SECONDARY)
        self.cell(0, 10, subtitle, new_x=XPos.LEFT, new_y=YPos.NEXT, align="C")
        self.ln(2)
        self.set_draw_color(*C_PRIMARY)
        self.set_line_width(0.8)
        self.line(60, self.get_y(), 150, self.get_y())
        self.ln(12)
        self.set_font("Arial", "", 11)
        self.set_text_color(*C_TEXT)
        for line in meta_lines:
            self.cell(0, 7, line, new_x=XPos.LEFT, new_y=YPos.NEXT, align="C")
        if author:
            self.ln(16)
            self.set_font("Arial", "", 10)
            self.set_text_color(*C_MUTED)
            self.cell(0, 7, f"Autor: {author}", new_x=XPos.LEFT, new_y=YPos.NEXT, align="C")
        self.set_line_width(0.2)

    def section(self, text, level=2):
        if level == 2:
            if self.get_y() > 240:
                self.add_page()
            self.ln(6)
            self.set_font("Arial", "B", 15)
            self.set_text_color(*C_PRIMARY)
            self.cell(0, 9, text, new_x=XPos.LEFT, new_y=YPos.NEXT)
            self.set_draw_color(*C_PRIMARY)
            self.set_line_width(0.5)
            self.line(18, self.get_y() + 1, 192, self.get_y() + 1)
            self.set_line_width(0.2)
            self.ln(5)
        else:
            if self.get_y() > 255:
                self.add_page()
            self.ln(4)
            self.set_font("Arial", "B", 12)
            self.set_text_color(*C_ACCENT)
            self.cell(0, 8, text, new_x=XPos.LEFT, new_y=YPos.NEXT)
            self.ln(2)

    def para(self, text):
        self.set_font("Arial", "", 10)
        self.set_text_color(*C_TEXT)
        parts = re.split(r'(\*\*[^*]+\*\*)', text)
        for part in parts:
            if part.startswith("**") and part.endswith("**"):
                self.set_font("Arial", "B", 10)
                self.write(5.5, part[2:-2])
                self.set_font("Arial", "", 10)
            else:
                self.write(5.5, part)
        self.ln(6)

    def bullet(self, text):
        self.set_font("Arial", "", 10)
        self.set_text_color(*C_TEXT)
        self.cell(6, 5.5, chr(8226), new_x=XPos.RIGHT, new_y=YPos.TOP)
        parts = re.split(r'(\*\*[^*]+\*\*)', text)
        for part in parts:
            if part.startswith("**") and part.endswith("**"):
                self.set_font("Arial", "B", 10)
                self.write(5.5, part[2:-2])
                self.set_font("Arial", "", 10)
            else:
                self.write(5.5, part)
        self.ln(6)

    def numbered_item(self, num, text):
        self.set_font("Arial", "", 10)
        self.set_text_color(*C_TEXT)
        self.cell(6, 5.5, f"{num}.", new_x=XPos.RIGHT, new_y=YPos.TOP)
        self.write(5.5, text)
        self.ln(6)

    def quote(self, text):
        self.ln(1)
        y = self.get_y()
        self.set_fill_color(*C_QUOTE_BG)
        self.rect(20, y, 170, 8, style="F")
        self.set_fill_color(*C_QUOTE_BAR)
        self.rect(20, y, 1.5, 8, style="F")
        self.set_xy(24, y + 1.5)
        self.set_font("Arial", "I", 9.5)
        self.set_text_color(*C_MUTED)
        self.write(5, text)
        self.set_y(y + 10)
        self.ln(2)

    def code_block(self, lines):
        self.ln(2)
        self.set_font("Cour", "", 8.5)
        y_start = self.get_y()
        line_h = 4.5
        block_h = len(lines) * line_h + 6
        if self.get_y() + block_h > 275:
            self.add_page()
            y_start = self.get_y()
        self.set_fill_color(*C_CODE_BG)
        self.set_draw_color(*C_BORDER)
        self.rect(18, y_start, 174, block_h, style="DF")
        self.set_xy(21, y_start + 3)
        self.set_text_color(60, 60, 60)
        for line in lines:
            self.cell(0, line_h, line, new_x=XPos.LEFT, new_y=YPos.NEXT)
            self.set_x(21)
        self.set_y(y_start + block_h + 3)

    def table(self, headers, rows, col_widths=None):
        n_cols = len(headers)
        avail = 174
        if col_widths is None:
            col_widths = [avail / n_cols] * n_cols

        row_h = 7

        # Header
        self.set_font("Arial", "B", 9)
        self.set_fill_color(*C_TH_BG)
        self.set_text_color(*C_TH_FG)
        self.set_draw_color(*C_BORDER)
        for i, h in enumerate(headers):
            self.cell(col_widths[i], row_h, h, border=1, fill=True,
                      new_x=XPos.RIGHT, new_y=YPos.TOP, align="C")
        self.ln(row_h)

        # Rows
        self.set_font("Arial", "", 9)
        self.set_text_color(*C_TEXT)
        for r_idx, row in enumerate(rows):
            if self.get_y() + row_h > 275:
                self.add_page()
                self.set_font("Arial", "B", 9)
                self.set_fill_color(*C_TH_BG)
                self.set_text_color(*C_TH_FG)
                for i, h in enumerate(headers):
                    self.cell(col_widths[i], row_h, h, border=1, fill=True,
                              new_x=XPos.RIGHT, new_y=YPos.TOP, align="C")
                self.ln(row_h)
                self.set_font("Arial", "", 9)
                self.set_text_color(*C_TEXT)

            bg = C_ROW_ALT if r_idx % 2 == 1 else C_ROW_NORM
            self.set_fill_color(*bg)
            for i, cell_text in enumerate(row):
                clean = cell_text.replace("**", "")
                self.cell(col_widths[i], row_h, clean, border=1, fill=True,
                          new_x=XPos.RIGHT, new_y=YPos.TOP)
            self.ln(row_h)
        self.ln(3)


def build_pdf():
    pdf = SpecPDF()
    pdf.alias_nb_pages()

    # ── Title page ──────────────────────────────────────────
    pdf.title_page(
        "Specyfikacja Projektu",
        "Elektronika",
        [
            "AccessControl Card Reader + Lock Controller v2.0",
            "",
            "Model: NanoESP32-CardReader",
            "Firmware: 2.0.0",
            "Data: 2026-04-11",
        ],
        author="Patryk Czechowski"
    )

    # ════════════════════════════════════════════════════════
    # 1. BOM
    # ════════════════════════════════════════════════════════
    pdf.add_page()
    pdf.section("1. BOM \u2014 Spis element\u00f3w elektronicznych")

    pdf.table(
        ["#", "Element", "Oznaczenie", "Ilo\u015b\u0107", "Opis"],
        [
            ["1", "Arduino Nano ESP32", "U1", "1", "Mikrokontroler ESP32-S3, USB-C"],
            ["2", "PN532 NFC/RFID Module", "U2", "1", "Czytnik NFC/RFID 13.56 MHz, I2C"],
            ["3", "HW-482 Relay Module", "K1", "1", "Przeka\u017anik 1-kan., active LOW, 5V"],
            ["4", "RGB LED (Common Cathode)", "LED1", "1", "LED RGB 5mm, wsp\u00f3lna katoda"],
            ["5", "TMB12A05 Active Buzzer", "BZ1", "1", "Buzzer aktywny 5V, 12mm"],
            ["6", "Przycisk chwilowy", "SW1", "1", "Tact switch, Factory Reset, NO"],
            ["7", "Rezystor 220\u03a9", "R1", "1", "Ograniczenie pr\u0105du LED R"],
            ["8", "Rezystor 100\u03a9", "R2, R3", "2", "Ograniczenie pr\u0105du LED G, B"],
            ["9", "Przewody po\u0142\u0105czeniowe", "\u2014", "~15", "Dupont male-female / male-male"],
            ["10", "Zasilacz USB 5V", "\u2014", "1", "Zasilanie USB-C mikrokontrolera"],
        ],
        col_widths=[8, 45, 24, 12, 85]
    )

    # ════════════════════════════════════════════════════════
    # 2. Schemat elektryczny
    # ════════════════════════════════════════════════════════
    pdf.section("2. Schemat elektryczny \u2014 Pinout")
    pdf.section("2.1 Tabela po\u0142\u0105cze\u0144", level=3)

    pdf.table(
        ["Komponent", "Pin", "ESP32", "GPIO", "Rezystor", "Uwagi"],
        [
            ["PN532", "SDA", "A4", "GPIO11", "4.7k\u03a9 pull-up*", "I2C SDA"],
            ["PN532", "SCL", "A5", "GPIO12", "4.7k\u03a9 pull-up*", "I2C SCL"],
            ["PN532", "VCC", "3V3", "\u2014", "\u2014", "Zasilanie 3.3V"],
            ["PN532", "GND", "GND", "\u2014", "\u2014", "Masa"],
            ["RGB LED", "R (Red)", "D2", "GPIO5", "220\u03a9", "Ograniczenie pr\u0105du"],
            ["RGB LED", "G (Green)", "D3", "GPIO6", "100\u03a9", "Ograniczenie pr\u0105du"],
            ["RGB LED", "B (Blue)", "D4", "GPIO7", "100\u03a9", "Ograniczenie pr\u0105du"],
            ["RGB LED", "Katoda (\u2013)", "GND", "\u2014", "\u2014", "Wsp\u00f3lna katoda"],
            ["Buzzer", "+ (sygna\u0142)", "D5", "GPIO8", "\u2014", "Bezpo\u015brednio z GPIO"],
            ["Buzzer", "\u2013", "GND", "\u2014", "\u2014", "Masa"],
            ["HW-482 Relay", "IN1", "D6", "GPIO9", "\u2014", "Wewn. optoizolacja"],
            ["HW-482 Relay", "VCC", "5V (VUSB)", "\u2014", "\u2014", "Zasilanie 5V"],
            ["HW-482 Relay", "GND", "GND", "\u2014", "\u2014", "Masa"],
            ["Reset Button", "Pin 1", "D7", "GPIO10", "~45k\u03a9 pull-up**", "INPUT_PULLUP, 3s"],
            ["Reset Button", "Pin 2", "GND", "\u2014", "\u2014", "Masa"],
        ],
        col_widths=[26, 26, 22, 18, 34, 48]
    )
    pdf.para("* Pull-up 4.7k\u03a9 na module PN532 (wbudowane). ** Wewn\u0119trzny pull-up ESP32 (~45k\u03a9).")

    pdf.section("2.2 Konfiguracja DIP Switch \u2014 PN532", level=3)
    pdf.table(
        ["Switch", "Pozycja", "Tryb"],
        [
            ["SW1", "ON", "I2C"],
            ["SW2", "OFF", "I2C"],
        ],
        col_widths=[58, 58, 58]
    )
    pdf.quote("Adres I2C modu\u0142u PN532: 0x24 (domy\u015blny)")

    # ════════════════════════════════════════════════════════
    # 3. Opis element\u00f3w
    # ════════════════════════════════════════════════════════
    pdf.section("3. Opis u\u017cytych element\u00f3w")

    # 3.1 ESP32
    pdf.section("3.1 Arduino Nano ESP32 (ESP32-S3)", level=3)
    pdf.table(
        ["Parametr", "Warto\u015b\u0107"],
        [
            ["Chipset", "Espressif ESP32-S3"],
            ["Rdze\u0144", "Dual-core Xtensa LX7, do 240 MHz"],
            ["RAM", "512 KB SRAM + 8 MB PSRAM"],
            ["Flash", "16 MB"],
            ["WiFi", "802.11 b/g/n, 2.4 GHz"],
            ["Bluetooth", "BLE 5.0 (nieu\u017cywany w projekcie)"],
            ["USB", "USB-C (natywne USB OTG)"],
            ["Napi\u0119cie logiki", "3.3V"],
            ["Wyj\u015bcia zasilania", "3V3 (3.3V), 5V (VUSB)"],
            ["Wymiary", "45 x 18 mm"],
            ["Interfejsy", "I2C, SPI, UART, GPIO, ADC, DAC"],
            ["Obudowa", "Modu\u0142 DIP, pinout Arduino Nano"],
            ["Pami\u0119\u0107 nieulotna", "EEPROM (Flash), Preferences API (NVS)"],
        ],
        col_widths=[45, 129]
    )
    pdf.para("Uwagi:")
    pdf.bullet("Domy\u015blne piny I2C (Wire.begin()) na Nano ESP32 to GPIO21/GPIO22, nie A4/A5. Firmware r\u0119cznie inicjalizuje Wire.begin(A4, A5).")
    pdf.bullet("Timeout I2C ustawiony na 3000 ms (clock stretching dla operacji RF na PN532).")
    pdf.bullet("Upload firmware przez protok\u00f3\u0142 DFU (USB).")

    # 3.2 PN532
    pdf.section("3.2 PN532 NFC/RFID Module", level=3)
    pdf.table(
        ["Parametr", "Warto\u015b\u0107"],
        [
            ["Chipset", "NXP PN532"],
            ["Standardy NFC", "ISO/IEC 14443A/B, FeliCa, ISO 18092"],
            ["Cz\u0119stotliwo\u015b\u0107", "13.56 MHz"],
            ["Odczyt kart", "MIFARE Classic 1K/4K, Ultralight, NTAG21x"],
            ["Interfejsy", "I2C, SPI, UART (wyb\u00f3r DIP switch)"],
            ["Napi\u0119cie zasilania", "3.3V \u2013 5.5V"],
            ["Pr\u0105d pracy", "~100 mA (skanowanie RF)"],
            ["Zasi\u0119g odczytu", "~5 cm"],
            ["Modu\u0142", "Breakout board z anten\u0105 PCB"],
            ["Adres I2C", "0x24"],
            ["Biblioteka", "Adafruit PN532 v1.3.4"],
        ],
        col_widths=[45, 129]
    )
    pdf.para("Uwagi:")
    pdf.bullet("Tryb pracy: polling I2C (bez pinu IRQ \u2014 parametr -1 w konstruktorze).")
    pdf.bullet("setPassiveActivationRetries(1) \u2014 1 retry = 2 pr\u00f3by RF na odczyt.")
    pdf.bullet("Przed Wire.begin() firmware wykonuje I2C bus recovery (18 impuls\u00f3w zegara + STOP).")

    # 3.3 Relay
    pdf.section("3.3 HW-482 Relay Module (1-kana\u0142owy)", level=3)
    pdf.table(
        ["Parametr", "Warto\u015b\u0107"],
        [
            ["Kana\u0142y", "1"],
            ["Napi\u0119cie sterowania", "5V"],
            ["Wej\u015bcie steruj\u0105ce", "Active LOW (LOW = za\u0142\u0105czony)"],
            ["Obci\u0105\u017calno\u015b\u0107 styku", "AC 250V/10A, DC 30V/10A"],
            ["Izolacja", "Optoizolacja (optocoupler)"],
            ["Zasilanie", "5V DC (z pinu VUSB na ESP32)"],
            ["Wska\u017anik", "LED na p\u0142ytce modu\u0142u"],
            ["Wymiary", "~50 x 26 x 18 mm"],
        ],
        col_widths=[45, 129]
    )
    pdf.para("Logika sterowania:")
    pdf.bullet("digitalWrite(RELAY_PIN, LOW)  -> przeka\u017anik otwarty (zamek odblokowany)")
    pdf.bullet("digitalWrite(RELAY_PIN, HIGH) -> przeka\u017anik zamkni\u0119ty (zamek zablokowany) \u2014 stan domy\u015blny")

    # 3.4 RGB LED
    pdf.section("3.4 RGB LED (Common Cathode)", level=3)
    pdf.table(
        ["Parametr", "Warto\u015b\u0107"],
        [
            ["Typ", "LED RGB 5mm, wsp\u00f3lna katoda"],
            ["Kolory", "Czerwony, Zielony, Niebieski"],
            ["Napi\u0119cie przewodzenia", "R: ~2.0V, G: ~3.0V, B: ~3.0V"],
            ["Pr\u0105d roboczy", "20 mA na kolor"],
            ["Sterowanie", "Cyfrowe (HIGH/LOW), bez PWM"],
        ],
        col_widths=[45, 129]
    )

    pdf.para("Sygnalizacja kolorami:")
    pdf.table(
        ["Kolor", "Znaczenie"],
        [
            ["Niebieski", "Gotowy \u2014 normalny tryb, NFC dost\u0119pne"],
            ["Pomara\u0144czowy (R+G)", "NFC niedost\u0119pne"],
            ["Zielony", "Dost\u0119p przyznany"],
            ["Czerwony", "Dost\u0119p odm\u00f3wiony"],
            ["\u017b\u00f3\u0142ty (R+G)", "Oczekiwanie na provisioning MQTT"],
            ["Fioletowy (R+B)", "Tryb enrollment (mruganie)"],
            ["Bia\u0142y (R+G+B)", "Factory reset potwierdzony"],
        ],
        col_widths=[45, 129]
    )

    # 3.5 Buzzer
    pdf.section("3.5 TMB12A05 Active Buzzer", level=3)
    pdf.table(
        ["Parametr", "Warto\u015b\u0107"],
        [
            ["Typ", "Buzzer aktywny (wbudowany generator)"],
            ["Napi\u0119cie pracy", "5V DC"],
            ["Cz\u0119stotliwo\u015b\u0107 d\u017awi\u0119ku", "~2300 Hz (sta\u0142a)"],
            ["Pr\u0105d", "~30 mA"],
            ["\u015arednica", "12 mm"],
            ["Sterowanie", "Proste HIGH/LOW"],
        ],
        col_widths=[45, 129]
    )
    pdf.para("Wzorce d\u017awi\u0119kowe:")
    pdf.table(
        ["Wzorzec", "Znaczenie"],
        [
            ["1x kr\u00f3tki beep (80 ms)", "OK / potwierdzenie"],
            ["2x beep (80+80 ms)", "Dost\u0119p przyznany / zamek otwarty"],
            ["3x szybki beep (60 ms)", "B\u0142\u0105d / dost\u0119p odm\u00f3wiony"],
        ],
        col_widths=[58, 116]
    )

    # 3.6 Button
    pdf.section("3.6 Przycisk chwilowy (Tact Switch)", level=3)
    pdf.table(
        ["Parametr", "Warto\u015b\u0107"],
        [
            ["Typ", "Momentary push button (NO)"],
            ["Podci\u0105ganie", "Wewn\u0119trzne INPUT_PULLUP (ESP32)"],
            ["Logika", "Active LOW (wci\u015bni\u0119ty = LOW)"],
            ["Funkcja", "Factory reset (3s hold przy starcie)"],
        ],
        col_widths=[45, 129]
    )

    # ════════════════════════════════════════════════════════
    # 4. Magistrale i protoko\u0142y
    # ════════════════════════════════════════════════════════
    pdf.section("4. Magistrale i protoko\u0142y komunikacyjne")

    # 4.1 I2C
    pdf.section("4.1 I2C (Inter-Integrated Circuit)", level=3)
    pdf.table(
        ["Parametr", "Warto\u015b\u0107"],
        [
            ["Zastosowanie", "Komunikacja ESP32 <-> PN532"],
            ["Linie", "SDA (A4/GPIO11), SCL (A5/GPIO12)"],
            ["Napi\u0119cie logiki", "3.3V"],
            ["Tryb", "Master (ESP32) \u2014 Slave (PN532)"],
            ["Adres slave", "0x24 (PN532)"],
            ["Timeout", "3000 ms (clock stretching)"],
            ["Pr\u0119dko\u015b\u0107", "Standard Mode (100 kHz)"],
            ["Pull-upy", "Na module PN532 (wbudowane)"],
        ],
        col_widths=[45, 129]
    )
    pdf.para("Odzyskiwanie magistrali (Bus Recovery):")
    pdf.para("Firmware implementuje procedur\u0119 odzyskiwania I2C przy starcie \u2014 18 impuls\u00f3w zegara + sygna\u0142 STOP \u2014 aby zwolni\u0107 lini\u0119 SDA, je\u015bli PN532 trzyma j\u0105 nisko po niespodziewanym resecie.")

    # 4.2 WiFi
    pdf.section("4.2 WiFi (IEEE 802.11)", level=3)
    pdf.table(
        ["Parametr", "Warto\u015b\u0107"],
        [
            ["Standard", "802.11 b/g/n"],
            ["Pasmo", "2.4 GHz"],
            ["Tryb", "Station (STA)"],
            ["Provisioning", "Captive Portal (WiFiManager)"],
            ["Timeout AP", "180 sekund"],
            ["Nazwa AP", "AccessControl-XXYY (ostatnie 2 B MAC)"],
            ["Hostname", "ac-XXXXXXXX (8 znak\u00f3w HWID)"],
        ],
        col_widths=[45, 129]
    )
    pdf.para("Przep\u0142yw provisioning WiFi:")
    pdf.numbered_item(1, "Przy pierwszym uruchomieniu (lub po factory reset) ESP32 tworzy Access Point.")
    pdf.numbered_item(2, "U\u017cytkownik \u0142\u0105czy si\u0119 z AP i konfiguruje WiFi przez captive portal.")
    pdf.numbered_item(3, "Dane WiFi zapisywane w NVS (Flash) \u2014 przetrwaj\u0105 restart.")

    # 4.3 MQTT
    pdf.section("4.3 MQTT (Message Queuing Telemetry Transport)", level=3)
    pdf.table(
        ["Parametr", "Warto\u015b\u0107"],
        [
            ["Zastosowanie", "Komunikacja urz\u0105dzenie <-> serwer backend"],
            ["Broker", "Mosquitto (docker) lub dowolny MQTT 3.1.1"],
            ["Port", "Konfigurowalny (domy\u015blny: 1883)"],
            ["QoS subskrypcji", "1 (at least once)"],
            ["Keep-alive", "60 sekund"],
            ["LWT", "accesscontrol/{hwid}/heartbeat -> offline"],
            ["Bufor wiadomo\u015bci", "512 bajt\u00f3w"],
            ["Biblioteka", "PubSubClient v2.8"],
            ["Reconnect", "Exponential backoff: 2s -> ... -> 30s"],
        ],
        col_widths=[45, 129]
    )

    pdf.para("Przestrze\u0144 nazw topik\u00f3w MQTT:")
    pdf.code_block([
        "accesscontrol/{hwid}/",
        "|-- announce          (PUB, retain)  ogloszenie urzadzenia",
        "|-- heartbeat         (PUB, retain)  status + uptime, RSSI, heap",
        "|-- card/",
        "|   |-- scanned       (PUB)          karta odczytana (tryb normalny)",
        "|   |-- enrolled      (PUB)          karta odczytana (enrollment)",
        "|   |-- enroll        (SUB)          start/cancel enrollment",
        "|   +-- result        (SUB)          wynik: granted / denied",
        "|-- config/",
        "|   |-- set           (SUB)          nowa konfiguracja",
        "|   +-- ack           (PUB)          potwierdzenie",
        "+-- lock/",
        "    |-- command       (SUB)          open / close",
        "    +-- status        (PUB)          stan zamka",
    ])

    pdf.para("Provisioning MQTT:")
    pdf.bullet("Przez HTTP POST na http://<ip>/api/provision z JSON {broker, port, username, password}")
    pdf.bullet("Przez komend\u0119 serialow\u0105: mqtt_set <ip> [port]")
    pdf.bullet("Zapis w EEPROM (adres 96\u2013227)")

    # 4.4 mDNS
    pdf.section("4.4 mDNS (Multicast DNS)", level=3)
    pdf.table(
        ["Parametr", "Warto\u015b\u0107"],
        [
            ["Zastosowanie", "Automatyczne odkrywanie urz\u0105dze\u0144 w LAN"],
            ["Typ us\u0142ugi", "_accesscontrol._tcp"],
            ["Port", "80"],
            ["Hostname", "ac-XXXXXXXX.local"],
        ],
        col_widths=[45, 129]
    )
    pdf.para("Rekordy TXT:")
    pdf.table(
        ["Klucz", "Warto\u015b\u0107", "Opis"],
        [
            ["hwid", "UUID urz\u0105dzenia", "Unikalny identyfikator (UUID v5 z MAC)"],
            ["model", "NanoESP32-CardReader", "Model urz\u0105dzenia"],
            ["mac", "XX:XX:XX:XX:XX:XX", "Adres MAC"],
            ["features", "17", "Maska bitowa funkcji (reader + lock)"],
            ["fw", "2.0.0", "Wersja firmware"],
        ],
        col_widths=[30, 50, 94]
    )

    # 4.5 HTTP
    pdf.section("4.5 HTTP (Provisioning Server)", level=3)
    pdf.table(
        ["Parametr", "Warto\u015b\u0107"],
        [
            ["Port", "80"],
            ["Endpoint", "POST /api/provision"],
            ["Content-Type", "application/json"],
            ["Aktywny", "Tylko gdy MQTT nie jest skonfigurowany"],
            ["Biblioteka", "WebServer (ESP32 wbudowany)"],
        ],
        col_widths=[45, 129]
    )

    # 4.6 UART
    pdf.section("4.6 UART (Serial)", level=3)
    pdf.table(
        ["Parametr", "Warto\u015b\u0107"],
        [
            ["Zastosowanie", "Debug / komendy serwisowe"],
            ["Pr\u0119dko\u015b\u0107", "115200 baud"],
            ["Komendy", "help, mqtt_set <ip> [port], mqtt_reset"],
        ],
        col_widths=[45, 129]
    )

    # ════════════════════════════════════════════════════════
    # 5. EEPROM Map
    # ════════════════════════════════════════════════════════
    pdf.section("5. Mapa pami\u0119ci EEPROM")
    pdf.table(
        ["Adres", "Rozmiar", "Magic Byte", "Dane"],
        [
            ["0", "1 B", "0xAC", "Device ID magic"],
            ["1\u201336", "36 B", "\u2014", "HWID (UUID, string ASCII)"],
            ["64", "1 B", "0xD0", "Config magic"],
            ["65\u201378", "14 B", "\u2014", "Konfiguracja (heartbeat, timeout, buzzer, LED, lock)"],
            ["96", "1 B", "0x4D", "MQTT config magic"],
            ["97\u2013160", "64 B", "\u2014", "Broker address (string)"],
            ["161\u2013162", "2 B", "\u2014", "Port (uint16, little-endian)"],
            ["163\u2013194", "32 B", "\u2014", "Username (string)"],
            ["195\u2013226", "32 B", "\u2014", "Password (string)"],
        ],
        col_widths=[25, 20, 28, 101]
    )
    pdf.para("\u0141\u0105czny rozmiar EEPROM: 256 bajt\u00f3w")

    # ════════════════════════════════════════════════════════
    # 6. Zasilanie
    # ════════════════════════════════════════════════════════
    pdf.section("6. Wymagania zasilania")
    pdf.table(
        ["\u0179r\u00f3d\u0142o", "Napi\u0119cie", "Odbiorcy"],
        [
            ["USB-C", "5V", "ESP32, Buzzer (TMB12A05), Relay (HW-482)"],
            ["Regulator ESP32", "3.3V", "PN532, RGB LED, logika ESP32"],
        ],
        col_widths=[40, 30, 104]
    )

    pdf.para("Szacunkowy pob\u00f3r pr\u0105du:")
    pdf.table(
        ["Stan", "Pr\u0105d (szacunkowo)"],
        [
            ["Idle (WiFi + MQTT)", "~80 mA"],
            ["Skanowanie NFC", "~180 mA"],
            ["Przeka\u017anik aktywny", "+70 mA"],
            ["Buzzer aktywny", "+30 mA"],
            ["Max (wszystko naraz)", "~360 mA"],
        ],
        col_widths=[87, 87]
    )

    # ════════════════════════════════════════════════════════
    # 7. Biblioteki
    # ════════════════════════════════════════════════════════
    pdf.section("7. Zale\u017cno\u015bci \u2014 Biblioteki firmware")
    pdf.table(
        ["Biblioteka", "Wersja", "Zastosowanie"],
        [
            ["Adafruit PN532", "^1.3.4", "Komunikacja z czytnikiem NFC (I2C)"],
            ["PubSubClient", "^2.8", "Klient MQTT"],
            ["WiFiManager", "^2.0.17", "Konfiguracja WiFi (Captive Portal)"],
            ["ArduinoJson", "^7.4.1", "Parsowanie / budowanie JSON"],
            ["Wire (wbudowana)", "\u2014", "Magistrala I2C"],
            ["WiFi (wbudowana)", "\u2014", "Obs\u0142uga WiFi ESP32"],
            ["ESPmDNS (wbudowana)", "\u2014", "Rozg\u0142aszanie us\u0142ugi mDNS"],
            ["WebServer (wbudowana)", "\u2014", "Serwer HTTP do provisioningu"],
            ["EEPROM (wbudowana)", "\u2014", "Zapis konfiguracji w pami\u0119ci Flash"],
        ],
        col_widths=[45, 22, 107]
    )
    pdf.para("Platforma: PlatformIO, espressif32, board arduino_nano_esp32, framework Arduino.")

    # ── Save ────────────────────────────────────────────────
    pdf.output(str(PDF_FILE_PL))
    return PDF_FILE_PL


def build_pdf_en():
    pdf = SpecPDF(
        header_text="Project Specification \u2014 Electronics | AccessControl v2.0",
        footer_format="Page {page}/{nb}",
    )
    pdf.alias_nb_pages()

    # ── Title page ──────────────────────────────────────────
    pdf.title_page(
        "Project Specification",
        "Electronics",
        [
            "AccessControl Card Reader + Lock Controller v2.0",
            "",
            "Model: NanoESP32-CardReader",
            "Firmware: 2.0.0",
            "Date: 2026-04-11",
        ],
        author="Patryk Czechowski"
    )

    # ════════════════════════════════════════════════════════
    # 1. BOM
    # ════════════════════════════════════════════════════════
    pdf.add_page()
    pdf.section("1. BOM \u2014 Bill of Materials")

    pdf.table(
        ["#", "Component", "Designator", "Qty", "Description"],
        [
            ["1", "Arduino Nano ESP32", "U1", "1", "ESP32-S3 microcontroller, USB-C"],
            ["2", "PN532 NFC/RFID Module", "U2", "1", "NFC/RFID reader 13.56 MHz, I2C"],
            ["3", "HW-482 Relay Module", "K1", "1", "1-ch relay, active LOW, 5V"],
            ["4", "RGB LED (Common Cathode)", "LED1", "1", "5mm RGB LED, common cathode"],
            ["5", "TMB12A05 Active Buzzer", "BZ1", "1", "Active buzzer 5V, 12mm"],
            ["6", "Momentary Push Button", "SW1", "1", "Tact switch, Factory Reset, NO"],
            ["7", "220\u03a9 Resistor", "R1", "1", "Current limiter for LED R"],
            ["8", "100\u03a9 Resistor", "R2, R3", "2", "Current limiter for LED G, B"],
            ["9", "Jumper Wires", "\u2014", "~15", "Dupont male-female / male-male"],
            ["10", "USB 5V Power Supply", "\u2014", "1", "USB-C power for microcontroller"],
        ],
        col_widths=[8, 45, 24, 12, 85]
    )

    # ════════════════════════════════════════════════════════
    # 2. Wiring Diagram
    # ════════════════════════════════════════════════════════
    pdf.section("2. Wiring Diagram \u2014 Pinout")
    pdf.section("2.1 Connection Table", level=3)

    pdf.table(
        ["Component", "Pin", "ESP32", "GPIO", "Resistor", "Notes"],
        [
            ["PN532", "SDA", "A4", "GPIO11", "4.7k\u03a9 pull-up*", "I2C SDA"],
            ["PN532", "SCL", "A5", "GPIO12", "4.7k\u03a9 pull-up*", "I2C SCL"],
            ["PN532", "VCC", "3V3", "\u2014", "\u2014", "3.3V power"],
            ["PN532", "GND", "GND", "\u2014", "\u2014", "Ground"],
            ["RGB LED", "R (Red)", "D2", "GPIO5", "220\u03a9", "Current limiting"],
            ["RGB LED", "G (Green)", "D3", "GPIO6", "100\u03a9", "Current limiting"],
            ["RGB LED", "B (Blue)", "D4", "GPIO7", "100\u03a9", "Current limiting"],
            ["RGB LED", "Cathode (\u2013)", "GND", "\u2014", "\u2014", "Common cathode"],
            ["Buzzer", "+ (signal)", "D5", "GPIO8", "\u2014", "Direct GPIO drive"],
            ["Buzzer", "\u2013", "GND", "\u2014", "\u2014", "Ground"],
            ["HW-482 Relay", "IN1", "D6", "GPIO9", "\u2014", "Built-in optocoupler"],
            ["HW-482 Relay", "VCC", "5V (VUSB)", "\u2014", "\u2014", "5V power"],
            ["HW-482 Relay", "GND", "GND", "\u2014", "\u2014", "Ground"],
            ["Reset Button", "Pin 1", "D7", "GPIO10", "~45k\u03a9 pull-up**", "INPUT_PULLUP, 3s"],
            ["Reset Button", "Pin 2", "GND", "\u2014", "\u2014", "Ground"],
        ],
        col_widths=[26, 26, 22, 18, 34, 48]
    )
    pdf.para("* 4.7k\u03a9 pull-ups on the PN532 module (built-in). ** Internal ESP32 pull-up (~45k\u03a9).")

    pdf.section("2.2 DIP Switch Configuration \u2014 PN532", level=3)
    pdf.table(
        ["Switch", "Position", "Mode"],
        [
            ["SW1", "ON", "I2C"],
            ["SW2", "OFF", "I2C"],
        ],
        col_widths=[58, 58, 58]
    )
    pdf.quote("PN532 I2C address: 0x24 (default)")

    # ════════════════════════════════════════════════════════
    # 3. Component Descriptions
    # ════════════════════════════════════════════════════════
    pdf.section("3. Component Descriptions")

    # 3.1 ESP32
    pdf.section("3.1 Arduino Nano ESP32 (ESP32-S3)", level=3)
    pdf.table(
        ["Parameter", "Value"],
        [
            ["Chipset", "Espressif ESP32-S3"],
            ["Core", "Dual-core Xtensa LX7, up to 240 MHz"],
            ["RAM", "512 KB SRAM + 8 MB PSRAM"],
            ["Flash", "16 MB"],
            ["WiFi", "802.11 b/g/n, 2.4 GHz"],
            ["Bluetooth", "BLE 5.0 (unused in this project)"],
            ["USB", "USB-C (native USB OTG)"],
            ["Logic voltage", "3.3V"],
            ["Power outputs", "3V3 (3.3V), 5V (VUSB)"],
            ["Dimensions", "45 x 18 mm"],
            ["Interfaces", "I2C, SPI, UART, GPIO, ADC, DAC"],
            ["Package", "DIP module, Arduino Nano pinout"],
            ["Non-volatile memory", "EEPROM (Flash), Preferences API (NVS)"],
        ],
        col_widths=[45, 129]
    )
    pdf.para("Notes:")
    pdf.bullet("Default I2C pins (Wire.begin()) on Nano ESP32 are GPIO21/GPIO22, not A4/A5. Firmware manually initializes Wire.begin(A4, A5).")
    pdf.bullet("I2C timeout set to 3000 ms (clock stretching for PN532 RF operations).")
    pdf.bullet("Firmware upload via DFU protocol (USB).")

    # 3.2 PN532
    pdf.section("3.2 PN532 NFC/RFID Module", level=3)
    pdf.table(
        ["Parameter", "Value"],
        [
            ["Chipset", "NXP PN532"],
            ["NFC Standards", "ISO/IEC 14443A/B, FeliCa, ISO 18092"],
            ["Frequency", "13.56 MHz"],
            ["Card support", "MIFARE Classic 1K/4K, Ultralight, NTAG21x"],
            ["Interfaces", "I2C, SPI, UART (DIP switch selectable)"],
            ["Supply voltage", "3.3V \u2013 5.5V"],
            ["Operating current", "~100 mA (RF scanning)"],
            ["Read range", "~5 cm"],
            ["Module", "Breakout board with PCB antenna"],
            ["I2C address", "0x24"],
            ["Library", "Adafruit PN532 v1.3.4"],
        ],
        col_widths=[45, 129]
    )
    pdf.para("Notes:")
    pdf.bullet("Operating mode: I2C polling (no IRQ pin \u2014 parameter -1 in constructor).")
    pdf.bullet("setPassiveActivationRetries(1) \u2014 1 retry = 2 RF read attempts.")
    pdf.bullet("Before Wire.begin(), firmware performs I2C bus recovery (18 clock pulses + STOP).")

    # 3.3 Relay
    pdf.section("3.3 HW-482 Relay Module (1-channel)", level=3)
    pdf.table(
        ["Parameter", "Value"],
        [
            ["Channels", "1"],
            ["Control voltage", "5V"],
            ["Control input", "Active LOW (LOW = energized)"],
            ["Contact rating", "AC 250V/10A, DC 30V/10A"],
            ["Isolation", "Opto-isolation (optocoupler)"],
            ["Power supply", "5V DC (from ESP32 VUSB pin)"],
            ["Indicator", "On-board LED"],
            ["Dimensions", "~50 x 26 x 18 mm"],
        ],
        col_widths=[45, 129]
    )
    pdf.para("Control logic:")
    pdf.bullet("digitalWrite(RELAY_PIN, LOW)  -> relay open (lock unlocked)")
    pdf.bullet("digitalWrite(RELAY_PIN, HIGH) -> relay closed (lock locked) \u2014 default state")

    # 3.4 RGB LED
    pdf.section("3.4 RGB LED (Common Cathode)", level=3)
    pdf.table(
        ["Parameter", "Value"],
        [
            ["Type", "5mm RGB LED, common cathode"],
            ["Colors", "Red, Green, Blue"],
            ["Forward voltage", "R: ~2.0V, G: ~3.0V, B: ~3.0V"],
            ["Operating current", "20 mA per color"],
            ["Control", "Digital (HIGH/LOW), no PWM"],
        ],
        col_widths=[45, 129]
    )

    pdf.para("Color signaling:")
    pdf.table(
        ["Color", "Meaning"],
        [
            ["Blue", "Ready \u2014 normal mode, NFC available"],
            ["Orange (R+G)", "NFC unavailable"],
            ["Green", "Access granted"],
            ["Red", "Access denied"],
            ["Yellow (R+G)", "Waiting for MQTT provisioning"],
            ["Purple (R+B)", "Enrollment mode (blinking)"],
            ["White (R+G+B)", "Factory reset confirmed"],
        ],
        col_widths=[45, 129]
    )

    # 3.5 Buzzer
    pdf.section("3.5 TMB12A05 Active Buzzer", level=3)
    pdf.table(
        ["Parameter", "Value"],
        [
            ["Type", "Active buzzer (built-in oscillator)"],
            ["Operating voltage", "5V DC"],
            ["Sound frequency", "~2300 Hz (fixed)"],
            ["Current", "~30 mA"],
            ["Diameter", "12 mm"],
            ["Control", "Simple HIGH/LOW"],
        ],
        col_widths=[45, 129]
    )
    pdf.para("Sound patterns:")
    pdf.table(
        ["Pattern", "Meaning"],
        [
            ["1x short beep (80 ms)", "OK / confirmation"],
            ["2x beep (80+80 ms)", "Access granted / lock opened"],
            ["3x fast beep (60 ms)", "Error / access denied"],
        ],
        col_widths=[58, 116]
    )

    # 3.6 Button
    pdf.section("3.6 Momentary Push Button (Tact Switch)", level=3)
    pdf.table(
        ["Parameter", "Value"],
        [
            ["Type", "Momentary push button (NO)"],
            ["Pull-up", "Internal INPUT_PULLUP (ESP32)"],
            ["Logic", "Active LOW (pressed = LOW)"],
            ["Function", "Factory reset (3s hold at startup)"],
        ],
        col_widths=[45, 129]
    )

    # ════════════════════════════════════════════════════════
    # 4. Buses and Communication Protocols
    # ════════════════════════════════════════════════════════
    pdf.section("4. Buses and Communication Protocols")

    # 4.1 I2C
    pdf.section("4.1 I2C (Inter-Integrated Circuit)", level=3)
    pdf.table(
        ["Parameter", "Value"],
        [
            ["Purpose", "ESP32 <-> PN532 communication"],
            ["Lines", "SDA (A4/GPIO11), SCL (A5/GPIO12)"],
            ["Logic voltage", "3.3V"],
            ["Mode", "Master (ESP32) \u2014 Slave (PN532)"],
            ["Slave address", "0x24 (PN532)"],
            ["Timeout", "3000 ms (clock stretching)"],
            ["Speed", "Standard Mode (100 kHz)"],
            ["Pull-ups", "On PN532 module (built-in)"],
        ],
        col_widths=[45, 129]
    )
    pdf.para("Bus Recovery:")
    pdf.para("Firmware implements an I2C bus recovery procedure at startup \u2014 18 clock pulses + STOP signal \u2014 to release the SDA line if the PN532 holds it low after an unexpected reset.")

    # 4.2 WiFi
    pdf.section("4.2 WiFi (IEEE 802.11)", level=3)
    pdf.table(
        ["Parameter", "Value"],
        [
            ["Standard", "802.11 b/g/n"],
            ["Band", "2.4 GHz"],
            ["Mode", "Station (STA)"],
            ["Provisioning", "Captive Portal (WiFiManager)"],
            ["AP timeout", "180 seconds"],
            ["AP name", "AccessControl-XXYY (last 2 B of MAC)"],
            ["Hostname", "ac-XXXXXXXX (8 chars of HWID)"],
        ],
        col_widths=[45, 129]
    )
    pdf.para("WiFi provisioning flow:")
    pdf.numbered_item(1, "On first boot (or after factory reset) the ESP32 creates an Access Point.")
    pdf.numbered_item(2, "User connects to the AP and configures WiFi via captive portal.")
    pdf.numbered_item(3, "WiFi credentials are stored in NVS (Flash) \u2014 survive restarts.")

    # 4.3 MQTT
    pdf.section("4.3 MQTT (Message Queuing Telemetry Transport)", level=3)
    pdf.table(
        ["Parameter", "Value"],
        [
            ["Purpose", "Device <-> backend server communication"],
            ["Broker", "Mosquitto (Docker) or any MQTT 3.1.1 broker"],
            ["Port", "Configurable (default: 1883)"],
            ["Subscription QoS", "1 (at least once)"],
            ["Keep-alive", "60 seconds"],
            ["LWT", "accesscontrol/{hwid}/heartbeat -> offline"],
            ["Message buffer", "512 bytes"],
            ["Library", "PubSubClient v2.8"],
            ["Reconnect", "Exponential backoff: 2s -> ... -> 30s"],
        ],
        col_widths=[45, 129]
    )

    pdf.para("MQTT topic namespace:")
    pdf.code_block([
        "accesscontrol/{hwid}/",
        "|-- announce          (PUB, retain)  device announcement",
        "|-- heartbeat         (PUB, retain)  status + uptime, RSSI, heap",
        "|-- card/",
        "|   |-- scanned       (PUB)          card scanned (normal mode)",
        "|   |-- enrolled      (PUB)          card scanned (enrollment)",
        "|   |-- enroll        (SUB)          start/cancel enrollment",
        "|   +-- result        (SUB)          result: granted / denied",
        "|-- config/",
        "|   |-- set           (SUB)          new configuration",
        "|   +-- ack           (PUB)          acknowledgment",
        "+-- lock/",
        "    |-- command       (SUB)          open / close",
        "    +-- status        (PUB)          lock state",
    ])

    pdf.para("MQTT provisioning:")
    pdf.bullet("Via HTTP POST to http://<ip>/api/provision with JSON {broker, port, username, password}")
    pdf.bullet("Via serial command: mqtt_set <ip> [port]")
    pdf.bullet("Stored in EEPROM (address 96\u2013227)")

    # 4.4 mDNS
    pdf.section("4.4 mDNS (Multicast DNS)", level=3)
    pdf.table(
        ["Parameter", "Value"],
        [
            ["Purpose", "Automatic device discovery on LAN"],
            ["Service type", "_accesscontrol._tcp"],
            ["Port", "80"],
            ["Hostname", "ac-XXXXXXXX.local"],
        ],
        col_widths=[45, 129]
    )
    pdf.para("TXT records:")
    pdf.table(
        ["Key", "Value", "Description"],
        [
            ["hwid", "Device UUID", "Unique identifier (UUID v5 from MAC)"],
            ["model", "NanoESP32-CardReader", "Device model"],
            ["mac", "XX:XX:XX:XX:XX:XX", "MAC address"],
            ["features", "17", "Feature bitmask (reader + lock)"],
            ["fw", "2.0.0", "Firmware version"],
        ],
        col_widths=[30, 50, 94]
    )

    # 4.5 HTTP
    pdf.section("4.5 HTTP (Provisioning Server)", level=3)
    pdf.table(
        ["Parameter", "Value"],
        [
            ["Port", "80"],
            ["Endpoint", "POST /api/provision"],
            ["Content-Type", "application/json"],
            ["Active", "Only when MQTT is not configured"],
            ["Library", "WebServer (ESP32 built-in)"],
        ],
        col_widths=[45, 129]
    )

    # 4.6 UART
    pdf.section("4.6 UART (Serial)", level=3)
    pdf.table(
        ["Parameter", "Value"],
        [
            ["Purpose", "Debug / service commands"],
            ["Baud rate", "115200 baud"],
            ["Commands", "help, mqtt_set <ip> [port], mqtt_reset"],
        ],
        col_widths=[45, 129]
    )

    # ════════════════════════════════════════════════════════
    # 5. EEPROM Map
    # ════════════════════════════════════════════════════════
    pdf.section("5. EEPROM Memory Map")
    pdf.table(
        ["Address", "Size", "Magic Byte", "Data"],
        [
            ["0", "1 B", "0xAC", "Device ID magic"],
            ["1\u201336", "36 B", "\u2014", "HWID (UUID, ASCII string)"],
            ["64", "1 B", "0xD0", "Config magic"],
            ["65\u201378", "14 B", "\u2014", "Configuration (heartbeat, timeout, buzzer, LED, lock)"],
            ["96", "1 B", "0x4D", "MQTT config magic"],
            ["97\u2013160", "64 B", "\u2014", "Broker address (string)"],
            ["161\u2013162", "2 B", "\u2014", "Port (uint16, little-endian)"],
            ["163\u2013194", "32 B", "\u2014", "Username (string)"],
            ["195\u2013226", "32 B", "\u2014", "Password (string)"],
        ],
        col_widths=[25, 20, 28, 101]
    )
    pdf.para("Total EEPROM size: 256 bytes")

    # ════════════════════════════════════════════════════════
    # 6. Power
    # ════════════════════════════════════════════════════════
    pdf.section("6. Power Requirements")
    pdf.table(
        ["Source", "Voltage", "Consumers"],
        [
            ["USB-C", "5V", "ESP32, Buzzer (TMB12A05), Relay (HW-482)"],
            ["ESP32 regulator", "3.3V", "PN532, RGB LED, ESP32 logic"],
        ],
        col_widths=[40, 30, 104]
    )

    pdf.para("Estimated current consumption:")
    pdf.table(
        ["State", "Current (estimated)"],
        [
            ["Idle (WiFi + MQTT)", "~80 mA"],
            ["NFC scanning", "~180 mA"],
            ["Relay active", "+70 mA"],
            ["Buzzer active", "+30 mA"],
            ["Max (all at once)", "~360 mA"],
        ],
        col_widths=[87, 87]
    )

    # ════════════════════════════════════════════════════════
    # 7. Libraries
    # ════════════════════════════════════════════════════════
    pdf.section("7. Dependencies \u2014 Firmware Libraries")
    pdf.table(
        ["Library", "Version", "Purpose"],
        [
            ["Adafruit PN532", "^1.3.4", "NFC reader communication (I2C)"],
            ["PubSubClient", "^2.8", "MQTT client"],
            ["WiFiManager", "^2.0.17", "WiFi configuration (Captive Portal)"],
            ["ArduinoJson", "^7.4.1", "JSON parsing / building"],
            ["Wire (built-in)", "\u2014", "I2C bus"],
            ["WiFi (built-in)", "\u2014", "ESP32 WiFi support"],
            ["ESPmDNS (built-in)", "\u2014", "mDNS service broadcasting"],
            ["WebServer (built-in)", "\u2014", "HTTP server for provisioning"],
            ["EEPROM (built-in)", "\u2014", "Configuration storage in Flash"],
        ],
        col_widths=[45, 22, 107]
    )
    pdf.para("Platform: PlatformIO, espressif32, board arduino_nano_esp32, framework Arduino.")

    # ── Save ────────────────────────────────────────────────
    pdf.output(str(PDF_FILE_EN))
    return PDF_FILE_EN


def verify_pdf(pdf_path: Path) -> dict:
    doc = pymupdf.open(str(pdf_path))
    results = {
        "file_size_kb": round(pdf_path.stat().st_size / 1024, 1),
        "page_count": len(doc),
        "pages": [],
        "issues": [],
    }

    full_text = ""
    for i, page in enumerate(doc):
        text = page.get_text()
        chars = len(text.strip())
        full_text += text
        results["pages"].append({"page": i + 1, "chars": chars, "ok": chars > 30})
        if chars < 30:
            results["issues"].append(f"Page {i+1}: too little content ({chars} chars)")

    results["total_chars"] = len(full_text)

    for keyword in ["BOM", "PN532", "HW-482", "I2C", "WiFi", "MQTT", "mDNS", "EEPROM"]:
        if keyword not in full_text:
            results["issues"].append(f"Missing: '{keyword}'")

    doc.close()
    return results


if __name__ == "__main__":
    for label, build_fn in [("PL", build_pdf), ("EN", build_pdf_en)]:
        print(f"\nGenerating {label} PDF with fpdf2...")
        path = build_fn()
        print(f"Saved: {path}")

        print(f"\n{'='*50}")
        print(f"VERIFICATION ({label})")
        print(f"{'='*50}")
        r = verify_pdf(path)
        print(f"Size:  {r['file_size_kb']} KB")
        print(f"Pages: {r['page_count']}")
        print(f"Text:  {r['total_chars']} chars")
        print()
        for p in r["pages"]:
            s = "OK" if p["ok"] else "WARN"
            print(f"  Page {p['page']:2d}: {p['chars']:5d} chars [{s}]")
        print()
        if r["issues"]:
            print(f"ISSUES ({len(r['issues'])}):")
            for iss in r["issues"]:
                print(f"  - {iss}")
        else:
            print("All checks passed!")
