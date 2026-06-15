using System;
using System.Collections.Generic;
using System.IO;

namespace RiotGalaxy.Core.Utils
{
    /// <summary>
    /// Профиль игрока на диске (save.yaml рядом с приложением; на Android — writable-каталог,
    /// тот же путь, что у settings.yaml). Хранит прогресс/рекорд/валюту. Загружается при старте,
    /// сохраняется в ключевых точках (конец партии, начало уровня).
    ///
    /// Расширяется по мере фич: валюта (этап 3 — магазин), купленные апгрейды и т.п.
    /// Игра работает и без файла (дефолты).
    /// </summary>
    public static class SaveData
    {
        private static string FilePath => Path.Combine(AppContext.BaseDirectory, "save.yaml");

        // ── Сохраняемое состояние ────────────────────────────────────────────
        public static int HighScore;            // рекорд очков
        public static int MaxLevelReached = 1;  // самый дальний достигнутый уровень
        public static int Currency;             // постоянная валюта (тратится в магазине)

        // Купленные уровни апгрейдов: id → уровень (см. UpgradeConfig).
        public static Dictionary<string, int> Upgrades = new Dictionary<string, int>();

        public static int GetUpgradeLevel(string id)
            => Upgrades.TryGetValue(id, out var lvl) ? lvl : 0;

        public static void SetUpgradeLevel(string id, int level)
            => Upgrades[id] = level;

        // Оружие: id → уровень. 0 = не открыто; 1..Max = открыто на этом уровне (см. WeaponConfig).
        public static Dictionary<string, int> WeaponLevels = new Dictionary<string, int>();

        public static int GetWeaponLevel(string id)
            => WeaponLevels.TryGetValue(id, out var lvl) ? lvl : 0;

        public static bool IsWeaponOwned(string id) => GetWeaponLevel(id) >= 1;

        public static void SetWeaponLevel(string id, int level) => WeaponLevels[id] = level;

        public static void Load()
        {
            var data = Yaml.LoadFile<SaveYaml>(FilePath);
            if (data == null)
                return;
            HighScore = Math.Max(0, data.HighScore);
            MaxLevelReached = Math.Max(1, data.MaxLevelReached);
            Currency = Math.Max(0, data.Currency);
            Upgrades = data.Upgrades ?? new Dictionary<string, int>();
            WeaponLevels = data.WeaponLevels ?? new Dictionary<string, int>();
        }

        public static void Save()
        {
            try
            {
                var data = new SaveYaml
                {
                    HighScore = HighScore,
                    MaxLevelReached = MaxLevelReached,
                    Currency = Currency,
                    Upgrades = Upgrades,
                    WeaponLevels = WeaponLevels,
                };
                File.WriteAllText(FilePath, Yaml.Serializer.Serialize(data));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== Save failed: {ex.Message} ===");
            }
        }

        /// <summary>Обновить рекорд, если результат лучше. Возвращает true, если это новый рекорд.</summary>
        public static bool ReportScore(int score)
        {
            if (score <= HighScore)
                return false;
            HighScore = score;
            return true;
        }

        /// <summary>Отметить достигнутый уровень (самый дальний).</summary>
        public static void ReportLevelReached(int level)
        {
            if (level > MaxLevelReached)
                MaxLevelReached = level;
        }

        // POCO для save.yaml (camelCase)
        public class SaveYaml
        {
            public int HighScore { get; set; }
            public int MaxLevelReached { get; set; } = 1;
            public int Currency { get; set; }
            public Dictionary<string, int> Upgrades { get; set; }
            public Dictionary<string, int> WeaponLevels { get; set; }
        }
    }
}
