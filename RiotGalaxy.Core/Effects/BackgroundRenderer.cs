using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RiotGalaxy.Core.Effects
{
    /// <summary>
    /// Фон сцены: небо-градиент биома + параллакс-звёзды с оттенком биома.
    /// Биом задаётся по акту/миссии (SetBiome). Вынесено из GameManager.
    /// </summary>
    public class BackgroundRenderer
    {
        private StarField _starField;
        private Utils.BiomeConfig.Biome _biome = Utils.BiomeConfig.Get("act1");

        /// <summary>Создать слои звёзд под виртуальное разрешение (звать из LoadContent).</summary>
        public void Init(int virtualWidth, int virtualHeight)
        {
            _starField = new StarField(virtualWidth, virtualHeight);
            _starField.Tint = _biome.Star;
        }

        /// <summary>Применить биом: цвет неба + оттенок звёзд.</summary>
        public void SetBiome(string id)
        {
            _biome = Utils.BiomeConfig.Get(id);
            if (_starField != null) _starField.Tint = _biome.Star;
        }

        public void Update(float dt) => _starField?.Update(dt);

        /// <summary>Небо (вертикальный градиент полосами) + звёзды. Рисовать до сцены/UI.</summary>
        public void Draw(SpriteBatch sb, Texture2D pixel, int screenW, int screenH)
        {
            const int strips = 32;
            float h = screenH / (float)strips;
            for (int i = 0; i < strips; i++)
            {
                Color c = Color.Lerp(_biome.SkyTop, _biome.SkyBottom, i / (float)(strips - 1));
                sb.Draw(pixel, new Rectangle(0, (int)(i * h), screenW, (int)Math.Ceiling(h) + 1), c);
            }
            _starField?.Draw(sb, pixel);
        }
    }
}
