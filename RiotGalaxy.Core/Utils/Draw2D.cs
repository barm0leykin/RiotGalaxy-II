using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RiotGalaxy.Core.Utils
{
    /// <summary>Примитивы 2D-отрисовки через 1px-текстуру (общий код HUD и экранов меню).</summary>
    public static class Draw2D
    {
        /// <summary>Прямоугольная рамка толщиной t (4 полосы).</summary>
        public static void Border(SpriteBatch sb, Texture2D pixel, Rectangle r, Color c, int t = 2)
        {
            if (pixel == null) return;
            sb.Draw(pixel, new Rectangle(r.X, r.Y, r.Width, t), c);
            sb.Draw(pixel, new Rectangle(r.X, r.Bottom - t, r.Width, t), c);
            sb.Draw(pixel, new Rectangle(r.X, r.Y, t, r.Height), c);
            sb.Draw(pixel, new Rectangle(r.Right - t, r.Y, t, r.Height), c);
        }
    }
}
