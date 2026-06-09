using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RiotGalaxy.Effects
{
    /// <summary>
    /// Лёгкая система частиц для «сочности»: взрывы, искры попаданий, дульные вспышки.
    /// Частицы рисуются простой текстурой (SimpleTexture из GameManager) — без ассетов.
    /// Живёт у GameManager, обновляется в UpdateGameplay, рисуется в DrawGameplay.
    /// </summary>
    public class ParticleSystem
    {
        /// <summary>Одна частица. Класс (не struct) — храним в пуле и переиспользуем.</summary>
        private sealed class Particle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Life;       // оставшееся время жизни, сек
            public float MaxLife;    // исходное время жизни, сек
            public float Size;       // размер в виртуальных пикселях
            public float Drag;       // коэффициент затухания скорости (1/сек)
            public Color Color;
            public bool Active;
        }

        private const int Capacity = 1024;
        private readonly Particle[] _pool = new Particle[Capacity];
        private readonly Random _rng = new Random();

        public ParticleSystem()
        {
            for (int i = 0; i < Capacity; i++)
                _pool[i] = new Particle();
        }

        /// <summary>Достаёт свободную частицу из пула (или null, если пул переполнен).</summary>
        private Particle Acquire()
        {
            for (int i = 0; i < Capacity; i++)
                if (!_pool[i].Active)
                    return _pool[i];
            return null; // пул заполнен — частицу просто пропускаем (визуальный эффект, не критично)
        }

        /// <summary>
        /// Взрыв: разлёт частиц во все стороны от точки.
        /// </summary>
        /// <param name="count">сколько частиц</param>
        /// <param name="speed">базовая скорость разлёта (пикс/сек)</param>
        /// <param name="size">базовый размер частицы</param>
        /// <param name="life">базовое время жизни (сек)</param>
        public void Explosion(Vector2 position, Color color, int count = 24,
                              float speed = 220f, float size = 6f, float life = 0.55f)
        {
            for (int i = 0; i < count; i++)
            {
                var p = Acquire();
                if (p == null) return;

                float angle = (float)(_rng.NextDouble() * Math.PI * 2.0);
                float spd = speed * (0.3f + (float)_rng.NextDouble());
                p.Position = position;
                p.Velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * spd;
                p.MaxLife = life * (0.6f + (float)_rng.NextDouble() * 0.8f);
                p.Life = p.MaxLife;
                p.Size = size * (0.5f + (float)_rng.NextDouble());
                p.Drag = 2.5f;
                p.Color = TintVariation(color);
                p.Active = true;
            }
        }

        /// <summary>Всплеск частиц по параметрам из конфига (взрыв/искра).</summary>
        public void Explosion(Vector2 position, Color color, Utils.EffectsConfig.Burst b)
        {
            Explosion(position, color, b.Count, b.Speed, b.Size, b.Life);
        }

        /// <summary>
        /// Искра попадания: короткий узкий разлёт (для hit-feedback). Параметры — из конфига.
        /// </summary>
        public void HitSpark(Vector2 position, Color color)
        {
            Explosion(position, color, Utils.EffectsConfig.HitSpark);
        }

        /// <summary>Слегка варьирует цвет, чтобы взрыв «играл» оттенками.</summary>
        private Color TintVariation(Color baseColor)
        {
            float t = (float)_rng.NextDouble();
            // подмешиваем к белому/жёлтому ядру для эффекта раскалённой вспышки
            return Color.Lerp(baseColor, Color.White, t * 0.5f);
        }

        public void Update(float dt)
        {
            for (int i = 0; i < Capacity; i++)
            {
                var p = _pool[i];
                if (!p.Active) continue;

                p.Life -= dt;
                if (p.Life <= 0f)
                {
                    p.Active = false;
                    continue;
                }

                p.Position += p.Velocity * dt;
                p.Velocity -= p.Velocity * p.Drag * dt; // экспоненциальное торможение
            }
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            if (pixel == null) return;
            var origin = new Vector2(pixel.Width / 2f, pixel.Height / 2f);

            for (int i = 0; i < Capacity; i++)
            {
                var p = _pool[i];
                if (!p.Active) continue;

                float k = p.Life / p.MaxLife;          // 1 → 0 за время жизни
                float alpha = k;                        // линейное затухание прозрачности
                float scale = (p.Size * k) / pixel.Width;

                spriteBatch.Draw(pixel, p.Position, null, p.Color * alpha,
                                 0f, origin, scale, SpriteEffects.None, 0f);
            }
        }

        /// <summary>Гасит все частицы (смена уровня/состояния).</summary>
        public void Clear()
        {
            for (int i = 0; i < Capacity; i++)
                _pool[i].Active = false;
        }
    }
}
