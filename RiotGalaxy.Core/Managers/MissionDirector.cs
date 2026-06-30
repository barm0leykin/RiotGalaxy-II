using System.Collections.Generic;
using RiotGalaxy.Core.Utils;

namespace RiotGalaxy.Core.Managers
{
    /// <summary>Тип шага миссии.</summary>
    public enum StepKind { Briefing, Battle, Boss, Shop }

    /// <summary>Один шаг миссии: брифинг / бой / босс / магазин.</summary>
    public class MissionStep
    {
        public StepKind Kind;
        public string Arg; // имя диалога или файла боя (для Shop не используется)
    }

    /// <summary>
    /// Кампания как последовательность миссий, каждая миссия — последовательность шагов
    /// (брифинг → бой → … → босс → брифинг → магазин). Сам прогон шагов ведёт GameManager;
    /// здесь только данные и навигация по ним.
    ///
    /// Кампания: Content/Missions/campaign.yaml (`missions: [m1, m2, ...]`).
    /// Миссия:   Content/Missions/&lt;id&gt;.yaml (id, title, steps).
    /// </summary>
    public class MissionDirector
    {
        private List<string> _missionIds = new List<string>();
        private int _mi = -1;          // индекс текущей миссии
        private MissionDef _cur;       // текущая миссия
        private int _si = -1;          // индекс текущего шага в миссии

        public int MissionNumber => _mi + 1;
        public int TotalMissions => _missionIds.Count;
        public string CurrentMissionTitle => _cur?.Title ?? "";
        public string CurrentMissionBackground => _cur?.Background;

        /// <summary>Индекс текущей миссии и шага (для чекпоинта/возобновления).</summary>
        public int MissionIndex => _mi;
        public int StepIndex => _si;

        /// <summary>Начать кампанию заново: загрузить список миссий, сбросить указатели.</summary>
        public void StartCampaign()
        {
            var camp = Yaml.LoadAsset<CampaignYaml>("Content/Missions/campaign.yaml");
            _missionIds = (camp?.Missions != null && camp.Missions.Count > 0)
                ? camp.Missions
                : new List<string> { "m1" }; // фолбэк
            _mi = -1;
            _cur = null;
            _si = -1;
            Log.Debug($"Campaign started: missions={_missionIds.Count}");
        }

        /// <summary>Список миссий кампании (id + заголовок) — для dev-меню выбора миссии.</summary>
        public static List<(string id, string title)> Catalog()
        {
            var res = new List<(string, string)>();
            var camp = Yaml.LoadAsset<CampaignYaml>("Content/Missions/campaign.yaml");
            var ids = (camp?.Missions != null && camp.Missions.Count > 0) ? camp.Missions : new List<string> { "m1" };
            foreach (var id in ids)
            {
                var def = MissionDef.Load(id);
                res.Add((id, def?.Title ?? id));
            }
            return res;
        }

        /// <summary>Перезапустить ТЕКУЩУЮ миссию с её первого шага (для рестарта после смерти).</summary>
        public void RestartMission()
        {
            if (_cur != null) _si = -1; // следующий Advance вернёт первый шаг текущей миссии
        }

        /// <summary>
        /// Возобновить кампанию с конкретной позиции (миссия/шаг) — для «Продолжить» после выхода.
        /// Возвращает false, если позиция некорректна (тогда зовущий стартует с начала).
        /// </summary>
        public bool ResumeAt(int missionIndex, int stepIndex)
        {
            StartCampaign(); // загрузить список миссий
            if (missionIndex < 0 || missionIndex >= _missionIds.Count)
                return false;
            _cur = MissionDef.Load(_missionIds[missionIndex]);
            if (_cur == null || _cur.Steps == null || _cur.Steps.Count == 0)
                return false;
            _mi = missionIndex;
            int s = stepIndex < 0 ? 0 : (stepIndex >= _cur.Steps.Count ? _cur.Steps.Count - 1 : stepIndex);
            _si = s - 1; // следующий Advance вернёт шаг s
            return true;
        }

        /// <summary>
        /// Следующий шаг кампании (с пересечением границ миссий). null — кампания пройдена.
        /// missionStarted=true, если этот шаг — первый в новой миссии.
        /// </summary>
        public MissionStep Advance(out bool missionStarted)
        {
            missionStarted = false;

            // Нужна ли следующая миссия?
            if (_cur == null || _si + 1 >= (_cur.Steps?.Count ?? 0))
            {
                _mi++;
                if (_mi >= _missionIds.Count)
                    return null; // кампания пройдена

                _cur = MissionDef.Load(_missionIds[_mi]);
                _si = -1;
                missionStarted = true;

                // Пустая/битая миссия — пропускаем дальше.
                if (_cur == null || _cur.Steps == null || _cur.Steps.Count == 0)
                    return Advance(out missionStarted);
            }

            _si++;
            return _cur.Steps[_si];
        }

        // ── модель данных ───────────────────────────────────────────────
        private class CampaignYaml { public List<string> Missions { get; set; } }

        public class MissionDef
        {
            public string Id;
            public string Title;
            public string Background;
            public List<MissionStep> Steps;

            public static MissionDef Load(string id)
            {
                var y = Yaml.LoadAsset<MissionYaml>($"Content/Missions/{id}.yaml");
                if (y == null) return null;
                var def = new MissionDef { Id = y.Id ?? id, Title = y.Title ?? "", Background = y.Background, Steps = new List<MissionStep>() };
                if (y.Steps != null)
                    foreach (var s in y.Steps)
                    {
                        if (!string.IsNullOrWhiteSpace(s.Briefing))
                            def.Steps.Add(new MissionStep { Kind = StepKind.Briefing, Arg = s.Briefing });
                        else if (!string.IsNullOrWhiteSpace(s.Boss))
                            def.Steps.Add(new MissionStep { Kind = StepKind.Boss, Arg = s.Boss });
                        else if (!string.IsNullOrWhiteSpace(s.Battle))
                            def.Steps.Add(new MissionStep { Kind = StepKind.Battle, Arg = s.Battle });
                        else if (s.Shop)
                            def.Steps.Add(new MissionStep { Kind = StepKind.Shop });
                    }
                return def;
            }
        }

        // POCO под Content/Missions/<id>.yaml
        private class MissionYaml
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Background { get; set; }
            public List<StepYaml> Steps { get; set; }
        }
        private class StepYaml
        {
            public string Briefing { get; set; }
            public string Battle { get; set; }
            public string Boss { get; set; }
            public bool Shop { get; set; }
        }
    }
}
