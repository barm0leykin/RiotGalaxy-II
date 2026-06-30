using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiotGalaxy.Core.Components;
using RiotGalaxy.Core.Managers;
using RiotGalaxy.Core.Weapons;

namespace RiotGalaxy.Core.GameObjects
{
    /// <summary>
    /// Типы врагов (как в CocosSharp).
    /// </summary>
    public enum EnemyType { RND = 0, SM_SCOUT, BLUE, GREEN, RED, BOSS, UKRO, KAMIK, HEAVY, UKRO_BOSS }

    /// <summary>
    /// Базовый класс врага. Адаптация Enemy из CocosSharp.
    /// Конкретные параметры (Hp, урон, скорость, спрайт, траектория) задают наследники.
    /// </summary>
    public class Enemy : GameObject
    {
        public EnemyType Type { get; protected set; } = EnemyType.RND;
        public int Hp { get; set; } = 10;
        public int MaxHp { get; set; } = 10;
        public int Damage { get; set; } = 10;
        public float MaxSpeed { get; set; } = 100f;
        public float CurrentSpeed { get; set; } = 100f;
        public float AttackSpeed { get; set; } = 100f; // скорость во время вылета/атаки из улья
        public int Reward { get; set; } = 1;            // валюта (кредиты) за убийство

        /// <summary>Цвет взрыва/искр попадания — под палитру спрайта врага.</summary>
        public Color ExplosionColor => Type switch
        {
            EnemyType.BLUE  => new Color(80, 160, 255),
            EnemyType.GREEN => new Color(120, 230, 120),
            EnemyType.RED   => new Color(255, 90, 70),
            EnemyType.BOSS  => new Color(255, 170, 60),
            _               => new Color(255, 210, 120), // scout / прочие
        };

        // Типизированный доступ к компоненту движения с отскоком
        protected EnemyBounceMovement Move;

        // Оружие врага (аналог Enemy.gun из CocosSharp)
        public Weapon Gun { get; protected set; }
        protected float ShootInterval = 3f; // сек между выстрелами
        private float _actionTime;

        // Поведение из конфига (enemies.yaml) вместо классов-наследников.
        private string _shootMode = "none";   // none / down / aim
        private bool _wander;                  // периодически менять курс/скорость (зелёный)
        private float _ideaTime;
        private const float IdeaInterval = 2f;
        private float _dirMin = 155f, _dirMax = 205f; // диапазон стартового курса при отскоке

        // ИИ-машина состояний (опционально; аналог Enemy.ai из CocosSharp). Если задан —
        // управляет движением/скоростью/стрельбой. Формация/маршрут (YAML) её отключают.
        public AI.EnemyAI Ai { get; set; }
        /// <summary>Если true — враг временно не стреляет (управляется состоянием ИИ, аналог gun.Safe).</summary>
        public bool ShootSafe { get; set; }

        protected static readonly Random Rnd = new Random();
        /// <summary>Источник случайности для состояний ИИ.</summary>
        public Random AiRandom => Rnd;

        /// <summary>
        /// Создаёт врага заданного типа, конфигурируя себя целиком из enemies.yaml
        /// (спрайт, масштаб, движение/ИИ, режим стрельбы, блуждание). Раньше это делали
        /// классы-наследники (EnemySmallBlue/Green/Red/Scout, EnemyBoss) — теперь данные.
        /// </summary>
        public Enemy(EnemyType type, Vector2 position) : base(position, new Vector2(45, 45))
        {
            Gun = new Weapon(this); // у врага простое оружие (дефолтные параметры)
            ApplyStats(type); // hp/damage/скорости/интервал стрельбы (+ рандом из конфига)

            var s = Utils.EnemyConfig.Get(type);
            _shootMode = s.Shoot ?? "none";
            _wander = s.Wander;
            _dirMin = s.DirMin;
            _dirMax = s.DirMax;

            if (!string.IsNullOrEmpty(s.Sprite))
                LoadSprite(s.Sprite);

            if (s.Scale != 1f)
            {
                Scale = new Vector2(s.Scale);
                Size = new Vector2(Size.X * s.Scale, Size.Y * s.Scale); // хитбокс крупнее
            }

            Collision = new EnemyCollisionComponent(this);

            // Движение: машина состояний (blue/red) ИЛИ движение с отскоком (+ опц. блуждание).
            switch ((s.Ai ?? "none").Trim().ToLowerInvariant())
            {
                case "blue": Ai = new AI.EnemyAIBlue(this); break;
                case "red":  Ai = new AI.EnemyAIRed(this); break;
                default:
                    Move = new EnemyBounceMovement(this, CurrentSpeed);
                    Movement = Move;
                    PickWanderDirection(); // стартовый курс из [dirMin..dirMax]
                    break;
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (!IsAlive)
                return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // ИИ-машина состояний (если задана): управляет движением/скоростью/стрельбой
            Ai?.Update(dt);

            // Блуждание (зелёный): периодически меняем курс и скорость при движении с отскоком.
            if (_wander && Move != null)
            {
                _ideaTime += dt;
                if (_ideaTime > IdeaInterval)
                {
                    _ideaTime = 0f;
                    CurrentSpeed = Utils.EnemyConfig.Get(Type).PickSpeed(Rnd);
                    PickWanderDirection();
                }
            }

            // Оружие (очереди/перезарядка)
            Gun?.Update(gameTime);

            // Решение о стрельбе по таймеру. ShootInterval<=0 или ShootSafe — не стреляет.
            if (ShootInterval > 0f && !ShootSafe)
            {
                _actionTime += dt;
                if (_actionTime >= ShootInterval)
                {
                    _actionTime = 0f;
                    Shoot();
                }
            }

            base.Update(gameTime); // движение через компонент
        }

        /// <summary>
        /// Поставить врага в формацию (улей): движение сменяется на полёт к ячейке + барражирование.
        /// </summary>
        public void JoinFormation(Hive hive, int cx, int cy)
        {
            Ai = null;          // формация управляет движением сама
            ShootSafe = true;   // в строю улья не стреляем (как в Galaga) — огонь только в вылете
            Movement = new FormationMovement(this, CurrentSpeed, hive, cx, cy);
            Move = null;
        }

        /// <summary>Пустить врага по маршруту; end — поведение после маршрута (отскок/разлёт/формация).</summary>
        public void SetRoute(Route route, RouteEndBehavior end = RouteEndBehavior.Bounce, Hive hive = null)
        {
            Ai = null; // маршрут управляет движением сам
            Movement = new RouteMovement(this, CurrentSpeed, route, end, hive);
            Move = null;
        }

        // ----- API для состояний ИИ (AI/AIState.cs) -----

        /// <summary>Переключить владельца на движение с отскоком (создаёт компонент по текущей скорости).</summary>
        public void UseBounceMovement()
        {
            Move = new EnemyBounceMovement(this, CurrentSpeed);
            Movement = Move;
        }

        /// <summary>Задать курс (градусы; 0=вверх,180=вниз), если активно движение с отскоком.</summary>
        public void SetMoveDirection(float angleDeg) => Move?.SetDirection(angleDeg);

        /// <summary>Сменить темп стрельбы (сек между выстрелами) и сбросить таймер.</summary>
        public void SetShootInterval(float seconds)
        {
            ShootInterval = seconds;
            _actionTime = 0f;
        }

        /// <summary>Отправить врага в вылет из улья (пике-атака с возвратом в ячейку cx,cy).
        /// Тактика пике выбирается случайно из списка типа врага (enemies.yaml).</summary>
        public void StartSortie(Hive hive, int cx, int cy)
        {
            Move = null;
            ShootSafe = false; // в вылете стреляем

            var tactics = Utils.EnemyConfig.Get(Type).Tactics;
            SortieTactic tactic = SortieTactic.Random;
            if (tactics != null && tactics.Count > 0)
                tactic = SortieMovement.ParseTactic(tactics[Rnd.Next(tactics.Count)]);

            Movement = new SortieMovement(this, AttackSpeed, hive, cx, cy, tactic);
        }

        /// <summary>
        /// Применить параметры из конфига (enemies.yaml) к врагу заданного типа,
        /// включая рандомизацию скорости/интервала стрельбы. Вызывается в конструкторах типов.
        /// </summary>
        protected void ApplyStats(EnemyType type)
        {
            Type = type;
            var s = Utils.EnemyConfig.Get(type);
            Hp = MaxHp = (int)s.Hp;
            Damage = (int)s.Damage;
            Reward = s.Reward;
            MaxSpeed = CurrentSpeed = s.PickSpeed(Rnd);
            float atk = s.PickAttackSpeed(Rnd);
            AttackSpeed = atk > 0f ? atk : MaxSpeed; // 0 в конфиге → берём обычную скорость
            ShootInterval = s.PickShootInterval(Rnd);
            _actionTime = (float)Rnd.NextDouble() * (ShootInterval > 0 ? ShootInterval : 1f); // разнобой старта
        }

        /// <summary>Стартовый курс при движении с отскоком: случайный из [dirMin..dirMax] градусов.</summary>
        private void PickWanderDirection()
        {
            float dir = _dirMin + (float)Rnd.NextDouble() * (_dirMax - _dirMin);
            Move?.SetDirection(dir);
        }

        /// <summary>Поведение стрельбы по режиму из конфига (none/down/aim).</summary>
        private void Shoot()
        {
            switch (_shootMode)
            {
                case "down": ShootDown(); break;
                case "aim":  ShootAimAtPlayer(); break;
                // "none" и прочее — не стреляет
            }
        }

        /// <summary>Выстрел строго вниз (аналог ObjBehShootDown).</summary>
        protected void ShootDown()
        {
            Gun.Aim(MathHelper.Pi); // 180° = вниз
            Gun.Fire();
        }

        /// <summary>Прицельный выстрел в игрока (аналог ObjBehShootAimToPlayer).</summary>
        protected void ShootAimAtPlayer()
        {
            var player = GameManager.Instance.Player;
            if (player == null)
                return;
            Vector2 d = player.Position - Position;
            float angle = (float)Math.Atan2(d.X, -d.Y); // конвенция Weapon.Aim: 0=вверх, π=вниз
            Gun.Aim(angle);
            Gun.Fire();
        }

        /// <summary>
        /// Загрузка спрайта врага из Content Pipeline (аналог draw.LoadGraphics).
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
                Console.WriteLine($"=== Enemy sprite '{asset}' load failed: {ex.Message} ===");
            }
        }

        /// <summary>
        /// Получение урона. Аналог Enemy.Hit из CocosSharp.
        /// </summary>
        public void TakeDamage(int damage)
        {
            Hp -= damage;
            if (Hp <= 0)
            {
                Hp = 0;
                Die();
            }
        }

        /// <summary>
        /// Уничтожение врага.
        /// </summary>
        protected virtual void Die()
        {
            IsAlive = false; // GameManager удалит объект и обновит счётчики
            AudioManager.Instance.PlayEffect("explode1");
        }
    }
}
