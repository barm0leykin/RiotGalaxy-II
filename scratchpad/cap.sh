#!/usr/bin/env bash
# Управление игрой на :0 и захват кадра.
# Использование: cap.sh "<keys>" <sleepAfter> <out.png>
#   keys      — последовательность для `xdotool key` через пробел (напр. "Down Down Return")
#   sleepAfter— пауза после нажатий перед снимком (сек), по умолчанию 0.8
#   out.png   — куда сохранить снимок, по умолчанию /tmp/shot.png
set -e
export DISPLAY=:0
KEYS="${1:-}"
SLP="${2:-0.8}"
OUT="${3:-/tmp/shot.png}"

# именно окно игры (в VS Code заголовок тоже содержит "RiotGalaxy", поэтому имя точное)
WID=$(xdotool search --name "RiotGalaxy.DesktopGL" 2>/dev/null | tail -1)
[ -z "$WID" ] && { echo "окно игры не найдено"; exit 1; }

xdotool windowactivate --sync "$WID" >/dev/null 2>&1 || true
xdotool windowfocus --sync "$WID"
# критично: убрать курсор с окна игры (оно на втором мониторе @2560+),
# иначе hover в меню перебивает клавиатурный выбор каждый кадр
xdotool mousemove 100 100
sleep 0.2
if [ -n "$KEYS" ]; then
  for k in $KEYS; do
    # держим клавишу ~0.15с и большой зазор между нажатиями — иначе edge-детектор
    # (на неосновном мониторе с vsync) теряет часть нажатий
    xdotool keydown --clearmodifiers "$k"
    sleep 0.15
    xdotool keyup --clearmodifiers "$k"
    sleep 0.35
  done
fi
sleep "$SLP"
import -window "$WID" "$OUT"
echo "saved $OUT"
