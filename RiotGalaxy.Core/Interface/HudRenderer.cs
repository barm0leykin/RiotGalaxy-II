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

            // Текущий акт и миссия — под оружием.
            float infoY = hpRect.Bottom + 8 + font.LineSpacing * Mid + 4;
            var gm = Managers.GameManager.Instance;
            if (gm.CurrentMissionNumber >= 1)
            {
                string mission = Utils.Loc.F("hud.actmission", gm.CurrentAct, gm.CurrentMissionNumber,
                                             gm.CurrentMissionTitle);
                spriteBatch.DrawString(font, mission, new Vector2(Margin, infoY),
                    new Color(150, 170, 210), 0f, Vector2.Zero, Small, SpriteEffects.None, 0f);
                infoY += font.LineSpacing * Small + 4;
            }

            // Активные баффы — компактным списком ниже.
            float by = infoY;
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

        /// <summary>
        /// Шкала HP босса — по центру сверху, с именем (описание боя) и засечками фаз (66%/33%),
        /// цвет заливки совпадает с фазой BossAI. Вызывается из GameManager.DrawGameplay, если есть босс.
        /// </summary>
        public void DrawBossBar(SpriteBatch sb, SpriteFont font, Texture2D pixel, Enemy boss, string name, int screenWidth)
        {
            if (boss == null || pixel == null || font == null) return;

            int w = (int)(screenWidth * 0.52f);
            const int h = 18, y = 48;
            var rect = new Rectangle((screenWidth - w) / 2, y, w, h);
            float frac = boss.MaxHp > 0 ? (float)boss.Hp / boss.MaxHp : 0f;

            // Цвет по фазе (как в BossAI: >66% / >33% / ниже).
            Color fill = frac > 0.66f ? new Color(220, 70, 70)
                       : frac > 0.33f ? new Color(240, 140, 40)
                                      : new Color(255, 40, 40);
            DrawBar(sb, pixel, rect, frac, fill);

            // Засечки границ фаз.
            DrawTick(sb, pixel, rect, 0.66f);
            DrawTick(sb, pixel, rect, 0.33f);

            // Имя босса по центру над полосой.
            if (!string.IsNullOrEmpty(name))
            {
                Vector2 size = font.MeasureString(name) * Small;
                sb.DrawString(font, name, new Vector2(rect.Center.X - size.X / 2f, y - font.LineSpacing * Small - 2),
                    Color.OrangeRed, 0f, Vector2.Zero, Small, SpriteEffects.None, 0f);
            }
        }

        /// <summary>Вертикальная засечка на полосе (граница фазы) по доле frac.</summary>
        private static void DrawTick(SpriteBatch sb, Texture2D pixel, Rectangle r, float frac)
        {
            int tx = r.X + (int)(r.Width * frac);
            sb.Draw(pixel, new Rectangle(tx, r.Y, 2, r.Height), new Color(20, 20, 28, 220));
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
