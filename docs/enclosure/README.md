# Obudowa IoT Access Control — ESP8266 + PN532 NFC

Parametryczny model obudowy w OpenSCAD dla modułów Wemos D1 Mini (ESP8266) i PN532 NFC/RFID V3.

## Wymagania

- [OpenSCAD](https://openscad.org/downloads.html) (>= 2021.01)
- Po instalacji dodaj do PATH: `C:\Program Files\OpenSCAD`

## Struktura plików

```
docs/enclosure/
├── enclosure.scad          # Główny model parametryczny
├── build.ps1               # Skrypt budowania STL
└── README.md               # Ten plik
```

## Szybki start

### 1. Podgląd GUI
```powershell
openscad .\enclosure.scad
```

### 2. Eksport STL z terminala
```powershell
# Dolna część obudowy
openscad -o enclosure_bottom.stl -D "part=""bottom""" enclosure.scad

# Górna pokrywa (odwrócona do druku)
openscad -o enclosure_top.stl -D "part=""top""" enclosure.scad

# Złożony widok (do podglądu)
openscad -o enclosure_assembled.stl -D "part=""assembled""" enclosure.scad
```

### 3. Automatyczny build
```powershell
.\build.ps1           # Buduje bottom + top STL
.\build.ps1 -Preview  # Otwiera podgląd w OpenSCAD
```

## Parametry do dostrojenia

| Parametr | Domyślnie | Opis |
|----------|-----------|------|
| `tol` | 0.3 mm | Tolerancja druku 3D |
| `wall` | 2.0 mm | Grubość ścianki |
| `nfc_wall` | 1.5 mm | Grubość nad anteną NFC (max 2mm!) |
| `corner_r` | 3.0 mm | Promień zaokrąglenia |

Zmiana parametru z terminala:
```powershell
openscad -o custom.stl -D "wall=2.5" -D "nfc_wall=1.2" -D "part=""bottom""" enclosure.scad
```

## Wskazówki druku 3D

- **Materiał:** PLA/PETG (NFC przechodzi przez oba)
- **Warstwa:** 0.2 mm (standardowa jakość)
- **Wypełnienie:** 20-30%
- **Podpory:** Nie wymagane (obie części drukowane płaską stroną)
- **Orientacja:** Dolna część — dnem do dołu, górna — już odwrócona w STL
