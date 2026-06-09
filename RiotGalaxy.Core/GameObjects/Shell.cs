using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiotGalaxy.Core.Managers;

namespace RiotGalaxy.Core.GameObjects
{
    /// <summary>
    /// Базовый класс снаряда. Аналог Shell из CocosSharp.
    /// Летит по прямой в направлении Direction со скоростью Speed,
    /// самоуничтожается при выходе за пределы экрана.
    /// </summary>
    public class Shell : GameObject
    {
        public float Speed { get; set; } = 200f;
        public Vector2 Direction { get; set; } = -Vector2.UnitY; // по умолчанию вверх по экрану
        public int Damage { get; set; } = 10;
        public int Hp { get; set; } = 1;
        public bool PlayerSide { get; set; } = true;   // true = выпущен игроком (для отключения friendly-fire)
        public bool IsPiercing { get; set; } = false; // летит насквозь (лазер)
        private bool _trailTick; // эмитим трассер через кадр (меньше частиц)

        /// <summary>
        /// Снаряд задаётся данными (спрайт + пробивание) — раньше были классы Bullet/Slug/Laser.
        /// Speed/Damage/Direction/PlayerSide выставляет оружие при выстреле (Weapon.FireOnce).
        /// </summary>
        public Shell(Vector2 position, string sprite = null, bool piercing = false)
            : base(position, new Vector2(8, 16))
        {
            IsPiercing = piercing;
            if (!string.IsNullOrEmpty(sprite))
                LoadSprite(sprite);
        }

        /// <summary>
        /// Загрузка спрайта снаряда из Content Pipeline (аналог draw.LoadGraphics).
        /// </summary>
        protected void LoadSprite(string asset)
        {
            try
            {
                Texture = GameManager.Instance.Content.Load<Texture2D>(asset);
                if (Texture != null)
                    Size = new Vector2(Texture.Width, Texture.Height);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== Shell sprite '{asset}' load failed: {ex.Message} ===");
            }
        }

        /// <summary>
        /// Движение и проверка выхода за экран. Аналог Shell.Activity из CocosSharp.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            if (!IsAlive)
                return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Position += Direction * Speed * dt;

            // Трассер: тусклый след за снарядом игрока (через кадр, чтобы не плодить частицы).
            _trailTick = !_trailTick;
            if (PlayerSide && _trailTick)
            {
                Color trailColor = IsPiercing ? new Color(120, 230, 200) : new Color(150, 190, 255);
                GameManager.Instance.Particles.Explosion(Position, trailColor, Utils.EffectsConfig.ShellTrail);
            }

            var gm = GameManager.Instance;
            if (Position.Y < -Height || Position.Y > gm.ScreenHeight + Height ||
                Position.X < -Width || Position.X > gm.ScreenWidth + Width)
            {
                IsAlive = false; // GameManager удалит объект из списка
            }
        }
    }
}
