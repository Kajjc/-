#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
Готовит иконку к игре (Resources/Icons/skill_<навык>.png, outcome_<исход>.png).

Что делает и зачем:
1. Обрезает по видимому содержимому. Генераторы картинок отдают иконку в большом
   квадрате (например, 1254x1254), где сама фигура занимает 25-40% кадра, а остальное —
   прозрачное поле. В игре иконка рисуется маленькой (16-72 px), и с таким полем фигура
   вышла бы крошечной.
2. Вписывает результат в квадрат с небольшим полем (пропорции не меняются) — все иконки
   одного набора получаются одинакового габарита.
3. Уменьшает до 256x256 (как badge_*.png): с запасом на HiDPI, но 600-900 КБ на файл
   превращаются в десятки КБ — билд и Git LFS не раздуваются.
Настоящая прозрачность у файла должна быть уже (шахматку из пикселей этот скрипт не
чистит — для этого есть prepare_portrait.py).

Использование:
    python scripts/prepare_icon.py вход1.png [вход2.png ...] --out-dir папка [--size 256]
Исходники не меняются, результат пишется в --out-dir под теми же именами.
"""

import argparse
import os
import sys

import numpy as np
from PIL import Image

ALPHA_THRESHOLD = 10     # пиксели с альфой ниже считаем пустым полем
MARGIN_FRACTION = 0.06   # поле вокруг фигуры, доля от стороны квадрата


def prepare(path, out_path, size):
    im = Image.open(path).convert("RGBA")
    alpha = np.asarray(im.getchannel("A"))
    ys, xs = np.where(alpha > ALPHA_THRESHOLD)
    if len(xs) == 0:
        raise SystemExit(f"{path}: у файла нет непрозрачных пикселей — нечего кадрировать.")

    cropped = im.crop((int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1))
    w, h = cropped.size
    side = int(round(max(w, h) / (1.0 - 2 * MARGIN_FRACTION)))
    square = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    square.paste(cropped, ((side - w) // 2, (side - h) // 2))
    square = square.resize((size, size), Image.LANCZOS)
    square.save(out_path, optimize=True)
    return square.size


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("inputs", nargs="+")
    parser.add_argument("--out-dir", required=True)
    parser.add_argument("--size", type=int, default=256)
    args = parser.parse_args()

    os.makedirs(args.out_dir, exist_ok=True)
    for path in args.inputs:
        out_path = os.path.join(args.out_dir, os.path.basename(path))
        size = prepare(path, out_path, args.size)
        print(f"{os.path.basename(path)} -> {size[0]}x{size[1]}, {os.path.getsize(out_path) // 1024} KB")


if __name__ == "__main__":
    sys.exit(main())
