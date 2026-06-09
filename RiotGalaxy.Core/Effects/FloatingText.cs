using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RiotGalaxy.Core.Effects
{
    /// <summary>
    /// Всплывающие числа в КООРДИНАТАХ МИРА (урон над врагом, очки над звездой):
    /// появляются у цели, плывут вверх и затухают. В отличие от MessageLog (фикс. позиция внизу),
    /// рисуются в игровой сцене. Параметры — в effects.yaml (`floatingText`).
    /// </summary>
    public static class FloatingText
    {
        private class Item
        {
            public string Text;
            public Vector2 Pos;
            public float Life;
            public float MaxLife;
            public float Scale;
            public Color Color;
        }

        private static readonly List<Item> _items = new List<Item>();

        /// <summary>Добавить всплывающее число/текст в точке мира.</summary>
        public static void Add(string text, Vector2 worldPos, Color color)
        {
            float life = Utils.EffectsConfig.FloatingTextLife;
            _items.Add(new Item
            {
                Text = text,
                Pos = worldPos,
                Life = life,
                MaxLife = life,
                Scale = Utils.EffectsConfig.FloatingTextScale,
                Color = color,
            });
        }

        public static void Clear() => _items.Clear();

        public static void Update(float dt)
        {
            float rise = Utils.EffectsConfig.FloatingTextRiseSpeed;
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var it = _items[i];
                it.Pos.Y -= rise * dt; // плывёт вверх
                it.Life -= dt;
                if (it.Life <= 0f)
                    _items.RemoveAt(i);
            }
        }

        public static void Draw(SpriteBatch spriteBatch, SpriteFont font)
        {
            if (font == null || _items.Count == 0)
                return;

            foreach (var it in _items)
            {
                float alpha = MathHelper.Clamp(it.Life / it.MaxLife, 0f, 1f);
                Vector2 origin = font.MeasureString(it.Text) / 2f; // центрируем над целью
                spriteBatch.DrawString(font, it.Text, it.Pos, it.Color * alpha,
                                       0f, origin, it.Scale, SpriteEffects.None, 0f);
            }
        }
    }
}
