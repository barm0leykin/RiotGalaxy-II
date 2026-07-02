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

        /// <summary>
        /// Мягкое radial-свечение: белый центр с плавным затуханием альфы к краям.
        /// Рисуется аддитивно и тонируется цветом — основа неонового «глоу» для UI.
        /// </summary>
        public static Texture2D CreateGlow(GraphicsDevice device, int size = 128)
        {
            var texture = new Texture2D(device, size, size);
            var data = new Color[size * size];
            float c = (size - 1) / 2f;
            float maxR = c;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - c) / maxR, dy = (y - c) / maxR;
                    float d = (float)System.Math.Sqrt(dx * dx + dy * dy); // 0 в центре, 1 на краю
                    float a = 1f - d;
                    if (a < 0f) a = 0f;
                    a = a * a * a; // резче спад — компактное ядро с мягким ореолом
                    byte v = (byte)(a * 255);
                    data[y * size + x] = new Color(v, v, v, v);
                }
            texture.SetData(data);
            return texture;
        }
    }
}
