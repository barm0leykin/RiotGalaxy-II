namespace RiotGalaxy.Core.Utils
{
    /// <summary>
    /// Основные настройки игры из Content/Config/options.yaml (аналог options.ini).
    /// Значения по умолчанию совпадают с оригиналом — игра работает и без файла.
    /// </summary>
    public static class GameOptions
    {
        public static int ScreenWidth = 1280;
        public static int ScreenHeight = 768;

        public static string PlayerName = "Nuke SkyRocker";
        public static int PlayerMaxHp = 50;
        public static float PlayerMaxSpeed = 450f;
        public static float PlayerAcceleration = 1500f;
        public static float PlayerBrakeSpeed = 800f;
        public static float PlayerInvulnTime = 2.0f; // секунды неуязвимости после попадания

        /// <summary>Макс. отклонение прицельного выстрела врага от вертикали (вниз), градусы.
        /// Ограничивает «почти горизонтальные» выстрелы, чтобы игрок мог увернуться.</summary>
        public static float EnemyAimMaxDeg = 55f;

        // ── Bloom (пост-процесс свечения, только десктоп; на Android шейдера нет — игнорируется) ──
        /// <summary>Включён ли bloom (если шейдер доступен).</summary>
        public static bool BloomEnabled = true;
        /// <summary>Порог яркости (0..1): ниже — светится больше зон (в т.ч. тонкий неон меню).</summary>
        public static float BloomThreshold = 0.45f;
        /// <summary>Сила свечения при аддитивной композиции.</summary>
        public static float BloomIntensity = 1.35f;
        /// <summary>«Широта» гаусса (радиус размытия свечения).</summary>
        public static float BloomBlurAmount = 2.4f;

        public static void Load()
        {
            var data = Yaml.LoadAsset<OptionsYaml>(Yaml.ConfigAsset("options.yaml"));
            if (data == null)
                return;

            if (data.Screen != null)
            {
                if (data.Screen.Width > 0) ScreenWidth = data.Screen.Width;
                if (data.Screen.Height > 0) ScreenHeight = data.Screen.Height;
            }
            if (data.Player != null)
            {
                if (!string.IsNullOrWhiteSpace(data.Player.Name)) PlayerName = data.Player.Name;
                if (data.Player.MaxHp > 0) PlayerMaxHp = data.Player.MaxHp;
                if (data.Player.MaxSpeed > 0) PlayerMaxSpeed = data.Player.MaxSpeed;
                if (data.Player.Acceleration > 0) PlayerAcceleration = data.Player.Acceleration;
                if (data.Player.BrakeSpeed > 0) PlayerBrakeSpeed = data.Player.BrakeSpeed;
                if (data.Player.InvulnTime > 0) PlayerInvulnTime = data.Player.InvulnTime;
            }
            if (data.Combat != null && data.Combat.AimMaxDeg > 0)
                EnemyAimMaxDeg = data.Combat.AimMaxDeg;

            if (data.Bloom != null)
            {
                BloomEnabled = data.Bloom.Enabled;
                if (data.Bloom.Threshold >= 0f) BloomThreshold = data.Bloom.Threshold;
                if (data.Bloom.Intensity > 0f) BloomIntensity = data.Bloom.Intensity;
                if (data.Bloom.BlurAmount > 0f) BloomBlurAmount = data.Bloom.BlurAmount;
            }
        }

        // POCO под структуру options.yaml (имена в YAML — camelCase)
        private class OptionsYaml
        {
            public ScreenYaml Screen { get; set; }
            public PlayerYaml Player { get; set; }
            public CombatYaml Combat { get; set; }
            public BloomYaml Bloom { get; set; }
        }
        private class CombatYaml
        {
            public float AimMaxDeg { get; set; }
        }
        private class BloomYaml
        {
            public bool Enabled { get; set; } = true;   // если поле отсутствует — считаем включённым
            public float Threshold { get; set; } = -1f; // <0 → не переопределять
            public float Intensity { get; set; } = -1f;
            public float BlurAmount { get; set; } = -1f;
        }
        private class ScreenYaml
        {
            public int Width { get; set; }
            public int Height { get; set; }
        }
        private class PlayerYaml
        {
            public string Name { get; set; }
            public int MaxHp { get; set; }
            public float MaxSpeed { get; set; }
            public float Acceleration { get; set; }
            public float BrakeSpeed { get; set; }
            public float InvulnTime { get; set; }
        }
    }
}
