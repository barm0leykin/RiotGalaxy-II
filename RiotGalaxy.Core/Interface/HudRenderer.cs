using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiotGalaxy.Core.GameObjects;

namespace RiotGalaxy.Core.Interface
{
    /// <summary>
    /// Боевой HUD: полоса HP с рамкой, оружие и баффы слева; очки и кредиты справа.
    /// Чистая отрисовка — состояние только читается. Шрифт мелкий (14pt), поэтому масштабируем.
    /// </summary>
    public class HudRenderer
    {
        private const float Big = 1.3f;    // очки/кредиты
        private const float Mid = 1.15f;   // оружие
        private const float Small = 1.0f;  // баффы/подпись HP
        private const int Margin = 16;

        public void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel,
                         PlayerShip player, int screenWidth)
        {
            if (player == null || font == null)
                return;

            // ── Слева: полоса HP с рамкой и числом ──────────────────────────
            var hpRect = new Rectangle(Margin, Margin, 320, 32);
            float hpFrac = player.MaxHealth > 0 ? (float)player.Health / player.MaxHealth : 0f;
            Color hpColor = hpFrac < 0.3f ? Color.OrangeRed : hpFrac < 0.6f ? Color.Gold : Color.LimeGreen;
            DrawBar(spriteBatch, pixel, hpRect, hpFrac, hpColor);
            DrawTextIn(spriteBatch, font, Utils.Loc.F("hud.hp", player.Health, player.MaxHealth), hpRect, Color.White, Small);

            // Оружие (название + уровень) под полосой HP.
            string weaponName = Weapons.WeaponConfig.Get(player.CurrentWeaponId)?.Name ?? player.CurrentWeaponId;
            spriteBatch.DrawString(font, Utils.Loc.F("hud.weapon", weaponName, player.Gun.Level),
                new Vector2(Margin, hpRect.Bottom + 8), Color.LightGray, 0f, Vector2.Zero, Mid, SpriteEffects.None, 0f);

            // Активные баффы — компактным списком ниже.
            float by = hpRect.Bottom + 8 + font.LineSpacing * Mid + 6;
            foreach (var b in player.ActiveBuffs)
            {
                int secs = (int)System.Math.Ceiling(b.Value);
                spriteBatch.DrawString(font, Utils.Loc.F("hud.buff", Utils.Loc.T("buff." + b.Key), secs),
                    new Vector2(Margin, by), Color.Cyan, 0f, Vector2.Zero, Small, SpriteEffects.None, 0f);
                by += font.LineSpacing * Small + 4;
            }

            // ── Справа: очки и кредиты (выровнены по правому краю) ───────────
            DrawTextRight(spriteBatch, font, Utils.Loc.F("hud.score", player.Score), screenWidth, Margin, Color.White, Big);
            DrawTextRight(spriteBatch, font, Utils.Loc.F("hud.credits", player.Currency),
                screenWidth, Margin + (int)(font.LineSpacing * Big) + 4, Color.Gold, Big);
        }

        /// <summary>Полоса со шкалой: тёмный фон + рамка + цветная заливка по доле fill (0..1).</summary>
        private static void DrawBar(SpriteBatch sb, Texture2D pixel, Rectangle r, float fill, Color fillColor)
        {
            if (pixel == null) return;
            fill = MathHelper.Clamp(fill, 0f, 1f);
            sb.Draw(pixel, r, new Color(20, 20, 28, 200));                                  // фон
            sb.Draw(pixel, new Rectangle(r.X, r.Y, (int)(r.Width * fill), r.Height), fillColor); // заливка
            var border = new Color(90, 130, 210, 220);                                      // рамка-акцент
            const int t = 2;
            sb.Draw(pixel, new Rectangle(r.X, r.Y, r.Width, t), border);
            sb.Draw(pixel, new Rectangle(r.X, r.Bottom - t, r.Width, t), border);
            sb.Draw(pixel, new Rectangle(r.X, r.Y, t, r.Height), border);
            sb.Draw(pixel, new Rectangle(r.Right - t, r.Y, t, r.Height), border);
        }

        /// <summary>Текст по центру прямоугольника (для подписи на полосе HP).</summary>
        private static void DrawTextIn(SpriteBatch sb, SpriteFont font, string text, Rectangle r, Color color, float scale)
        {
            Vector2 size = font.MeasureString(text) * scale;
            var pos = new Vector2(r.Center.X - size.X / 2f, r.Center.Y - size.Y / 2f);
            sb.DrawString(font, text, pos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        /// <summary>Текст, выровненный по правому краю экрана.</summary>
        private static void DrawTextRight(SpriteBatch sb, SpriteFont font, string text, int screenWidth, int y, Color color, float scale)
        {
            float w = font.MeasureString(text).X * scale;
            sb.DrawString(font, text, new Vector2(screenWidth - w - Margin, y), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}
