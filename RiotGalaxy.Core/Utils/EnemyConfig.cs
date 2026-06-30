using System;
using System.Collections.Generic;
using RiotGalaxy.Core.GameObjects;

namespace RiotGalaxy.Core.Utils
{
    /// <summary>
    /// Параметры врагов из Content/Config/enemies.yaml. Скорость и интервал стрельбы
    /// поддерживают рандомизацию (min/max). Дефолты в коде = фолбэк, игра работает без файла.
    /// </summary>
    public static class EnemyConfig
    {
        public class Stats
        {
            public float Hp { get; set; } = 10;
            public float Damage { get; set; } = 10;
            public int Reward { get; set; } = 1; // валюта (кредиты) за убийство
            public float Speed { get; set; } = 100;
            public float SpeedMin { get; set; }
            public float SpeedMax { get; set; }
            public float ShootInterval { get; set; } = 3f;
            public float ShootIntervalMin { get; set; }
            public float ShootIntervalMax { get; set; }

            // Скорость во время вылета/атаки из улья (0 — использовать обычную Speed).
            public float AttackSpeed { get; set; }
            public float AttackSpeedMin { get; set; }
            public float AttackSpeedMax { get; set; }

            /// <summary>Тактики пике при вылете из улья (random/snake/ram/ellipse). Пусто — random.</summary>
            public List<string> Tactics { get; set; }

            // ── Внешний вид и поведение (data-driven вместо классов-наследников) ──
            /// <summary>Ассет спрайта (напр. "Images/enemyBlue").</summary>
            public string Sprite { get; set; }
            /// <summary>Масштаб спрайта и хитбокса (1 — обычный; босс крупнее).</summary>
            public float Scale { get; set; } = 1f;
            /// <summary>Стрельба: "none" / "down" (вниз) / "aim" (прицельно в игрока).</summary>
            public string Shoot { get; set; } = "none";
            /// <summary>ИИ-машина состояний: "none" / "blue" / "red". Иначе — движение с отскоком.</summary>
            public string Ai { get; set; } = "none";
            /// <summary>Периодически менять курс/скорость (как зелёный). Только при движении с отскоком.</summary>
            public bool Wander { get; set; }
            /// <summary>Диапазон стартового курса при движении с отскоком (градусы; 180 — строго вниз).</summary>
            public float DirMin { get; set; } = 155f;
            public float DirMax { get; set; } = 205f;

            public float PickSpeed(Random r) => Pick(Speed, SpeedMin, SpeedMax, r);
            public float PickShootInterval(Random r) => Pick(ShootInterval, ShootIntervalMin, ShootIntervalMax, r);
            /// <summary>Скорость атаки; 0 — не задана (вызывающий берёт обычную скорость).</summary>
            public float PickAttackSpeed(Random r) => Pick(AttackSpeed, AttackSpeedMin, AttackSpeedMax, r);

            private static float Pick(float fixedValue, float min, float max, Random r) =>
                (max > min) ? min + (float)r.NextDouble() * (max - min) : fixedValue;
        }

        // Дефолтные параметры (совпадают с прежними хардкодами)
        private static readonly Dictionary<EnemyType, Stats> _defaults = new Dictionary<EnemyType, Stats>
        {
            [EnemyType.BLUE]     = new Stats { Hp = 10, Damage = 10, Speed = 130, AttackSpeed = 200, ShootInterval = 3, Tactics = new List<string> { "snake", "ram" },
                                               Sprite = "Images/enemyBlue", Ai = "blue", Shoot = "down", Reward = 15 },
            [EnemyType.GREEN]    = new Stats { Hp = 10, Damage = 10, SpeedMin = 100, SpeedMax = 150, AttackSpeed = 200, ShootInterval = 3, Tactics = new List<string> { "random", "snake" },
                                               Sprite = "Images/enemyGreen", Shoot = "down", Wander = true, DirMin = 135, DirMax = 225, Reward = 15 },
            [EnemyType.RED]      = new Stats { Hp = 20, Damage = 10, Speed = 60, AttackSpeed = 160, ShootInterval = 3, Tactics = new List<string> { "ram", "ellipse" },
                                               Sprite = "Images/enemyRed", Ai = "red", Shoot = "aim", Reward = 25 },
            [EnemyType.SM_SCOUT] = new Stats { Hp = 10, Damage = 5, SpeedMin = 60, SpeedMax = 100, AttackSpeed = 150, ShootInterval = 0, Tactics = new List<string> { "random" },
                                               Sprite = "Images/enemySmallScout", Shoot = "none", DirMin = 155, DirMax = 205, Reward = 10 },
            [EnemyType.BOSS]     = new Stats { Hp = 200, Damage = 20, Speed = 50, ShootInterval = 1.2f,
                                               Sprite = "Images/enemyRed", Scale = 2.5f, Shoot = "aim", DirMin = 135, DirMax = 135, Reward = 120 },

            // ── Акт II: укропитеки (оранжевые/жёлтые) + Косморика-тяжёлые ──
            [EnemyType.UKRO]      = new Stats { Hp = 12, Damage = 10, SpeedMin = 90, SpeedMax = 140, AttackSpeed = 210, ShootInterval = 3.5f, Tactics = new List<string> { "random", "snake" },
                                                Sprite = "Images/Enemies/fat_orange_1", Shoot = "down", Wander = true, DirMin = 140, DirMax = 220, Reward = 18 },
            [EnemyType.KAMIK]     = new Stats { Hp = 8, Damage = 18, SpeedMin = 180, SpeedMax = 230, AttackSpeed = 320, ShootInterval = 0, Tactics = new List<string> { "ram" },
                                                Sprite = "Images/Enemies/thin_yellow_1", Shoot = "none", DirMin = 170, DirMax = 190, Reward = 14 },
            [EnemyType.HEAVY]     = new Stats { Hp = 40, Damage = 12, Speed = 55, AttackSpeed = 150, ShootInterval = 2.5f, Tactics = new List<string> { "ram", "ellipse" },
                                                Sprite = "Images/Enemies/fat_orange_2", Scale = 1.2f, Ai = "red", Shoot = "aim", Reward = 35 },
            [EnemyType.UKRO_BOSS] = new Stats { Hp = 320, Damage = 22, Speed = 45, ShootInterval = 1.0f,
                                                Sprite = "Images/Enemies/fat_orange_6", Scale = 2.6f, Shoot = "aim", DirMin = 135, DirMax = 135, Reward = 200 },
        };

        private static Dictionary<EnemyType, Stats> _stats;

        public static Stats Get(EnemyType type)
        {
            if (_stats != null && _stats.TryGetValue(type, out var s))
                return s;
            return _defaults.TryGetValue(type, out var d) ? d : new Stats();
        }

        public static void Load()
        {
            var data = Yaml.LoadAsset<Dictionary<string, Stats>>(Yaml.ConfigAsset("enemies.yaml"));
            if (data == null)
                return;

            _stats = new Dictionary<EnemyType, Stats>(_defaults);
            foreach (var kv in data)
            {
                if (TryParseType(kv.Key, out var type) && kv.Value != null)
                    _stats[type] = kv.Value;
            }
        }

        private static bool TryParseType(string name, out EnemyType type)
        {
            switch (name.Trim().ToLowerInvariant())
            {
                case "blue": type = EnemyType.BLUE; return true;
                case "green": type = EnemyType.GREEN; return true;
                case "red": type = EnemyType.RED; return true;
                case "scout":
                case "smscout": type = EnemyType.SM_SCOUT; return true;
                case "boss": type = EnemyType.BOSS; return true;
                case "ukro": type = EnemyType.UKRO; return true;
                case "kamik":
                case "kamikaze": type = EnemyType.KAMIK; return true;
                case "heavy": type = EnemyType.HEAVY; return true;
                case "ukroboss": type = EnemyType.UKRO_BOSS; return true;
                default: type = EnemyType.RND; return false;
            }
        }
    }
}
