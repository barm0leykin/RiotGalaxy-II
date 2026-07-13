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

        // ── Босс (BossAI) ────────────────────────────────────────────────────
        public class BossParams
        {
            public float HoverYFrac = 0.18f;      // точка зависания, доля высоты экрана
            public float CenterXFrac = 0.5f;      // центр свипа, доля ширины
            public float SweepAmpFrac = 0.32f;    // амплитуда свипа, доля ширины
            public float EntrySpeed = 90f;        // скорость влёта, пикс/сек
            public float FirstAttackDelay = 1.2f; // пауза перед первой атакой, сек
            public float Phase2HpFrac = 0.66f;    // ниже этой доли HP — фаза 2
            public float Phase3HpFrac = 0.33f;    // ниже — фаза 3
            public float[] AttackIntervals = { 2.2f, 1.8f, 1.4f }; // по фазам 1..3, сек
            public float[] SweepSpeeds = { 0.8f, 1.2f, 1.7f };     // скорость свипа по фазам
            public float TelegraphTime = 0.6f;    // вспышка перед залпом, сек
            public float PhaseAttackDelay = 0.6f; // пауза до атаки после смены фазы
            public int BurstCount = 3;            // фаза 1: снарядов в прицельной очереди
            public float BurstSpreadDeg = 10f;    // шаг веера очереди
            public int FanCount = 7;              // фаза 2: снарядов в веере вниз
            public float FanSpreadDeg = 110f;     // ширина веера
            public int RadialCount = 12;          // фаза 3: снарядов в радиальном залпе
            public float BobAmplitude = 18f;      // вертикальное покачивание, пикс
            public float BobSpeed = 0.6f;         // частота покачивания
            public int AddsCount = 2;             // подмога в фазе 3
            public float AddsOffsetX = 70f;       // разлёт подмоги от центра босса
        }
        public static BossParams Boss = new BossParams();

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
            if (d.Boss != null)
            {
                var b = d.Boss;
                if (b.HoverYFrac > 0f) Boss.HoverYFrac = b.HoverYFrac;
                if (b.CenterXFrac > 0f) Boss.CenterXFrac = b.CenterXFrac;
                if (b.SweepAmpFrac > 0f) Boss.SweepAmpFrac = b.SweepAmpFrac;
                if (b.EntrySpeed > 0f) Boss.EntrySpeed = b.EntrySpeed;
                if (b.FirstAttackDelay > 0f) Boss.FirstAttackDelay = b.FirstAttackDelay;
                if (b.Phase2HpFrac > 0f) Boss.Phase2HpFrac = b.Phase2HpFrac;
                if (b.Phase3HpFrac > 0f) Boss.Phase3HpFrac = b.Phase3HpFrac;
                if (b.AttackIntervals != null && b.AttackIntervals.Length == 3) Boss.AttackIntervals = b.AttackIntervals;
                if (b.SweepSpeeds != null && b.SweepSpeeds.Length == 3) Boss.SweepSpeeds = b.SweepSpeeds;
                if (b.TelegraphTime > 0f) Boss.TelegraphTime = b.TelegraphTime;
                if (b.PhaseAttackDelay > 0f) Boss.PhaseAttackDelay = b.PhaseAttackDelay;
                if (b.BurstCount > 0) Boss.BurstCount = b.BurstCount;
                if (b.BurstSpreadDeg > 0f) Boss.BurstSpreadDeg = b.BurstSpreadDeg;
                if (b.FanCount > 0) Boss.FanCount = b.FanCount;
                if (b.FanSpreadDeg > 0f) Boss.FanSpreadDeg = b.FanSpreadDeg;
                if (b.RadialCount > 0) Boss.RadialCount = b.RadialCount;
                if (b.BobAmplitude > 0f) Boss.BobAmplitude = b.BobAmplitude;
                if (b.BobSpeed > 0f) Boss.BobSpeed = b.BobSpeed;
                if (b.AddsCount > 0) Boss.AddsCount = b.AddsCount;
                if (b.AddsOffsetX > 0f) Boss.AddsOffsetX = b.AddsOffsetX;
            }
            Log.Debug("AiConfig loaded (ai.yaml)");
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
            public BossYaml Boss { get; set; }
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
            public float Phase2HpFrac { get; set; }
            public float Phase3HpFrac { get; set; }
            public float[] AttackIntervals { get; set; }
            public float[] SweepSpeeds { get; set; }
            public float TelegraphTime { get; set; }
            public float PhaseAttackDelay { get; set; }
            public int BurstCount { get; set; }
            public float BurstSpreadDeg { get; set; }
            public int FanCount { get; set; }
            public float FanSpreadDeg { get; set; }
            public int RadialCount { get; set; }
            public float BobAmplitude { get; set; }
            public float BobSpeed { get; set; }
            public int AddsCount { get; set; }
            public float AddsOffsetX { get; set; }
        }
    }
}
