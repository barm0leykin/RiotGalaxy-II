using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RiotGalaxy.Core.Effects
{
    /// <summary>
    /// Параллакс-фон из нескольких слоёв звёзд, плывущих вниз с разной скоростью.
    /// Создаёт ощущение полёта вперёд. Рисуется простой текстурой (SimpleTexture) —
    /// без ассетов. Координаты — в виртуальном кадре (1280×768).
    /// </summary>
    public class StarField
    {
        private struct Star
        {
            public float X, Y;       // позиция в виртуальных пикселях
            public float Size;       // размер точки
            public float Brightness; // 0..1 — яркость (альфа/цвет)
        }

        private sealed class Layer
        {
            public Star[] Stars;
            public float Speed;      // скорость прокрутки вниз, пикс/сек
        }

        private readonly Layer[] _layers;
        private readonly float _width;
        private readonly float _height;
        private readonly Random _rng = new Random();

        /// <summary>Оттенок звёзд (палитра биома акта). По умолчанию — белые.</summary>
        public Color Tint { get; set; } = Color.White;

        /// <summary>
        /// Слои параллакса берутся из <see cref="Utils.EffectsConfig.StarLayers"/>
        /// (порядок: дальний → ближний). Параметры задаются в Content/Config/effects.yaml.
        /// </summary>
        public StarField(int virtualWidth, int virtualHeight)
        {
            _width = virtualWidth;
            _height = virtualHeight;

            var specs = Utils.EffectsConfig.StarLayers;
            _layers = new Layer[specs.Count];
            for (int i = 0; i < specs.Count; i++)
                _layers[i] = MakeLayer(specs[i]);
        }

        private Layer MakeLayer(Utils.EffectsConfig.StarLayer spec)
        {
            var stars = new Star[spec.Count];
            for (int i = 0; i < spec.Count; i++)
                stars[i] = NewStar(spec.BrightMin, spec.BrightMax, spec.SizeMin, spec.SizeMax, anyY: true);

            return new Layer { Stars = stars, Speed = spec.Speed };
        }

        private Star NewStar(float brightMin, float brightMax, float sizeMin, float sizeMax, bool anyY)
        {
            return new Star
            {
                X = (float)_rng.NextDouble() * _width,
                Y = anyY ? (float)_rng.NextDouble() * _height : 0f,
                Size = sizeMin + (float)_rng.NextDouble() * (sizeMax - sizeMin),
                Brightness = brightMin + (float)_rng.NextDouble() * (brightMax - brightMin),
            };
        }

        public void Update(float dt)
        {
            foreach (var layer in _layers)
            {
                for (int i = 0; i < layer.Stars.Length; i++)
                {
                    layer.Stars[i].Y += layer.Speed * dt;
                    if (layer.Stars[i].Y > _height)
                    {
                        // ушла за нижний край — возвращаем сверху со случайным X.
                        layer.Stars[i].Y -= _height;
                        layer.Stars[i].X = (float)_rng.NextDouble() * _width;
                    }
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            if (pixel == null) return;

            foreach (var layer in _layers)
            {
                foreach (var s in layer.Stars)
                {
                    float scale = s.Size / pixel.Width;
                    spriteBatch.Draw(pixel, new Vector2(s.X, s.Y), null,
                                     Tint * s.Brightness, 0f, Vector2.Zero,
                                     scale, SpriteEffects.None, 0f);
                }
            }
        }
    }
}
