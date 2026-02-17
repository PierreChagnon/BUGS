#!/usr/bin/env python3
"""
export_tdd.py — Lit Docs/TDD.md et écrase le contenu d'un Google Doc existant
via l'API Google Docs.

Variable d'environnement requise :
    GDOC_TDD_ID : ID du document Google Docs cible

Authentification :
    - Service Account : variable GOOGLE_APPLICATION_CREDENTIALS pointant vers le JSON
    - OAuth Desktop  : fichier credentials.json dans le répertoire courant
      (un token.json sera créé automatiquement au premier lancement)

Dépendances :
    pip install google-auth google-auth-oauthlib google-api-python-client python-dotenv
"""

from __future__ import annotations

import os
import re
import sys
from pathlib import Path

from dotenv import load_dotenv

# Charger le .env depuis le répertoire du script
load_dotenv(Path(__file__).parent / ".env")

from google.oauth2 import service_account
from googleapiclient.discovery import build

# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------
SCOPES = ["https://www.googleapis.com/auth/documents"]
TDD_PATH = Path(__file__).parent / "Docs" / "TDD.md"

# ---------------------------------------------------------------------------
# Authentification Google
# ---------------------------------------------------------------------------

def get_credentials():
    """Retourne les credentials Google (Service Account ou OAuth Desktop)."""
    sa_path = os.environ.get("GOOGLE_APPLICATION_CREDENTIALS")
    if sa_path and Path(sa_path).exists():
        return service_account.Credentials.from_service_account_file(sa_path, scopes=SCOPES)

    # Fallback : OAuth Desktop
    from google.auth.transport.requests import Request
    from google.oauth2.credentials import Credentials
    from google_auth_oauthlib.flow import InstalledAppFlow

    token_path = Path(__file__).parent / "token.json"
    creds = None
    if token_path.exists():
        creds = Credentials.from_authorized_user_file(str(token_path), SCOPES)
    if not creds or not creds.valid:
        if creds and creds.expired and creds.refresh_token:
            creds.refresh(Request())
        else:
            flow = InstalledAppFlow.from_client_secrets_file("credentials.json", SCOPES)
            creds = flow.run_local_server(port=0)
        token_path.write_text(creds.to_json())
    return creds

# ---------------------------------------------------------------------------
# Markdown → liste de blocs structurés
# ---------------------------------------------------------------------------

class Block:
    """Un bloc de contenu à insérer dans le Google Doc."""
    pass

class TextRun(Block):
    """Texte avec style optionnel."""
    def __init__(self, text: str, bold=False, italic=False, code=False, link=None):
        self.text = text
        self.bold = bold
        self.italic = italic
        self.code = code
        self.link = link

class Heading(Block):
    def __init__(self, level: int, text: str):
        self.level = level
        self.text = text

class Paragraph(Block):
    """Un paragraphe composé de runs inline."""
    def __init__(self, runs: list[TextRun]):
        self.runs = runs

class CodeBlock(Block):
    def __init__(self, text: str, lang: str = ""):
        self.text = text
        self.lang = lang

class BulletItem(Block):
    def __init__(self, runs: list[TextRun], level: int = 0):
        self.runs = runs
        self.level = level

class TableBlock(Block):
    def __init__(self, headers: list[str], rows: list[list[str]], alignments: list[str] | None = None):
        self.headers = headers
        self.rows = rows
        self.alignments = alignments

class HorizontalRule(Block):
    pass

# ---------------------------------------------------------------------------
# Parser Markdown → Blocs
# ---------------------------------------------------------------------------

def parse_inline(text: str) -> list[TextRun]:
    """Parse inline markdown (bold, italic, code, links) en liste de TextRun."""
    runs: list[TextRun] = []
    # Pattern pour capturer : **bold**, *italic*, _italic_, `code`, [text](url)
    pattern = re.compile(
        r'(\*\*(.+?)\*\*)'           # bold
        r'|(\*(.+?)\*)'              # italic *
        r'|(_(.+?)_)'                # italic _
        r'|(`([^`]+)`)'             # inline code
        r'|(\[([^\]]+)\]\(([^)]+)\))'  # link
    )
    pos = 0
    for m in pattern.finditer(text):
        # Texte avant le match
        if m.start() > pos:
            plain = text[pos:m.start()]
            if plain:
                runs.append(TextRun(plain))

        if m.group(2) is not None:    # bold
            runs.append(TextRun(m.group(2), bold=True))
        elif m.group(4) is not None:  # italic *
            runs.append(TextRun(m.group(4), italic=True))
        elif m.group(6) is not None:  # italic _
            runs.append(TextRun(m.group(6), italic=True))
        elif m.group(8) is not None:  # code
            runs.append(TextRun(m.group(8), code=True))
        elif m.group(10) is not None: # link
            runs.append(TextRun(m.group(10), link=m.group(11)))

        pos = m.end()

    # Texte restant
    if pos < len(text):
        remaining = text[pos:]
        if remaining:
            runs.append(TextRun(remaining))

    if not runs:
        runs.append(TextRun(text))

    return runs


def parse_table(lines: list[str]) -> TableBlock | None:
    """Parse un bloc de lignes qui forment un tableau markdown."""
    if len(lines) < 2:
        return None

    def split_row(line: str) -> list[str]:
        line = line.strip()
        if line.startswith("|"):
            line = line[1:]
        if line.endswith("|"):
            line = line[:-1]
        return [cell.strip() for cell in line.split("|")]

    headers = split_row(lines[0])
    # Ligne 2 = séparateurs (----, :----, etc.)
    sep_cells = split_row(lines[1])
    alignments = []
    for cell in sep_cells:
        cell = cell.strip()
        if cell.startswith(":") and cell.endswith(":"):
            alignments.append("CENTER")
        elif cell.endswith(":"):
            alignments.append("END")
        else:
            alignments.append("START")

    rows = []
    for line in lines[2:]:
        rows.append(split_row(line))

    return TableBlock(headers, rows, alignments)


def markdown_to_blocks(md_text: str) -> list[Block]:
    """Convertit le texte Markdown en liste de Blocs structurés."""
    blocks: list[Block] = []
    lines = md_text.split("\n")
    i = 0

    while i < len(lines):
        line = lines[i]

        # Ligne vide → skip
        if not line.strip():
            i += 1
            continue

        # Headings
        heading_match = re.match(r'^(#{1,6})\s+(.+)$', line)
        if heading_match:
            level = len(heading_match.group(1))
            text = heading_match.group(2).strip()
            # Retirer le gras des titres (**text**)
            text = re.sub(r'\*\*(.+?)\*\*', r'\1', text)
            blocks.append(Heading(level, text))
            i += 1
            continue

        # Code block
        if line.strip().startswith("```"):
            lang = line.strip()[3:].strip()
            code_lines = []
            i += 1
            while i < len(lines) and not lines[i].strip().startswith("```"):
                code_lines.append(lines[i])
                i += 1
            i += 1  # skip closing ```
            blocks.append(CodeBlock("\n".join(code_lines), lang))
            continue

        # Horizontal rule
        if re.match(r'^---+\s*$', line.strip()):
            blocks.append(HorizontalRule())
            i += 1
            continue

        # Table
        if "|" in line and i + 1 < len(lines) and re.match(r'^[\s|:\-]+$', lines[i + 1]):
            table_lines = []
            while i < len(lines) and "|" in lines[i] and lines[i].strip():
                table_lines.append(lines[i])
                i += 1
            table = parse_table(table_lines)
            if table:
                blocks.append(table)
            continue

        # Bullet list item
        bullet_match = re.match(r'^(\s*)[-*]\s+(.+)$', line)
        if bullet_match:
            indent = len(bullet_match.group(1))
            level = indent // 2
            text = bullet_match.group(2)
            runs = parse_inline(text)
            blocks.append(BulletItem(runs, level))
            i += 1
            continue

        # Blockquote
        if line.strip().startswith(">"):
            text = line.strip().lstrip(">").strip()
            if text:
                runs = parse_inline(text)
                blocks.append(Paragraph([TextRun(run.text, run.bold, True, run.code, run.link) for run in runs]))
            i += 1
            continue

        # Ligne avec contenu inline (→ description après le >)
        if line.strip().startswith("→"):
            text = line.strip()
            runs = parse_inline(text)
            blocks.append(Paragraph(runs))
            i += 1
            continue

        # Paragraphe normal — accumuler les lignes consécutives
        para_lines = []
        while i < len(lines) and lines[i].strip() and not lines[i].strip().startswith("#") \
                and not lines[i].strip().startswith("```") and not lines[i].strip().startswith("|") \
                and not re.match(r'^---+\s*$', lines[i].strip()) \
                and not re.match(r'^\s*[-*]\s+', lines[i]):
            para_lines.append(lines[i].strip())
            i += 1

        if para_lines:
            text = " ".join(para_lines)
            runs = parse_inline(text)
            blocks.append(Paragraph(runs))

    return blocks

# ---------------------------------------------------------------------------
# Segments : découpage des blocs en texte / tableaux
# ---------------------------------------------------------------------------

def split_into_segments(blocks: list[Block]) -> list[list[Block] | TableBlock]:
    """Découpe les blocs en segments alternant texte et tableaux.

    Retourne une liste où chaque élément est :
    - une liste de blocs texte (non-table)
    - un TableBlock seul
    """
    segments: list[list[Block] | TableBlock] = []
    current_text: list[Block] = []

    for block in blocks:
        if isinstance(block, TableBlock):
            if current_text:
                segments.append(current_text)
                current_text = []
            segments.append(block)
        else:
            current_text.append(block)

    if current_text:
        segments.append(current_text)

    return segments

# ---------------------------------------------------------------------------
# Blocs texte → Requêtes Google Docs API
# ---------------------------------------------------------------------------

# Mapping heading level → Google Docs named style
HEADING_STYLES = {
    1: "HEADING_1",
    2: "HEADING_2",
    3: "HEADING_3",
    4: "HEADING_4",
    5: "HEADING_5",
    6: "HEADING_6",
}

# Couleurs
COLOR_CODE_BG = {"red": 0.95, "green": 0.95, "blue": 0.95}  # gris clair
COLOR_CODE_FG = {"red": 0.2, "green": 0.2, "blue": 0.2}
COLOR_HEADER_BG = {"red": 0.85, "green": 0.85, "blue": 0.92}  # bleu-gris clair
COLOR_LINK = {"red": 0.06, "green": 0.46, "blue": 0.88}


def build_text_requests(blocks: list[Block], insert_index: int) -> list[dict]:
    """
    Convertit les blocs texte en requêtes Google Docs API.

    Args:
        blocks: liste de blocs (ne doit pas contenir de TableBlock)
        insert_index: position dans le document où insérer le texte

    Approche en 2 phases :
      Phase 1 — Construire le texte complet + collecter les ranges de style
      Phase 2 — Générer les requêtes : 1 insertText puis les styles
    """
    # ── Phase 1 : construire le texte et collecter les styles ──
    parts: list[str] = []
    pos = 0  # position dans le texte qu'on construit

    # Styles : (start, end, type, data)
    styles: list[tuple[int, int, str, object]] = []

    for block in blocks:
        if isinstance(block, Heading):
            text = block.text + "\n"
            styles.append((pos, pos + len(text), "heading", block.level))
            parts.append(text)
            pos += len(text)

        elif isinstance(block, Paragraph):
            for run in block.runs:
                start = pos
                parts.append(run.text)
                pos += len(run.text)
                if run.bold:
                    styles.append((start, pos, "bold", None))
                if run.italic:
                    styles.append((start, pos, "italic", None))
                if run.code:
                    styles.append((start, pos, "code_inline", None))
                if run.link:
                    styles.append((start, pos, "link", run.link))
            parts.append("\n")
            pos += 1

        elif isinstance(block, BulletItem):
            start = pos
            for run in block.runs:
                run_start = pos
                parts.append(run.text)
                pos += len(run.text)
                if run.bold:
                    styles.append((run_start, pos, "bold", None))
                if run.italic:
                    styles.append((run_start, pos, "italic", None))
                if run.code:
                    styles.append((run_start, pos, "code_inline", None))
                if run.link:
                    styles.append((run_start, pos, "link", run.link))
            parts.append("\n")
            pos += 1
            styles.append((start, pos, "bullet", block.level))

        elif isinstance(block, CodeBlock):
            text = block.text.rstrip("\n") + "\n"
            if block.lang:
                text = f"[{block.lang}]\n" + text
            start = pos
            parts.append(text)
            pos += len(text)
            styles.append((start, pos, "code_block", None))
            # Ligne vide après
            parts.append("\n")
            pos += 1

        elif isinstance(block, HorizontalRule):
            rule = "────────────────────────────────────────\n"
            styles.append((pos, pos + len(rule), "hr", None))
            parts.append(rule)
            pos += len(rule)

    full_text = "".join(parts)
    if not full_text:
        return []

    # ── Phase 2 : générer les requêtes API ──
    requests: list[dict] = []

    # 1. Insérer tout le texte d'un coup à insert_index
    requests.append({
        "insertText": {"location": {"index": insert_index}, "text": full_text}
    })

    # 2. Appliquer les styles (l'offset doc = offset texte + insert_index)
    for start, end, stype, data in styles:
        ds = start + insert_index  # doc start
        de = end + insert_index    # doc end

        if stype == "heading":
            requests.append({
                "updateParagraphStyle": {
                    "range": {"startIndex": ds, "endIndex": de},
                    "paragraphStyle": {"namedStyleType": HEADING_STYLES.get(data, "HEADING_3")},
                    "fields": "namedStyleType",
                }
            })

        elif stype == "bold":
            requests.append({
                "updateTextStyle": {
                    "range": {"startIndex": ds, "endIndex": de},
                    "textStyle": {"bold": True},
                    "fields": "bold",
                }
            })

        elif stype == "italic":
            requests.append({
                "updateTextStyle": {
                    "range": {"startIndex": ds, "endIndex": de},
                    "textStyle": {"italic": True},
                    "fields": "italic",
                }
            })

        elif stype == "code_inline":
            requests.append({
                "updateTextStyle": {
                    "range": {"startIndex": ds, "endIndex": de},
                    "textStyle": {
                        "weightedFontFamily": {"fontFamily": "Roboto Mono"},
                        "fontSize": {"magnitude": 9, "unit": "PT"},
                    },
                    "fields": "weightedFontFamily,fontSize",
                }
            })

        elif stype == "link":
            requests.append({
                "updateTextStyle": {
                    "range": {"startIndex": ds, "endIndex": de},
                    "textStyle": {
                        "link": {"url": data},
                        "foregroundColor": {"color": {"rgbColor": COLOR_LINK}},
                    },
                    "fields": "link,foregroundColor",
                }
            })

        elif stype == "bullet":
            requests.append({
                "createParagraphBullets": {
                    "range": {"startIndex": ds, "endIndex": de},
                    "bulletPreset": "BULLET_DISC_CIRCLE_SQUARE",
                }
            })
            if data > 0:  # indentation sous-niveau
                requests.append({
                    "updateParagraphStyle": {
                        "range": {"startIndex": ds, "endIndex": de},
                        "paragraphStyle": {
                            "indentStart": {"magnitude": data * 36, "unit": "PT"},
                            "indentFirstLine": {"magnitude": data * 36, "unit": "PT"},
                        },
                        "fields": "indentStart,indentFirstLine",
                    }
                })

        elif stype == "code_block":
            requests.append({
                "updateTextStyle": {
                    "range": {"startIndex": ds, "endIndex": de},
                    "textStyle": {
                        "weightedFontFamily": {"fontFamily": "Roboto Mono"},
                        "fontSize": {"magnitude": 8, "unit": "PT"},
                        "foregroundColor": {"color": {"rgbColor": COLOR_CODE_FG}},
                    },
                    "fields": "weightedFontFamily,fontSize,foregroundColor",
                }
            })
            requests.append({
                "updateParagraphStyle": {
                    "range": {"startIndex": ds, "endIndex": de},
                    "paragraphStyle": {
                        "shading": {
                            "backgroundColor": {"color": {"rgbColor": COLOR_CODE_BG}},
                        },
                        "spaceAbove": {"magnitude": 6, "unit": "PT"},
                        "spaceBelow": {"magnitude": 6, "unit": "PT"},
                    },
                    "fields": "shading.backgroundColor,spaceAbove,spaceBelow",
                }
            })

        elif stype == "hr":
            requests.append({
                "updateTextStyle": {
                    "range": {"startIndex": ds, "endIndex": de},
                    "textStyle": {
                        "foregroundColor": {"color": {"rgbColor": {"red": 0.7, "green": 0.7, "blue": 0.7}}},
                        "fontSize": {"magnitude": 6, "unit": "PT"},
                    },
                    "fields": "foregroundColor,fontSize",
                }
            })

    return requests


# ---------------------------------------------------------------------------
# Tableaux natifs Google Docs
# ---------------------------------------------------------------------------

def _get_cell_text(runs: list[TextRun]) -> str:
    """Extrait le texte brut d'une liste de TextRun (sans marqueurs markdown)."""
    return "".join(run.text for run in runs)


def build_table_fill_requests(table_element: dict, table_block: TableBlock) -> tuple[list[dict], list[list[int]]]:
    """Remplit les cellules d'un tableau natif Google Docs avec le texte des cellules.

    Lit la structure du tableau depuis l'API, extrait le startIndex de chaque cellule,
    et insère le texte en ordre inverse (bottom-right → top-left) pour éviter le décalage.

    Args:
        table_element: élément table du document lu via l'API
        table_block: le TableBlock parsé du markdown

    Returns:
        (requests, cell_text_lengths) où cell_text_lengths[r][c] = longueur du texte inséré
    """
    table_data = table_element["table"]
    table_rows = table_data["tableRows"]
    all_rows = [table_block.headers] + table_block.rows
    n_rows = len(table_rows)
    n_cols = len(table_block.headers)

    # Collecter les index de début de chaque cellule et le texte à insérer
    cells_info: list[tuple[int, str]] = []  # (start_index, text)
    cell_text_lengths: list[list[int]] = []

    for r in range(n_rows):
        row_lengths: list[int] = []
        row_data = table_rows[r]["tableCells"]
        source_row = all_rows[r] if r < len(all_rows) else []

        for c in range(n_cols):
            cell_element = row_data[c] if c < len(row_data) else None
            cell_text_raw = source_row[c] if c < len(source_row) else ""

            # Parser le markdown inline pour obtenir le texte brut
            runs = parse_inline(cell_text_raw)
            plain_text = _get_cell_text(runs)

            if cell_element and plain_text:
                # Chaque cellule contient au moins un paragraphe avec un startIndex
                cell_content = cell_element.get("content", [])
                if cell_content:
                    cell_start = cell_content[0]["startIndex"]
                    cells_info.append((cell_start, plain_text))
                    row_lengths.append(len(plain_text))
                else:
                    row_lengths.append(0)
            else:
                row_lengths.append(0)

        cell_text_lengths.append(row_lengths)

    # Insérer en ordre inverse pour ne pas décaler les index
    requests: list[dict] = []
    for start_index, text in reversed(cells_info):
        requests.append({
            "insertText": {
                "location": {"index": start_index},
                "text": text,
            }
        })

    return requests, cell_text_lengths


def build_table_style_requests(
    table_element: dict,
    table_block: TableBlock,
    cell_text_lengths: list[list[int]],
) -> list[dict]:
    """Applique les styles aux cellules du tableau après insertion du texte.

    - Bold sur toute la row header (row 0)
    - Bold/italic/code inline selon parse_inline() de chaque cellule
    - Fond coloré bleu-gris sur la row header via updateTableCellStyle
    - Taille de police réduite pour tout le tableau

    Le table_element est relu après le fill, donc ses index sont déjà corrects.
    """
    table_data = table_element["table"]
    table_rows = table_data["tableRows"]
    all_rows = [table_block.headers] + table_block.rows
    n_rows = len(table_rows)
    n_cols = len(table_block.headers)

    requests: list[dict] = []

    # Obtenir le startIndex et endIndex du tableau complet pour le style de police
    table_start = table_element["startIndex"]
    table_end = table_element["endIndex"]

    # Style de police pour tout le tableau (taille réduite)
    requests.append({
        "updateTextStyle": {
            "range": {"startIndex": table_start, "endIndex": table_end},
            "textStyle": {
                "fontSize": {"magnitude": 9, "unit": "PT"},
            },
            "fields": "fontSize",
        }
    })

    for r in range(n_rows):
        row_data = table_rows[r]["tableCells"]
        source_row = all_rows[r] if r < len(all_rows) else []

        for c in range(n_cols):
            cell_text_raw = source_row[c] if c < len(source_row) else ""
            if not cell_text_raw.strip():
                continue

            cell_element = row_data[c] if c < len(row_data) else None
            if not cell_element:
                continue

            cell_content = cell_element.get("content", [])
            if not cell_content:
                continue

            cell_start = cell_content[0]["startIndex"]
            text_len = cell_text_lengths[r][c] if c < len(cell_text_lengths[r]) else 0

            if text_len == 0:
                continue

            # Bold pour la row header
            if r == 0:
                requests.append({
                    "updateTextStyle": {
                        "range": {"startIndex": cell_start, "endIndex": cell_start + text_len},
                        "textStyle": {"bold": True},
                        "fields": "bold",
                    }
                })

            # Styles inline (bold, italic, code) pour chaque cellule
            runs = parse_inline(cell_text_raw)
            run_offset = 0
            for run in runs:
                run_len = len(run.text)
                rs = cell_start + run_offset
                re_ = rs + run_len

                if run.bold and r != 0:  # row 0 est déjà bold
                    requests.append({
                        "updateTextStyle": {
                            "range": {"startIndex": rs, "endIndex": re_},
                            "textStyle": {"bold": True},
                            "fields": "bold",
                        }
                    })
                if run.italic:
                    requests.append({
                        "updateTextStyle": {
                            "range": {"startIndex": rs, "endIndex": re_},
                            "textStyle": {"italic": True},
                            "fields": "italic",
                        }
                    })
                if run.code:
                    requests.append({
                        "updateTextStyle": {
                            "range": {"startIndex": rs, "endIndex": re_},
                            "textStyle": {
                                "weightedFontFamily": {"fontFamily": "Roboto Mono"},
                                "fontSize": {"magnitude": 8, "unit": "PT"},
                            },
                            "fields": "weightedFontFamily,fontSize",
                        }
                    })
                if run.link:
                    requests.append({
                        "updateTextStyle": {
                            "range": {"startIndex": rs, "endIndex": re_},
                            "textStyle": {
                                "link": {"url": run.link},
                                "foregroundColor": {"color": {"rgbColor": COLOR_LINK}},
                            },
                            "fields": "link,foregroundColor",
                        }
                    })

                run_offset += run_len

    # Fond coloré sur la row header (row 0) via updateTableCellStyle
    table_start_index = table_element["startIndex"]
    requests.append({
        "updateTableCellStyle": {
            "tableRange": {
                "tableCellLocation": {
                    "tableStartLocation": {"index": table_start_index},
                    "rowIndex": 0,
                    "columnIndex": 0,
                },
                "rowSpan": 1,
                "columnSpan": n_cols,
            },
            "tableCellStyle": {
                "backgroundColor": {"color": {"rgbColor": COLOR_HEADER_BG}},
            },
            "fields": "backgroundColor",
        }
    })

    return requests


def _find_last_table_element(doc: dict) -> dict | None:
    """Trouve le dernier élément 'table' dans le body du document."""
    body = doc.get("body", {})
    content = body.get("content", [])
    for element in reversed(content):
        if "table" in element:
            return element
    return None


# ---------------------------------------------------------------------------
# Point d'entrée
# ---------------------------------------------------------------------------

def main():
    doc_id = os.environ.get("GDOC_TDD_ID")
    if not doc_id:
        print("Erreur : la variable d'environnement GDOC_TDD_ID n'est pas définie.", file=sys.stderr)
        sys.exit(1)

    if not TDD_PATH.exists():
        print(f"Erreur : fichier introuvable — {TDD_PATH}", file=sys.stderr)
        sys.exit(1)

    md_text = TDD_PATH.read_text(encoding="utf-8")
    print(f"Lecture de {TDD_PATH} ({len(md_text)} caractères)")

    # Parse le markdown en blocs
    blocks = markdown_to_blocks(md_text)
    print(f"Parsing terminé : {len(blocks)} blocs détectés")

    # Découper en segments texte / tableaux
    segments = split_into_segments(blocks)
    n_text = sum(1 for s in segments if isinstance(s, list))
    n_tables = sum(1 for s in segments if isinstance(s, TableBlock))
    print(f"Segments : {n_text} texte, {n_tables} tableaux")

    # Authentification
    creds = get_credentials()
    service = build("docs", "v1", credentials=creds)

    # Étape 1 : lire le document pour connaître la longueur actuelle
    doc = service.documents().get(documentId=doc_id).execute()
    body = doc.get("body", {})
    content = body.get("content", [])
    end_index = content[-1]["endIndex"] if content else 1

    print(f"Document Google Docs trouvé : \"{doc.get('title')}\"")
    print(f"Contenu actuel : {end_index - 1} caractères")

    # Étape 2 : effacer tout le contenu existant (sauf le dernier \n obligatoire)
    if end_index > 2:
        service.documents().batchUpdate(
            documentId=doc_id, body={"requests": [{
                "deleteContentRange": {
                    "range": {"startIndex": 1, "endIndex": end_index - 1}
                }
            }]}
        ).execute()
        print("Contenu existant effacé")

    # Étape 3 : traiter chaque segment séquentiellement
    BATCH_SIZE = 500
    total_api_calls = 0

    for seg_idx, segment in enumerate(segments):
        if isinstance(segment, list):
            # ── Segment texte ──
            # Lire le doc pour obtenir l'index de fin actuel
            doc = service.documents().get(documentId=doc_id).execute()
            content = doc.get("body", {}).get("content", [])
            end_index = content[-1]["endIndex"] if content else 1
            total_api_calls += 1

            insert_at = end_index - 1
            requests = build_text_requests(segment, insert_at)

            if requests:
                for i in range(0, len(requests), BATCH_SIZE):
                    batch = requests[i:i + BATCH_SIZE]
                    service.documents().batchUpdate(
                        documentId=doc_id, body={"requests": batch}
                    ).execute()
                    total_api_calls += 1

                print(f"  [{seg_idx + 1}/{len(segments)}] Texte : {len(requests)} requêtes")

        elif isinstance(segment, TableBlock):
            # ── Segment tableau natif ──
            n_data_rows = len(segment.rows)
            n_total_rows = 1 + n_data_rows  # header + data
            n_cols = len(segment.headers)

            if n_cols == 0:
                continue

            # 1. Lire le doc pour obtenir l'index de fin actuel
            doc = service.documents().get(documentId=doc_id).execute()
            content = doc.get("body", {}).get("content", [])
            end_index = content[-1]["endIndex"] if content else 1
            insert_at = end_index - 1
            total_api_calls += 1

            # 2. Insérer le tableau natif
            service.documents().batchUpdate(
                documentId=doc_id, body={"requests": [{
                    "insertTable": {
                        "rows": n_total_rows,
                        "columns": n_cols,
                        "location": {"index": insert_at},
                    }
                }]}
            ).execute()
            total_api_calls += 1

            # 3. Relire le doc pour obtenir la structure du tableau
            doc = service.documents().get(documentId=doc_id).execute()
            total_api_calls += 1

            table_element = _find_last_table_element(doc)
            if not table_element:
                print(f"  [{seg_idx + 1}/{len(segments)}] ERREUR : tableau introuvable après insertion")
                continue

            # 4. Remplir les cellules
            fill_requests, cell_text_lengths = build_table_fill_requests(table_element, segment)
            if fill_requests:
                service.documents().batchUpdate(
                    documentId=doc_id, body={"requests": fill_requests}
                ).execute()
                total_api_calls += 1

            # 5. Relire le doc pour les index post-fill et appliquer les styles
            doc = service.documents().get(documentId=doc_id).execute()
            total_api_calls += 1

            table_element = _find_last_table_element(doc)
            if table_element:
                style_requests = build_table_style_requests(table_element, segment, cell_text_lengths)
                if style_requests:
                    for i in range(0, len(style_requests), BATCH_SIZE):
                        batch = style_requests[i:i + BATCH_SIZE]
                        service.documents().batchUpdate(
                            documentId=doc_id, body={"requests": batch}
                        ).execute()
                        total_api_calls += 1

            print(f"  [{seg_idx + 1}/{len(segments)}] Tableau : {n_total_rows}×{n_cols}")

    print(f"\nExport terminé avec succès. ({total_api_calls} appels API)")


if __name__ == "__main__":
    main()
