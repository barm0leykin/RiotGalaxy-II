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

        // POCO под ai.yaml (camelCase)
        private class AiYaml
        {
            public CommonYaml Common { get; set; }
            public StatesYaml States { get; set; }
            public RedYaml Red { get; set; }
            public BlueYaml Blue { get; set; }
            public BossYaml Boss { get; set; }
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
