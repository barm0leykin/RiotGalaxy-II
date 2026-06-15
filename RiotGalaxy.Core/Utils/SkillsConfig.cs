using System.Collections.Generic;

namespace RiotGalaxy.Core.Utils
{
    /// <summary>
    /// Определения активных навыков из Content/Config/skills.yaml. Эффект по id реализован
    /// в PlayerShip.UseSkill; кулдаун ведёт PlayerShip. Дефолты в коде — игра работает без файла.
    /// </summary>
    public static class SkillsConfig
    {
        public class Skill
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Icon { get; set; }
            public string Key { get; set; }      // клавиша на десктопе (имя из Keys)
            public float Cooldown { get; set; }  // сек
            public float Duration { get; set; }  // сек (для длящихся эффектов, напр. щит)
        }

        private static readonly List<Skill> _defaults = new List<Skill>
        {
            new Skill { Id = "shield", Name = "Щит",   Icon = "Images/btn_god",     Key = "Q", Cooldown = 15f, Duration = 4f },
            new Skill { Id = "nuke",   Name = "Бомба", Icon = "Images/btn_killall", Key = "E", Cooldown = 25f, Duration = 0f },
        };

        public static List<Skill> All { get; private set; } = _defaults;

        public static Skill Get(string id) => All.Find(s => s.Id == id);

        public static void Load()
        {
            var data = Yaml.LoadAsset<SkillsYaml>(Yaml.ConfigAsset("skills.yaml"));
            if (data?.Skills != null && data.Skills.Count > 0)
                All = data.Skills;
        }

        private class SkillsYaml
        {
            public List<Skill> Skills { get; set; }
        }
    }
}
