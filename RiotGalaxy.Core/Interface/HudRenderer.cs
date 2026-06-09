using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiotGalaxy.Core.GameObjects;

namespace RiotGalaxy.Core.Interface
{
    /// <summary>
    /// Отрисовка боевого HUD (HP/очки/оружие/полоса здоровья).
    /// Вынесено из GameManager — чистая отрисовка, состояние только читается.
    /// </summary>
    public class HudRenderer
    {
        public void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel,
                         PlayerShip player, int screenWidth)
        {
            if (player == null)
                return;

            // Текст здоровья, очков и оружия.
            if (font != null)
            {
                spriteBatch.DrawString(font, Utils.Loc.F("hud.hp", player.Health, player.MaxHealth),
                    new Vector2(10, 10), Color.White);

                string scoreText = Utils.Loc.F("hud.score", player.Score);
                float scoreW = font.MeasureString(scoreText).X;
                spriteBatch.DrawString(font, scoreText, new Vector2(screenWidth - scoreW - 10, 10), Color.White);

                string weaponText = Utils.Loc.F("hud.weapon", player.CurrentWeapon, player.Gun.Level + 1);
                spriteBatch.DrawString(font, weaponText, new Vector2(10, 60), Color.LightGray);

                // Валюта (кредиты) за текущую партию — справа под очками.
                string credits = Utils.Loc.F("hud.credits", player.Currency);
                float cw = font.MeasureString(credits).X;
                spriteBatch.DrawString(font, credits, new Vector2(screenWidth - cw - 10, 34), Color.Gold);
            }

            // Полоса здоровья: тёмный фон + цветная заполненная часть.
            if (pixel != null)
            {
                spriteBatch.Draw(pixel, new Rectangle(10, 38, 200, 16), new Color(40, 40, 40, 180));
                int healthWidth = (int)(200 * (float)player.Health / player.MaxHealth);
                DrawHealthBar(spriteBatch, pixel, 10, 38, healthWidth, 16, player.Health);
            }
        }

        /// <summary>Цветная полоса HP (зелёная → жёлтая → красная по остатку здоровья).</summary>
        private static void DrawHealthBar(SpriteBatch spriteBatch, Texture2D pixel,
                                          int x, int y, int width, int height, int health)
        {
            Color barColor = Color.Green;
            if (health < 30) barColor = Color.Red;
            else if (health < 60) barColor = Color.Yellow;

            spriteBatch.Draw(pixel, new Rectangle(x, y, width, height), barColor);
        }
    }
}
