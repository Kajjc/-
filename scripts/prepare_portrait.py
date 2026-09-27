#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
Готовит портрет оппонента к игре (Resources/Portraits/<сфера>_<настроение>.png).

Что делает и зачем:
1. Шахматка -> настоящая прозрачность. Генераторы картинок часто отдают PNG, где
   "прозрачный" фон нарисован серо-белой шахматкой прямо в пикселях (режим RGB,
   альфа-канала нет). В игре такой файл показал бы серую клетку на тёмной плашке.
   Фон вычищается заливкой от краёв кадра по нейтральным (почти без цвета) светлым
   пикселям; белая блузка/светлые детали внутри фигуры не затрагиваются, потому что
   до них залив от краёв не дотягивается. Если у файла уже есть настоящий альфа-
   канал, этот шаг пропускается. Принимает и PNG, и JPG (результат всегда PNG).
2. Кадрирование "голова и плечи" в пропорциях 1.2:1 — тех же, что у слота портрета
   в диалоге (DialogueUIController.PortraitAspect: ширина слота — доля канваса, высота
   выводится по этим пропорциям, так что картинка не растягивается при любой форме
   окна). Портреты приходят квадратными с половиной тела, а слот небольшой (~210x175
   при канвасе 1280x800), поэтому лицо иначе получалось бы мелким.
3. Уменьшение до 640 px по ширине — с запасом на HiDPI, но без раздувания билда.

Рамка кадрирования считается по каждому файлу отдельно от головы (центр по x — центр
масс верхней части силуэта), но высота и пропорции одинаковы для всех — при смене
настроения портрет не "прыгает".

Использование:
    python scripts/prepare_portrait.py вход1.png [вход2.png ...] --out-dir папка
Исходники не меняются, результат пишется в --out-dir под теми же именами.
"""

import argparse
import os
import sys

import numpy as np
from PIL import Image, ImageFilter

SLOT_ASPECT = 1.2             # ширина/высота арта = DialogueUIController.PortraitAspect
CROP_WIDTH_FRACTION = 0.76    # доля ширины исходника: голова и плечи
OUT_WIDTH = 640

MIN_BRIGHTNESS = 165          # серая клетка шахматки светлее этого (JPG даёт шум ниже)
MAX_SATURATION = 10           # фон нейтральный: max(R,G,B) - min(R,G,B) мал
POCKET_MIN_AREA = 30          # карман шахматки внутри фигуры: не меньше и не больше
POCKET_MAX_AREA = 12000       # (белая блузка на порядок крупнее)


def has_real_alpha(im):
    if "A" not in im.getbands():
        return False
    return np.asarray(im.getchannel("A")).min() < 250


def background_mask(rgb):
    """True там, где пиксель — шахматка, достижимая от краёв кадра."""
    a = rgb.astype(np.int16)
    brightness = a.mean(axis=2)
    saturation = a.max(axis=2) - a.min(axis=2)
    candidate = (brightness >= MIN_BRIGHTNESS) & (saturation <= MAX_SATURATION)

    h, w = candidate.shape
    # Заливка стартует с верхнего, левого и правого краёв, но НЕ с нижнего: портрет
    # обрезан по грудь, и внизу кадра, как правило, фигура (например, белая рубашка,
    # доходящая до нижней границы) — через неё залив съел бы светлую одежду изнутри.
    # Углы внизу всё равно достаются заливкой с боков.
    reached = np.zeros_like(candidate)
    reached[0, :] = candidate[0, :]
    reached[:, 0] = candidate[:, 0]
    reached[:, -1] = candidate[:, -1]

    while True:
        grown = reached.copy()
        grown[1:, :] |= reached[:-1, :]
        grown[:-1, :] |= reached[1:, :]
        grown[:, 1:] |= reached[:, :-1]
        grown[:, :-1] |= reached[:, 1:]
        grown &= candidate
        if grown.sum() == reached.sum():
            return reached
        reached = grown


def enclosed_checker_pockets(rgb, bg):
    """Мелкие кусочки шахматки, зажатые внутри силуэта (например, между прядью
    волос и шеей): залив от краёв их не достаёт. От белой блузки, зубов и бликов
    отличаем по размеру (маленькие) и по составу (и серые, и белые клетки)."""
    a = rgb.astype(np.int16)
    brightness = a.mean(axis=2)
    saturation = a.max(axis=2) - a.min(axis=2)
    cand = (brightness >= MIN_BRIGHTNESS) & (saturation <= MAX_SATURATION) & ~bg

    h, w = cand.shape
    seen = np.zeros_like(cand)
    pockets = np.zeros_like(cand)
    for y, x in np.argwhere(cand):
        if seen[y, x]:
            continue
        stack = [(y, x)]
        seen[y, x] = True
        comp = []
        while stack:
            cy, cx = stack.pop()
            comp.append((cy, cx))
            for ny, nx in ((cy + 1, cx), (cy - 1, cx), (cy, cx + 1), (cy, cx - 1)):
                if 0 <= ny < h and 0 <= nx < w and cand[ny, nx] and not seen[ny, nx]:
                    seen[ny, nx] = True
                    stack.append((ny, nx))
        if len(comp) < POCKET_MIN_AREA or len(comp) > POCKET_MAX_AREA:
            continue
        ys, xs = zip(*comp)
        b = brightness[list(ys), list(xs)]
        if (b < 225).mean() >= 0.25 and (b >= 240).mean() >= 0.25:
            pockets[list(ys), list(xs)] = True
    return pockets


def prepare(path, out_path):
    src = Image.open(path)
    if has_real_alpha(src):
        rgba = src.convert("RGBA")
    else:
        rgb = np.asarray(src.convert("RGB"))
        bg = background_mask(rgb)
        bg |= enclosed_checker_pockets(rgb, bg)
        # Кайма на волосах — смесь цвета и клетки; сжимаем маску на 2 px и
        # слегка размываем край, чтобы не оставалось светлого ореола.
        alpha_img = Image.fromarray(np.where(bg, 0, 255).astype(np.uint8))
        alpha_img = alpha_img.filter(ImageFilter.MinFilter(5)).filter(ImageFilter.GaussianBlur(0.9))
        rgba = Image.fromarray(rgb).convert("RGBA")
        rgba.putalpha(alpha_img)

    alpha = np.asarray(rgba.getchannel("A")) > 128
    rows = np.where(alpha.any(axis=1))[0]
    top = int(rows[0])
    w, h = rgba.size

    crop_w = int(w * CROP_WIDTH_FRACTION)
    crop_h = int(round(crop_w / SLOT_ASPECT))
    head_rows = alpha[top: top + int(h * 0.30)]
    cols = np.where(head_rows.any(axis=0))[0]
    head_center_x = float((cols[0] + cols[-1]) / 2.0)

    left = int(round(head_center_x - crop_w / 2.0))
    left = max(0, min(left, w - crop_w))
    top_crop = max(0, min(top - int(h * 0.01), h - crop_h))
    cropped = rgba.crop((left, top_crop, left + crop_w, top_crop + crop_h))

    out_h = int(round(OUT_WIDTH / SLOT_ASPECT))
    cropped = cropped.resize((OUT_WIDTH, out_h), Image.LANCZOS)
    cropped.save(out_path)
    return cropped.size


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("inputs", nargs="+")
    parser.add_argument("--out-dir", required=True)
    args = parser.parse_args()

    os.makedirs(args.out_dir, exist_ok=True)
    for path in args.inputs:
        out_name = os.path.splitext(os.path.basename(path))[0] + ".png"
        out_path = os.path.join(args.out_dir, out_name)
        size = prepare(path, out_path)
        print(f"{os.path.basename(path)} -> {out_name} {size[0]}x{size[1]}")


if __name__ == "__main__":
    sys.exit(main())
