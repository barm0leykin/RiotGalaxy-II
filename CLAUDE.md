# настройки для llm agent

## llm agent context

- Проект: **RiotGalaxy II** — 2D космический шутер на MonoGame (десктоп + Android).
- Вырос из завершённой миграции с CocosSharp (лог — `MIGRATION.md`). Развиваем самостоятельно,
  **без оглядки на старый движок/проект**.

## llm agent instructions

- всегда говори на русском
- эталонная локаль — русская (ru)
- не выдумывай: если данных нет — скажи; не знаешь — предложи поискать в интернете
- после изменений убедись, что всё собирается/работает; если сломалось — найди причину, исправь, перепроверь
- руководствуйся `README.md`, `TODO.md` (открытые задачи) и `ARCHITECTURE.md`
- по мере работ актуализируй `ARCHITECTURE.md` и `TODO.md`
- перед удалением чего-либо — спрашивай разрешение
- сборка Android — **только в Docker** (`docker/*.sh`), пакеты в хост не ставим

## как собрать/запустить

- Десктоп: `./run_game.sh` или `dotnet run --project RiotGalaxy.DesktopGL/RiotGalaxy.DesktopGL.csproj`
  (один раз после клона: `dotnet tool restore`). Тестировать удобно на desktop.
- Android: `./docker/build-image.sh` (один раз) → `./docker/build-apk.sh Debug` → `./docker/run-on-device.sh`.
- Логи игры — `riot.log` рядом с бинарником (на Android — logcat, тег `DOTNET`).

## ключевые файлы

- `RiotGalaxy.Core/Managers/GameManager.cs` — состояния, цикл, столкновения, отрисовка
- `RiotGalaxy.Core/GameObjects/` — корабль, враги, снаряды, бонусы, World/Hive/Route
- `RiotGalaxy.Core/AI/` — машина состояний врагов
- `RiotGalaxy.Content/Config/*.yaml`, `Content/Levels/*.yaml` — конфиги и уровни
