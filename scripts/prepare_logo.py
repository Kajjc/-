#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
Готовит логотип к главному меню (Resources/Icons/logo.png).

Что делает и зачем:
1. Обрезает по видимому содержимому (генератор отдаёт логотип с широким прозрачным полем).
2. Перекрашивает надпись в светлый. Логотип нарисован под светлый фон: эмблема цветная,
   а надпись под ней ("АРЕНА / ПЕРЕГОВОРОВ") — тёмно-синяя, почти цвета фона меню (Navy),
   и на тёмном меню она пропадает. Надпись — это все строки содержимого ниже эмблемы
   (эмблема — самый высокий сплошной блок строк); их цвет заменяется на Parchment, а
   полупрозрачность краёв (сглаживание) сохраняется. Эмблему скрипт не трогает.
3. Уменьшает до --height px по высоте (пропорции сохраняются): в меню логотип рисуется
   ~300 px высотой, с запасом на HiDPI, но без раздувания билда.

Использование:
    python scripts/prepare_logo.py logo.png --out logo_готовый.png [--height 640]
Исходник не меняется.
"""

import argparse
import sys

import numpy as np
from PIL import Image

ALPHA_THRESHOLD = 10
PARCHMENT = (0xF3, 0xEF, 0xE7)


def content_segments(alpha):
    """Непрерывные блоки строк, где есть непрозрачные пиксели: [(первая, последняя), ...]."""
    has = (alpha > ALPHA_THRESHOLD).any(axis=1)
    segments, start = [], None
    for y, filled in enumerate(has):
        if filled and start is None:
            start = y
        elif not filled and start is not None:
            segments.append((start, y - 1))
            start = None
    if start is not None:
        segments.append((start, len(has) - 1))
    return segments


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("input")
    parser.add_argument("--out", required=True)
    parser.add_argument("--height", type=int, default=640)
    args = parser.parse_args()

    im = Image.open(args.input).convert("RGBA")
    px = np.asarray(im).copy()
    ys, xs = np.where(px[..., 3] > ALPHA_THRESHOLD)
    px = px[ys.min(): ys.max() + 1, xs.min(): xs.max() + 1]

    segments = content_segments(px[..., 3])
    emblem = max(segments, key=lambda s: s[1] - s[0])
    recolored = 0
    for start, end in segments:
        if start > emblem[1]:
            px[start: end + 1, :, :3] = PARCHMENT
            recolored += 1
    print(f"блоков строк: {len(segments)}, надпись (перекрашено): {recolored}")

    out = Image.fromarray(px, "RGBA")
    width = int(round(out.width * args.height / out.height))
    out = out.resize((width, args.height), Image.LANCZOS)
    out.save(args.out, optimize=True)
    print(f"{args.out}: {out.width}x{out.height}")


if __name__ == "__main__":
    sys.exit(main())
