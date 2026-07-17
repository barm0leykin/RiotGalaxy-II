#!/usr/bin/env bash
# Собрать ДЕСКТОП-версию (self-contained) внутри сборочного контейнера (см. Dockerfile.desktop).
# Как и Android: всё в контейнере, хост-система чистая. Причина контейнеризации — HLSL-шейдер
# Bloom.fx компилируется mgfxc через wine, а настраивать wine на хосте/раннере хрупко.
#
# Контейнер запускается ПОД ТЕКУЩИМ пользователем (--user): артефакты bin/obj/publish твои,
# чистятся без sudo. NuGet/dotnet-кэш — в docker/.home (на хосте, gitignored). wine-префикс
# запечён в образ (/opt/.winemonogame, права 0777 → доступен и не-root).
# Готовые zip — в dist/.
#
# Использование:
#   ./docker/build-desktop.sh [linux-x64|win-x64|all]   (по умолчанию all)
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
IMAGE="riotgalaxy-desktop-build"

RIDS="${1:-all}"
[ "${RIDS}" = "all" ] && RIDS="linux-x64 win-x64"

HOME_CACHE="${SCRIPT_DIR}/.home"   # HOME контейнера (кэш NuGet/dotnet), переиспользуется
mkdir -p "${HOME_CACHE}"

echo ">> Building desktop image (${IMAGE})"
docker build -f "${SCRIPT_DIR}/Dockerfile.desktop" -t "${IMAGE}" "${ROOT_DIR}"

mkdir -p "${ROOT_DIR}/dist"
for rid in ${RIDS}; do
    echo ">> Publishing ${rid}"
    # wine отказывается работать с префиксом, «не принадлежащим тебе». Образ печёт префикс-эталон
    # в /opt под root; под --user копируем его в свой HOME (cp делает копию нашей — владелец совпал),
    # копия кэшируется в docker/.home и переиспользуется.
    docker run --rm \
        --user "$(id -u):$(id -g)" \
        -e HOME=/home/build \
        -e XDG_RUNTIME_DIR=/tmp/xdg \
        -e MGFXC_WINE_PATH=/home/build/.winemonogame \
        -v "${HOME_CACHE}:/home/build" \
        -v "${ROOT_DIR}:/src" \
        -w /src \
        "${IMAGE}" \
        bash -c "mkdir -p /tmp/xdg && \
            [ -d /home/build/.winemonogame ] || cp -a /opt/.winemonogame /home/build/.winemonogame && \
            dotnet tool restore && \
            dotnet publish RiotGalaxy.DesktopGL/RiotGalaxy.DesktopGL.csproj \
              -c Release -r ${rid} --self-contained true -p:PublishReadyToRun=false \
              -o publish/${rid}"
    ( cd "${ROOT_DIR}/publish/${rid}" && rm -f "../../dist/RiotGalaxy-${rid}.zip" && zip -rq "../../dist/RiotGalaxy-${rid}.zip" . )
    echo ">> dist/RiotGalaxy-${rid}.zip"
done
echo ">> Готово."
