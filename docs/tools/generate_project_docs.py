"""
Generator dokumentacji projektu AccessControl.

Tworzy dwa pliki PDF w katalogu docs/:
  * dokumentacja-projektu.pdf  -- dokumentacja techniczna + serwisowa + uzytkownika
  * prezentacja-projektu.pdf   -- prezentacja projektu (slajdy, orientacja pozioma)

Uzywa fpdf2 (pelna kontrola layoutu, bez LaTeX-a). Czcionki rozpoznawane
sa automatycznie dla macOS / Windows / Linux (potrzebne glify polskie).

Uruchomienie:
    pip install -r docs/tools/requirements.txt
    python docs/tools/generate_project_docs.py
"""

from pathlib import Path

from fpdf import FPDF, XPos, YPos
from fpdf.enums import MethodReturnValue

try:
    import pymupdf  # weryfikacja wygenerowanych PDF (opcjonalna)
except ImportError:  # pragma: no cover
    pymupdf = None

DOCS_DIR = Path(__file__).resolve().parent.parent
PDF_DOC = DOCS_DIR / "dokumentacja-projektu.pdf"
PDF_PRES = DOCS_DIR / "prezentacja-projektu.pdf"

# ── Paleta kolorow (grafit + bursztyn, bez niebieskiego) ────
C_PRIMARY = (43, 47, 54)        # grafit — nagłówki, pasy
C_SECONDARY = (62, 67, 75)      # jaśniejszy grafit
C_ACCENT = (188, 104, 24)       # bursztyn ciemny — podsekcje, kod
C_AMBER = (224, 138, 42)        # bursztyn — główny akcent
C_TH_BG = (43, 47, 54)          # nagłówek tabeli — grafit
C_TH_FG = (255, 255, 255)
C_ROW_ALT = (249, 245, 239)     # ciepły wiersz parzysty
C_ROW_NORM = (255, 255, 255)
C_TEXT = (47, 44, 40)           # ciepła czerń
C_MUTED = (130, 124, 116)       # ciepła szarość
C_CODE_BG = (246, 244, 240)
C_BORDER = (222, 216, 206)      # ciepła ramka
C_QUOTE_BG = (253, 245, 233)    # krem (bursztynowy tint)
C_OK = (39, 132, 73)
C_WARN = (196, 96, 22)
C_SLIDE_BG = (247, 249, 252)

# ── Paleta prezentacji (bez akcentów niebieskich) ───────────
P_INK = (43, 47, 54)        # grafit — pasy nagłówka, tekst mocny
P_INK2 = (62, 67, 75)       # jaśniejszy grafit — nagłówki kart
P_AMBER = (224, 138, 42)    # bursztyn — główny akcent
P_AMBER_DK = (188, 104, 24) # ciemniejszy bursztyn
P_BG = (250, 248, 244)      # ciepła biel — tło slajdu
P_CARD = (255, 255, 255)
P_CARD_BORDER = (224, 217, 206)
P_TEXT = (47, 44, 40)       # ciepła czerń — tekst
P_MUTED = (130, 124, 116)   # ciepła szarość

# ── Rozpoznawanie czcionek (Polish glyphs required) ─────────
_MAC = "/System/Library/Fonts/Supplemental"
_WIN = "C:/Windows/Fonts"
_FONT_CANDIDATES = {
    ("Body", ""): [f"{_MAC}/Arial.ttf", f"{_WIN}/arial.ttf",
                   "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"],
    ("Body", "B"): [f"{_MAC}/Arial Bold.ttf", f"{_WIN}/arialbd.ttf",
                    "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"],
    ("Body", "I"): [f"{_MAC}/Arial Italic.ttf", f"{_WIN}/ariali.ttf",
                    "/usr/share/fonts/truetype/dejavu/DejaVuSans-Oblique.ttf"],
    ("Body", "BI"): [f"{_MAC}/Arial Bold Italic.ttf", f"{_WIN}/arialbi.ttf",
                     "/usr/share/fonts/truetype/dejavu/DejaVuSans-BoldOblique.ttf"],
    ("Mono", ""): [f"{_MAC}/Courier New.ttf", f"{_WIN}/cour.ttf",
                   "/usr/share/fonts/truetype/dejavu/DejaVuSansMono.ttf"],
    ("Mono", "B"): [f"{_MAC}/Courier New Bold.ttf", f"{_WIN}/courbd.ttf",
                    "/usr/share/fonts/truetype/dejavu/DejaVuSansMono-Bold.ttf"],
}

BODY = "Body"
MONO = "Mono"


def _resolve(paths):
    for p in paths:
        if Path(p).exists():
            return p
    return None


def register_fonts(pdf: FPDF):
    for (family, style), candidates in _FONT_CANDIDATES.items():
        path = _resolve(candidates)
        if path is None:
            raise FileNotFoundError(
                f"Brak czcionki dla {family}/{style!r}. Sprawdzone: {candidates}")
        pdf.add_font(family, style, path)


# ════════════════════════════════════════════════════════════
#  Klasa bazowa z blokami budujacymi (wspolna dla obu PDF)
# ════════════════════════════════════════════════════════════
class BasePDF(FPDF):
    def content_width(self):
        return self.w - self.l_margin - self.r_margin

    def bottom_limit(self):
        return self.h - self.b_margin

    # ── Tabela z automatycznym zawijaniem tekstu ────────────
    def auto_table(self, headers, rows, col_widths,
                   font_size=8.6, header_size=8.8, aligns=None):
        """Tabela: wysokosc wiersza dopasowana do najdluzszej komorki."""
        line_h = font_size * 0.46 + 1.4
        n = len(headers)
        if aligns is None:
            aligns = ["L"] * n
        x0 = self.l_margin

        def draw_header():
            self.set_font(BODY, "B", header_size)
            self.set_fill_color(*C_TH_BG)
            self.set_text_color(*C_TH_FG)
            self.set_draw_color(*C_BORDER)
            y = self.get_y()
            x = x0
            h = line_h + 2.2
            for i, head in enumerate(headers):
                self.set_xy(x, y)
                self.multi_cell(col_widths[i], h, head, border=1, fill=True,
                                align="C", new_x=XPos.RIGHT, new_y=YPos.TOP,
                                max_line_height=line_h)
                x += col_widths[i]
            self.set_xy(x0, y + h)

        draw_header()
        self.set_font(BODY, "", font_size)

        for r_idx, row in enumerate(rows):
            # zmierz liczbe linii
            max_lines = 1
            for i, cell in enumerate(row):
                lines = self.multi_cell(col_widths[i], line_h, str(cell),
                                        dry_run=True, output=MethodReturnValue.LINES)
                max_lines = max(max_lines, len(lines))
            row_h = max_lines * line_h + 2.0

            if self.get_y() + row_h > self.bottom_limit():
                self.add_page()
                draw_header()
                self.set_font(BODY, "", font_size)

            y = self.get_y()
            bg = C_ROW_ALT if r_idx % 2 else C_ROW_NORM
            x = x0
            for i, cell in enumerate(row):
                self.set_fill_color(*bg)
                self.set_draw_color(*C_BORDER)
                self.rect(x, y, col_widths[i], row_h, style="DF")
                self.set_xy(x, y + 1.0)
                self.set_text_color(*C_TEXT)
                self.multi_cell(col_widths[i], line_h, str(cell), border=0,
                                align=aligns[i], new_x=XPos.LEFT, new_y=YPos.TOP,
                                max_line_height=line_h)
                x += col_widths[i]
            self.set_xy(x0, y + row_h)
        self.ln(3)


# ════════════════════════════════════════════════════════════
#  Dokumentacja (A4 pionowa)
# ════════════════════════════════════════════════════════════
class DocPDF(BasePDF):
    def __init__(self):
        super().__init__(orientation="P", unit="mm", format="A4")
        self.set_auto_page_break(auto=True, margin=20)
        self.set_margins(18, 18, 18)
        register_fonts(self)
        self._part_label = ""

    def header(self):
        if self.page_no() == 1:
            return
        self.set_font(BODY, "I", 8)
        self.set_text_color(*C_MUTED)
        self.cell(0, 6, "AccessControl — Dokumentacja Projektu",
                  new_x=XPos.LEFT, new_y=YPos.NEXT, align="L")
        self.set_draw_color(*C_BORDER)
        self.line(18, self.get_y(), 192, self.get_y())
        self.ln(3)

    def footer(self):
        self.set_y(-15)
        self.set_font(BODY, "I", 8)
        self.set_text_color(*C_MUTED)
        self.cell(0, 10, f"Strona {self.page_no()}/{{nb}}",
                  new_x=XPos.RIGHT, new_y=YPos.TOP, align="C")

    # ── Strona tytulowa ─────────────────────────────────────
    def title_page(self):
        self.add_page()
        self.set_fill_color(*C_PRIMARY)
        self.rect(0, 0, self.w, 88, style="F")
        self.set_fill_color(*C_AMBER)
        self.rect(0, 88, self.w, 2.2, style="F")

        self.set_xy(0, 30)
        self.set_font(BODY, "B", 30)
        self.set_text_color(255, 255, 255)
        self.cell(0, 14, "AccessControl", new_x=XPos.LEFT, new_y=YPos.NEXT, align="C")
        self.set_font(BODY, "", 15)
        self.set_text_color(200, 214, 234)
        self.cell(0, 9, "System Kontroli Dostępu IoT",
                  new_x=XPos.LEFT, new_y=YPos.NEXT, align="C")
        self.ln(2)
        self.set_font(BODY, "", 11)
        self.cell(0, 7, "Dokumentacja techniczna · serwisowa · użytkownika",
                  new_x=XPos.LEFT, new_y=YPos.NEXT, align="C")

        self.set_xy(0, 120)
        self.set_font(BODY, "", 11)
        self.set_text_color(*C_TEXT)
        for line in [
            "Czytnik kart NFC + sterownik zamka (Arduino Nano ESP32)",
            "Backend .NET 10 · Blazor WebAssembly · MQTT · PostgreSQL",
            "",
            "Wersja firmware: 2.0.0",
            "Model urządzenia: NanoESP32-CardReader",
            "Data wydania: 13.06.2026",
        ]:
            self.cell(0, 7, line, new_x=XPos.LEFT, new_y=YPos.NEXT, align="C")

        self.set_xy(0, 250)
        self.set_font(BODY, "", 10)
        self.set_text_color(*C_MUTED)
        self.cell(0, 6, "Autor: Patryk Czechowski",
                  new_x=XPos.LEFT, new_y=YPos.NEXT, align="C")

    # ── Strona-przerywnik czesci dokumentu ──────────────────
    def part_page(self, number, title, summary):
        self.add_page()
        self._part_label = title
        self.ln(70)
        self.set_font(BODY, "B", 13)
        self.set_text_color(*C_AMBER)
        self.cell(0, 9, f"CZĘŚĆ {number}", new_x=XPos.LEFT, new_y=YPos.NEXT, align="C")
        self.set_font(BODY, "B", 26)
        self.set_text_color(*C_PRIMARY)
        self.cell(0, 14, title, new_x=XPos.LEFT, new_y=YPos.NEXT, align="C")
        self.set_draw_color(*C_PRIMARY)
        self.set_line_width(0.8)
        self.line(70, self.get_y() + 2, 140, self.get_y() + 2)
        self.set_line_width(0.2)
        self.ln(12)
        self.set_font(BODY, "", 11)
        self.set_text_color(*C_MUTED)
        self.multi_cell(0, 6.5, summary, align="C")

    # ── Bloki tresci ────────────────────────────────────────
    def h1(self, text):
        if self.get_y() > 235:
            self.add_page()
        self.ln(5)
        self.set_font(BODY, "B", 15)
        self.set_text_color(*C_PRIMARY)
        self.cell(0, 9, text, new_x=XPos.LEFT, new_y=YPos.NEXT)
        self.set_draw_color(*C_PRIMARY)
        self.set_line_width(0.5)
        self.line(18, self.get_y() + 1, 192, self.get_y() + 1)
        self.set_line_width(0.2)
        self.ln(5)

    def h2(self, text):
        if self.get_y() > 252:
            self.add_page()
        self.ln(3)
        self.set_font(BODY, "B", 12)
        self.set_text_color(*C_ACCENT)
        self.cell(0, 8, text, new_x=XPos.LEFT, new_y=YPos.NEXT)
        self.ln(1.5)

    def para(self, text):
        self.set_font(BODY, "", 10)
        self.set_text_color(*C_TEXT)
        self._rich(text, 5.5)
        self.ln(5.5)

    def _rich(self, text, lh):
        # obsluga **pogrubienia** i `kodu`
        import re
        parts = re.split(r'(\*\*[^*]+\*\*|`[^`]+`)', text)
        for part in parts:
            if part.startswith("**") and part.endswith("**"):
                self.set_font(BODY, "B", 10)
                self.write(lh, part[2:-2])
                self.set_font(BODY, "", 10)
            elif part.startswith("`") and part.endswith("`"):
                self.set_font(MONO, "", 9.2)
                self.set_text_color(*C_ACCENT)
                self.write(lh, part[1:-1])
                self.set_font(BODY, "", 10)
                self.set_text_color(*C_TEXT)
            else:
                self.write(lh, part)

    @staticmethod
    def _strip(text):
        import re
        text = re.sub(r'\*\*([^*]+)\*\*', r'\1', text)
        return re.sub(r'`([^`]+)`', r'\1', text)

    def _marked_item(self, marker, marker_w, text, indent=0):
        # rysuje znacznik + zawijany tekst, kursor schodzi pod ostatnia linie
        self.set_font(BODY, "", 10)
        self.set_text_color(*C_TEXT)
        y0 = self.get_y()
        self.set_xy(18 + indent, y0)
        self.cell(marker_w, 5.5, marker, new_x=XPos.RIGHT, new_y=YPos.TOP)
        start_x = self.get_x()
        avail = 192 - start_x
        self.set_xy(start_x, y0)
        self.multi_cell(avail, 5.5, self._strip(text),
                        new_x=XPos.LMARGIN, new_y=YPos.NEXT)
        self.ln(1.4)

    def bullet(self, text, indent=0):
        self._marked_item("•", 5, text, indent)

    def numbered(self, num, text):
        self._marked_item(f"{num}.", 7, text)

    def quote(self, text):
        self.ln(1)
        self.set_font(BODY, "I", 9.5)
        lines = self.multi_cell(166, 5, text, dry_run=True,
                                output=MethodReturnValue.LINES)
        h = len(lines) * 5 + 4
        if self.get_y() + h > self.bottom_limit():
            self.add_page()
        y = self.get_y()
        self.set_fill_color(*C_QUOTE_BG)
        self.rect(20, y, 172, h, style="F")
        self.set_fill_color(*C_AMBER)
        self.rect(20, y, 1.6, h, style="F")
        self.set_xy(25, y + 2)
        self.set_text_color(*C_MUTED)
        self.multi_cell(162, 5, text, new_x=XPos.LEFT, new_y=YPos.TOP)
        self.set_y(y + h + 2)

    def code_block(self, lines):
        self.ln(1)
        self.set_font(MONO, "", 8.4)
        line_h = 4.4
        block_h = len(lines) * line_h + 6
        if self.get_y() + block_h > self.bottom_limit():
            self.add_page()
        y = self.get_y()
        self.set_fill_color(*C_CODE_BG)
        self.set_draw_color(*C_BORDER)
        self.rect(18, y, 174, block_h, style="DF")
        self.set_xy(22, y + 3)
        self.set_text_color(60, 64, 70)
        for ln in lines:
            self.cell(0, line_h, ln, new_x=XPos.LEFT, new_y=YPos.NEXT)
            self.set_x(22)
        self.set_y(y + block_h + 3)


# ════════════════════════════════════════════════════════════
#  Budowa dokumentacji
# ════════════════════════════════════════════════════════════
def build_documentation():
    pdf = DocPDF()
    pdf.alias_nb_pages()
    pdf.title_page()

    # ── Spis tresci ─────────────────────────────────────────
    pdf.add_page()
    pdf.h1("Spis treści")
    toc = [
        ("CZĘŚĆ I — DOKUMENTACJA TECHNICZNA", True),
        ("1. Przegląd systemu i przeznaczenie", False),
        ("2. Architektura oprogramowania", False),
        ("3. Model danych i encje domenowe", False),
        ("4. Logika kontroli dostępu", False),
        ("5. Specyfikacja sprzętowa (BOM, pinout)", False),
        ("6. Protokoły komunikacyjne", False),
        ("7. Parametry konfiguracyjne urządzenia", False),
        ("8. API REST", False),
        ("CZĘŚĆ II — DOKUMENTACJA SERWISOWA", True),
        ("9. Sygnalizacja i diagnostyka urządzenia", False),
        ("10. Instalacja i provisioning", False),
        ("11. Komendy serwisowe (Serial)", False),
        ("12. Factory reset", False),
        ("13. Rozwiązywanie problemów", False),
        ("14. Konserwacja sprzętu", False),
        ("15. Aktualizacja firmware", False),
        ("CZĘŚĆ III — DOKUMENTACJA UŻYTKOWNIKA", True),
        ("16. Pierwsze logowanie", False),
        ("17. Panel główny i nawigacja", False),
        ("18. Konfiguracja systemu krok po kroku", False),
        ("19. Rejestrowanie kart (enrollment)", False),
        ("20. Codzienne użytkowanie czytnika", False),
        ("21. Dziennik zdarzeń dostępu", False),
        ("22. Dobre praktyki i bezpieczeństwo", False),
    ]
    for text, is_part in toc:
        if is_part:
            pdf.ln(2)
            pdf.set_font(BODY, "B", 10.5)
            pdf.set_text_color(*C_PRIMARY)
        else:
            pdf.set_font(BODY, "", 10)
            pdf.set_text_color(*C_TEXT)
            pdf.set_x(22)
        pdf.cell(0, 6.5, text, new_x=XPos.LEFT, new_y=YPos.NEXT)

    # ════════════════════════════════════════════════════════
    #  CZESC I — TECHNICZNA
    # ════════════════════════════════════════════════════════
    pdf.part_page(
        "I", "Dokumentacja Techniczna",
        "Architektura systemu, model danych, logika kontroli dostępu, "
        "specyfikacja sprzętowa, protokoły komunikacyjne oraz interfejs API. "
        "Adresat: programiści, integratorzy i architekci systemu.")

    pdf.add_page()
    pdf.h1("1. Przegląd systemu i przeznaczenie")
    pdf.para(
        "**AccessControl** to kompletny system kontroli dostępu klasy IoT. "
        "Steruje fizycznym dostępem do pomieszczeń za pomocą bezstykowych kart "
        "NFC/RFID (13,56 MHz) odczytywanych przez urządzenia oparte na mikrokontrolerze "
        "ESP32-S3. Decyzje o przyznaniu dostępu podejmuje centralny serwer, który "
        "weryfikuje uprawnienia karty i zdalnie otwiera zamek elektryczny.")
    pdf.para("System składa się z czterech współpracujących elementów:")
    pdf.bullet("**Urządzenie brzegowe (firmware)** — czytnik kart ESP32 z czujnikiem PN532 "
               "i przekaźnikiem sterującym zamkiem.")
    pdf.bullet("**Backend (.NET 10)** — logika biznesowa, REST API, obsługa MQTT, baza danych.")
    pdf.bullet("**Aplikacja webowa (Blazor WebAssembly)** — panel administracyjny do zarządzania.")
    pdf.bullet("**Broker MQTT (Mosquitto)** — magistrala wiadomości między urządzeniami a serwerem.")
    pdf.para(
        "Komunikacja urządzenie–serwer odbywa się asynchronicznie przez MQTT, a "
        "urządzenia są automatycznie wykrywane w sieci lokalnej przez mDNS. Całość "
        "uruchamiana jest jednym poleceniem `docker compose up`.")

    pdf.h2("Kluczowe funkcje")
    pdf.bullet("Bezstykowa autoryzacja kartą NFC z weryfikacją uprawnień po stronie serwera.")
    pdf.bullet("Stref dostępu, profile uprawnień i przypisania kart do osób.")
    pdf.bullet("Zdalne otwieranie zamków i sterowanie urządzeniami w danej strefie.")
    pdf.bullet("Pełny dziennik zdarzeń (audyt) każdej próby dostępu — nawet odrzuconej.")
    pdf.bullet("Automatyczne wykrywanie i zdalne provisionowanie nowych urządzeń.")
    pdf.bullet("Rejestracja kart w trybie enrollment sterowanym z panelu.")
    pdf.bullet("Podgląd zdarzeń na żywo (SignalR) oraz status online/offline urządzeń.")

    pdf.h1("2. Architektura oprogramowania")
    pdf.para(
        "Backend zbudowany jest w oparciu o **Clean Architecture** oraz wzorzec **CQRS** "
        "z biblioteką MediatR. Zależności skierowane są do wewnątrz — warstwy "
        "zewnętrzne znają wewnętrzne, nigdy odwrotnie.")
    pdf.code_block([
        "Domain        encje, value objects, wyjatki domenowe",
        "   ^",
        "Application   komendy, zapytania, DTO, walidatory, interfejsy",
        "   ^",
        "Infrastructure  EF Core, Identity, MQTT, wykrywanie urzadzen",
        "   ^",
        "Api           minimal API endpoints, globalny handler bledow",
    ])
    pdf.para(
        "Każda akcja użytkownika przechodzi ścieżką `Endpoint -> IRequest -> Handler`. "
        "Komendy zmieniają stan, zapytania tylko odczytują. Walidacja FluentValidation "
        "uruchamia się w pipeline przed handlerem, a `GlobalExceptionHandler` mapuje "
        "wyjątki domenowe na odpowiedzi RFC 7807 (ProblemDetails).")

    pdf.h2("Stos technologiczny")
    pdf.auto_table(
        ["Warstwa", "Technologia", "Zastosowanie"],
        [
            ["Backend", ".NET 10, Minimal API, MediatR", "Logika biznesowa i REST API"],
            ["Walidacja", "FluentValidation", "Walidacja komend w pipeline"],
            ["Baza danych", "PostgreSQL + EF Core (snake_case)", "Trwałe przechowywanie danych"],
            ["Tożsamość", "ASP.NET Core Identity + JWT", "Uwierzytelnianie i role"],
            ["Komunikacja", "MQTT (Mosquitto), MQTTnet", "Wiadomości urządzenie–serwer"],
            ["Czas rzeczywisty", "SignalR", "Zdarzenia dostępu na żywo w UI"],
            ["Wykrywanie", "mDNS (_accesscontrol._tcp)", "Automatyczne odkrywanie urządzeń"],
            ["Frontend", "Blazor WebAssembly + MudBlazor 9", "Panel administracyjny (Material 3)"],
            ["HTTP klient", "Flurl.Http + Bearer token", "Komunikacja UI z API"],
            ["Firmware", "Arduino / PlatformIO (ESP32-S3)", "Oprogramowanie czytnika kart"],
            ["Konteneryzacja", "Docker Compose", "Uruchomienie całego stacku"],
            ["Testy", "xUnit, FluentAssertions, NSubstitute", "Testy jednostkowe"],
        ],
        [28, 62, 84])

    pdf.h1("3. Model danych i encje domenowe")
    pdf.para("System operuje na następujących encjach domenowych:")
    pdf.auto_table(
        ["Encja", "Opis"],
        [
            ["Cardholder", "Posiadacz karty (osoba). Może mieć przypisany profil dostępu."],
            ["AccessCard", "Karta NFC o unikalnym UID (normalizowany do wielkich liter), "
                           "aktywna/nieaktywna, opcjonalnie przypisana do posiadacza."],
            ["AccessProfile", "Profil uprawnień — grupuje strefy, do których ma "
                              "dostęp przypisany posiadacz."],
            ["AccessZone", "Strefa fizyczna (np. wejście główne, serwerownia)."],
            ["AccessProfileZone", "Relacja wiele-do-wielu łącząca profile ze strefami."],
            ["Device", "Urządzenie (czytnik/zamek) przypisane do strefy, z typem "
                       "adaptera, maską funkcji i konfiguracją."],
            ["AccessLog", "Wpis audytu: kto, gdzie, kiedy i z jakim wynikiem próbował "
                          "uzyskać dostęp."],
        ],
        [38, 136])
    pdf.h2("Powiązania")
    pdf.para(
        "Ścieżka uprawnień: **Karta -> Posiadacz -> Profil dostępu -> Strefy**. "
        "Urządzenie należy do jednej **Strefy**. Karta ma dostęp do urządzenia tylko "
        "wtedy, gdy strefa urządzenia znajduje się wśród stref jej profilu.")
    pdf.code_block([
        "Cardholder ---* AccessCard",
        "Cardholder ---- AccessProfile ---* AccessProfileZone *--- AccessZone",
        "AccessZone ---* Device",
        "(skan karty) --> AccessLog",
    ])

    pdf.h1("4. Logika kontroli dostępu")
    pdf.para(
        "Gdy urządzenie odczyta kartę, publikuje jej UID przez MQTT. Serwis "
        "`CardAccessService` realizuje następujący przepływ:")
    pdf.numbered(1, "Odnalezienie urządzenia po jego sprzętowym ID (HardwareId). Karta z "
                    "nieznanego urządzenia jest odrzucana.")
    pdf.numbered(2, "Normalizacja UID karty (trim + wielkie litery).")
    pdf.numbered(3, "Sprawdzenie, czy karta ma dostęp do strefy urządzenia "
                    "(`HasAccessToZoneAsync`) — przez łańcuch profil → strefy.")
    pdf.numbered(4, "Zapis wpisu do dziennika dostępu (audyt) — zawsze, niezależnie od wyniku.")
    pdf.numbered(5, "Powiadomienie panelu na żywo przez SignalR.")
    pdf.numbered(6, "Odesłanie wyniku (granted/denied) do urządzenia skanującego.")
    pdf.numbered(7, "Jeśli dostęp przyznany — wysłanie komendy `open` do wszystkich "
                    "urządzeń z funkcją LockControl, które są online w danej strefie.")
    pdf.quote("Zasada odporności: wpis do dziennika zapisywany jest jako pierwszy, "
              "zanim nastąpi publikacja MQTT — dzięki temu każda próba dostępu jest "
              "rejestrowana nawet jeśli późniejsze kroki zawiodą.")
    pdf.para("Czas otwarcia zamka pobierany jest z konfiguracji urządzenia "
             "(`lockOpenDurationSec`, domyślnie 5 s).")

    pdf.h1("5. Specyfikacja sprzętowa")
    pdf.para("Urządzenie buduje się na płytce **Arduino Nano ESP32 (ESP32-S3)**. "
             "Pełny opis komponentów i parametrów elektrycznych znajduje się w osobnym "
             "dokumencie `docs/elektronika.pdf`.")
    pdf.h2("5.1 Lista elementów (BOM)")
    pdf.auto_table(
        ["#", "Element", "Ozn.", " Il.", "Opis"],
        [
            ["1", "Arduino Nano ESP32", "U1", "1", "Mikrokontroler ESP32-S3, USB-C"],
            ["2", "PN532 NFC/RFID", "U2", "1", "Czytnik NFC 13,56 MHz, I2C"],
            ["3", "HW-482 Relay", "K1", "1", "Przekaźnik 1-kanałowy, active LOW, 5 V"],
            ["4", "RGB LED (wsp. katoda)", "LED1", "1", "Sygnalizacja stanu, 5 mm"],
            ["5", "Buzzer TMB12A05", "BZ1", "1", "Buzzer aktywny 5 V, 12 mm"],
            ["6", "Przycisk tact", "SW1", "1", "Factory reset, NO"],
            ["7", "Rezystory", "R1–R3", "3", "Ograniczenie prądu LED"],
            ["8", "Zamek elektryczny", "—", "1", "Sterowany przez przekaźnik (NO/COM)"],
        ],
        [8, 44, 18, 12, 92])
    pdf.h2("5.2 Połączenia (pinout)")
    pdf.auto_table(
        ["Komponent", "Pin", "ESP32", "GPIO", "Uwagi"],
        [
            ["PN532", "SDA", "A4", "GPIO11", "I2C SDA, pull-up 4,7 kΩ"],
            ["PN532", "SCL", "A5", "GPIO12", "I2C SCL, pull-up 4,7 kΩ"],
            ["RGB LED", "R / G / B", "D2 / D3 / D4", "GPIO5/6/7", "Rezystory szeregowe"],
            ["Buzzer", "+", "D5", "GPIO8", "Driver NPN dla 5 V"],
            ["HW-482", "IN1", "D6", "GPIO9", "Active LOW, optoizolacja"],
            ["Przycisk", "—", "D7", "GPIO10", "INPUT_PULLUP, reset 3 s"],
        ],
        [28, 26, 34, 28, 58])
    pdf.quote("Domyślne piny I2C Nano ESP32 to GPIO21/22 — firmware ręcznie "
              "inicjalizuje Wire.begin(A4, A5). Adres I2C modułu PN532: 0x24.")

    pdf.h1("6. Protokoły komunikacyjne")
    pdf.h2("6.1 MQTT")
    pdf.para("Główny kanał komunikacji urządzenie–serwer. Broker: Mosquitto "
             "(MQTT 3.1.1), domyślny port 1883, QoS 1, keep-alive 60 s. Przestrzeń "
             "nazw topików:")
    pdf.code_block([
        "accesscontrol/{hwid}/",
        "|-- announce      (PUB, retain)  ogloszenie urzadzenia",
        "|-- heartbeat     (PUB, retain)  status + uptime, RSSI, heap",
        "|-- card/scanned  (PUB)          karta odczytana (tryb normalny)",
        "|-- card/enrolled (PUB)          karta odczytana (enrollment)",
        "|-- card/enroll   (SUB)          start / anulowanie enrollment",
        "|-- card/result   (SUB)          wynik: granted / denied",
        "|-- config/set    (SUB)          nowa konfiguracja urzadzenia",
        "|-- config/ack    (PUB)          potwierdzenie konfiguracji",
        "|-- lock/command  (SUB)          open / close zamka",
        "+-- lock/status   (PUB)          stan zamka",
    ])
    pdf.para("Routing wiadomości po stronie serwera realizują handlery implementujące "
             "`IMqttMessageHandler` (dopasowanie tematu przez regex + `HandleAsync`).")
    pdf.h2("6.2 Pozostałe protokoły")
    pdf.auto_table(
        ["Protokół", "Rola w systemie"],
        [
            ["mDNS", "Automatyczne wykrywanie urządzeń w LAN; usługa "
                     "_accesscontrol._tcp na porcie 80; rekordy TXT: hwid, model, mac, "
                     "features, fw."],
            ["WiFi 802.11 b/g/n", "Łączność urządzenia (2,4 GHz, tryb STA). "
                                  "Konfiguracja przez captive portal (WiFiManager)."],
            ["HTTP (port 80)", "Serwer provisioningu na urządzeniu — POST "
                               "/api/provision z danymi brokera MQTT (aktywny dopóki MQTT "
                               "nie jest skonfigurowany)."],
            ["I2C (100 kHz)", "Magistrala ESP32 <-> PN532; bus recovery przy starcie."],
            ["UART (115200)", "Konsola serwisowa / debug urządzenia."],
            ["HTTPS + JWT", "Klient UI <-> REST API; token Bearer w nagłówku."],
        ],
        [34, 140])

    pdf.h1("7. Parametry konfiguracyjne urządzenia")
    pdf.para("Konfiguracja urządzenia jest walidowana w domenie (`Device.UpdateConfiguration`) "
             "i wysyłana do urządzenia przez topic `config/set`. Dozwolone klucze i zakresy:")
    pdf.auto_table(
        ["Klucz", "Typ", "Zakres", "Znaczenie"],
        [
            ["lockOpenDurationSec", "int", "1–60", "Czas otwarcia zamka (s)"],
            ["heartbeatIntervalSec", "int", "5–300", "Częstotliwość heartbeat (s)"],
            ["enrollmentTimeoutSec", "int", "1–120", "Limit czasu trybu enrollment (s)"],
            ["buzzerEnabled", "bool", "true/false", "Włączenie sygnalizacji dźwiękowej"],
            ["ledBrightness", "int", "0–255", "Jasność diody LED"],
        ],
        [44, 16, 26, 88])
    pdf.quote("Nieznany klucz lub wartość poza zakresem powoduje wyjątek "
              "DomainValidationException i odrzucenie całej zmiany konfiguracji.")

    pdf.h1("8. API REST")
    pdf.para("Endpointy pogrupowane są wg funkcji (`MapGroup`). Wszystkie operacje na "
             "kartach i urządzeniach wymagają roli **Admin**. Endpointy logowania "
             "objęte są limitem 10 żądań/min.")
    pdf.auto_table(
        ["Grupa", "Wybrane endpointy", "Opis"],
        [
            ["/api/auth", "POST /login, /change-password", "Logowanie, wydanie JWT"],
            ["/api/zones", "GET, POST, PUT, DELETE", "Zarządzanie strefami"],
            ["/api/access-profiles", "GET, POST, PUT, DELETE", "Profile uprawnień"],
            ["/api/cardholders", "GET, POST, PUT, DELETE", "Posiadacze kart"],
            ["/api/cards", "GET, POST, PUT, DELETE", "Karty dostępu"],
            ["/api/devices", "GET, POST, PUT, DELETE", "Urządzenia (CRUD)"],
            ["/api/devices", "POST /scan, /discovered", "Skan i lista wykrytych urządzeń"],
            ["/api/devices/{id}", "POST /provision", "Wysłanie konfiguracji MQTT"],
            ["/api/devices/{id}", "POST /enrollment/start|cancel", "Tryb rejestracji kart"],
            ["/api/devices/{id}", "PUT /config", "Aktualizacja konfiguracji urządzenia"],
            ["/api/access-logs", "GET", "Dziennik zdarzeń dostępu"],
            ["/health", "GET", "Status usługi i bazy danych"],
        ],
        [40, 70, 64])
    pdf.quote("Interaktywna dokumentacja API (Scalar UI) dostępna w trybie deweloperskim "
              "pod /scalar/v1.")

    # ════════════════════════════════════════════════════════
    #  CZESC II — SERWISOWA
    # ════════════════════════════════════════════════════════
    pdf.part_page(
        "II", "Dokumentacja Serwisowa",
        "Diagnostyka, instalacja, provisioning, komendy serwisowe, factory reset, "
        "rozwiązywanie problemów, konserwacja oraz aktualizacja firmware. "
        "Adresat: technicy instalujący i serwisujący urządzenia.")

    pdf.add_page()
    pdf.h1("9. Sygnalizacja i diagnostyka urządzenia")
    pdf.para("Urządzenie sygnalizuje swój stan diodą RGB oraz buzzerem. To podstawowe "
             "narzędzie diagnostyczne podczas instalacji i serwisu.")
    pdf.h2("9.1 Kody kolorów LED")
    pdf.auto_table(
        ["Kolor", "Znaczenie"],
        [
            ["Niebieski", "Gotowy — tryb normalny, NFC dostępne"],
            ["Pomarańczowy", "NFC niedostępne (sprawdź połączenie PN532)"],
            ["Zielony", "Dostęp przyznany"],
            ["Czerwony", "Dostęp odmówiony"],
            ["Żółty", "Oczekiwanie na provisioning MQTT"],
            ["Fioletowy (miga)", "Tryb enrollment (rejestracja karty)"],
            ["Biały", "Factory reset potwierdzony"],
        ],
        [44, 130])
    pdf.h2("9.2 Wzorce dźwiękowe")
    pdf.auto_table(
        ["Wzorzec", "Znaczenie"],
        [
            ["1× krótki beep (80 ms)", "OK / potwierdzenie"],
            ["2× beep", "Dostęp przyznany / zamek otwarty"],
            ["3× szybki beep (60 ms)", "Błąd / dostęp odmówiony"],
        ],
        [56, 118])
    pdf.h2("9.3 Heartbeat i status online")
    pdf.para("Urządzenie cyklicznie publikuje `heartbeat` (status, uptime, RSSI, wolny "
             "heap). Brak heartbeat powoduje oznaczenie urządzenia jako **offline** w "
             "panelu. LWT (Last Will) ustawia status `offline` przy nagłej utracie "
             "połączenia. Status online/offline widoczny jest na liście urządzeń.")

    pdf.h1("10. Instalacja i provisioning")
    pdf.h2("10.1 Montaż i okablowanie")
    pdf.numbered(1, "Połącz komponenty zgodnie z tabelą pinout (rozdz. 5.2). Zwróć uwagę "
                    "na rezystory LED i driver NPN buzzera.")
    pdf.numbered(2, "Zamek podłącz do styków przekaźnika: COM + zasilanie zamka, NO + zamek. "
                    "NO = fail-secure (zamknięty bez zasilania), NC = fail-safe (otwarty "
                    "bez zasilania — wymagane przy drogach ewakuacyjnych).")
    pdf.numbered(3, "Ustaw przełączniki DIP modułu PN532: SW1=ON, SW2=OFF (tryb I2C).")
    pdf.numbered(4, "Zasil urządzenie przez USB-C.")
    pdf.h2("10.2 Provisioning WiFi")
    pdf.numbered(1, "Przy pierwszym uruchomieniu urządzenie tworzy punkt dostępowy "
                    "`AccessControl-XXYY`.")
    pdf.numbered(2, "Połącz się z tym AP — otworzy się captive portal.")
    pdf.numbered(3, "Wybierz sieć WiFi i podaj hasło. Dane zapisywane są w pamięci "
                    "nieulotnej (NVS) i przetrwają restart.")
    pdf.h2("10.3 Provisioning MQTT (z panelu)")
    pdf.numbered(1, "Uruchom serwer w sieci LAN (nie w Dockerze — mDNS wymaga multicastu).")
    pdf.numbered(2, "W panelu otwórz **Devices -> Scan for Devices**.")
    pdf.numbered(3, "Dodaj wykryte urządzenie (**Add**) — backend automatycznie wyśle "
                    "poświadczenia MQTT przez HTTP POST na `http://<ip>/api/provision`.")
    pdf.numbered(4, "Urządzenie zapisuje konfigurację, restartuje się, łączy z brokerem "
                    "i rozpoczyna skanowanie kart (LED niebieski).")
    pdf.quote("Bezpieczeństwo: poświadczenia MQTT wysyłane są po HTTP (ESP32 nie "
              "obsługuje TLS). Akceptowalne w zaufanej sieci LAN — w produkcji użyj "
              "poświadczeń MQTT o ograniczonym zakresie (per-device).")

    pdf.h1("11. Komendy serwisowe (Serial)")
    pdf.para("Konsola serwisowa dostępna przez UART, 115200 baud "
             "(`pio device monitor --baud 115200`).")
    pdf.auto_table(
        ["Komenda", "Działanie"],
        [
            ["help", "Lista dostępnych komend"],
            ["mqtt_set <ip> [port]", "Ręczne ustawienie adresu brokera MQTT"],
            ["mqtt_reset", "Wyczyszczenie konfiguracji MQTT → powrót do trybu "
                           "provisioningu po restarcie"],
        ],
        [52, 122])
    pdf.h2("Ponowny provisioning MQTT")
    pdf.bullet("Z panelu: strona urządzenia → **Push MQTT Config**.")
    pdf.bullet("Z konsoli: `mqtt_reset` → restart → urządzenie wraca do trybu provisioningu.")

    pdf.h1("12. Factory reset")
    pdf.para("Reset fabryczny przywraca urządzenie do stanu początkowego (z wyjątkiem "
             "tożsamości sprzętowej).")
    pdf.numbered(1, "Naciśnij i przytrzymaj przycisk reset (D7 ↔ GND).")
    pdf.numbered(2, "Trzymając przycisk, włącz zasilanie (lub naciśnij RESET płytki).")
    pdf.numbered(3, "Przytrzymaj **3 sekundy** — LED miga na czerwono (odliczanie).")
    pdf.numbered(4, "Sukces: 3× beep + biały błysk LED → urządzenie restartuje się.")
    pdf.numbered(5, "Urządzenie wchodzi w tryb provisioningu WiFi (`AccessControl-XXYY`).")
    pdf.para("Zwolnienie przycisku przed upływem 3 s anuluje reset (normalny rozruch).")
    pdf.h2("Co jest czyszczone")
    pdf.auto_table(
        ["Dane", "Czyszczone?"],
        [
            ["Poświadczenia WiFi", "Tak"],
            ["Konfiguracja brokera MQTT", "Tak"],
            ["Konfiguracja urządzenia (timery, buzzer, LED)", "Tak"],
            ["Hardware ID (HWID)", "Nie — urządzenie zachowuje tożsamość w systemie"],
        ],
        [110, 64])

    pdf.h1("13. Rozwiązywanie problemów")
    pdf.auto_table(
        ["Objaw", "Prawdopodobna przyczyna", "Rozwiązanie"],
        [
            ["Urządzenie nie pojawia się w skanie",
             "Serwer w Dockerze / inny segment sieci",
             "Uruchom serwer w tej samej sieci LAN co ESP32 (mDNS = multicast)"],
            ["LED żółty, nie łączy z MQTT",
             "Brak / błędna konfiguracja brokera",
             "Wykonaj provisioning z panelu lub `mqtt_set <ip>`"],
            ["LED pomarańczowy (nfc:false)",
             "Zawieszona magistrala I2C po DFU",
             "Wykonaj power-cycle płytki — firmware odzyskuje magistralę przy starcie"],
            ["Flash zakończony, brak restartu",
             "Tryb DFU Nano ESP32",
             "Naciśnij RESET (lub dwukrotnie) aby uruchomić nowy firmware"],
            ["Karta odczytana, brak otwarcia zamka",
             "Brak urządzenia LockControl online w strefie",
             "Sprawdź status urządzenia z zamkiem i jego funkcję LockControl"],
            ["Wszystkie karty odrzucane",
             "Brak profilu / strefy przypisanej do karty",
             "Przypisz posiadaczowi profil obejmujący strefę urządzenia"],
            ["Status ciągle offline",
             "Brak heartbeat (WiFi/MQTT)",
             "Sprawdź zasięg WiFi (RSSI w heartbeat) i dostępność brokera"],
        ],
        [44, 60, 70], font_size=8.2, header_size=8.4)

    pdf.h1("14. Konserwacja sprzętu")
    pdf.bullet("Okresowo sprawdzaj pewność połączeń Dupont — wibracje mogą je poluźniać.")
    pdf.bullet("Czyść obszar anteny PN532 z kurzu; nie zaklęjaj go metalem (zaburza pole RF).")
    pdf.bullet("Sprawdź temperaturę modułu przekaźnika przy częstym przełączaniu.")
    pdf.bullet("Obudowa: druk 3D z PLA/PETG (NFC przechodzi przez oba); ścianka nad "
               "anteną max 2 mm.")
    pdf.bullet("Upewnij się, że zasilacz USB 5 V dostarcza min. ~400 mA (szczyt poboru ~360 mA).")
    pdf.bullet("Przy instalacji zewnętrznej zadbaj o ochronę przed wilgocią.")

    pdf.h1("15. Aktualizacja firmware")
    pdf.para("Firmware budowany i wgrywany jest przez **PlatformIO** (board "
             "`arduino_nano_esp32`, framework Arduino).")
    pdf.code_block([
        "cd firmware/card-reader-standalone",
        "pio run                 # kompilacja",
        "pio run -t upload       # wgranie (DFU, port auto-detect)",
        "pio device monitor --baud 115200   # podglad logow",
    ])
    pdf.para("Po wgraniu — jeśli urządzenie nie restartuje się samo — naciśnij RESET. "
             "HWID jest zachowywany, więc urządzenie pozostaje rozpoznawalne w systemie. "
             "Konfiguracja WiFi/MQTT przetrwa aktualizację (zapis w NVS/EEPROM).")

    # ════════════════════════════════════════════════════════
    #  CZESC III — UZYTKOWNIKA
    # ════════════════════════════════════════════════════════
    pdf.part_page(
        "III", "Dokumentacja Użytkownika",
        "Instrukcja obsługi panelu administracyjnego oraz codziennego korzystania "
        "z czytnika. Adresat: administratorzy systemu i użytkownicy końcowi.")

    pdf.add_page()
    pdf.h1("16. Pierwsze logowanie")
    pdf.numbered(1, "Otwórz aplikację webową w przeglądarce (adres podaje administrator).")
    pdf.numbered(2, "Na ekranie **Login** podaj e-mail i hasło konta administratora.")
    pdf.numbered(3, "Przy pierwszym logowaniu system wymusi zmianę hasła "
                    "(`MustChangePassword`). Ustaw nowe, bezpieczne hasło.")
    pdf.quote("Domyślne konto deweloperskie: admin@accesscontrol.local / Admin123! "
              "— zmień je niezwłocznie po pierwszym uruchomieniu.")

    pdf.h1("17. Panel główny i nawigacja")
    pdf.para("Po zalogowaniu dostępne są następujące sekcje (menu boczne):")
    pdf.auto_table(
        ["Sekcja", "Przeznaczenie"],
        [
            ["Dashboard", "Podsumowanie: liczba kart, urządzeń, ostatnie zdarzenia"],
            ["Zones", "Strefy fizyczne (np. wejście, magazyn)"],
            ["Access Profiles", "Profile uprawnień łączące strefy"],
            ["Cardholders", "Posiadacze kart (osoby) i ich profile"],
            ["Cards", "Karty NFC, ich status i przypisania"],
            ["Devices", "Czytniki/zamki, status online, konfiguracja"],
            ["Access Logs", "Historia wszystkich prób dostępu"],
            ["Settings", "Ustawienia konta i systemu"],
        ],
        [44, 130])

    pdf.h1("18. Konfiguracja systemu krok po kroku")
    pdf.para("Zalecana kolejność wstępnej konfiguracji nowego systemu:")
    pdf.numbered(1, "**Utwórz strefy** (Zones) — np. \"Wejście główne\", \"Serwerownia\".")
    pdf.numbered(2, "**Utwórz profile dostępu** (Access Profiles) i przypisz do nich strefy "
                    "— np. profil \"Pracownik\" = wejście główne; \"Administrator IT\" = "
                    "wejście + serwerownia.")
    pdf.numbered(3, "**Dodaj posiadaczy kart** (Cardholders) i przypisz im profil.")
    pdf.numbered(4, "**Podłącz urządzenia** (Devices): zeskanuj, dodaj, przypisz do strefy.")
    pdf.numbered(5, "**Zarejestruj karty** (enrollment) i przypisz je do posiadaczy.")
    pdf.numbered(6, "Sprawdź działanie przykładową kartą i zweryfikuj wpis w Access Logs.")

    pdf.h1("19. Rejestrowanie kart (enrollment)")
    pdf.para("Tryb enrollment pozwala odczytać UID nowej karty bezpośrednio z czytnika, "
             "bez ręcznego przepisywania numeru.")
    pdf.numbered(1, "Wejdź na stronę urządzenia (Devices) i uruchom **Start Enrollment**.")
    pdf.numbered(2, "Czytnik przechodzi w tryb enrollment (LED fioletowy, miga).")
    pdf.numbered(3, "Przyłóż nową kartę do czytnika — jej UID pojawi się w panelu.")
    pdf.numbered(4, "Zapisz kartę, nadaj etykietę i przypisz do posiadacza.")
    pdf.numbered(5, "Tryb wygasa po `enrollmentTimeoutSec` lub po anulowaniu (**Cancel**).")
    pdf.quote("UID karty jest normalizowany do wielkich liter. Jedna karta nie może być "
              "zarejestrowana dwukrotnie — system odrzuci duplikat.")

    pdf.h1("20. Codzienne użytkowanie czytnika")
    pdf.para("Z perspektywy użytkownika końcowego obsługa jest maksymalnie prosta:")
    pdf.numbered(1, "Przyłóż kartę do czytnika (zasięg ~5 cm).")
    pdf.numbered(2, "**Dostęp przyznany**: LED zielony + 2× beep, zamek otwiera się na "
                    "skonfigurowany czas (domyślnie 5 s).")
    pdf.numbered(3, "**Dostęp odmówiony**: LED czerwony + 3× szybki beep, zamek pozostaje "
                    "zamknięty.")
    pdf.para("Stan spoczynkowy czytnika to niebieski LED (gotowy). Każda próba — udana "
             "czy nie — trafia do dziennika zdarzeń.")

    pdf.h1("21. Dziennik zdarzeń dostępu")
    pdf.para("Sekcja **Access Logs** to pełny audyt systemu. Każdy wpis zawiera:")
    pdf.bullet("Znacznik czasu zdarzenia.")
    pdf.bullet("UID karty oraz nazwę/etykietę posiadacza (jeśli przypisany).")
    pdf.bullet("Nazwę urządzenia i strefę, w której nastąpił odczyt.")
    pdf.bullet("Wynik (przyznano / odmówiono) oraz komunikat.")
    pdf.para("Nowe zdarzenia pojawiają się na żywo (SignalR) bez odświeżania strony. "
             "Dziennik służy do analizy incydentów i weryfikacji uprawnień.")

    pdf.h1("22. Dobre praktyki i bezpieczeństwo")
    pdf.bullet("Zmień domyślne hasło administratora przy pierwszym uruchomieniu.")
    pdf.bullet("Dezaktywuj (zamiast usuwać) karty zgubione — zachowasz historię w audycie.")
    pdf.bullet("Stosuj zasadę minimalnych uprawnień: profil obejmuje tylko niezbędne strefy.")
    pdf.bullet("Nie uruchamiaj API w sieci publicznej bez firewalla — nasłuchuje na 0.0.0.0.")
    pdf.bullet("W produkcji używaj per-device poświadczeń MQTT o ograniczonym zakresie.")
    pdf.bullet("Regularnie przeglądaj Access Logs pod kątem nietypowych prób dostępu.")
    pdf.bullet("Dla dróg ewakuacyjnych stosuj zamki fail-safe (NC) zgodnie z przepisami ppoż.")

    pdf.output(str(PDF_DOC))
    return PDF_DOC


# ════════════════════════════════════════════════════════════
#  Prezentacja (A4 pozioma, slajdy)
# ════════════════════════════════════════════════════════════
class PresPDF(BasePDF):
    CONTENT_TOP = 42
    CONTENT_BOTTOM = 190

    def __init__(self):
        super().__init__(orientation="L", unit="mm", format="A4")
        self.set_auto_page_break(auto=False)
        self.set_margins(18, 16, 18)
        register_fonts(self)

    def footer(self):
        if self.page_no() == 1:
            return
        self.set_y(-11)
        self.set_font(BODY, "", 8)
        self.set_text_color(*P_MUTED)
        self.cell(0, 6, "AccessControl — System Kontroli Dostępu IoT",
                  new_x=XPos.LEFT, new_y=YPos.TOP, align="L")
        self.set_y(-11)
        self.cell(0, 6, f"{self.page_no()}", new_x=XPos.RIGHT, new_y=YPos.TOP, align="R")

    # ── Slajd tytulowy ──────────────────────────────────────
    def cover(self):
        self.add_page()
        self.set_fill_color(*P_INK)
        self.rect(0, 0, self.w, self.h, style="F")
        self.set_fill_color(*P_AMBER)
        self.rect(0, 120, self.w, 2.2, style="F")
        self.set_fill_color(*P_AMBER_DK)
        self.rect(0, 122.2, self.w, 0.8, style="F")

        self.set_xy(0, 64)
        self.set_font(BODY, "B", 46)
        self.set_text_color(255, 255, 255)
        self.cell(0, 18, "AccessControl", new_x=XPos.LEFT, new_y=YPos.NEXT, align="C")
        self.set_font(BODY, "", 20)
        self.set_text_color(214, 208, 198)
        self.cell(0, 12, "System Kontroli Dostępu IoT",
                  new_x=XPos.LEFT, new_y=YPos.NEXT, align="C")

        self.set_xy(0, 134)
        self.set_font(BODY, "", 12)
        self.set_text_color(212, 178, 132)
        self.cell(0, 7, "Czytnik kart NFC + sterownik zamka · ESP32 · .NET 10 · Blazor · MQTT",
                  new_x=XPos.LEFT, new_y=YPos.NEXT, align="C")

        self.set_xy(0, 180)
        self.set_font(BODY, "", 11)
        self.set_text_color(168, 161, 152)
        self.cell(0, 6, "Prezentacja projektu · 13.06.2026 · Patryk Czechowski",
                  new_x=XPos.LEFT, new_y=YPos.NEXT, align="C")

    # ── Naglowek slajdu ─────────────────────────────────────
    def slide(self, kicker, title):
        self.add_page()
        self.set_fill_color(*P_BG)
        self.rect(0, 0, self.w, self.h, style="F")
        self.set_fill_color(*P_INK)
        self.rect(0, 0, self.w, 30, style="F")
        self.set_fill_color(*P_AMBER)
        self.rect(0, 30, self.w, 1.8, style="F")
        self.set_xy(18, 6.5)
        self.set_font(BODY, "B", 9)
        self.set_text_color(214, 178, 122)
        self.cell(0, 5, kicker.upper(), new_x=XPos.LEFT, new_y=YPos.NEXT)
        self.set_xy(18, 12.5)
        self.set_font(BODY, "B", 20)
        self.set_text_color(255, 255, 255)
        self.cell(0, 11, title, new_x=XPos.LEFT, new_y=YPos.NEXT)

    # ── Akapit wprowadzajacy pod naglowkiem ─────────────────
    def intro(self, text, top=42):
        self.set_xy(24, top)
        self.set_font(BODY, "", 13)
        self.set_text_color(*P_TEXT)
        self.multi_cell(self.w - 48, 7, text)
        return self.get_y()

    # ── Punktory (auto-wysrodkowane w pionie) ───────────────
    def big_bullets(self, items, size=13, line_h=7.0, gap=10.0, top=None, bottom=None):
        top = self.CONTENT_TOP if top is None else top
        bottom = self.CONTENT_BOTTOM if bottom is None else bottom
        x = 28
        text_w = self.w - x - 24
        self.set_font(BODY, "", size)
        heights = [
            len(self.multi_cell(text_w, line_h, it, dry_run=True,
                                output=MethodReturnValue.LINES)) * line_h
            for it in items
        ]
        total = sum(heights) + gap * (len(items) - 1)
        y = top + max(0.0, (bottom - top - total) / 2)
        for it, h in zip(items, heights):
            self.set_fill_color(*P_AMBER)
            ms = 2.8
            cx, cy = x - 9, y + line_h * 0.5
            self.polygon([(cx, cy - ms), (cx + ms, cy), (cx, cy + ms), (cx - ms, cy)],
                         style="F")
            self.set_xy(x, y)
            self.set_font(BODY, "", size)
            self.set_text_color(*P_TEXT)
            self.multi_cell(text_w, line_h, it, new_x=XPos.LEFT, new_y=YPos.TOP)
            y += h + gap

    # ── Rzad kart (wysokosc dopasowana do tresci) ───────────
    def boxes_row(self, boxes, region_top=None, region_bottom=None,
                  reserve_top=0.0, reserve_bottom=0.0, body_size=10.0, line_h=7.0):
        region_top = self.CONTENT_TOP if region_top is None else region_top
        region_bottom = self.CONTENT_BOTTOM if region_bottom is None else region_bottom
        n = len(boxes)
        gap = 8
        total_w = self.w - self.l_margin - self.r_margin
        bw = (total_w - gap * (n - 1)) / n
        head_h = 10.0
        pad = 7.0
        maxlines = max(len(lines) for _, lines in boxes)
        height = head_h + pad + maxlines * line_h + pad
        # wysrodkowanie rzedu kart w dostepnym obszarze
        a_top = region_top + reserve_top
        a_bottom = region_bottom - reserve_bottom
        top = a_top + max(0.0, (a_bottom - a_top - height) / 2)
        x = self.l_margin
        for title, lines in boxes:
            self.set_fill_color(*P_CARD)
            self.set_draw_color(*P_CARD_BORDER)
            self.rect(x, top, bw, height, style="DF")
            self.set_fill_color(*P_AMBER)
            self.rect(x, top, bw, head_h, style="F")
            self.set_xy(x, top + 2.0)
            self.set_font(BODY, "B", 10.5)
            self.set_text_color(*P_INK)
            self.multi_cell(bw, 6, title, align="C", new_x=XPos.LEFT, new_y=YPos.TOP)
            cy = top + head_h + pad
            for ln in lines:
                self.set_xy(x + 5, cy)
                self.set_font(BODY, "", body_size)
                self.set_text_color(*P_AMBER_DK)
                self.cell(3.4, line_h, "•")
                self.set_text_color(*P_TEXT)
                self.set_xy(x + 8.6, cy)
                self.multi_cell(bw - 12, line_h, ln, new_x=XPos.LEFT, new_y=YPos.TOP)
                cy += line_h
            x += bw + gap
        return top + height

    # ── Sekwencja krokow ────────────────────────────────────
    def flow(self, steps, top=70):
        n = len(steps)
        gap = 6
        total_w = self.w - self.l_margin - self.r_margin
        bw = (total_w - gap * (n - 1)) / n
        x = self.l_margin
        h = 27
        for i, step in enumerate(steps):
            self.set_fill_color(*P_INK)
            self.set_draw_color(*P_INK)
            self.rect(x, top, bw, h, style="DF", round_corners=True, corner_radius=2)
            self.set_xy(x + 2, top + 4.5)
            self.set_font(BODY, "B", 9.4)
            self.set_text_color(255, 255, 255)
            self.multi_cell(bw - 4, 5, step, align="C", new_x=XPos.LEFT, new_y=YPos.TOP)
            if i < n - 1:
                self.set_font(BODY, "B", 15)
                self.set_text_color(*P_AMBER)
                self.set_xy(x + bw - 1, top + h / 2 - 4.5)
                self.cell(gap + 2, 9, "›", align="C")
            x += bw + gap

    # ── Pasek z uwaga ───────────────────────────────────────
    def note(self, text, top):
        w = self.w - self.l_margin - self.r_margin
        self.set_font(BODY, "I", 10)
        lines = self.multi_cell(w - 12, 5.5, text, dry_run=True,
                                output=MethodReturnValue.LINES)
        h = len(lines) * 5.5 + 5
        self.set_fill_color(253, 245, 233)
        self.set_draw_color(*P_AMBER)
        self.rect(self.l_margin, top, w, h, style="DF")
        self.set_fill_color(*P_AMBER)
        self.rect(self.l_margin, top, 2.0, h, style="F")
        self.set_xy(self.l_margin + 6, top + 2.5)
        self.set_text_color(*P_TEXT)
        self.multi_cell(w - 12, 5.5, text, new_x=XPos.LEFT, new_y=YPos.TOP)

    # ── Slajd ze zrzutami ekranu (1–2 obok siebie) ──────────
    def shots(self, items, top=44, img_ratio=1.58):
        """items: lista (sciezka_png, podpis). Skaluje, ramka, podpis."""
        items = [it for it in items if Path(it[0]).exists()]
        if not items:
            return
        n = len(items)
        gap = 12
        total_w = self.w - self.l_margin - self.r_margin
        iw = (total_w - gap * (n - 1)) / n
        ih = iw / img_ratio
        max_h = self.CONTENT_BOTTOM - top - 10  # zostaw miejsce na podpis
        if ih > max_h:
            ih = max_h
            iw = ih * img_ratio
        row_w = iw * n + gap * (n - 1)
        x = self.l_margin + (total_w - row_w) / 2
        y = top + max(0, (self.CONTENT_BOTTOM - top - (ih + 9)) / 2)
        for path, caption in items:
            self.set_draw_color(*P_CARD_BORDER)
            self.set_fill_color(*P_CARD)
            self.rect(x - 1, y - 1, iw + 2, ih + 2, style="DF")
            self.image(path, x=x, y=y, w=iw, h=ih)
            self.set_xy(x, y + ih + 2)
            self.set_font(BODY, "B", 9.5)
            self.set_text_color(*P_AMBER_DK)
            self.multi_cell(iw, 5, caption, align="C", new_x=XPos.LEFT, new_y=YPos.TOP)
            x += iw + gap


SHOTS = DOCS_DIR / "screenshots"


def build_presentation():
    pdf = PresPDF()
    pdf.cover()

    # Slajd 2 — Problem i cel
    pdf.slide("Wprowadzenie", "Problem i cel projektu")
    pdf.big_bullets([
        "Tradycyjne klucze i zamki: brak kontroli kto, gdzie i kiedy wchodzi.",
        "Brak audytu — niemożliwe odtworzenie historii dostępu.",
        "Trudne zarządzanie uprawnieniami wielu osób i wielu pomieszczeń.",
        "Cel: tani, otwarty system kontroli dostępu IoT z centralnym zarządzaniem, "
        "audytem i zdalną obsługą zamków.",
    ])

    # Slajd 3 — Czym jest
    pdf.slide("Przegląd", "Czym jest AccessControl")
    pdf.intro(
        "Kompletny system kontroli dostępu klasy IoT: bezstykowe karty NFC, "
        "centralna weryfikacja uprawnień i zdalne sterowanie zamkami elektrycznymi "
        "— zarządzany z poziomu aplikacji webowej.", top=46)
    pdf.boxes_row([
        ("Sprzęt", ["ESP32-S3", "Czytnik PN532", "Przekaźnik + zamek", "LED + buzzer"]),
        ("Backend", [".NET 10 API", "Clean Arch + CQRS", "PostgreSQL", "MQTT + SignalR"]),
        ("Aplikacja", ["Blazor WASM", "MudBlazor 9", "Zarządzanie", "Audyt na żywo"]),
        ("Sieć", ["MQTT (Mosquitto)", "mDNS discovery", "WiFi 2,4 GHz", "Docker Compose"]),
    ], region_top=70)

    # Slajd 4 — Architektura
    pdf.slide("Architektura", "Jak zbudowany jest system")
    pdf.boxes_row([
        ("Urządzenie (ESP32)", ["Odczyt karty NFC", "Sygnalizacja LED/dźwięk",
                                 "Sterowanie zamkiem", "Publikacja MQTT"]),
        ("Broker MQTT", ["Mosquitto", "Topiki accesscontrol/", "QoS 1, retain",
                         "Last Will (offline)"]),
        ("Backend .NET", ["Weryfikacja dostępu", "REST API + JWT", "EF Core / PostgreSQL",
                          "SignalR + mDNS"]),
        ("Panel Blazor", ["Zarządzanie danymi", "Konfiguracja urządzeń",
                          "Dziennik zdarzeń", "Material Design"]),
    ], reserve_bottom=34)
    pdf.note("Clean Architecture + CQRS (MediatR): Endpoint → IRequest → Handler. "
             "Zależności skierowane do wewnątrz; walidacja w pipeline; błędy jako RFC 7807.",
             top=158)

    # Slajd 5 — Jak dziala kontrola dostepu
    pdf.slide("Działanie", "Przepływ kontroli dostępu")
    pdf.intro("Od przyłożenia karty do otwarcia zamka — każdy krok jest rejestrowany:",
              top=46)
    pdf.flow(["Odczyt\nkarty NFC", "Publikacja\nMQTT", "Weryfikacja\nuprawnień",
              "Zapis do\ndziennika", "Wynik do\nczytnika", "Otwarcie\nzamka"], top=66)
    pdf.big_bullets([
        "Ścieżka uprawnień: Karta → Posiadacz → Profil → Strefy.",
        "Dostęp tylko gdy strefa urządzenia jest w profilu karty.",
        "Audyt zapisywany jako pierwszy — odporność na awarię MQTT.",
        "Otwierane wszystkie zamki online w strefie (czas wg konfiguracji).",
    ], top=106, bottom=190, size=12, gap=9)

    # Slajd 6 — Sprzet
    pdf.slide("Sprzęt", "Komponenty urządzenia")
    pdf.boxes_row([
        ("Arduino Nano ESP32", ["ESP32-S3 dual-core", "WiFi 2,4 GHz", "16 MB Flash",
                                "USB-C, 3,3 V logika"]),
        ("PN532 NFC/RFID", ["13,56 MHz", "MIFARE / NTAG", "I2C (0x24)", "Zasięg ~5 cm"]),
        ("HW-482 + zamek", ["Przekaźnik 1-kan.", "Active LOW", "AC 250V/10A",
                            "Fail-secure/safe"]),
        ("Sygnalizacja", ["RGB LED (statusy)", "Buzzer 5 V", "Przycisk reset",
                          "Pobór max ~360 mA"]),
    ], reserve_bottom=34)
    pdf.note("Pełna specyfikacja elektroniczna (BOM, pinout, mapa EEPROM, pobór prądu) "
             "znajduje się w dokumencie docs/elektronika.pdf.", top=158)

    # Slajd 7 — Komunikacja
    pdf.slide("Komunikacja", "Sieć i protokoły")
    pdf.boxes_row([
        ("MQTT", ["Magistrala urządzeń", "accesscontrol/{hwid}/", "card / lock / config",
                  "heartbeat + LWT"]),
        ("mDNS", ["Auto-wykrywanie", "_accesscontrol._tcp", "Rekordy TXT",
                  "Zero konfiguracji IP"]),
        ("WiFi", ["802.11 b/g/n", "Captive portal", "WiFiManager", "Zapis w NVS"]),
        ("Provisioning", ["Skan z panelu", "HTTP push MQTT", "Restart + połączenie",
                          "Re-provision na żądanie"]),
    ], reserve_bottom=34)
    pdf.note("Urządzenia są wykrywane automatycznie i konfigurowane zdalnie — "
             "technik nie musi ręcznie wpisywać adresów ani poświadczeń.", top=158)

    # Slajd 8 — Funkcje aplikacji
    pdf.slide("Aplikacja", "Funkcje panelu administracyjnego")
    pdf.big_bullets([
        "Strefy, profile uprawnień i posiadacze kart — elastyczny model dostępu.",
        "Rejestracja kart wprost z czytnika (tryb enrollment).",
        "Zarządzanie urządzeniami: skan, dodanie, konfiguracja, status online.",
        "Dziennik zdarzeń dostępu na żywo (SignalR) — pełny audyt.",
        "Sterowanie zamkami i parametrami urządzeń (czas otwarcia, buzzer, LED).",
    ], size=13, gap=11)

    # Slajdy ze zrzutami ekranu aplikacji
    pdf.slide("Aplikacja w działaniu", "Logowanie i pulpit")
    pdf.shots([
        (str(SHOTS / "01-login.png"), "Ekran logowania (JWT)"),
        (str(SHOTS / "02-dashboard.png"), "Pulpit administratora"),
    ])

    pdf.slide("Aplikacja w działaniu", "Karty, posiadacze i strefy")
    pdf.shots([
        (str(SHOTS / "06-cards.png"), "Karty dostępu i ich status"),
        (str(SHOTS / "05-cardholders.png"), "Posiadacze kart i profile"),
    ])

    pdf.slide("Aplikacja w działaniu", "Urządzenia i dziennik dostępu")
    pdf.shots([
        (str(SHOTS / "07-devices.png"), "Urządzenia — status online/offline"),
        (str(SHOTS / "08-access-logs.png"), "Dziennik zdarzeń — granted / denied"),
    ])

    # Slajd 9 — Bezpieczenstwo i jakosc
    pdf.slide("Jakość", "Bezpieczeństwo i niezawodność")
    pdf.boxes_row([
        ("Bezpieczeństwo", ["JWT + role (Admin)", "Wymuszona zmiana hasła",
                            "Rate limiting logowania", "Audyt każdej próby"]),
        ("Niezawodność", ["Audyt przed MQTT", "Reconnect (backoff)", "I2C bus recovery",
                          "Last Will / heartbeat"]),
        ("Jakość kodu", ["Clean Architecture", "Walidacja w pipeline", "Testy xUnit",
                         "FluentAssertions"]),
    ])

    # Slajd 10 — Podsumowanie
    pdf.slide("Podsumowanie", "Co zostało osiągnięte")
    pdf.big_bullets([
        "Działający, kompletny system kontroli dostępu: sprzęt + backend + UI.",
        "Pełen cykl: od odczytu karty po otwarcie zamka i wpis w audycie.",
        "Automatyczne wykrywanie i zdalna konfiguracja urządzeń.",
        "Czysta architektura, walidacja i testy jednostkowe.",
        "Uruchomienie jednym poleceniem — docker compose up.",
    ], top=44, bottom=152, size=13, gap=10)
    pdf.note("Dokumentacja: techniczna, serwisowa i użytkownika — "
             "dokumentacja-projektu.pdf. Dziękuję za uwagę.", top=162)

    pdf.output(str(PDF_PRES))
    return PDF_PRES


# ════════════════════════════════════════════════════════════
#  Weryfikacja
# ════════════════════════════════════════════════════════════
def verify(pdf_path: Path, keywords):
    if pymupdf is None:
        print("  (pominieto weryfikacje — brak PyMuPDF)")
        return
    doc = pymupdf.open(str(pdf_path))
    full = ""
    empty = 0
    for page in doc:
        t = page.get_text()
        full += t
        if len(t.strip()) < 15:
            empty += 1
    size_kb = round(pdf_path.stat().st_size / 1024, 1)
    print(f"  Rozmiar: {size_kb} KB | Strony: {len(doc)} | Znaki: {len(full)} | "
          f"Puste strony: {empty}")
    missing = [k for k in keywords if k not in full]
    if missing:
        print(f"  UWAGA brak fraz: {missing}")
    else:
        print("  Wszystkie kluczowe frazy obecne.")
    doc.close()


if __name__ == "__main__":
    print("Generowanie dokumentacji projektu (fpdf2)...")
    d = build_documentation()
    print(f"  Zapisano: {d}")
    verify(d, ["AccessControl", "MQTT", "Clean Architecture", "Factory reset",
               "enrollment", "Access Logs", "PN532"])

    print("\nGenerowanie prezentacji projektu...")
    p = build_presentation()
    print(f"  Zapisano: {p}")
    verify(p, ["AccessControl", "ESP32", "MQTT", "kontroli dostępu"])

    print("\nGotowe.")
