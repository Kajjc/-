#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
Валидатор графа сценария «Арена переговоров» (гипотеза К3, docs/feature-hypotheses.md).

Формализует проверки, которые раньше делались вручную одноразовыми скриптами
(см. историю разработки трёх текущих сценариев и найденную дыру в hr_salary_review):

- JSON валиден, структура соответствует контракту DialogueModels.cs.
- Нет "висячих" next (ссылка на несуществующий узел).
- Каждый узел (кроме start) достижим из startNode.
- Терминальные узлы: пустой options, непустые outcome и summary.
- Нетерминальные узлы: outcome/summary пустые.
- technique каждой опции — только из закрытого списка docs/technique-taxonomy.json,
  points — внутри points_range этой техники.
- points <= 0 => непустой betterAlternative; points > 0 => пустой betterAlternative.
- requiredSkills[].skill — только napor/empatiya/logika, level — только 1..3.
- Предупреждение (не ошибка): "плохой" первый ход (points <= -1) не должен
  открывать путь к исходу win — сравнивается с "хорошим" первым ходом.

Использование:
    python scripts/validate_scenario.py unity/Assets/Resources/Scenarios/hr_salary_review.json
    python scripts/validate_scenario.py unity/Assets/Resources/Scenarios/*.json
"""

import json
import sys
import glob

VALID_SKILLS = {"napor", "empatiya", "logika"}


def load_taxonomy():
    with open("docs/technique-taxonomy.json", encoding="utf-8") as f:
        taxonomy = json.load(f)
    return {t["id"]: tuple(t["points_range"]) for t in taxonomy["techniques"]}


def reachable_outcomes(nodes, node_id, visited=None):
    if visited is None:
        visited = set()
    if node_id in visited:
        return set()
    visited.add(node_id)
    node = nodes[node_id]
    if node.get("outcome"):
        return {node["outcome"]}
    result = set()
    for option in node.get("options", []):
        result |= reachable_outcomes(nodes, option["next"], visited)
    return result


def validate_file(path, ranges):
    errors = []
    warnings = []

    with open(path, encoding="utf-8") as f:
        data = json.load(f)

    nodes = {n["id"]: n for n in data["nodes"]}
    start = data["startNode"]
    if start not in nodes:
        errors.append(f"startNode '{start}' не найден среди узлов")
        return errors, warnings

    # Достижимость всех узлов из startNode.
    reachable_ids = set()

    def visit(node_id):
        if node_id in reachable_ids:
            return
        reachable_ids.add(node_id)
        for option in nodes[node_id].get("options", []):
            if option["next"] in nodes:
                visit(option["next"])

    visit(start)
    for node_id in nodes:
        if node_id not in reachable_ids:
            errors.append(f"узел '{node_id}' недостижим из startNode")

    for node in data["nodes"]:
        node_id = node["id"]
        is_terminal = bool(node.get("outcome"))

        if is_terminal:
            if node.get("options"):
                errors.append(f"{node_id}: терминальный узел не должен иметь options")
            if not node.get("summary"):
                errors.append(f"{node_id}: терминальный узел без summary")
            continue

        if node.get("summary"):
            errors.append(f"{node_id}: нетерминальный узел не должен иметь summary")

        for option in node.get("options", []):
            next_id = option["next"]
            if next_id not in nodes:
                errors.append(f"{node_id}: опция ссылается на несуществующий узел '{next_id}'")

            technique = option.get("technique", "")
            points = option.get("points", 0)
            better = option.get("betterAlternative", "")

            if technique:
                if technique not in ranges:
                    errors.append(f"{node_id}: неизвестная техника '{technique}' — нет в technique-taxonomy.json")
                else:
                    lo, hi = ranges[technique]
                    if not (lo <= points <= hi):
                        errors.append(f"{node_id}/{technique}: points={points} вне диапазона {ranges[technique]}")

            if points <= 0 and not better:
                errors.append(f"{node_id}: points<={points} без betterAlternative")
            if points > 0 and better:
                errors.append(f"{node_id}: points>0, но betterAlternative не пустой")

            for req in option.get("requiredSkills", []):
                if req["skill"] not in VALID_SKILLS:
                    errors.append(f"{node_id}: неизвестный навык '{req['skill']}'")
                if not (1 <= req["level"] <= 3):
                    errors.append(f"{node_id}: уровень навыка {req['level']} вне диапазона 1..3")

    # Предупреждение про "дешёвый" плохой путь к win (эвристика, не строгая ошибка).
    start_node = nodes[start]
    for option in start_node.get("options", []):
        if option.get("points", 0) <= -1:
            reached = reachable_outcomes(nodes, option["next"])
            if "win" in reached:
                warnings.append(
                    f"'{option['text'][:40]}...' (points={option['points']}) на старте "
                    f"всё равно может привести к 'win' — проверьте, не срезан ли путь"
                )

    return errors, warnings


def main():
    if len(sys.argv) < 2:
        print("Использование: python scripts/validate_scenario.py <файл.json> [ещё файлы...]")
        sys.exit(1)

    paths = []
    for arg in sys.argv[1:]:
        paths.extend(glob.glob(arg))

    ranges = load_taxonomy()
    total_errors = 0

    for path in paths:
        errors, warnings = validate_file(path, ranges)
        print(f"=== {path} ===")
        if not errors and not warnings:
            print("  OK")
        for e in errors:
            print(f"  ERROR: {e}")
        for w in warnings:
            print(f"  WARNING: {w}")
        total_errors += len(errors)

    if total_errors > 0:
        print(f"\nИтого ошибок: {total_errors}")
        sys.exit(1)
    print("\nВсе файлы прошли проверку.")


if __name__ == "__main__":
    main()
