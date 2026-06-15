using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using RiotGalaxy.Core.Components;
using RiotGalaxy.Core.Weapons;

namespace RiotGalaxy.Core.GameObjects
{
    /// <summary>
    /// Класс корабля игрока
    /// Аналог PlayerShip из CocosSharp для MonoGame
    /// </summary>
    public class PlayerShip : GameObject
    {
        // Параметры здоровья
        private int _health;
        public int Health
        {
            get { return _health; }
            set
            {
                int oldHealth = _health;
                _health = Math.Max(0, Math.Min(value, MaxHealth));
                
                // Вызываем событие об изменении здоровья
                OnHealthChanged(oldHealth, _health);
                
                // Проверяем состояние игрока
                if (_health <= 0 && IsAlive)
                {
                    Die();
                }
            }
        }
        public int MaxHealth { get; set; }

        // Параметры движения
        public float Speed { get; set; }

        // Параметры оружия (id текущего оружия — см. WeaponConfig).
        public string CurrentWeaponId { get; private set; }
        public float FireRate { get; set; }
        private float _timeSinceLastShot = 0f;

        // Множители от постоянных апгрейдов (магазин). Оружие читает их при выстреле.
        public float DamageMult { get; set; } = 1f;
        public float FireRateMult { get; set; } = 1f;

        // Активные навыки: оставшийся кулдаун по id (см. SkillsConfig). 0/нет — готов.
        private readonly Dictionary<string, float> _skillCd = new Dictionary<string, float>();

        // Временные баффы-подборы: id → оставшееся время (см. BonusConfig.Buffs).
        private readonly Dictionary<string, float> _buffs = new Dictionary<string, float>();
        public IReadOnlyDictionary<string, float> ActiveBuffs => _buffs;

        // Текущее оружие (аналог playerShip.gun из CocosSharp)
        public Weapon Gun { get; private set; }

        // Очки игрока (за звёзды-бонусы)
        public int Score { get; set; }

        // Валюта (кредиты), заработанная за текущую партию; в конце игры идёт в профиль (SaveData).
        public int Currency { get; set; }

        // Параметры неуязвимости (аналог GodMode в CocosSharp)
        public bool IsInvulnerable { get; private set; }
        private float _invulnerabilityTime = 0f; // секунды; длительность — из options.yaml (GameOptions.PlayerInvulnTime)
        
        // Состояние игрока (переопределяем базовый)
        public new bool IsAlive { get; private set; } = true;
        
        // События
        public event Action<int, int> HealthChanged; // oldHealth, newHealth
        public event Action PlayerDied;
        public event Action PlayerRespawned;

        public PlayerShip(Vector2 position) : base(position, new Vector2(60, 60))
        {
            // Базовые параметры из options.yaml (апгрейды наложит ApplyUpgrades ниже).
            MaxHealth = Utils.GameOptions.PlayerMaxHp;
            Health = MaxHealth;
            Speed = Utils.GameOptions.PlayerMaxSpeed;
            FireRate = 2f; // выстрелов в секунду
            IsAlive = true;

            // Инициализируем компоненты
            var movement = new PlayerMovementComponent(this, Speed);
            movement.Acceleration = Utils.GameOptions.PlayerAcceleration;
            movement.BrakingSpeed = Utils.GameOptions.PlayerBrakeSpeed;
            Movement = movement;
            Shooting = new PlayerShootingComponent(this);
            Collision = new PlayerCollisionComponent(this);

            // Стартовое оружие — стартер из реестра (бластер: слабый, скорострельный).
            Gun = new Weapon(this);
            EquipWeapon(WeaponConfig.Starter?.Id);

            // Наложить постоянные апгрейды из профиля (HP/скорость/множители).
            ApplyUpgrades();

            // Текстура загружается в LoadContent (спрайт "Images/ship")
            Texture = null;
        }

        /// <summary>
        /// Применить постоянные апгрейды из профиля ([[SaveData]].Upgrades → UpgradeConfig).
        /// Вызывается в конструкторе и при продолжении между уровнями (после магазина) —
        /// чтобы покупки сразу влияли на живой корабль. Множители урона/темпа читает Weapon.
        /// </summary>
        public void ApplyUpgrades()
        {
            int newMax = Utils.GameOptions.PlayerMaxHp + Utils.UpgradeConfig.MaxHpBonus;
            int delta = newMax - MaxHealth;
            MaxHealth = newMax;
            if (delta > 0) Health += delta; // прирост макс. HP даём как бонус-хил

            Speed = Utils.GameOptions.PlayerMaxSpeed * Utils.UpgradeConfig.SpeedMult;
            if (Movement is PlayerMovementComponent pm) pm.MaxSpeed = Speed;

            DamageMult = Utils.UpgradeConfig.DamageMult;
            FireRateMult = Utils.UpgradeConfig.FireRateMult;

            // Переэкипировать текущее оружие — подхватить уровень, купленный в магазине.
            if (!string.IsNullOrEmpty(CurrentWeaponId))
                EquipWeapon(CurrentWeaponId);
        }

        // ── Активные навыки ──────────────────────────────────────────────────

        /// <summary>Готов ли навык (кулдаун истёк).</summary>
        public bool SkillReady(string id) => SkillCooldownRemaining(id) <= 0f;

        public float SkillCooldownRemaining(string id)
            => _skillCd.TryGetValue(id, out var v) ? v : 0f;

        /// <summary>Доля оставшегося кулдауна 1→0 (для индикатора на кнопке).</summary>
        public float SkillCooldownFraction(string id)
        {
            var s = Utils.SkillsConfig.Get(id);
            if (s == null || s.Cooldown <= 0f) return 0f;
            return MathHelper.Clamp(SkillCooldownRemaining(id) / s.Cooldown, 0f, 1f);
        }

        /// <summary>Активировать навык по id (если готов): применить эффект и запустить кулдаун.</summary>
        public void UseSkill(string id)
        {
            var s = Utils.SkillsConfig.Get(id);
            if (s == null || !SkillReady(id))
                return;

            switch (id)
            {
                case "shield":
                    ActivateInvulnerability(s.Duration);
                    break;
                case "nuke":
                    Managers.GameManager.Instance.KillAllEnemies();
                    break;
                // новые навыки — добавлять сюда (эффект) + в skills.yaml (данные)
            }

            _skillCd[id] = s.Cooldown;
            Managers.MessageLog.Add(s.Name, Color.Cyan);
        }

        private void TickSkillCooldowns(float dt)
        {
            if (_skillCd.Count == 0) return;
            // Снимок ключей: меняем значения, не структуру.
            var ids = new List<string>(_skillCd.Keys);
            foreach (var id in ids)
                if (_skillCd[id] > 0f)
                    _skillCd[id] = Math.Max(0f, _skillCd[id] - dt);
        }

        // ── Временные баффы-подборы ──────────────────────────────────────────

        /// <summary>Подобрать/продлить временный бафф по id (power/rapid/speed — см. BonusConfig).</summary>
        public void ApplyBuff(string id)
        {
            var b = Utils.BonusConfig.Buff(id);
            if (b == null) return;
            float cur = _buffs.TryGetValue(id, out var v) ? v : 0f;
            _buffs[id] = Math.Max(cur, b.Duration); // повторный подбор продлевает до полной длительности
        }

        /// <summary>Множитель активного баффа (1, если не активен).</summary>
        private float BuffMult(string id)
            => _buffs.TryGetValue(id, out var v) && v > 0f ? (Utils.BonusConfig.Buff(id)?.Mult ?? 1f) : 1f;

        /// <summary>Итоговые множители для оружия: апгрейд × активный бафф.</summary>
        public float EffectiveDamageMult => DamageMult * BuffMult("power");
        public float EffectiveFireRateMult => FireRateMult * BuffMult("rapid");

        private void TickBuffs(float dt)
        {
            if (_buffs.Count == 0) return;
            var ids = new List<string>(_buffs.Keys);
            foreach (var id in ids)
            {
                _buffs[id] -= dt;
                if (_buffs[id] <= 0f) _buffs.Remove(id);
            }
        }

        /// <summary>
        /// Имя спрайта корабля в Content Pipeline (атлас CocosSharp -> Images/ship)
        /// </summary>
        public const string ShipSpriteAsset = "Images/ship";

        /// <summary>
        /// Обновление состояния корабля
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            if (!IsAlive)
                return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Обновляем таймеры
            _timeSinceLastShot += deltaTime;

            // Обновляем состояние неуязвимости
            if (IsInvulnerable)
            {
                _invulnerabilityTime -= deltaTime;
                if (_invulnerabilityTime <= 0)
                {
                    DeactivateInvulnerability();
                }
            }

            // Кулдауны навыков и таймеры временных баффов
            TickSkillCooldowns(deltaTime);
            TickBuffs(deltaTime);
            // Бафф скорости влияет на максимальную скорость движения.
            if (Movement is PlayerMovementComponent pm)
                pm.MaxSpeed = Speed * BuffMult("speed");

            // Обновляем оружие (очереди/перезарядка)
            Gun?.Update(gameTime);

            // Вызываем базовый метод (обновляет компоненты)
            base.Update(gameTime);
        }

        /// <summary>
        /// Выстрел из текущего оружия. Аналог вызова playerShip.gun.Fire().
        /// </summary>
        public void Fire()
        {
            if (!IsAlive)
                return;
            Gun?.Fire();
        }

        /// <summary>
        /// Отрисовка корабля
        /// </summary>
        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Отладочное сообщение
            
            if (!IsAlive)
                return;

            // Если неуязвим, делаем мигание
            if (IsInvulnerable && (int)(_invulnerabilityTime * 10) % 2 == 0)
            {
                return; // Пропускаем отрисовку каждый второй кадр
            }

            // Вызываем базовый метод отрисовки
            base.Draw(gameTime, spriteBatch);

            // Рисуем дополнительную информацию (если нужно)
            DrawAdditionalInfo(spriteBatch);
        }

        /// <summary>
        /// Попытка выстрелить
        /// </summary>
        public bool TryShoot()
        {
            if (!IsAlive || _timeSinceLastShot < 1f / FireRate)
            {
                return false;
            }

            _timeSinceLastShot = 0f;
            return true;
        }

        /// <summary>
        /// Получение урона
        /// Аналог Hit из CocosSharp
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (!IsAlive || IsInvulnerable)
                return;

            
            // Наносим урон
            Health -= damage;

            // Если еще живы, активируем временную неуязвимость (щит)
            if (IsAlive)
            {
                ActivateInvulnerability(Utils.GameOptions.PlayerInvulnTime);
            }
        }

        /// <summary>
        /// Восстановление здоровья
        /// Аналог HpUp из CocosSharp
        /// </summary>
        public void Heal(int amount)
        {
            Health += amount;
        }

        /// <summary>
        /// Активация неуязвимости на указанное время
        /// Аналог AddMyshield из CocosSharp
        /// </summary>
        public void ActivateInvulnerability(float duration)
        {
            IsInvulnerable = true;
            _invulnerabilityTime = duration;
        }

        /// <summary>
        /// Деактивация неуязвимости
        /// </summary>
        private void DeactivateInvulnerability()
        {
            IsInvulnerable = false;
            _invulnerabilityTime = 0;
        }

        /// <summary>
        /// Смерть игрока
        /// </summary>
        private void Die()
        {
            if (!IsAlive) return;
            
            IsAlive = false;
            
            // Вызываем событие смерти
            PlayerDied?.Invoke();
            
            // Здесь будет событие смерти игрока
            // Например: GameManager.Instance.ChangeGameState(GameManager.GameState.GameOver);
        }

        /// <summary>
        /// Воскрешение игрока
        /// </summary>
        public void Respawn()
        {
            Health = MaxHealth;
            IsAlive = true;
            IsInvulnerable = false;
            
            // Сброс позиции (может быть настроен в потомках)
            // Position = new Vector2(GameManager.Instance.ScreenWidth / 2, GameManager.Instance.ScreenHeight - 100);
            
            // Даем временную неуязвимость после воскрешения
            ActivateInvulnerability(Utils.GameOptions.PlayerInvulnTime);
            
            // Сброс других параметров
            _timeSinceLastShot = 0;
            
            // Вызываем событие воскрешения
            PlayerRespawned?.Invoke();
        }

        /// <summary>
        /// Сменить оружие по id, если оно открыто (есть в профиле). С сообщением для игрока.
        /// </summary>
        public void ChangeWeapon(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (!Utils.SaveData.IsWeaponOwned(id))
            {
                var d = WeaponConfig.Get(id);
                Managers.MessageLog.Add($"{d?.Name ?? id}: не открыто (магазин)", Color.Gray);
                return;
            }
            EquipWeapon(id);
            Managers.MessageLog.Add("Оружие: " + (WeaponConfig.Get(id)?.Name ?? id), Color.Cyan);
        }

        /// <summary>Экипировать оружие по id с уровнем из профиля (без сообщения). null — стартер.</summary>
        public void EquipWeapon(string id)
        {
            var def = WeaponConfig.Get(id) ?? WeaponConfig.Starter;
            if (def == null) return;
            CurrentWeaponId = def.Id;
            int lvl = System.Math.Max(1, Utils.SaveData.GetWeaponLevel(def.Id)); // стартер всегда ур.1+
            Gun.SetWeapon(def, lvl);
        }

        /// <summary>
        /// Сброс состояния корабля (например, при рестарте уровня)
        /// </summary>
        public void Reset()
        {
            Health = MaxHealth;
            IsAlive = true;
            IsInvulnerable = false;
            _timeSinceLastShot = 0f;

            // Сброс позиции (пока заглушка)
            // Position = new Vector2(GameManager.Instance.ScreenWidth / 2, GameManager.Instance.ScreenHeight - 100);
            Position = new Vector2(640, 668); // 1280/2, 768-100
        }

        /// <summary>
        /// Создание простой текстуры для корабля
        /// </summary>
        private Texture2D CreateSimpleTexture(Color color)
        {
            // Создаем текстуру 64x64 (квадрат)
            Texture2D texture = new Texture2D(_graphicsDevice ?? throw new Exception("GraphicsDevice not set"), 64, 64);
            Color[] data = new Color[64 * 64];
            
            // Создаем простой корабль как набор пикселей
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    int index = y * 64 + x;
                    
                    // Создаем простую форму корабля
                    if (y >= 40) // Нижняя часть (корпус)
                    {
                        if (x >= 20 && x <= 43)
                            data[index] = color;
                    }
                    else if (y >= 30) // Средняя часть
                    {
                        if (x >= 25 && x <= 38)
                            data[index] = color;
                    }
                    else if (y >= 20) // Верхняя часть (кабина)
                    {
                        if (x >= 28 && x <= 35)
                            data[index] = color;
                    }
                    else // Верхушка
                    {
                        if (x >= 30 && x <= 33)
                            data[index] = color;
                    }
                }
            }
            
            texture.SetData(data);
            return texture;
        }

        /// <summary>
        /// Дополнительная отрисовка (например, для здоровья или эффектов)
        /// </summary>
        private void DrawAdditionalInfo(SpriteBatch spriteBatch)
        {
            // Если неуязвим, рисуем щит (аналог AddMyshield из CocosSharp)
            if (IsInvulnerable)
            {
                // Создаем простую текстуру для щита (если еще не создана)
                // Это просто пример, в реальном приложении текстуры лучше кэшировать
                if (_graphicsDevice == null) return; // Пропускаем если не установлен GraphicsDevice
                Texture2D shieldTexture = CreateSimpleShieldTexture();
                
                // Рисуем прозрачный внешний слой щита
                spriteBatch.Draw(
                    shieldTexture, 
                    Position, 
                    null, 
                    new Color(100, 150, 255, 40), // Очень прозрачный синий
                    Rotation, 
                    new Vector2(32, 32), 
                    1.2f, 
                    SpriteEffects.None, 
                    0f
                );
                
                // Рисуем основной слой щита
                spriteBatch.Draw(
                    shieldTexture, 
                    Position, 
                    null, 
                    new Color(0, 100, 255, 80), // Полупрозрачный синий
                    Rotation, 
                    new Vector2(32, 32), 
                    1.0f, 
                    SpriteEffects.None, 
                    0f
                );
            }
        }
        
        /// <summary>
        /// Создание простой текстуры для щита
        /// </summary>
        private Texture2D CreateSimpleShieldTexture()
        {
            // Создаем текстуру 64x64 в форме круга для щита
            Texture2D texture = new Texture2D(_graphicsDevice, 64, 64);
            Color[] data = new Color[64 * 64];
            
            int centerX = 32;
            int centerY = 32;
            int radius = 30;
            
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    int index = y * 64 + x;
                    // Создаем круг для щита
                    float distance = (float)Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2));
                    if (distance <= radius)
                    {
                        // Градиент от центра к краям
                        float alpha = 1.0f - (distance / radius);
                        byte alphaByte = (byte)(alpha * 255);
                        data[index] = new Color((byte)0, (byte)150, (byte)255, alphaByte);
                    }
                    else
                    {
                        // Прозрачные пиксели вне круга
                        data[index] = Color.Transparent;
                    }
                }
            }
            
            texture.SetData(data);
            return texture;
        }

        /// <summary>
        /// Получение GraphicsDevice (нужно для создания текстур)
        /// </summary>
        // private GraphicsDevice GraphicsDevice => GameManager.Instance.GraphicsDevice;
        // Используем заглушку вместо GameManager пока не реализован
        private GraphicsDevice _graphicsDevice;
        
        /// <summary>
        /// Установка GraphicsDevice
        /// </summary>
        public void SetGraphicsDevice(GraphicsDevice graphicsDevice)
        {
            // GraphicsDevice нужен для генерации текстуры щита (CreateSimpleShieldTexture)
            // и для фолбэк-заглушки, если спрайт не загрузится.
            _graphicsDevice = graphicsDevice;
        }

        /// <summary>
        /// Загрузка спрайта корабля из Content Pipeline.
        /// Заменяет прежнюю заглушку (зелёный квадрат) реальным спрайтом "Images/ship".
        /// </summary>
        public void LoadContent(ContentManager content)
        {
            try
            {
                Texture = content.Load<Texture2D>(ShipSpriteAsset);
            }
            catch (Exception ex)
            {
                // Фолбэк на заглушку, чтобы игра не падала, если ассет недоступен
                Console.WriteLine($"=== Failed to load '{ShipSpriteAsset}', falling back to placeholder: {ex.Message} ===");
                if (_graphicsDevice != null)
                    Texture = CreateSimpleTexture(Color.Lime);
            }
        }
        
        /// <summary>
        /// Обработчик изменения здоровья
        /// </summary>
        private void OnHealthChanged(int oldHealth, int newHealth)
        {
            
            // Вызываем событие об изменении здоровья
            HealthChanged?.Invoke(oldHealth, newHealth);
            
            // Здесь можно добавить дополнительную логику, например:
            // - Визуальные эффекты при получении урона
            // - Звуковые эффекты
            // - Обновление UI
        }
    }
}