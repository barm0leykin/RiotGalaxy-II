using System;
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
        public static int Currency;             // постоянная валюта (для магазина, этап 3)

        public static void Load()
        {
            var data = Yaml.LoadFile<SaveYaml>(FilePath);
            if (data == null)
                return;
            HighScore = Math.Max(0, data.HighScore);
            MaxLevelReached = Math.Max(1, data.MaxLevelReached);
            Currency = Math.Max(0, data.Currency);
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
        }
    }
}
