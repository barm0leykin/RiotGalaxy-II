using System.Collections.Generic;

namespace RiotGalaxy.Core.Utils
{
    /// <summary>
    /// Параметры ИИ врагов и боссов из Content/Config/ai.yaml (раньше — константы в коде).
    /// Дефолты в коде = прежние значения; игра работает без файла. «0/пусто → оставить дефолт»,
    /// по образцу остальных *Config.
    /// </summary>
    public static class AiConfig
    {
        // ── Общее (EnemyAI) ──────────────────────────────────────────────────
        /// <summary>Зона роения: враг «влетел», когда прошёл эту долю высоты экрана сверху.</summary>
        public static float SwarmZoneFrac = 0.3f;
        /// <summary>Период «новой идеи» состояния (смена курса и т.п.), сек.</summary>
        public static float IdeaInterval = 3f;

        // ── Состояния (AIState) ─────────────────────────────────────────────
        public static float TakeoffCourseMinDeg = 135f;   // влёт: курс вниз 135..225
        public static float TakeoffCourseMaxDeg = 225f;
        public static float SwarmingSpeedFrac = 0.2f;     // роение: доля MaxSpeed (было /5)
        public static float AttackCourseMinDeg = 155f;    // атака: курс вниз 155..205
        public static float AttackCourseMaxDeg = 205f;

        // ── Красный / синий контроллеры ─────────────────────────────────────
        public static float RedSwarmShootInterval = 6f;
        public static float BlueSwarmShootInterval = 8f;
        public static float BlueAttackShootInterval = 3f;
        /// <summary>Шанс (0..1) сорваться из роения в атаку на каждой «идее» (было 1 из 5).</summary>
        public static float BlueAttackChance = 0.2f;

        // ── Вылеты из улья (SortieMovement) ─────────────────────────────────
        // Доли (…Frac) — от attackSpeed врага; амплитуды/радиусы — пиксели.
        public class SortieParams
        {
            public float MaxDiveTime = 9f;          // страховка: сек в пике до возврата
            public float SnakeAmplitude = 100f;
            public float SnakeDescendFrac = 0.85f;
            public float SnakeWaveSpeedFrac = 0.5f;
            public float EllipseRadius = 110f;
            public float EllipseDescendFrac = 0.3f;
            public float ZigzagAmplitude = 90f;
            public float ZigzagDescendFrac = 0.85f;
            public float ZigzagWaveSpeedFrac = 0.6f;
            public float SpiralStartRadius = 34f;
            public float SpiralRadiusGrow = 65f;
            public float SpiralOmegaDivisor = 85f;
            public float SpiralDescendFrac = 0.55f;
            public float SwoopSideFrac = 0.7f;
            public float SwoopDownFrac = 0.4f;
            public float SwoopAccelFrac = 1.3f;
            public float StrafeTurnYFrac = 0.42f;
            public float HomingTurnSpeed = 2.2f;
            public float BoomerangSideFrac = 0.35f;
            public float BoomerangDownFrac = 0.9f;
            public float BoomerangBrakeFrac = 1.15f;
            public float RandomCourseMinDeg = 155f;
            public float RandomCourseMaxDeg = 205f;
        }
        public static SortieParams Sortie = new SortieParams();

        // ── Улей (Hive) ─────────────────────────────────────────────────────
        public class HiveParams
        {
            public float SwayAmplitude = 70f;        // качание улья по X, пикс
            public float SwaySpeed = 1.0f;           // рад/сек
            public float SettleRadius = 50f;         // «осел» возле ячейки — готов к вылету
            public float DefaultSortieInterval = 4f; // если бой не задал sortieInterval/Count
            public int DefaultSortieCount = 1;
        }
        public static HiveParams Hive = new HiveParams();

        // ── Боссы (BossAI) ───────────────────────────────────────────────────
        // Каждый босс — свой конфиг (bosses.<тип> в ai.yaml), незаданное берётся из bossDefault.
        // Фазы — список от полного HP к низкому; у каждой свои атаки/темп/оружие/подмога.

        /// <summary>Один паттерн стрельбы в залпе фазы.</summary>
        public class AttackDef
        {
            public string Type = "aimedShot"; // aimedBurst | aimedFan | fanDown | radial | spiral | weapon | aimedShot
            public int Count = 1;             // снарядов в паттерне
            public float SpreadDeg = 10f;     // aimedBurst/aimedFan/spiral: шаг между снарядами; fanDown: полная ширина
            public float ShotDelay;           // >0 → «волна»: снаряды выходят поочерёдно с этим интервалом (сек)
            public string WeaponId;           // type: weapon — id из weapons.yaml (minigun/laser/spread/...)
        }

        /// <summary>Фаза босса: активна, пока HpFrac > HpAbove (список фаз — от сильного HP к слабому).</summary>
        public class PhaseDef
        {
            public float HpAbove;                  // порог: фаза активна при hpFrac > HpAbove
            public float AttackInterval = 2f;      // пауза между залпами, сек
            public float SweepSpeed = 1f;          // скорость паттерна движения (частота синуса/обхода)
            public string Movement = "sweep";      // sweep | static | figure8 | circle | roam
            public List<AttackDef> Attacks = new List<AttackDef>(); // паттерны одного залпа (все разом)
            public float ShellSpeed;               // >0 → «оружие фазы»: скорость снаряда
            public float ShellDamage;              // >0 → урон снаряда
            public int AddsCount;                  // >0 → подмога при входе в фазу
            public string AddsType = "scout";      // тип подмоги (имя из enemies.yaml)
        }

        /// <summary>Полный конфиг босса: движение + телеграф + список фаз.</summary>
        public class BossDef
        {
            public float HoverYFrac = 0.18f;      // точка зависания, доля высоты экрана
            public float CenterXFrac = 0.5f;      // центр свипа, доля ширины
            public float SweepAmpFrac = 0.32f;    // амплитуда свипа, доля ширины
            public float EntrySpeed = 90f;        // скорость влёта, пикс/сек
            public float FirstAttackDelay = 1.2f; // пауза перед первой атакой, сек
            public float TelegraphTime = 0.6f;    // вспышка перед залпом, сек
            public float PhaseAttackDelay = 0.6f; // пауза до атаки после смены фазы
            public float BobAmplitude = 18f;      // вертикальное покачивание, пикс
            public float BobSpeed = 0.6f;         // частота покачивания
            public float AddsOffsetX = 70f;       // разлёт подмоги от центра босса
            public float MoveSpeed = 280f;        // макс. скорость перелёта к точке паттерна, пикс/сек
            public float VertAmpFrac = 0.08f;     // вертикальная амплитуда figure8/circle/roam (доля высоты)
            public List<PhaseDef> Phases = DefaultPhases();

            /// <summary>Классические 3 фазы (прежнее захардкоженное поведение).</summary>
            public static List<PhaseDef> DefaultPhases() => new List<PhaseDef>
            {
                new PhaseDef { HpAbove = 0.66f, AttackInterval = 2.2f, SweepSpeed = 0.8f,
                    Attacks = { new AttackDef { Type = "aimedBurst", Count = 3, SpreadDeg = 10f } } },
                new PhaseDef { HpAbove = 0.33f, AttackInterval = 1.8f, SweepSpeed = 1.2f,
                    Attacks = { new AttackDef { Type = "fanDown", Count = 7, SpreadDeg = 110f },
                                new AttackDef { Type = "aimedShot" } } },
                new PhaseDef { HpAbove = 0f, AttackInterval = 1.4f, SweepSpeed = 1.7f, AddsCount = 2,
                    Attacks = { new AttackDef { Type = "radial", Count = 12 } } },
            };
        }

        /// <summary>Дефолтный босс (bossDefault в yaml) — база для всех.</summary>
        public static BossDef BossDefault = new BossDef();

        // Пер-босс конфиги: ключ — имя типа врага (boss/ukroboss/trapp/reaper/overmind).
        private static Dictionary<string, BossDef> _bosses = new Dictionary<string, BossDef>();

        /// <summary>Конфиг босса по типу врага; нет персонального — дефолтный.</summary>
        public static BossDef GetBoss(GameObjects.EnemyType type)
        {
            string key = type.ToString().ToLowerInvariant().Replace("_", ""); // UKRO_BOSS → ukroboss
            return _bosses.TryGetValue(key, out var def) ? def : BossDefault;
        }

        public static void Load()
        {
            var d = Yaml.LoadAsset<AiYaml>(Yaml.ConfigAsset("ai.yaml"));
            if (d == null)
            {
                Log.Debug("AiConfig: ai.yaml не найден — использую дефолты");
                return;
            }

            if (d.Common != null)
            {
                if (d.Common.SwarmZoneFrac > 0f) SwarmZoneFrac = d.Common.SwarmZoneFrac;
                if (d.Common.IdeaInterval > 0f) IdeaInterval = d.Common.IdeaInterval;
            }
            if (d.States != null)
            {
                if (d.States.Takeoff != null)
                {
                    if (d.States.Takeoff.CourseMinDeg > 0f) TakeoffCourseMinDeg = d.States.Takeoff.CourseMinDeg;
                    if (d.States.Takeoff.CourseMaxDeg > 0f) TakeoffCourseMaxDeg = d.States.Takeoff.CourseMaxDeg;
                }
                if (d.States.Swarming != null && d.States.Swarming.SpeedFrac > 0f)
                    SwarmingSpeedFrac = d.States.Swarming.SpeedFrac;
                if (d.States.Attack != null)
                {
                    if (d.States.Attack.CourseMinDeg > 0f) AttackCourseMinDeg = d.States.Attack.CourseMinDeg;
                    if (d.States.Attack.CourseMaxDeg > 0f) AttackCourseMaxDeg = d.States.Attack.CourseMaxDeg;
                }
            }
            if (d.Red != null && d.Red.SwarmShootInterval > 0f)
                RedSwarmShootInterval = d.Red.SwarmShootInterval;
            if (d.Blue != null)
            {
                if (d.Blue.SwarmShootInterval > 0f) BlueSwarmShootInterval = d.Blue.SwarmShootInterval;
                if (d.Blue.AttackShootInterval > 0f) BlueAttackShootInterval = d.Blue.AttackShootInterval;
                if (d.Blue.AttackChance > 0f) BlueAttackChance = d.Blue.AttackChance;
            }
            if (d.Sortie != null)
            {
                var y = d.Sortie;
                if (y.MaxDiveTime > 0f) Sortie.MaxDiveTime = y.MaxDiveTime;
                Apply2(y.Snake, ref Sortie.SnakeAmplitude, ref Sortie.SnakeDescendFrac, ref Sortie.SnakeWaveSpeedFrac);
                if (y.Ellipse != null)
                {
                    if (y.Ellipse.Radius > 0f) Sortie.EllipseRadius = y.Ellipse.Radius;
                    if (y.Ellipse.DescendFrac > 0f) Sortie.EllipseDescendFrac = y.Ellipse.DescendFrac;
                }
                Apply2(y.Zigzag, ref Sortie.ZigzagAmplitude, ref Sortie.ZigzagDescendFrac, ref Sortie.ZigzagWaveSpeedFrac);
                if (y.Spiral != null)
                {
                    if (y.Spiral.StartRadius > 0f) Sortie.SpiralStartRadius = y.Spiral.StartRadius;
                    if (y.Spiral.RadiusGrow > 0f) Sortie.SpiralRadiusGrow = y.Spiral.RadiusGrow;
                    if (y.Spiral.OmegaDivisor > 0f) Sortie.SpiralOmegaDivisor = y.Spiral.OmegaDivisor;
                    if (y.Spiral.DescendFrac > 0f) Sortie.SpiralDescendFrac = y.Spiral.DescendFrac;
                }
                if (y.Swoop != null)
                {
                    if (y.Swoop.SideFrac > 0f) Sortie.SwoopSideFrac = y.Swoop.SideFrac;
                    if (y.Swoop.DownFrac > 0f) Sortie.SwoopDownFrac = y.Swoop.DownFrac;
                    if (y.Swoop.AccelFrac > 0f) Sortie.SwoopAccelFrac = y.Swoop.AccelFrac;
                }
                if (y.Strafe != null && y.Strafe.TurnYFrac > 0f) Sortie.StrafeTurnYFrac = y.Strafe.TurnYFrac;
                if (y.Homing != null && y.Homing.TurnSpeed > 0f) Sortie.HomingTurnSpeed = y.Homing.TurnSpeed;
                if (y.Boomerang != null)
                {
                    if (y.Boomerang.SideFrac > 0f) Sortie.BoomerangSideFrac = y.Boomerang.SideFrac;
                    if (y.Boomerang.DownFrac > 0f) Sortie.BoomerangDownFrac = y.Boomerang.DownFrac;
                    if (y.Boomerang.BrakeFrac > 0f) Sortie.BoomerangBrakeFrac = y.Boomerang.BrakeFrac;
                }
                if (y.Random != null)
                {
                    if (y.Random.CourseMinDeg > 0f) Sortie.RandomCourseMinDeg = y.Random.CourseMinDeg;
                    if (y.Random.CourseMaxDeg > 0f) Sortie.RandomCourseMaxDeg = y.Random.CourseMaxDeg;
                }
            }
            if (d.Hive != null)
            {
                if (d.Hive.SwayAmplitude > 0f) Hive.SwayAmplitude = d.Hive.SwayAmplitude;
                if (d.Hive.SwaySpeed > 0f) Hive.SwaySpeed = d.Hive.SwaySpeed;
                if (d.Hive.SettleRadius > 0f) Hive.SettleRadius = d.Hive.SettleRadius;
                if (d.Hive.DefaultSortieInterval > 0f) Hive.DefaultSortieInterval = d.Hive.DefaultSortieInterval;
                if (d.Hive.DefaultSortieCount > 0) Hive.DefaultSortieCount = d.Hive.DefaultSortieCount;
            }
            if (d.BossDefault != null)
                BossDefault = ConvertBoss(d.BossDefault, new BossDef());

            _bosses = new Dictionary<string, BossDef>();
            if (d.Bosses != null)
                foreach (var kv in d.Bosses)
                    if (kv.Value != null && !string.IsNullOrEmpty(kv.Key))
                        // База пер-босса — копия дефолта (незаданные поля наследуются).
                        _bosses[kv.Key.Trim().ToLowerInvariant()] = ConvertBoss(kv.Value, CloneBoss(BossDefault));

            Log.Debug($"AiConfig loaded (ai.yaml): боссов с личным конфигом — {_bosses.Count}");
        }

        /// <summary>Наложить yaml-поля боссa на базу (>0 → переопределить; фазы — целиком, если заданы).</summary>
        private static BossDef ConvertBoss(BossYaml y, BossDef b)
        {
            if (y.HoverYFrac > 0f) b.HoverYFrac = y.HoverYFrac;
            if (y.CenterXFrac > 0f) b.CenterXFrac = y.CenterXFrac;
            if (y.SweepAmpFrac > 0f) b.SweepAmpFrac = y.SweepAmpFrac;
            if (y.EntrySpeed > 0f) b.EntrySpeed = y.EntrySpeed;
            if (y.FirstAttackDelay > 0f) b.FirstAttackDelay = y.FirstAttackDelay;
            if (y.TelegraphTime > 0f) b.TelegraphTime = y.TelegraphTime;
            if (y.PhaseAttackDelay > 0f) b.PhaseAttackDelay = y.PhaseAttackDelay;
            if (y.BobAmplitude > 0f) b.BobAmplitude = y.BobAmplitude;
            if (y.BobSpeed > 0f) b.BobSpeed = y.BobSpeed;
            if (y.AddsOffsetX > 0f) b.AddsOffsetX = y.AddsOffsetX;
            if (y.MoveSpeed > 0f) b.MoveSpeed = y.MoveSpeed;
            if (y.VertAmpFrac > 0f) b.VertAmpFrac = y.VertAmpFrac;
            if (y.Phases != null && y.Phases.Count > 0)
            {
                b.Phases = new List<PhaseDef>();
                foreach (var p in y.Phases)
                {
                    if (p == null) continue;
                    var phase = new PhaseDef
                    {
                        HpAbove = p.HpAbove,
                        AttackInterval = p.AttackInterval > 0f ? p.AttackInterval : 2f,
                        SweepSpeed = p.SweepSpeed > 0f ? p.SweepSpeed : 1f,
                        Movement = string.IsNullOrEmpty(p.Movement) ? "sweep" : p.Movement,
                        ShellSpeed = p.ShellSpeed,
                        ShellDamage = p.ShellDamage,
                        AddsCount = p.AddsCount,
                        AddsType = string.IsNullOrEmpty(p.AddsType) ? "scout" : p.AddsType,
                    };
                    if (p.Attacks != null)
                        foreach (var a in p.Attacks)
                            if (a != null && !string.IsNullOrEmpty(a.Type))
                                phase.Attacks.Add(new AttackDef
                                {
                                    Type = a.Type,
                                    Count = a.Count > 0 ? a.Count : 1,
                                    SpreadDeg = a.SpreadDeg > 0f ? a.SpreadDeg : 10f,
                                    ShotDelay = a.ShotDelay,
                                    WeaponId = a.WeaponId,
                                });
                    if (phase.Attacks.Count == 0)
                        phase.Attacks.Add(new AttackDef()); // хотя бы одиночный прицельный
                    b.Phases.Add(phase);
                }
            }
            return b;
        }

        /// <summary>Копия конфига босса (фазы копируются по значению — база не мутируется).</summary>
        private static BossDef CloneBoss(BossDef src)
        {
            var b = new BossDef
            {
                HoverYFrac = src.HoverYFrac, CenterXFrac = src.CenterXFrac, SweepAmpFrac = src.SweepAmpFrac,
                EntrySpeed = src.EntrySpeed, FirstAttackDelay = src.FirstAttackDelay,
                TelegraphTime = src.TelegraphTime, PhaseAttackDelay = src.PhaseAttackDelay,
                BobAmplitude = src.BobAmplitude, BobSpeed = src.BobSpeed, AddsOffsetX = src.AddsOffsetX,
                MoveSpeed = src.MoveSpeed, VertAmpFrac = src.VertAmpFrac,
                Phases = new List<PhaseDef>(),
            };
            foreach (var p in src.Phases)
            {
                var phase = new PhaseDef
                {
                    HpAbove = p.HpAbove, AttackInterval = p.AttackInterval, SweepSpeed = p.SweepSpeed,
                    Movement = p.Movement,
                    ShellSpeed = p.ShellSpeed, ShellDamage = p.ShellDamage,
                    AddsCount = p.AddsCount, AddsType = p.AddsType,
                };
                foreach (var a in p.Attacks)
                    phase.Attacks.Add(new AttackDef
                    {
                        Type = a.Type, Count = a.Count, SpreadDeg = a.SpreadDeg,
                        ShotDelay = a.ShotDelay, WeaponId = a.WeaponId,
                    });
                b.Phases.Add(phase);
            }
            return b;
        }

        /// <summary>Хелпер: применить тройку «амплитуда/спуск/частота» волновой тактики (>0 → переопределить).</summary>
        private static void Apply2(WaveYaml y, ref float amplitude, ref float descendFrac, ref float waveSpeedFrac)
        {
            if (y == null) return;
            if (y.Amplitude > 0f) amplitude = y.Amplitude;
            if (y.DescendFrac > 0f) descendFrac = y.DescendFrac;
            if (y.WaveSpeedFrac > 0f) waveSpeedFrac = y.WaveSpeedFrac;
        }

        // POCO под ai.yaml (camelCase)
        private class AiYaml
        {
            public CommonYaml Common { get; set; }
            public StatesYaml States { get; set; }
            public RedYaml Red { get; set; }
            public BlueYaml Blue { get; set; }
            public SortieYaml Sortie { get; set; }
            public HiveYaml Hive { get; set; }
            public BossYaml BossDefault { get; set; }
            public Dictionary<string, BossYaml> Bosses { get; set; }
        }
        private class SortieYaml
        {
            public float MaxDiveTime { get; set; }
            public WaveYaml Snake { get; set; }
            public EllipseYaml Ellipse { get; set; }
            public WaveYaml Zigzag { get; set; }
            public SpiralYaml Spiral { get; set; }
            public SwoopYaml Swoop { get; set; }
            public StrafeYaml Strafe { get; set; }
            public HomingYaml Homing { get; set; }
            public BoomerangYaml Boomerang { get; set; }
            public CourseYaml Random { get; set; }
        }
        private class WaveYaml { public float Amplitude { get; set; } public float DescendFrac { get; set; } public float WaveSpeedFrac { get; set; } }
        private class EllipseYaml { public float Radius { get; set; } public float DescendFrac { get; set; } }
        private class SpiralYaml { public float StartRadius { get; set; } public float RadiusGrow { get; set; } public float OmegaDivisor { get; set; } public float DescendFrac { get; set; } }
        private class SwoopYaml { public float SideFrac { get; set; } public float DownFrac { get; set; } public float AccelFrac { get; set; } }
        private class StrafeYaml { public float TurnYFrac { get; set; } }
        private class HomingYaml { public float TurnSpeed { get; set; } }
        private class BoomerangYaml { public float SideFrac { get; set; } public float DownFrac { get; set; } public float BrakeFrac { get; set; } }
        private class HiveYaml
        {
            public float SwayAmplitude { get; set; }
            public float SwaySpeed { get; set; }
            public float SettleRadius { get; set; }
            public float DefaultSortieInterval { get; set; }
            public int DefaultSortieCount { get; set; }
        }
        private class CommonYaml { public float SwarmZoneFrac { get; set; } public float IdeaInterval { get; set; } }
        private class StatesYaml { public CourseYaml Takeoff { get; set; } public SwarmYaml Swarming { get; set; } public CourseYaml Attack { get; set; } }
        private class CourseYaml { public float CourseMinDeg { get; set; } public float CourseMaxDeg { get; set; } }
        private class SwarmYaml { public float SpeedFrac { get; set; } }
        private class RedYaml { public float SwarmShootInterval { get; set; } }
        private class BlueYaml
        {
            public float SwarmShootInterval { get; set; }
            public float AttackShootInterval { get; set; }
            public float AttackChance { get; set; }
        }
        private class BossYaml
        {
            public float HoverYFrac { get; set; }
            public float CenterXFrac { get; set; }
            public float SweepAmpFrac { get; set; }
            public float EntrySpeed { get; set; }
            public float FirstAttackDelay { get; set; }
            public float TelegraphTime { get; set; }
            public float PhaseAttackDelay { get; set; }
            public float BobAmplitude { get; set; }
            public float BobSpeed { get; set; }
            public float AddsOffsetX { get; set; }
            public float MoveSpeed { get; set; }
            public float VertAmpFrac { get; set; }
            public List<PhaseYaml> Phases { get; set; }
        }
        private class PhaseYaml
        {
            public float HpAbove { get; set; }
            public float AttackInterval { get; set; }
            public float SweepSpeed { get; set; }
            public string Movement { get; set; }
            public float ShellSpeed { get; set; }
            public float ShellDamage { get; set; }
            public int AddsCount { get; set; }
            public string AddsType { get; set; }
            public List<AttackYaml> Attacks { get; set; }
        }
        private class AttackYaml
        {
            public string Type { get; set; }
            public int Count { get; set; }
            public float SpreadDeg { get; set; }
            public float ShotDelay { get; set; }
            public string WeaponId { get; set; }
        }
    }
}
