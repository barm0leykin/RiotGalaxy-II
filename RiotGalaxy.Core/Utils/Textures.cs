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
        /// Пиксельный силуэт корабля (фолбэк, если спрайт "Images/ship" не загрузился).
        /// Вынесено из PlayerShip.
        /// </summary>
        public static Texture2D CreateShipPlaceholder(GraphicsDevice device, Color color, int size = 64)
        {
            var texture = new Texture2D(device, size, size);
            var data = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int index = y * size + x;
                    bool solid =
                        y >= 40 ? (x >= 20 && x <= 43) :  // нижняя часть (корпус)
                        y >= 30 ? (x >= 25 && x <= 38) :  // средняя часть
                        y >= 20 ? (x >= 28 && x <= 35) :  // кабина
                                  (x >= 30 && x <= 33);   // верхушка
                    if (solid) data[index] = color;
                }
            texture.SetData(data);
            return texture;
        }

        /// <summary>
        /// Круг-щит с градиентом от центра к краю (тонируется при отрисовке).
        /// Вынесено из PlayerShip; кэшировать на вызывающей стороне (раньше создавался каждый кадр).
        /// </summary>
        public static Texture2D CreateShieldCircle(GraphicsDevice device, int size = 64)
        {
            var texture = new Texture2D(device, size, size);
            var data = new Color[size * size];
            float c = size / 2f;
            float radius = c - 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float distance = (float)System.Math.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    if (distance <= radius)
                    {
                        float alpha = 1.0f - distance / radius; // градиент от центра к краю
                        byte a = (byte)(alpha * 255);
                        data[y * size + x] = new Color((byte)0, (byte)150, (byte)255, a);
                    }
                }
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
