#!/usr/bin/env python3
"""Нарезка спрайт-листа (равномерная сетка rows×cols) на отдельные PNG с прозрачным фоном.

Фон (тёмный) убирается ЗАЛИВКОЙ ОТ КРАЁВ каждой ячейки — так тёмные детали ВНУТРИ спрайта
(тени, зрачок) сохраняются, а убирается только фон, связанный с рамкой. Затем автообрезка.

Использование:
  python3 tools/cut_sprites.py <sheet.png> <rows> <cols> <out_dir> <name0,name1,...> [height] [thr]
  height — уменьшить каждый спрайт до этой высоты (px, LANCZOS); 0/нет — исходный размер.
  thr — порог «тёмного» фона (max(r,g,b) <= thr считается фоном), по умолчанию 40.
Имена задают файлы по порядку (слева-направо, сверху-вниз); лишние ячейки пропускаются.
"""
import sys
from collections import deque
from pathlib import Path

from PIL import Image


def strip_bg(cell: Image.Image, thr: int) -> Image.Image:
    cell = cell.convert("RGBA")
    w, h = cell.size
    px = cell.load()
    visited = bytearray(w * h)
    q = deque()

    def is_bg(x, y):
        r, g, b, a = px[x, y]
        return max(r, g, b) <= thr

    # старт заливки — все краевые пиксели, похожие на фон
    for x in range(w):
        for y in (0, h - 1):
            if not visited[y * w + x] and is_bg(x, y):
                q.append((x, y)); visited[y * w + x] = 1
    for y in range(h):
        for x in (0, w - 1):
            if not visited[y * w + x] and is_bg(x, y):
                q.append((x, y)); visited[y * w + x] = 1

    while q:
        x, y = q.popleft()
        r, g, b, a = px[x, y]
        px[x, y] = (r, g, b, 0)  # прозрачный
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < w and 0 <= ny < h and not visited[ny * w + nx] and is_bg(nx, ny):
                visited[ny * w + nx] = 1
                q.append((nx, ny))

    bbox = cell.getbbox()
    return cell.crop(bbox) if bbox else cell


def main(sheet, rows, cols, out_dir, names, height=0, thr=40):
    rows, cols, height, thr = int(rows), int(cols), int(height), int(thr)
    out_dir = Path(out_dir); out_dir.mkdir(parents=True, exist_ok=True)
    names = [n for n in names.split(",") if n]
    img = Image.open(sheet).convert("RGBA")
    W, H = img.size
    cw, ch = W // cols, H // rows

    i = 0
    for r in range(rows):
        for c in range(cols):
            if i >= len(names):
                return
            cell = img.crop((c * cw, r * ch, (c + 1) * cw, (r + 1) * ch))
            sprite = strip_bg(cell, thr)
            if height and sprite.height != height:
                w = max(1, round(sprite.width * height / sprite.height))
                sprite = sprite.resize((w, height), Image.LANCZOS)
            out = out_dir / f"{names[i]}.png"
            sprite.save(out)
            print(f"  [{r},{c}] -> {out}  {sprite.size}")
            i += 1


if __name__ == "__main__":
    args = sys.argv[1:]
    main(*args[:5],
         height=args[5] if len(args) > 5 else 0,
         thr=args[6] if len(args) > 6 else 40)
