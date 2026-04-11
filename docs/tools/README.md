# docs/tools

Helper scripts for generating project documentation PDFs.

## Prerequisites

```bash
pip install -r docs/tools/requirements.txt
```

## Usage

```bash
python docs/tools/generate_pdf.py
```

Generates two files in `docs/`:

| File | Language |
|------|----------|
| `elektronika.pdf` | Polish |
| `electronics.pdf` | English |

Both PDFs contain the full electronics specification: BOM, pinout with resistor values, component descriptions, communication protocols, EEPROM map, power requirements, and firmware dependencies.
