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
        /// <summary>Сколько профилей-слотов доступно игроку.</summary>
        public const int ProfileCount = 3;

        /// <summary>Текущий выбранный профиль (1..ProfileCount).</summary>
        public static int CurrentProfile = 1;

        // Слот 1 хранится в legacy-файле save.yaml (совместимость со старыми сейвами),
        // слоты 2/3 — в save2.yaml / save3.yaml. Все в writable-каталоге рядом с приложением.
        private static string FilePathFor(int slot)
            => Path.Combine(AppContext.BaseDirectory, slot <= 1 ? "save.yaml" : $"save{slot}.yaml");
        private static string FilePath => FilePathFor(CurrentProfile);

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

        // ── Чекпоинт кампании (для «Продолжить» после выхода в меню) ──────────
        public static int CampaignMission = -1; // индекс миссии; -1 = чекпоинта нет
        public static int CampaignStep = 0;     // индекс шага (волны) в миссии
        public static int CampaignScore = 0;    // счёт на момент чекпоинта

        /// <summary>Есть ли сохранённая позиция кампании, с которой можно продолжить.</summary>
        public static bool HasCheckpoint => CampaignMission >= 0;

        /// <summary>Запомнить позицию кампании (миссия/волна/счёт) и сохранить профиль.</summary>
        public static void SetCheckpoint(int mission, int step, int score)
        {
            CampaignMission = mission;
            CampaignStep = step;
            CampaignScore = score;
            Save();
        }

        /// <summary>Сбросить чекпоинт (кампания пройдена или начата заново).</summary>
        public static void ClearCheckpoint()
        {
            CampaignMission = -1;
            CampaignStep = 0;
            CampaignScore = 0;
            Save();
        }

        public static void Load()
        {
            // Сначала сбрасываем к дефолтам — важно при переключении на пустой слот,
            // чтобы прогресс прошлого профиля не «протёк» в новый.
            HighScore = 0;
            MaxLevelReached = 1;
            Currency = 0;
            Upgrades = new Dictionary<string, int>();
            WeaponLevels = new Dictionary<string, int>();
            CampaignMission = -1;
            CampaignStep = 0;
            CampaignScore = 0;

            var data = Yaml.LoadFile<SaveYaml>(FilePath);
            if (data == null)
                return;
            HighScore = Math.Max(0, data.HighScore);
            MaxLevelReached = Math.Max(1, data.MaxLevelReached);
            Currency = Math.Max(0, data.Currency);
            Upgrades = data.Upgrades ?? new Dictionary<string, int>();
            WeaponLevels = data.WeaponLevels ?? new Dictionary<string, int>();
            CampaignMission = data.CampaignMission;
            CampaignStep = Math.Max(0, data.CampaignStep);
            CampaignScore = Math.Max(0, data.CampaignScore);
        }

        /// <summary>Выбрать профиль (слот 1..ProfileCount) и загрузить его прогресс.</summary>
        public static void SelectProfile(int slot)
        {
            CurrentProfile = Math.Clamp(slot, 1, ProfileCount);
            Load();
        }

        /// <summary>Есть ли сохранённый прогресс в слоте.</summary>
        public static bool ProfileExists(int slot) => File.Exists(FilePathFor(slot));

        /// <summary>Краткая сводка слота без смены текущего профиля (для экрана выбора).</summary>
        public static (bool exists, int high, int currency, int level) Peek(int slot)
        {
            var d = Yaml.LoadFile<SaveYaml>(FilePathFor(slot));
            if (d == null)
                return (false, 0, 0, 1);
            return (true, Math.Max(0, d.HighScore), Math.Max(0, d.Currency), Math.Max(1, d.MaxLevelReached));
        }

        /// <summary>Начать заново: стереть прогресс/рекорд/валюту слота (очки и кредиты обнуляются).</summary>
        public static void ResetProfile(int slot)
        {
            if (slot == CurrentProfile)
            {
                HighScore = 0;
                MaxLevelReached = 1;
                Currency = 0;
                Upgrades = new Dictionary<string, int>();
                WeaponLevels = new Dictionary<string, int>();
                Save();
            }
            else
            {
                try { File.WriteAllText(FilePathFor(slot), Yaml.Serializer.Serialize(new SaveYaml())); }
                catch (Exception ex) { Console.WriteLine($"=== Reset profile {slot} failed: {ex.Message} ==="); }
            }
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
                    CampaignMission = CampaignMission,
                    CampaignStep = CampaignStep,
                    CampaignScore = CampaignScore,
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
            public int CampaignMission { get; set; } = -1;
            public int CampaignStep { get; set; }
            public int CampaignScore { get; set; }
        }
    }
}
