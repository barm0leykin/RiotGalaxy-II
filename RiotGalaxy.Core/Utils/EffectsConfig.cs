using System.Collections.Generic;

namespace RiotGalaxy.Utils
{
    /// <summary>
    /// Параметры визуальных эффектов и параллакс-фона из Content/Config/effects.yaml.
    /// Значения по умолчанию заданы здесь — игра работает и без файла (фолбэк).
    /// Образец data-driven-конфига: см. <see cref="GameOptions"/>.
    /// </summary>
    public static class EffectsConfig
    {
        /// <summary>Параметры «всплеска» частиц (взрыв/искра).</summary>
        public struct Burst
        {
            public int Count;
            public float Speed;
            public float Size;
            public float Life;
        }

        /// <summary>Параметры тряски экрана.</summary>
        public struct Shake
        {
            public float Magnitude;
            public float Duration;
        }

        /// <summary>Параметры одного слоя звёзд параллакса.</summary>
        public struct StarLayer
        {
            public int Count;
            public float Speed;
            public float SizeMin, SizeMax;
            public float BrightMin, BrightMax;
        }

        // ── Частицы ──────────────────────────────────────────────────────────
        public static Burst EnemyExplosion = new Burst { Count = 24, Speed = 220f, Size = 6f, Life = 0.55f };
        public static Burst BossExplosion  = new Burst { Count = 80, Speed = 320f, Size = 11f, Life = 0.9f };
        public static Burst HitSpark       = new Burst { Count = 6,  Speed = 140f, Size = 4f, Life = 0.25f };

        // ── Тряска экрана ────────────────────────────────────────────────────
        public static Shake EnemyDeathShake = new Shake { Magnitude = 4f,  Duration = 0.18f };
        public static Shake BossDeathShake  = new Shake { Magnitude = 14f, Duration = 0.5f };
        public static Shake NukeShake       = new Shake { Magnitude = 16f, Duration = 0.6f };
        public static Shake PlayerHitShake  = new Shake { Magnitude = 7f,  Duration = 0.25f };

        // ── Параллакс ────────────────────────────────────────────────────────
        // Дальний → ближний. Используется StarField при инициализации.
        public static List<StarLayer> StarLayers = new List<StarLayer>
        {
            new StarLayer { Count = 70, Speed = 18f, SizeMin = 1.0f, SizeMax = 2.0f, BrightMin = 0.25f, BrightMax = 0.5f },
            new StarLayer { Count = 45, Speed = 45f, SizeMin = 1.5f, SizeMax = 2.5f, BrightMin = 0.45f, BrightMax = 0.75f },
            new StarLayer { Count = 22, Speed = 95f, SizeMin = 2.0f, SizeMax = 3.5f, BrightMin = 0.7f,  BrightMax = 1.0f },
        };

        public static void Load()
        {
            var data = Yaml.LoadAsset<EffectsYaml>(Yaml.ConfigAsset("effects.yaml"));
            if (data == null)
                return;

            if (data.Particles != null)
            {
                ApplyBurst(ref EnemyExplosion, data.Particles.EnemyExplosion);
                ApplyBurst(ref BossExplosion,  data.Particles.BossExplosion);
                ApplyBurst(ref HitSpark,       data.Particles.HitSpark);
            }

            if (data.ScreenShake != null)
            {
                ApplyShake(ref EnemyDeathShake, data.ScreenShake.EnemyDeath);
                ApplyShake(ref BossDeathShake,  data.ScreenShake.BossDeath);
                ApplyShake(ref NukeShake,       data.ScreenShake.Nuke);
                ApplyShake(ref PlayerHitShake,  data.ScreenShake.PlayerHit);
            }

            if (data.StarField?.Layers != null && data.StarField.Layers.Count > 0)
            {
                var layers = new List<StarLayer>();
                foreach (var l in data.StarField.Layers)
                {
                    if (l == null) continue;
                    layers.Add(new StarLayer
                    {
                        Count = l.Count > 0 ? l.Count : 1,
                        Speed = l.Speed,
                        SizeMin = l.SizeMin > 0 ? l.SizeMin : 1f,
                        SizeMax = l.SizeMax > 0 ? l.SizeMax : 2f,
                        BrightMin = l.BrightMin,
                        BrightMax = l.BrightMax > 0 ? l.BrightMax : 1f,
                    });
                }
                if (layers.Count > 0)
                    StarLayers = layers;
            }
        }

        // Перекрываем поля Burst только положительными значениями (0/нет — оставляем дефолт).
        private static void ApplyBurst(ref Burst target, BurstYaml y)
        {
            if (y == null) return;
            if (y.Count > 0) target.Count = y.Count;
            if (y.Speed > 0) target.Speed = y.Speed;
            if (y.Size > 0)  target.Size  = y.Size;
            if (y.Life > 0)  target.Life  = y.Life;
        }

        private static void ApplyShake(ref Shake target, ShakeYaml y)
        {
            if (y == null) return;
            if (y.Magnitude > 0) target.Magnitude = y.Magnitude;
            if (y.Duration > 0)  target.Duration  = y.Duration;
        }

        // ── POCO под структуру effects.yaml (camelCase) ──────────────────────
        private class EffectsYaml
        {
            public ParticlesYaml Particles { get; set; }
            public ScreenShakeYaml ScreenShake { get; set; }
            public StarFieldYaml StarField { get; set; }
        }
        private class ParticlesYaml
        {
            public BurstYaml EnemyExplosion { get; set; }
            public BurstYaml BossExplosion { get; set; }
            public BurstYaml HitSpark { get; set; }
        }
        private class BurstYaml
        {
            public int Count { get; set; }
            public float Speed { get; set; }
            public float Size { get; set; }
            public float Life { get; set; }
        }
        private class ScreenShakeYaml
        {
            public ShakeYaml EnemyDeath { get; set; }
            public ShakeYaml BossDeath { get; set; }
            public ShakeYaml Nuke { get; set; }
            public ShakeYaml PlayerHit { get; set; }
        }
        private class ShakeYaml
        {
            public float Magnitude { get; set; }
            public float Duration { get; set; }
        }
        private class StarFieldYaml
        {
            public List<StarLayerYaml> Layers { get; set; }
        }
        private class StarLayerYaml
        {
            public int Count { get; set; }
            public float Speed { get; set; }
            public float SizeMin { get; set; }
            public float SizeMax { get; set; }
            public float BrightMin { get; set; }
            public float BrightMax { get; set; }
        }
    }
}
