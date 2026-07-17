using System.Collections.Generic;

namespace RiotGalaxy.Core.Utils
{
    /// <summary>
    /// Карта звуковых событий из Content/Config/sounds.yaml: событие → файлы-варианты/громкость/
    /// разброс тона/троттлинг + имена музыкальных треков. Проигрывание — <see cref="Managers.AudioManager"/>.
    /// Игра работает и без файла (просто беззвучно).
    /// </summary>
    public static class SoundConfig
    {
        public class SoundEvent
        {
            public List<string> Files { get; set; }
            public float Volume { get; set; } = 1f;
            public float PitchVar { get; set; } = 0f;
            /// <summary>Не чаще раза в N секунд (0 — без ограничения).</summary>
            public float MinInterval { get; set; } = 0f;
        }

        private class SoundsYaml
        {
            public Dictionary<string, SoundEvent> Events { get; set; }
            public Dictionary<string, string> Music { get; set; }
        }

        private static Dictionary<string, SoundEvent> _events = new Dictionary<string, SoundEvent>();
        private static Dictionary<string, string> _music = new Dictionary<string, string>();

        public static void Load()
        {
            var data = Yaml.LoadAsset<SoundsYaml>(Yaml.ConfigAsset("sounds.yaml"));
            _events = data?.Events ?? new Dictionary<string, SoundEvent>();
            _music = data?.Music ?? new Dictionary<string, string>();
            Log.Debug($"Sounds loaded: {_events.Count} events, {_music.Count} music tracks");
        }

        /// <summary>Описание события (или null — событие беззвучно).</summary>
        public static SoundEvent Get(string key)
            => key != null && _events.TryGetValue(key, out var e) && e?.Files is { Count: > 0 } ? e : null;

        /// <summary>Все события (для предзагрузки эффектов).</summary>
        public static IEnumerable<SoundEvent> AllEvents => _events.Values;

        /// <summary>Имя трека по ключу ("menu"/"boss"; треки боёв — из биома), или null.</summary>
        public static string MusicTrack(string key)
            => key != null && _music.TryGetValue(key, out var t) && !string.IsNullOrWhiteSpace(t) ? t : null;
    }
}
