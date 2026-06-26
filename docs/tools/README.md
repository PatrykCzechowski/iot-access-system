# docs/tools

Helper scripts for generating project documentation PDFs.

## Prerequisites

```bash
pip install -r docs/tools/requirements.txt
```

## Usage

### Electronics specification

```bash
python docs/tools/generate_pdf.py
```

Generates two files in `docs/`:

| File | Language |
|------|----------|
| `elektronika.pdf` | Polish |
| `electronics.pdf` | English |

Both PDFs contain the full electronics specification: BOM, pinout with resistor values, component descriptions, communication protocols, EEPROM map, power requirements, and firmware dependencies.

### Project documentation + presentation

```bash
python docs/tools/generate_project_docs.py
```

Generates two files in `docs/`:

| File | Content |
|------|---------|
| `dokumentacja-projektu.pdf` | Documentation: technical + service + user (Polish) |
| `prezentacja-projektu.pdf` | Project presentation, slide deck (Polish, landscape) |

The documentation covers software architecture, the data model, the access-control flow,
hardware/protocols, REST API, diagnostics, provisioning, factory reset, troubleshooting,
firmware updates, and an end-user guide for the web panel and the reader.

Fonts are resolved automatically for macOS / Windows / Linux (Polish glyphs required).
