#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
Собирает сводную сопроводительную документацию docs/documentation.md.

Разделы 1–7 файла пишутся вручную и остаются как есть. Раздел 8 «Приложения»
целиком пересобирается из исходных документов: docs/*.md и
docs/technique-taxonomy.json. Главные — исходные файлы: после их правки
запустите скрипт ещё раз, копии в documentation.md обновятся.

Что делает с каждым документом:
- его заголовок первого уровня заменяется заголовком «Приложение X. …»;
- остальные заголовки сдвигаются на уровень ниже (кроме строк внутри блоков кода);
- ссылки на сам documentation.md заменяются на «разделы 1–7 этого документа».
Таксономия из JSON превращается в читаемые карточки техник.

Использование:
    python scripts/build_documentation.py
Нужен только стандартный Python.
"""

import json
import os
import re
import sys

DOCS = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "docs"))
TARGET = os.path.join(DOCS, "documentation.md")

SECTION_TITLE = "8. Приложения"

APPENDICES = [
    ("А", "Методология оценки", "eval-rubric.md"),
    ("Б", "Таксономия 15 техник", "technique-taxonomy.json"),
    ("В", "Тест навыков", "skill-test.md"),
    ("Г", "Выбор трёх сфер", "negotiation-domains.md"),
    ("Д", "Визуальный стиль и промпты для арта", "visual-style-guide.md"),
    ("Е", "Бэклог гипотез", "feature-hypotheses.md"),
    ("Ж", "Дизайн-документ", "game-design-document.md"),
    ("З", "План и хронология работ", "roadmap.md"),
]

EXTERNAL = [
    ("Презентация решения", "[`presentation/arena-peregovorov.pdf`](presentation/arena-peregovorov.pdf), "
                            "[`.pptx`](presentation/arena-peregovorov.pptx)"),
    ("Техническое задание хакатона (документ организаторов)", "[`9. ОЭЗ ППТ Алабуга.pdf`](../9.%20ОЭЗ%20ППТ%20Алабуга.pdf)"),
]

CENTRALITY = {"central": "центральная", "secondary": "второстепенная", "n/a": "не применяется"}
DOMAINS = [("hr", "HR"), ("sales", "продажи"), ("procurement", "закупки")]


def slug(text):
    """Якорь заголовка так, как его строит GitHub."""
    s = text.strip().lower()
    s = re.sub(r"[^\w\- ]", "", s, flags=re.UNICODE)
    return s.replace(" ", "-")


def appendix_heading(letter, title):
    return f"Приложение {letter}. {title}"


def embed_markdown(text):
    """Возвращает (исходный заголовок H1, тело со сдвинутыми заголовками)."""
    out, original_title, in_code = [], None, False
    for line in text.splitlines():
        if line.lstrip().startswith("```"):
            in_code = not in_code
            out.append(line)
            continue
        m = None if in_code else re.match(r"^(#{1,6})\s+(.*)$", line)
        if m:
            level = len(m.group(1))
            if level == 1 and original_title is None:
                original_title = m.group(2).strip()
                continue
            line = "#" * min(6, level + 1) + " " + m.group(2)
        out.append(line)
    body = "\n".join(out).strip("\n")
    body = re.sub(r"\[`?(?:docs/)?documentation\.md`?\]\(documentation\.md\)", "разделы 1–7 этого документа", body)
    return original_title, body


def embed_taxonomy(path):
    data = json.load(open(path, encoding="utf-8"))
    lines = [
        "Закрытый список техник, которыми размечен каждый вариант ответа в сценариях. "
        "Машиночитаемый источник — `docs/technique-taxonomy.json`, копия для игры — "
        "`unity/Assets/Resources/TechniqueTaxonomy.json`; методология — приложение А.",
        "",
    ]
    for t in data["techniques"]:
        lo, hi = t["points_range"]
        pts = f"{lo:+d}" if lo == hi else f"{lo:+d}…{hi:+d}"
        centr = ", ".join(f"{name} — {CENTRALITY.get(t['domain_centrality'].get(key), '—')}" for key, name in DOMAINS)
        lines += [
            f"### {t['ru_name']} · `{t['id']}`",
            "",
            f"- **Баллы:** {pts}",
            f"- **Определение:** {t['definition_ru']}",
            f"- **Сигнал в реплике:** {t['signal_ru']}",
            f"- **Теория:** {t['theory_note_ru']} — *{t['theory_source']}*",
            f"- **Значимость для сфер:** {centr}",
            "",
        ]
    return "\n".join(lines).strip("\n")


def build_section():
    parts = [
        f"## {SECTION_TITLE}",
        "",
        "Рабочие документы команды включены сюда целиком, как есть. Это история проекта и подробности "
        "по отдельным темам: часть статусов в них (например, в дизайн-документе от 18.09) с тех пор "
        "изменилась. Там, где приложения расходятся с разделами 1–7, актуальны разделы 1–7.",
        "",
    ]
    for letter, title, fname in APPENDICES:
        heading = appendix_heading(letter, title)
        parts.append(f"- [{heading}](#{slug(heading)}) — `docs/{fname}`")
    parts.append("")
    parts.append("Отдельные материалы, которые не входят в этот файл:")
    parts.append("")
    for title, link in EXTERNAL:
        parts.append(f"- {title}: {link}")
    for letter, title, fname in APPENDICES:
        path = os.path.join(DOCS, fname)
        heading = appendix_heading(letter, title)
        if fname.endswith(".json"):
            original, body = None, embed_taxonomy(path)
        else:
            original, body = embed_markdown(open(path, encoding="utf-8").read())
        parts += ["", "---", "", f"## {heading}", ""]
        source = f"*Исходный файл: `docs/{fname}`"
        source += f" — «{original}».*" if original else ".*"
        parts += [source, "", body]
    return "\n".join(parts) + "\n"


def rebuild_toc(core):
    lines = core.split("\n")
    idx = next(i for i, l in enumerate(lines) if re.match(r"^8\. \[", l))
    j = idx + 1
    while j < len(lines) and lines[j].startswith("   - [Приложение"):
        j += 1
    new = [f"8. [Приложения](#{slug(SECTION_TITLE)})"]
    for letter, title, _ in APPENDICES:
        heading = appendix_heading(letter, title)
        new.append(f"   - [{heading}](#{slug(heading)})")
    return "\n".join(lines[:idx] + new + lines[j:])


def main():
    text = open(TARGET, encoding="utf-8").read()
    m = re.search(r"^## 8\. .*$", text, flags=re.MULTILINE)
    if not m:
        sys.exit("В documentation.md не найден раздел «## 8. …» — не знаю, куда вставлять приложения.")
    core = rebuild_toc(text[:m.start()].rstrip("\n")) + "\n\n"
    result = core + build_section()
    open(TARGET, "w", encoding="utf-8", newline="\n").write(result)
    print(f"{TARGET}: {len(result.splitlines())} строк, приложений: {len(APPENDICES)}")


if __name__ == "__main__":
    main()
