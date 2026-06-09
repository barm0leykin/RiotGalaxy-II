using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiotGalaxy.Core.Components;
using RiotGalaxy.Core.Managers;

namespace RiotGalaxy.Core.GameObjects
{
    /// <summary>
    /// Базовый класс для всех игровых объектов в игре.
    /// Аналог CCNode из CocosSharp
    /// </summary>
    public class GameObject
    {
        // Позиция и трансформация
        public Vector2 Position { get; set; }
        public Vector2 Scale { get; set; } = Vector2.One;
        public float Rotation { get; set; } = 0f;
        public float Opacity { get; set; } = 1f;
        public Color Tint { get; set; } = Color.White; // оттенок спрайта (умножается на цвет)
        
        // Размеры объекта
        public Vector2 Size { get; set; }
        
        // Удобное свойство для доступа к ширине и высоте
        public float Width => Size.X;
        public float Height => Size.Y;
        
        // Родительский объект для поддержки иерархии
        public GameObject Parent { get; set; }
        
        // Компоненты объекта
        public MovementComponent Movement { get; set; }
        public ShootingComponent Shooting { get; set; }
        public CollisionComponent Collision { get; set; }
        
        // Состояние объекта
        public bool IsVisible { get; set; } = true;
        public bool IsEnabled { get; set; } = true;
        public bool IsAlive { get; set; } = true;
        
        // Текстура для отображения
        public Texture2D Texture { get; set; }
        
        public GameObject()
        {
            Position = Vector2.Zero;
            Size = new Vector2(64, 64); // Размер по умолчанию
        }
        
        public GameObject(Vector2 position, Vector2 size)
        {
            Position = position;
            Size = size;
        }
        
        /// <summary>
        /// Обновление состояния объекта
        /// </summary>
        public virtual void Update(GameTime gameTime)
        {
            if (!IsEnabled || !IsAlive)
                return;
                
            // Обновляем компоненты
            Movement?.Update(gameTime);
            Shooting?.Update(gameTime);
            Collision?.Update(gameTime);
        }
        
        /// <summary>
        /// Отрисовка объекта
        /// </summary>
        public virtual void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (!IsVisible || !IsAlive)
                return;
                
            // Если нет текстуры, рисуем простую заглушку
            if (Texture == null)
            {
                DrawSimpleShape(spriteBatch);
                return;
            }
                
            var origin = new Vector2(Texture.Width / 2, Texture.Height / 2);
            
            spriteBatch.Draw(
                Texture,
                Position,
                null,
                Tint * Opacity,
                Rotation,
                origin,
                Scale,
                SpriteEffects.None,
                0f
            );
        }
        
        /// <summary>
        /// Рисование простой фигуры для заглушки
        /// </summary>
        private void DrawSimpleShape(SpriteBatch spriteBatch)
        {
            // Создаем простую текстуру один раз
            if (GameManager.Instance.SimpleTexture == null)
            {
                GameManager.Instance.SimpleTexture =
                    RiotGalaxy.Core.Utils.Textures.CreateSolid(GameManager.Instance.GraphicsDevice, Color.White);
            }

            spriteBatch.Draw(
                GameManager.Instance.SimpleTexture,
                Position,
                null,
                Color.White * Opacity,
                Rotation,
                new Vector2(32, 32),
                1f,
                SpriteEffects.None,
                0f
            );
        }

        /// <summary>
        /// Проверка пересечения с другим объектом
        /// </summary>
        public bool Intersects(GameObject other)
        {
            if (other == null)
                return false;
                
            var rect = GetBounds();
            var otherRect = other.GetBounds();
            
            return rect.Intersects(otherRect);
        }
        
        /// <summary>
        /// Получение ограничивающего прямоугольника
        /// </summary>
        public Rectangle GetBounds()
        {
            return new Rectangle(
                (int)(Position.X - Size.X / 2),
                (int)(Position.Y - Size.Y / 2),
                (int)Size.X,
                (int)Size.Y
            );
        }
    }
}
