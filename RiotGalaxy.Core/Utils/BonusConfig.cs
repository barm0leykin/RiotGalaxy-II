using System.Collections.Generic;

namespace RiotGalaxy.Core.Utils
{
    /// <summary>
    /// Параметры бонусов из Content/Config/bonuses.yaml (фолбэк — дефолты в коде).
    /// </summary>
    public static class BonusConfig
    {
        public class BuffDef
        {
            public float Mult { get; set; } = 1.5f;
            public float Duration { get; set; } = 10f;
        }

        /// <summary>Порог серии убийств и множитель очков на нём.</summary>
        public class ComboTier
        {
            public int Kills { get; set; }
            public int Mult { get; set; } = 1;
        }

        /// <summary>Комбо-множитель очков: окно серии, сброс уроном и пороги.</summary>
        public class ComboDef
        {
            public float Window { get; set; } = 3.5f;
            public bool ResetOnDamage { get; set; } = true;
            public List<ComboTier> Tiers { get; set; } = new List<ComboTier>
            {
                new ComboTier { Kills = 5,  Mult = 2 },
                new ComboTier { Kills = 12, Mult = 3 },
                new ComboTier { Kills = 22, Mult = 4 },
                new ComboTier { Kills = 35, Mult = 5 },
            };
        }

        public class Data
        {
            public int HpUpAmount { get; set; } = 25;   // HP за BonusHpUp
            public int StarCredits { get; set; } = 10;   // кредиты за звезду-фолбэк (обычно звезда несёт enemy.Reward)

            // Временные баффы-подборы по id (power/rapid/speed).
            public Dictionary<string, BuffDef> Buffs { get; set; } = new Dictionary<string, BuffDef>
            {
                ["power"] = new BuffDef { Mult = 2.0f, Duration = 10f },
                ["rapid"] = new BuffDef { Mult = 1.6f, Duration = 10f },
                ["speed"] = new BuffDef { Mult = 1.4f, Duration = 10f },
            };

            public int BuffDropChance { get; set; } = 0; // % «фонового» случайного баффа с любого врага; 0 = только авторские дропы (drop: в YAML уровня)

            // Бонус-кредиты за прохождение уровня: base + perLevel * номер_уровня.
            public int LevelClearBonusBase { get; set; } = 25;
            public int LevelClearBonusPerLevel { get; set; } = 15;

            // Дробление награды на звёзды: номинал одной звезды и максимум звёзд за убийство.
            public int StarValue { get; set; } = 10;       // reward/StarValue = число звёзд
            public int MaxStarsPerKill { get; set; } = 30;

            // Сколько секунд после зачистки уровня дать на сбор звёзд перед переходом.
            public float LevelClearCollectSeconds { get; set; } = 2.5f;

            // Комбо-множитель очков за серию убийств (Managers.Combo).
            public ComboDef Combo { get; set; } = new ComboDef();
        }

        public static Data Current { get; private set; } = new Data();

        public static void Load()
        {
            var data = Yaml.LoadAsset<Data>(Yaml.ConfigAsset("bonuses.yaml"));
            if (data != null)
            {
                // Если в файле нет секции buffs/combo — сохраняем дефолтные (иначе обнулятся).
                if (data.Buffs == null || data.Buffs.Count == 0)
                    data.Buffs = Current.Buffs;
                if (data.Combo == null)
                    data.Combo = Current.Combo;
                else if (data.Combo.Tiers == null || data.Combo.Tiers.Count == 0)
                    data.Combo.Tiers = Current.Combo.Tiers;
                Current = data;
            }
        }

        /// <summary>Определение баффа по id (или null).</summary>
        public static BuffDef Buff(string id)
            => Current.Buffs != null && Current.Buffs.TryGetValue(id, out var b) ? b : null;
    }
}
