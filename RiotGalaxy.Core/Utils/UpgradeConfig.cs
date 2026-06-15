using System;
using System.Collections.Generic;

namespace RiotGalaxy.Core.Utils
{
    /// <summary>
    /// Определения постоянных апгрейдов из Content/Config/upgrades.yaml (магазин, этап 3).
    /// Купленные уровни хранятся в профиле ([[SaveData]].Upgrades). Эффект применяется к кораблю
    /// при старте партии (PlayerShip / Weapon / магнит). Дефолты в коде — игра работает без файла.
    /// </summary>
    public static class UpgradeConfig
    {
        public class Upgrade
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Desc { get; set; }
            public int MaxLevel { get; set; } = 1;
            public int BaseCost { get; set; } = 50;
            public float CostGrowth { get; set; } = 1.6f;
            public float PerLevel { get; set; }

            /// <summary>Цена следующего уровня при текущем уровне level (0-based). int.MaxValue если максимум.</summary>
            public int CostForNext(int level)
            {
                if (level >= MaxLevel) return int.MaxValue;
                return (int)Math.Round(BaseCost * Math.Pow(CostGrowth, level));
            }
        }

        private static readonly List<Upgrade> _defaults = new List<Upgrade>
        {
            new Upgrade { Id = "damage",   Name = "Урон",            Desc = "Снаряды бьют сильнее",        MaxLevel = 5, BaseCost = 250, CostGrowth = 1.6f, PerLevel = 0.15f },
            new Upgrade { Id = "firerate", Name = "Скорострельность", Desc = "Стреляешь чаще",              MaxLevel = 5, BaseCost = 250, CostGrowth = 1.6f, PerLevel = 0.12f },
            new Upgrade { Id = "maxhp",    Name = "Прочность",        Desc = "+HP к максимуму здоровья",    MaxLevel = 5, BaseCost = 200, CostGrowth = 1.5f, PerLevel = 10f },
            new Upgrade { Id = "speed",    Name = "Манёвренность",    Desc = "Корабль быстрее",             MaxLevel = 3, BaseCost = 300, CostGrowth = 1.7f, PerLevel = 0.10f },
            new Upgrade { Id = "magnet",   Name = "Магнит",           Desc = "Звёзды притягиваются дальше", MaxLevel = 3, BaseCost = 200, CostGrowth = 1.6f, PerLevel = 0.35f },
        };

        public static List<Upgrade> All { get; private set; } = _defaults;

        public static Upgrade Get(string id) => All.Find(u => u.Id == id);

        public static void Load()
        {
            var data = Yaml.LoadAsset<UpgradesYaml>(Yaml.ConfigAsset("upgrades.yaml"));
            if (data?.Upgrades != null && data.Upgrades.Count > 0)
                All = data.Upgrades;
        }

        // ── Применённый эффект (с учётом купленного уровня из SaveData) ──────
        /// <summary>Накопленный эффект апгрейда: perLevel * купленный_уровень (0, если нет).</summary>
        public static float Effect(string id)
        {
            var u = Get(id);
            return u == null ? 0f : u.PerLevel * SaveData.GetUpgradeLevel(id);
        }

        // Готовые множители/бонусы для применения к кораблю.
        public static int   MaxHpBonus    => (int)Effect("maxhp");
        public static float DamageMult    => 1f + Effect("damage");
        public static float FireRateMult  => 1f + Effect("firerate");
        public static float SpeedMult     => 1f + Effect("speed");
        public static float MagnetMult    => 1f + Effect("magnet");

        private class UpgradesYaml
        {
            public List<Upgrade> Upgrades { get; set; }
        }
    }
}
