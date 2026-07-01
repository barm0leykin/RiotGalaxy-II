using System;
using System.Collections.Generic;
using RiotGalaxy.Core.Utils;

namespace RiotGalaxy.Core.Weapons
{
    /// <summary>
    /// Описание одного оружия (data-driven). Поведение задаётся данными, а не подклассами:
    /// спрайт, пробивание, разброс (jitter), веер (fan), параметры по уровням, цена разблокировки
    /// и улучшений. Стартовое оружие — с UnlockCost = 0.
    /// </summary>
    public class WeaponDef
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Sprite { get; set; } = "Images/wpn_bullet";
        public string Icon { get; set; } = "Images/btn_cannon"; // иконка кнопки/магазина
        public string Key { get; set; } = "";                   // клавиша десктопа (имя из Keys: D1..D5)
        public bool Piercing { get; set; }                      // снаряд летит насквозь (лазер)
        public int JitterDeg { get; set; }                      // разброс ±град (пулемёт); 0 — нет
        public int FanCount { get; set; } = 1;                  // снарядов в веере на 1-м уровне (>1 — веер)
        public int FanPerLevel { get; set; }                    // +снарядов веера за уровень
        public float FanStepDeg { get; set; } = 12f;            // угол между снарядами веера
        public int UnlockCost { get; set; }                     // цена разблокировки (0 — доступно с начала)
        public int BaseCost { get; set; } = 60;                 // цена улучшения ур.1→2
        public float CostGrowth { get; set; } = 1.6f;
        public List<WeaponOptions> Levels { get; set; } = new List<WeaponOptions>();

        public int MaxLevel => Levels.Count; // число уровней (1..MaxLevel)

        /// <summary>
        /// Цена следующей покупки при текущем (1-based) уровне: 0 → разблокировка (UnlockCost),
        /// иначе улучшение до curLevel+1 (BaseCost·CostGrowth^(curLevel-1)). int.MaxValue если максимум.
        /// </summary>
        public int CostForLevel(int curLevel)
        {
            if (curLevel <= 0) return UnlockCost;
            if (curLevel >= MaxLevel) return int.MaxValue;
            return (int)Math.Round(BaseCost * Math.Pow(CostGrowth, curLevel - 1));
        }

        /// <summary>Параметры для текущего (1-based) уровня.</summary>
        public WeaponOptions OptionsForLevel(int curLevel)
        {
            if (Levels.Count == 0) return new WeaponOptions(1, 0.25f, 1f, 10f, 250f);
            int idx = Math.Clamp(curLevel - 1, 0, Levels.Count - 1);
            return Levels[idx];
        }
    }

    /// <summary>
    /// Реестр оружия и магнит. Грузится из Content/Config/weapons.yaml (Load()).
    /// Дефолты в коде — игра работает и без файла.
    /// </summary>
    public static class WeaponConfig
    {
        /// <summary>Магнит корабля — притяжение звёзд-очков (оборудование, из weapons.yaml).</summary>
        public class MagnetOptions
        {
            public float Radius { get; set; } = 250f;
            public float PullSpeed { get; set; } = 300f;
            public float TurnSpeed { get; set; } = 360f;
        }
        public static MagnetOptions Magnet = new MagnetOptions();

        public static List<WeaponDef> All { get; private set; } = Defaults();

        public static WeaponDef Get(string id) => All.Find(w => w.Id == id);

        /// <summary>Стартовое оружие (первое с UnlockCost == 0) — обычно «blaster».</summary>
        public static WeaponDef Starter => All.Find(w => w.UnlockCost <= 0) ?? (All.Count > 0 ? All[0] : null);

        private static List<WeaponDef> Defaults() => new List<WeaponDef>
        {
            // Бластер — стартовый: слабый, но очень скорострельный (не скучно с первой секунды).
            new WeaponDef
            {
                Id = "blaster", Name = "Бластер", Sprite = "Images/wpn_blaster", Icon = "Images/btn_auto_cannon",
                Key = "D1", UnlockCost = 0, BaseCost = 200, CostGrowth = 1.6f,
                Levels =
                {
                    new WeaponOptions(1, 0.1f, 0.14f, 3f, 360f),
                    new WeaponOptions(1, 0.1f, 0.12f, 4f, 380f),
                    new WeaponOptions(2, 0.08f, 0.18f, 4f, 400f),
                },
            },
            // Пушка — медленно, но больно.
            new WeaponDef
            {
                Id = "cannon", Name = "Пушка", Sprite = "Images/wpn_bullet", Icon = "Images/btn_cannon",
                Key = "D2", UnlockCost = 400, BaseCost = 350, CostGrowth = 1.6f,
                Levels =
                {
                    new WeaponOptions(1, 0.25f, 0.8f, 16f, 280f),
                    new WeaponOptions(1, 0.25f, 0.65f, 20f, 300f),
                    new WeaponOptions(2, 0.2f, 0.9f, 22f, 320f),
                },
            },
            // Пулемёт — очереди слабых снарядов с разбросом.
            new WeaponDef
            {
                Id = "minigun", Name = "Пулемёт", Sprite = "Images/wpn_slug", Icon = "Images/btn_minigun",
                Key = "D3", JitterDeg = 6, UnlockCost = 600, BaseCost = 350, CostGrowth = 1.6f,
                Levels =
                {
                    new WeaponOptions(3, 0.08f, 0.7f, 4f, 320f),
                    new WeaponOptions(4, 0.08f, 0.6f, 4f, 340f),
                    new WeaponOptions(5, 0.07f, 0.55f, 5f, 360f),
                },
            },
            // Лазер — пробивающий.
            new WeaponDef
            {
                Id = "laser", Name = "Лазер", Sprite = "Images/wpn_laser", Icon = "Images/btn_laser",
                Key = "D4", Piercing = true, UnlockCost = 1000, BaseCost = 450, CostGrowth = 1.6f,
                Levels =
                {
                    new WeaponOptions(1, 0.25f, 0.6f, 10f, 460f),
                    new WeaponOptions(1, 0.2f, 0.5f, 13f, 480f),
                    new WeaponOptions(2, 0.2f, 0.55f, 14f, 500f),
                },
            },
            // Разлёт — веер снарядов (число растёт с уровнем).
            new WeaponDef
            {
                Id = "spread", Name = "Разлёт", Sprite = "Images/wpn_bullet", Icon = "Images/btn_BulletUp",
                Key = "D5", FanCount = 3, FanPerLevel = 1, FanStepDeg = 12f,
                UnlockCost = 750, BaseCost = 450, CostGrowth = 1.6f,
                Levels =
                {
                    new WeaponOptions(1, 0.25f, 0.7f, 6f, 280f),
                    new WeaponOptions(1, 0.25f, 0.6f, 7f, 300f),
                    new WeaponOptions(1, 0.25f, 0.55f, 7f, 320f),
                },
            },
        };

        public static void Load()
        {
            var data = Yaml.LoadAsset<WeaponsYaml>(Yaml.ConfigAsset("weapons.yaml"));
            if (data == null) return;

            if (data.Weapons != null && data.Weapons.Count > 0)
            {
                var list = new List<WeaponDef>();
                foreach (var w in data.Weapons)
                {
                    if (w == null || string.IsNullOrEmpty(w.Id)) continue;
                    list.Add(new WeaponDef
                    {
                        Id = w.Id, Name = w.Name, Sprite = w.Sprite ?? "Images/wpn_bullet",
                        Icon = w.Icon ?? "Images/btn_cannon", Key = w.Key ?? "",
                        Piercing = w.Piercing, JitterDeg = w.JitterDeg,
                        FanCount = w.FanCount > 0 ? w.FanCount : 1, FanPerLevel = w.FanPerLevel,
                        FanStepDeg = w.FanStepDeg > 0 ? w.FanStepDeg : 12f,
                        UnlockCost = w.UnlockCost, BaseCost = w.BaseCost > 0 ? w.BaseCost : 60,
                        CostGrowth = w.CostGrowth > 0 ? w.CostGrowth : 1.6f,
                        Levels = ConvertLevels(w.Levels),
                    });
                }
                if (list.Count > 0) All = list;
            }

            if (data.Magnet != null)
            {
                if (data.Magnet.Radius > 0) Magnet.Radius = data.Magnet.Radius;
                if (data.Magnet.PullSpeed > 0) Magnet.PullSpeed = data.Magnet.PullSpeed;
                if (data.Magnet.TurnSpeed > 0) Magnet.TurnSpeed = data.Magnet.TurnSpeed;
            }
        }

        private static List<WeaponOptions> ConvertLevels(List<WeaponLevelYaml> levels)
        {
            var arr = new List<WeaponOptions>();
            if (levels == null) return arr;
            foreach (var l in levels)
                arr.Add(new WeaponOptions(l.Burst, l.BurstInterval, l.ReloadSpeed, l.Damage, l.ShellSpeed));
            return arr;
        }

        // POCO под структуру weapons.yaml
        private class WeaponsYaml
        {
            public List<WeaponYaml> Weapons { get; set; }
            public MagnetYaml Magnet { get; set; }
        }
        private class WeaponYaml
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Sprite { get; set; }
            public string Icon { get; set; }
            public string Key { get; set; }
            public bool Piercing { get; set; }
            public int JitterDeg { get; set; }
            public int FanCount { get; set; }
            public int FanPerLevel { get; set; }
            public float FanStepDeg { get; set; }
            public int UnlockCost { get; set; }
            public int BaseCost { get; set; }
            public float CostGrowth { get; set; }
            public List<WeaponLevelYaml> Levels { get; set; }
        }
        private class MagnetYaml
        {
            public float Radius { get; set; }
            public float PullSpeed { get; set; }
            public float TurnSpeed { get; set; }
        }
        private class WeaponLevelYaml
        {
            public int Burst { get; set; }
            public float BurstInterval { get; set; }
            public float ReloadSpeed { get; set; }
            public float Damage { get; set; }
            public float ShellSpeed { get; set; }
        }
    }
}
