# -*- coding: utf-8 -*-
"""
Готовит HTML из docs/documentation.md для Word-версии документации
(дальше scripts/build_docx.ps1 превращает этот HTML в DOCX).

Отличия от Markdown:
- оглавление заменяется маркером [[TOC]] — Word вставит своё оглавление с номерами страниц;
- схемы Mermaid (Word их не рисует) заменяются картинками docs/images/diagram-*.png,
  отрисованными из тех же схем; если схема в Markdown поменялась, перерисуйте картинку;
- относительные ссылки и якоря становятся обычным текстом: в отдельном файле они не работают.

Использование:
    python -m pip install markdown-it-py pillow
    python scripts/docx_html.py build/documentation.html
"""
import html
import os
import re
import sys

from markdown_it import MarkdownIt

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")).replace(os.sep, "/")
DOCS = ROOT + "/docs"
SRC = DOCS + "/documentation.md"
OUT = sys.argv[1]
os.makedirs(os.path.dirname(os.path.abspath(OUT)), exist_ok=True)

md_text = open(SRC, encoding="utf-8").read()

# 1. Оглавление Markdown -> маркер для оглавления Word
md_text = re.sub(r"\*\*Содержание\*\*\n\n(?:(?:\d+\.|   -) .*\n)+", "[[TOC]]\n", md_text)

# 2. Схемы Mermaid -> картинки
diagrams = iter([("diagram-user-path.png", 460, "Путь пользователя"), ("diagram-components.png", 640, "Компоненты решения")])


def img_tag(path, width, alt):
    from PIL import Image
    w, h = Image.open(path).size
    return f'<p class="figure"><img src="file:///{path}" width="{width}" height="{round(width * h / w)}" alt="{alt}"></p>'


def mermaid_to_img(_m):
    name, width, alt = next(diagrams)
    return img_tag(f"{DOCS}/images/{name}", width, alt) + "\n"


md_text = re.sub(r"```mermaid\n.*?```\n", mermaid_to_img, md_text, flags=re.S)

# 3. Картинки из docs/images -> абсолютные пути
md_text = re.sub(r"!\[([^\]]*)\]\((images/[^)]+)\)",
                 lambda m: img_tag(f"{DOCS}/{m.group(2)}", 600, html.escape(m.group(1))),
                 md_text)

# 4. Относительные ссылки и якоря -> просто текст (в отдельном файле Word они не работают)
md_text = re.sub(r"(?<!!)\[([^\]]+)\]\((?!https?://|mailto:)[^)]*\)", r"\1", md_text)

md = MarkdownIt("commonmark", {"html": True, "typographer": False}).enable("table").enable("strikethrough")
body = md.render(md_text)

# заголовок документа — стиль Title, а не Heading 1 (чтобы не попадал в оглавление)
body = re.sub(r"<h1>(.*?)</h1>", r'<p class="MsoTitle">\1</p>', body, count=1)
# приложения и раздел 8 — с новой страницы
body = re.sub(r"<h2>(8\. Приложения|Приложение )", r'<h2 class="newpage">\1', body)
# разделители перед приложениями не нужны — там разрыв страницы
body = body.replace("<hr />", "")
body = body.replace("<table>", '<table width="100%" style="width:100%">')
body = body.replace("<p>[[TOC]]</p>", '<p class="toc">[[TOC]]</p>')

css = """
body { font-family: 'Segoe UI', Calibri, sans-serif; font-size: 10.5pt; color: #1C1D22; line-height: 1.3; }
p.MsoTitle { font-size: 24pt; font-weight: bold; color: #520978; margin: 0 0 12pt 0; }
h1 { font-size: 20pt; color: #520978; }
h2 { font-size: 16pt; color: #520978; margin-top: 18pt; margin-bottom: 6pt; border-bottom: 1px solid #D9CFEA; }
h2.newpage { page-break-before: always; }
h3 { font-size: 13pt; color: #310F53; margin-top: 12pt; margin-bottom: 4pt; }
h4 { font-size: 11.5pt; color: #310F53; margin-top: 10pt; margin-bottom: 3pt; }
h5, h6 { font-size: 10.5pt; color: #310F53; margin-top: 8pt; margin-bottom: 2pt; }
p { margin: 0 0 6pt 0; }
li { margin-bottom: 3pt; }
table { border-collapse: collapse; margin: 4pt 0 10pt 0; }
th, td { border: 1px solid #C9C3DC; padding: 3pt 5pt; vertical-align: top; font-size: 9pt; }
th { background: #EFEAF6; font-weight: bold; }
code { font-family: Consolas, 'Courier New', monospace; font-size: 9pt; color: #520978; }
pre { font-family: Consolas, 'Courier New', monospace; font-size: 8.5pt; background: #F4F2F8; border: 1px solid #DDD8E8; padding: 6pt; }
pre code { color: #1C1D22; }
blockquote { border-left: 3px solid #FF0053; margin-left: 0; padding-left: 8pt; color: #3A3B44; }
p.figure { text-align: center; margin: 6pt 0 10pt 0; }
a { color: #520978; }
"""

doc = f"""<!DOCTYPE html>
<html lang="ru"><head><meta charset="utf-8"><title>Арена переговоров — сопроводительная документация</title>
<style>{css}</style></head><body>
{body}
</body></html>"""
open(OUT, "w", encoding="utf-8").write(doc)
print("html:", OUT, len(doc), "chars;", body.count("<img"), "images;", body.count("<table"), "tables;", "TOC marker:", "[[TOC]]" in body)
