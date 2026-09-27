#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
Нарезает записи "бормотания" (gibberish-голос) на короткие слово-подобные куски для
OpponentVoice (Resources/Audio/Blips/<настроение>/).

Зачем: OpponentVoice играет ОДИН звук на каждое слово реплики оппонента (~7 раз в
секунду при печати). Исходные образцы — целые фразы по 1-3 секунды, повторённые на
каждое слово они слились бы в кашу. Скрипт режет запись по паузам и провалам громкости на
куски 0.12-0.6 с — каждый кусок звучит как одно "слово" бормотания.

Алгоритм:
1. Огибающая громкости (окна по 5 мс). Звучащими считаются окна не тише пика минус 35 дБ.
2. Соседние звучащие участки с паузой короче 30 мс склеиваются.
3. Участки длиннее --max секунд режутся по самому глубокому провалу огибающей (не тише
   половины от соседних максимумов), рекурсивно; если провала нет — по минимуму в середине.
4. Слишком короткие куски (< --min секунд) отбрасываются.
5. У каждого куска: fade-in 8 мс, fade-out 30 мс (без щелчков на стыках) и выравнивание
   по RMS до -20 дБFS (примерно как у прежних звуков голоса) с защитой от клиппинга
   (пик не выше -1 дБFS).

Использование:
    python scripts/slice_voice.py вход.wav --prefix calm --out-dir папка [--max 0.6] [--min 0.12]
Результат: папка/calm_1.wav, calm_2.wav, ... (моно, 16 бит, частота как у входа).
"""

import argparse
import os
import struct
import sys

import numpy as np

FRAME = 0.005
FLOOR_DB = -35.0
MERGE_GAP = 0.03
TARGET_RMS_DB = -20.0
PEAK_LIMIT_DB = -1.0


def read_wav(path):
    b = open(path, "rb").read()
    if b[:4] != b"RIFF" or b[8:12] != b"WAVE":
        raise SystemExit(f"{path}: не WAV")
    pos, fmt, data = 12, None, None
    while pos + 8 <= len(b):
        cid = b[pos:pos + 4]
        size = struct.unpack("<I", b[pos + 4:pos + 8])[0]
        body = b[pos + 8:pos + 8 + size]
        if cid == b"fmt ":
            tag, ch, sr, _, _, bits = struct.unpack("<HHIIHH", body[:16])
            if tag == 0xFFFE and len(body) >= 26:
                tag = struct.unpack("<H", body[24:26])[0]
            fmt = (tag, ch, sr, bits)
        elif cid == b"data":
            data = body
        pos += 8 + size + (size & 1)
    tag, ch, sr, bits = fmt
    if tag == 1 and bits == 16:
        x = np.frombuffer(data, "<i2").astype(np.float64) / 32768
    elif tag == 3 and bits == 32:
        x = np.frombuffer(data, "<f4").astype(np.float64)
    else:
        raise SystemExit(f"{path}: поддерживается PCM 16 бит и float 32 (получено tag={tag}, bits={bits})")
    return sr, x.reshape(-1, ch).mean(axis=1)


def write_wav16(path, sr, x):
    pcm = (np.clip(x, -1, 1) * 32767).astype("<i2")
    data = pcm.tobytes()
    hdr = (b"RIFF" + struct.pack("<I", 36 + len(data)) + b"WAVE" + b"fmt " +
           struct.pack("<IHHIIHH", 16, 1, 1, sr, sr * 2, 2, 16) + b"data" + struct.pack("<I", len(data)))
    open(path, "wb").write(hdr + data)


def envelope(x, sr):
    w = int(sr * FRAME)
    n = len(x) // w
    return np.sqrt((x[:n * w].reshape(n, w) ** 2).mean(axis=1) + 1e-12)


def active_segments(env):
    peak = env.max()
    active = env > peak * 10 ** (FLOOR_DB / 20)
    segs, start = [], None
    for i, a in enumerate(active):
        if a and start is None:
            start = i
        elif not a and start is not None:
            segs.append([start, i])
            start = None
    if start is not None:
        segs.append([start, len(active)])
    merged = []
    gap = int(MERGE_GAP / FRAME)
    for s in segs:
        if merged and s[0] - merged[-1][1] <= gap:
            merged[-1][1] = s[1]
        else:
            merged.append(s)
    return merged


def split(env, a, b, max_frames):
    """Режет [a, b) по провалам огибающей, пока куски не станут короче max_frames."""
    if b - a <= max_frames:
        return [(a, b)]
    k = max(2, int(0.02 / FRAME))
    sm = np.convolve(env, np.ones(k) / k, mode="same")
    lo, hi = a + int(0.15 / FRAME), b - int(0.15 / FRAME)
    if hi <= lo:
        return [(a, b)]
    best, best_ratio = None, 1.0
    for i in range(lo + 1, hi - 1):
        if sm[i] <= sm[i - 1] and sm[i] <= sm[i + 1]:
            left, right = sm[a:i].max(), sm[i + 1:b].max()
            ratio = sm[i] / max(1e-9, min(left, right))
            if ratio < best_ratio:
                best, best_ratio = i, ratio
    if best is None or best_ratio > 0.5:
        mid = (a + b) // 2
        span = max(1, (b - a) // 6)
        seg = sm[mid - span: mid + span]
        best = mid - span + int(np.argmin(seg))
    return split(env, a, best, max_frames) + split(env, best, b, max_frames)


def finish(clip, sr):
    n_in, n_out = int(sr * 0.008), int(sr * 0.030)
    clip = clip.copy()
    clip[:n_in] *= np.linspace(0, 1, n_in)
    clip[-n_out:] *= np.linspace(1, 0, n_out)
    rms = np.sqrt((clip ** 2).mean() + 1e-12)
    clip *= 10 ** (TARGET_RMS_DB / 20) / rms
    peak = np.abs(clip).max()
    limit = 10 ** (PEAK_LIMIT_DB / 20)
    if peak > limit:
        clip *= limit / peak
    return clip


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("input")
    ap.add_argument("--prefix", required=True)
    ap.add_argument("--out-dir", required=True)
    ap.add_argument("--max", type=float, default=0.6)
    ap.add_argument("--min", type=float, default=0.12)
    args = ap.parse_args()

    sr, x = read_wav(args.input)
    env = envelope(x, sr)
    w = int(sr * FRAME)
    os.makedirs(args.out_dir, exist_ok=True)

    pieces = []
    for a, b in active_segments(env):
        pieces += split(env, a, b, int(args.max / FRAME))
    kept = [(a, b) for a, b in pieces if (b - a) * FRAME >= args.min]

    for n, (a, b) in enumerate(kept, 1):
        clip = finish(x[a * w: b * w], sr)
        write_wav16(os.path.join(args.out_dir, f"{args.prefix}_{n}.wav"), sr, clip)
    print(f"{os.path.basename(args.input)}: {len(pieces)} кусков, оставлено {len(kept)}: " +
          ", ".join(f"{(b - a) * FRAME:.2f}s" for a, b in kept))


if __name__ == "__main__":
    sys.exit(main())
