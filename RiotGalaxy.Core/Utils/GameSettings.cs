using System;
using System.IO;
using Microsoft.Xna.Framework;
using RiotGalaxy.Core.Managers;

namespace RiotGalaxy.Core.Utils
{
    /// <summary>
    /// Пользовательские настройки (громкость) в файле settings.yaml рядом с приложением.
    /// </summary>
    public static class GameSettings
    {
        private static string FilePath => Path.Combine(AppContext.BaseDirectory, "settings.yaml");

        /// <summary>Выбранный язык интерфейса (код локали: "ru"/"en").</summary>
        public static string Language { get; set; } = "ru";

        public static void Load()
        {
            var data = Yaml.LoadFile<SettingsYaml>(FilePath);
            if (data == null)
                return;
            AudioManager.Instance.EffectsVolume = MathHelper.Clamp(data.EffectsVolume, 0f, 1f);
            if (!string.IsNullOrWhiteSpace(data.Language))
                Language = data.Language;
        }

        public static void Save()
        {
            try
            {
                var data = new SettingsYaml
                {
                    EffectsVolume = AudioManager.Instance.EffectsVolume,
                    Language = Language,
                };
                File.WriteAllText(FilePath, Yaml.Serializer.Serialize(data));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== Settings save failed: {ex.Message} ===");
            }
        }

        // POCO для settings.yaml
        public class SettingsYaml
        {
            public float EffectsVolume { get; set; } = 0.1f;
            public string Language { get; set; } = "ru";
        }
    }
}
