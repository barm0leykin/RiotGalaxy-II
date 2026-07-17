using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace RiotGalaxy.Core.Managers
{
    /// <summary>
    /// Аудио: звуковые события (SFX) и фоновая музыка. Полностью data-driven:
    /// событие → файлы/громкость/тон — Content/Config/sounds.yaml (<see cref="Utils.SoundConfig"/>),
    /// треки боёв — по биому (biomes.yaml, поле music). Событие без описания — просто беззвучно.
    ///
    /// SFX: варианты выбираются случайно, лёгкий разброс тона (pitchVar) оживляет повторы,
    /// minInterval защищает от спама одним звуком. Музыка — MediaPlayer (Song, loop);
    /// повторный запуск того же трека игнорируется. Всё в try/catch: на машинах без
    /// аудио-железа игра продолжает работать беззвучно.
    /// </summary>
    public class AudioManager
    {
        private static AudioManager _instance;
        public static AudioManager Instance => _instance ??= new AudioManager();

        private ContentManager _content;
        private readonly Dictionary<string, SoundEffect> _effects = new Dictionary<string, SoundEffect>();
        private readonly Dictionary<string, Song> _songs = new Dictionary<string, Song>();
        private readonly Dictionary<string, long> _lastPlayedMs = new Dictionary<string, long>();
        private readonly Random _rng = new Random();
        private string _currentTrack;

        /// <summary>Общая громкость эффектов (0..1); пер-событийный множитель — в sounds.yaml.</summary>
        public float EffectsVolume { get; set; } = 0.8f;

        private float _musicVolume = 0.6f;
        /// <summary>Громкость музыки (0..1); применяется к MediaPlayer сразу.</summary>
        public float MusicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = MathHelper.Clamp(value, 0f, 1f);
                Try(() => MediaPlayer.Volume = _musicVolume, "set music volume");
            }
        }

        /// <summary>Трек текущего боя (из биома миссии); включается при входе в бой.</summary>
        public string BattleTrack { get; set; } = "act1";

        private AudioManager() { }

        /// <summary>Загрузка: конфиг событий + предзагрузка всех упомянутых эффектов.</summary>
        public void LoadContent(ContentManager content)
        {
            _content = content;
            Utils.SoundConfig.Load();
            foreach (var ev in Utils.SoundConfig.AllEvents)
                if (ev?.Files != null)
                    foreach (var f in ev.Files)
                        LoadEffect(f);
        }

        private void LoadEffect(string file)
        {
            if (string.IsNullOrWhiteSpace(file) || _effects.ContainsKey(file)) return;
            try
            {
                _effects[file] = _content.Load<SoundEffect>("Sounds/" + file);
            }
            catch (Exception ex)
            {
                Utils.Log.Error($"=== Failed to load SoundEffect 'Sounds/{file}': {ex.Message} ===");
                _effects[file] = null; // не долбить Content.Load повторно
            }
        }

        /// <summary>Проиграть звуковое событие из sounds.yaml (неизвестное — тишина).</summary>
        public void Play(string eventKey)
        {
            var ev = Utils.SoundConfig.Get(eventKey);
            if (ev == null || EffectsVolume <= 0f) return;

            // Троттлинг: не чаще раза в minInterval.
            long now = Environment.TickCount64;
            if (ev.MinInterval > 0f && _lastPlayedMs.TryGetValue(eventKey, out long last)
                && now - last < (long)(ev.MinInterval * 1000))
                return;
            _lastPlayedMs[eventKey] = now;

            string file = ev.Files[ev.Files.Count == 1 ? 0 : _rng.Next(ev.Files.Count)];
            if (!_effects.TryGetValue(file, out var effect) || effect == null) return;

            float pitch = ev.PitchVar > 0f ? ((float)_rng.NextDouble() * 2f - 1f) * ev.PitchVar : 0f;
            Try(() => effect.Play(MathHelper.Clamp(ev.Volume * EffectsVolume, 0f, 1f),
                                  MathHelper.Clamp(pitch, -1f, 1f), 0f), $"play '{eventKey}'");
        }

        // ── Музыка ──────────────────────────────────────────────────────────

        /// <summary>Включить трек по ключу sounds.yaml→music ("menu"/"boss").</summary>
        public void PlayMusicKey(string key) => PlayMusic(Utils.SoundConfig.MusicTrack(key));

        /// <summary>Включить трек Content/Music/&lt;track&gt; в цикле; тот же трек — не перезапускается.</summary>
        public void PlayMusic(string track)
        {
            if (string.IsNullOrWhiteSpace(track) || track == _currentTrack) return;

            if (!_songs.TryGetValue(track, out var song))
            {
                try
                {
                    song = _content?.Load<Song>("Music/" + track);
                }
                catch (Exception ex)
                {
                    Utils.Log.Error($"=== Failed to load Song 'Music/{track}': {ex.Message} ===");
                    song = null;
                }
                _songs[track] = song; // и null — чтобы не грузить повторно
            }
            if (song == null) return;

            _currentTrack = track;
            Try(() =>
            {
                MediaPlayer.IsRepeating = true;
                MediaPlayer.Volume = _musicVolume;
                MediaPlayer.Play(song);
            }, $"play music '{track}'");
        }

        /// <summary>Остановить музыку (GameOver/Victory).</summary>
        public void StopMusic()
        {
            _currentTrack = null;
            Try(MediaPlayer.Stop, "stop music");
        }

        // Например, NoAudioHardwareException на машинах без звука — игра работает беззвучно.
        private static void Try(Action action, string what)
        {
            try { action(); }
            catch (Exception ex) { Utils.Log.Error($"=== Audio: failed to {what}: {ex.Message} ==="); }
        }
    }
}
