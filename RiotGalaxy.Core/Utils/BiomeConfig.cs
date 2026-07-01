using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace RiotGalaxy.Core.Utils
{
    /// <summary>
    /// Биомы актов из Content/Config/biomes.yaml (фолбэк — дефолты в коде). Биом задаёт вид неба
    /// (вертикальный градиент верх→низ) и оттенок звёзд. Применяется при старте миссии (по акту).
    /// </summary>
    public static class BiomeConfig
    {
        public class Biome
        {
            public Color SkyTop;
            public Color SkyBottom;
            public Color Star;
        }

        // Дефолты по актам.
        private static readonly Dictionary<string, Biome> _defaults = new Dictionary<string, Biome>
        {
            ["act1"] = new Biome { SkyTop = new Color(32, 66, 140),  SkyBottom = new Color(8, 16, 46),  Star = new Color(190, 212, 255) }, // холодный сине-стальной
            ["act2"] = new Biome { SkyTop = new Color(140, 48, 32),  SkyBottom = new Color(36, 12, 12), Star = new Color(255, 188, 140) }, // багрово-рыжая война
            ["act3"] = new Biome { SkyTop = new Color(92, 36, 122),  SkyBottom = new Color(20, 10, 34), Star = new Color(222, 182, 255) }, // зловещий фиолет
        };

        private static Dictionary<string, Biome> _biomes;

        /// <summary>Биом по id (act1/act2/...); неизвестный → act1.</summary>
        public static Biome Get(string id)
        {
            var map = _biomes ?? _defaults;
            if (!string.IsNullOrEmpty(id) && map.TryGetValue(id, out var b)) return b;
            return _defaults["act1"];
        }

        public static void Load()
        {
            var data = Yaml.LoadAsset<Dictionary<string, BiomeYaml>>(Yaml.ConfigAsset("biomes.yaml"));
            if (data == null) return;
            _biomes = new Dictionary<string, Biome>(_defaults);
            foreach (var kv in data)
                if (kv.Value != null) _biomes[kv.Key] = kv.Value.ToBiome();
        }

        private static Color C(List<int> v, Color fallback)
            => (v != null && v.Count >= 3) ? new Color(v[0], v[1], v[2]) : fallback;

        // POCO под biomes.yaml: skyTop/skyBottom/star = [r,g,b].
        private class BiomeYaml
        {
            public List<int> SkyTop { get; set; }
            public List<int> SkyBottom { get; set; }
            public List<int> Star { get; set; }
            public Biome ToBiome() => new Biome
            {
                SkyTop = C(SkyTop, new Color(20, 28, 66)),
                SkyBottom = C(SkyBottom, new Color(4, 5, 16)),
                Star = C(Star, new Color(200, 215, 255)),
            };
        }
    }
}
