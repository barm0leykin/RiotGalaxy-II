using System.Collections.Generic;

namespace RiotGalaxy.Core.Utils
{
    /// <summary>
    /// Реплики боссов из Content/Config/bosstaunts.yaml, ключ — id миссии (один босс на миссию).
    /// Показываются через MessageLog (не прерывая бой): intro (появление), phase2/phase3 (смена фазы),
    /// defeat (гибель). Нет файла/ключа — босс просто молчит.
    /// </summary>
    public static class BossTauntConfig
    {
        public class Taunt
        {
            public string Name { get; set; }   // имя босса (префикс реплики)
            public string Intro { get; set; }
            public string Phase2 { get; set; }
            public string Phase3 { get; set; }
            public string Defeat { get; set; }
        }

        private static Dictionary<string, Taunt> _data;

        public static Taunt Get(string missionId)
            => (_data != null && missionId != null && _data.TryGetValue(missionId, out var t)) ? t : null;

        public static void Load()
            => _data = Yaml.LoadAsset<Dictionary<string, Taunt>>(Yaml.ConfigAsset("bosstaunts.yaml"));
    }
}
