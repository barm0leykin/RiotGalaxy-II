using System;
using Microsoft.Xna.Framework;
using RiotGalaxy.Core.Components;
using RiotGalaxy.Core.GameObjects;

namespace RiotGalaxy.Core.Managers
{
    /// <summary>
    /// Оркестрация уровней: загрузка данных уровня и World/Hive, спавн врагов по таймлайну,
    /// счётчики врагов и прогрессия по кампании. Вынесено из GameManager.
    ///
    /// Объекты добавляются в общий список GameManager.Instance.GameObjects; смену состояния
    /// игры (Victory/NextLevel) принимает GameManager — здесь только данные уровня.
    /// </summary>
    public class LevelDirector
    {
        private World _world;
        private Hive _hive;
        private Utils.Level _level;
        private static readonly Random _spawnRnd = new Random();

        public int CurrentLevel { get; private set; } = 1;
        public int TotalLevels { get; private set; } = 1;
        public string CurrentLevelDescription => _level?.Description ?? "";

        public int EnemiesKilled { get; private set; }
        public int EnemiesRemaining { get; private set; }

        /// <summary>Уровень пройден: все враги уровня заспавнены и уничтожены.</summary>
        public bool LevelComplete => _level != null && _level.AllSpawned && EnemiesRemaining <= 0;

        /// <summary>Есть ли следующий уровень в кампании.</summary>
        public bool HasNextLevel => CurrentLevel < TotalLevels;

        /// <summary>Подсчитать число уровней по файлам Content/Levels/level*.yaml.</summary>
        public void InitTotalLevels()
        {
            TotalLevels = Utils.Level.CountLevels();
            if (TotalLevels < 1) TotalLevels = 1;
        }

        public void ResetToFirst() => CurrentLevel = 1;

        /// <summary>Перейти к следующему уровню и загрузить его данные.</summary>
        public void GoToNextLevel(int screenW, int screenH)
        {
            CurrentLevel++;
            Load(CurrentLevel, screenW, screenH);
        }

        /// <summary>Учёт убитого врага (вызывается при удалении врага из игры).</summary>
        public void OnEnemyKilled()
        {
            EnemiesKilled++;
            EnemiesRemaining = Math.Max(0, EnemiesRemaining - 1);
        }

        /// <summary>Загрузить уровень N: данные из YAML, счётчики врагов, World/Hive. Игрока не трогает.</summary>
        public void Load(int number, int screenW, int screenH)
        {
            CurrentLevel = number;
            _level = new Utils.Level();
            if (!_level.Load(number))
            {
                Console.WriteLine($"=== Level {number} not loaded ===");
                EnemiesRemaining = 0;
                return;
            }
            EnemiesKilled = 0;
            EnemiesRemaining = _level.TotalEnemies;
            Utils.Log.Debug($"Level {CurrentLevel} loaded: \"{_level.Description}\", enemies={_level.TotalEnemies}");

            // Мир и улей (формации) — пересоздаём на каждый уровень (сброс занятых ячеек)
            _world = new World(screenW, screenH);
            _hive = new Hive(_world, 4, 1, 8, 2); // 8×2 у верхней кромки

            // Вылеты из улья (Galaga) — если включены в описании уровня
            if (_level.Sortie)
                _hive.EnableSortie(_level.SortieInterval, _level.SortieCount);
        }

        /// <summary>Обновление за кадр: барражирование улья + спавн врагов по таймлайну.</summary>
        public void Update(float dt)
        {
            _hive?.Update(dt);

            if (_level != null)
                foreach (var info in _level.Tick(dt))
                    SpawnEnemy(info.Type, info.Formation, info.Route, info.After);
        }

        /// <summary>
        /// Создать врага сверху экрана. formation — в улей; routeName — пустить по маршруту.
        /// </summary>
        private void SpawnEnemy(EnemyType type, bool formation, string routeName = null, string after = null)
        {
            int screenW = GameManager.Instance.ScreenWidth;

            // Пытаемся занять ячейку улья для формации
            int cx = -1, cy = -1;
            bool inFormation = formation && _hive != null && _hive.TryTakeCell(out cx, out cy);

            // Маршрут (если не в формации и задан)
            Route route = (!inFormation && !string.IsNullOrEmpty(routeName) && _world != null)
                ? Route.Load(routeName, _world)
                : null;

            // Точка появления: ячейка улья / первая точка маршрута / случайно по X
            Vector2 pos;
            if (inFormation)
                pos = new Vector2(_hive.CellWorldPos(cx, cy).X, -30);
            else if (route != null && route.HasPoints)
                pos = route.Current;
            else
            {
                float border = screenW * 0.12f;
                float x = border + (float)_spawnRnd.NextDouble() * (screenW - 2 * border);
                pos = new Vector2(x, -30);
            }

            // Враг конфигурирует себя из enemies.yaml по типу (фабрика-данные вместо подклассов).
            // Неизвестный/RND трактуем как разведчика — как было в прежнем switch.
            var t = type == EnemyType.RND ? EnemyType.SM_SCOUT : type;
            Enemy e = new Enemy(t, pos);

            if (inFormation)
            {
                e.JoinFormation(_hive, cx, cy);
                _hive.Register(e, cx, cy); // учёт в улье — для координации вылетов
            }
            else if (route != null && route.HasPoints)
                e.SetRoute(route, ParseRouteEnd(after), _hive);

            GameManager.Instance.GameObjects.Add(e);
        }

        private static RouteEndBehavior ParseRouteEnd(string s)
        {
            switch (s?.Trim().ToLowerInvariant())
            {
                case "formation": return RouteEndBehavior.Formation;
                case "scatter": return RouteEndBehavior.Scatter;
                default: return RouteEndBehavior.Bounce;
            }
        }
    }
}
