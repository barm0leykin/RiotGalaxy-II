using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiotGalaxy.Core.Managers;

namespace RiotGalaxy.Core.GameObjects
{
    /// <summary>
    /// Типы бонусов (как в CocosSharp).
    /// </summary>
    public enum BonusType { BULLET_UP = 0, HP_UP, NUKE_BOMB, STAR, POWER, RAPID, SPEED }

    /// <summary>
    /// Базовый бонус. Падает вниз, при подборе игроком применяет эффект.
    /// Адаптация Bonus из CocosSharp.
    /// </summary>
    public class Bonus : GameObject
    {
        public BonusType Type { get; protected set; }
        protected float CurrentSpeed = 200f;
        protected Vector2 Velocity;
        public bool IsCollected { get; private set; }

        protected Bonus(Vector2 position) : base(position, new Vector2(20, 20))
        {
            SetDirection(180f); // по умолчанию падает вниз
        }

        /// <summary>
        /// Простой бонус-усиление, заданный типом (HP_UP/BULLET_UP/NUKE_BOMB) — раньше это были
        /// отдельные классы. Спрайт выбирается по типу, эффект — в базовом Apply.
        /// (STAR имеет уникальное поведение — отдельный класс BonusStar.)
        /// </summary>
        public Bonus(BonusType type, Vector2 position) : this(position)
        {
            Type = type;
            string sprite = type switch
            {
                BonusType.HP_UP     => "Images/bonusHPUp",
                BonusType.BULLET_UP => "Images/bonusBulletUp",
                BonusType.NUKE_BOMB => "Images/bonusNukeBomb",
                BonusType.POWER     => "Images/btn_BulletUp",   // ×2 урон (временно)
                BonusType.RAPID     => "Images/btn_minigun",    // быстрее стрельба
                BonusType.SPEED     => "Images/btn_auto_cannon",// быстрее корабль
                _                   => null,
            };
            if (sprite != null)
                LoadSprite(sprite);
        }

        /// <summary>Разбор id бонуса из YAML уровня (drop:) в тип. false — неизвестный id.</summary>
        public static bool TryParseType(string id, out BonusType type)
        {
            switch ((id ?? "").Trim().ToLowerInvariant())
            {
                case "hp": case "hpup":        type = BonusType.HP_UP;     return true;
                case "power": case "bulletup": type = BonusType.POWER;     return true;
                case "rapid":                  type = BonusType.RAPID;     return true;
                case "speed":                  type = BonusType.SPEED;     return true;
                case "nuke": case "nukebomb":  type = BonusType.NUKE_BOMB; return true;
                default:                       type = BonusType.STAR;      return false;
            }
        }

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
                Console.WriteLine($"=== Bonus sprite '{asset}' load failed: {ex.Message} ===");
            }
        }

        /// <summary>Направление движения (градусы): 0 = вверх, 180 = вниз.</summary>
        protected void SetDirection(float angleDeg)
        {
            float rad = MathHelper.ToRadians(angleDeg);
            Velocity = new Vector2((float)Math.Sin(rad), -(float)Math.Cos(rad)) * CurrentSpeed;
        }

        public override void Update(GameTime gameTime)
        {
            if (!IsAlive)
                return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Position += Velocity * dt;

            var gm = GameManager.Instance;
            if (Position.Y > gm.ScreenHeight + 50 || Position.Y < -50)
                IsAlive = false;
        }

        /// <summary>Подбор бонуса игроком.</summary>
        public void Collect()
        {
            IsCollected = true;
            IsAlive = false;
        }

        /// <summary>
        /// Применить эффект простого бонуса по типу. STAR переопределяет (свой Apply).
        /// </summary>
        public virtual void Apply(PlayerShip player)
        {
            switch (Type)
            {
                case BonusType.HP_UP:
                    int before = player.Health;
                    player.Heal(Utils.BonusConfig.Current.HpUpAmount);
                    int gained = player.Health - before;
                    Managers.MessageLog.Add(gained > 0 ? $"+{gained} HP" : "HP полное", Color.Lime);
                    break;
                case BonusType.BULLET_UP:
                    // Оружие качается в магазине; этот подбор даёт временное усиление урона.
                    player.ApplyBuff("power");
                    Managers.MessageLog.Add("Усиленный урон!", Color.OrangeRed);
                    break;
                case BonusType.NUKE_BOMB:
                    GameManager.Instance.KillAllEnemies();
                    Managers.MessageLog.Add("Бомба! Всех в труху", Color.Orange);
                    break;
                case BonusType.POWER:
                    player.ApplyBuff("power");
                    Managers.MessageLog.Add("Усиленный урон!", Color.OrangeRed);
                    break;
                case BonusType.RAPID:
                    player.ApplyBuff("rapid");
                    Managers.MessageLog.Add("Скорострельность!", Color.Cyan);
                    break;
                case BonusType.SPEED:
                    player.ApplyBuff("speed");
                    Managers.MessageLog.Add("Ускорение!", Color.LightGreen);
                    break;
            }
        }
    }

    /// <summary>
    /// Звезда: даёт КРЕДИТЫ (на магазин), притягивается к игроку в радиусе магнита, вращается.
    /// Номинал кредитов несёт сама звезда (обычно = reward убитого врага). Адаптация BonusStar.
    /// </summary>
    public class BonusStar : Bonus
    {
        private const float FallSpeed = 70f;    // скорость падения вне зоны магнита
        private float _angleDeg = 180f;          // текущий курс (град); 180 = вниз
        private readonly float _rotateSpeed;
        private readonly int _credits;           // кредиты при подборе

        private static readonly Random Rnd = new Random();

        public BonusStar(Vector2 pos, int credits = 0) : base(pos)
        {
            Type = BonusType.STAR;
            _credits = credits > 0 ? credits : Utils.BonusConfig.Current.StarCredits;
            CurrentSpeed = FallSpeed;
            LoadSprite("Images/bonusStar");
            SetDirection(_angleDeg); // по умолчанию падает вниз
            _rotateSpeed = (float)(Rnd.NextDouble() * 4.0 - 2.0); // вращение вокруг своей оси
        }

        public override void Update(GameTime gameTime)
        {
            if (!IsAlive)
                return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            var player = GameManager.Instance.Player;
            if (player != null)
            {
                var magnet = Weapons.WeaponConfig.Magnet; // оборудование корабля (weapons.yaml)
                float radius = magnet.Radius * Utils.UpgradeConfig.MagnetMult; // апгрейд магнита
                Vector2 d = player.Position - Position;
                float target;
                if (d.LengthSquared() < radius * radius)
                {
                    // в зоне магнита — плавно доворачиваем КУРС на игрока и ускоряемся
                    CurrentSpeed = magnet.PullSpeed;
                    target = MathHelper.ToDegrees((float)Math.Atan2(d.X, -d.Y));
                }
                else
                {
                    // вне зоны — плавно возвращаемся к падению вниз
                    CurrentSpeed = FallSpeed;
                    target = 180f;
                }
                _angleDeg = ApproachAngle(_angleDeg, target, magnet.TurnSpeed * dt);
                SetDirection(_angleDeg);
            }

            Position += Velocity * dt;
            Rotation += _rotateSpeed * dt;

            var gm = GameManager.Instance;
            if (Position.Y > gm.ScreenHeight + 50 || Position.Y < -50)
                IsAlive = false;
        }

        /// <summary>Плавный доворот current→target за шаг maxStep (град), кратчайшим путём.</summary>
        private static float ApproachAngle(float current, float target, float maxStep)
        {
            // Кратчайшая разница в диапазоне (-180, 180] — иначе на границе ±180° доворот «длинным путём»
            float diff = Mod360(target - current + 180f) - 180f;
            if (Math.Abs(diff) <= maxStep)
                return Mod360(target);
            return Mod360(current + Math.Sign(diff) * maxStep);
        }

        private static float Mod360(float a)
        {
            a %= 360f;
            return a < 0 ? a + 360f : a;
        }

        public override void Apply(PlayerShip player)
        {
            player.Currency += _credits; // звезда = кредиты (на магазин)
            Effects.FloatingText.Add($"+{_credits}", Position, Color.Gold); // число у места подбора
        }
    }
}
