using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RiotGalaxy.Core.Utils
{
    /// <summary>
    /// Помощники для процедурных текстур. Раньше код создания «сплошного квадрата»
    /// был продублирован в Game1/GameObject/GameManager — теперь единая точка.
    /// </summary>
    public static class Textures
    {
        /// <summary>
        /// Создаёт сплошную квадратную текстуру заданного цвета
        /// (используется для частиц, полос HP, заглушек-спрайтов).
        /// </summary>
        public static Texture2D CreateSolid(GraphicsDevice device, Color color, int size = 64)
        {
            var texture = new Texture2D(device, size, size);
            var data = new Color[size * size];
            for (int i = 0; i < data.Length; i++)
                data[i] = color;
            texture.SetData(data);
            return texture;
        }
    }
}
