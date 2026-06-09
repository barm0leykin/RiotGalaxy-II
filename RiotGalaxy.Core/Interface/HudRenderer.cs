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
                spriteBatch.DrawString(font, $"HP: {player.Health}/{player.MaxHealth}",
                    new Vector2(10, 10), Color.White);

                string scoreText = $"Очки: {player.Score}";
                float scoreW = font.MeasureString(scoreText).X;
                spriteBatch.DrawString(font, scoreText, new Vector2(screenWidth - scoreW - 10, 10), Color.White);

                string weaponText = $"Оружие: {player.CurrentWeapon} (ур. {player.Gun.Level + 1})";
                spriteBatch.DrawString(font, weaponText, new Vector2(10, 60), Color.LightGray);
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
