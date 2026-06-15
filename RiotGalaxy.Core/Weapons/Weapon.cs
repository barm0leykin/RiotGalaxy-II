using System;
using Microsoft.Xna.Framework;
using RiotGalaxy.Core.GameObjects;
using RiotGalaxy.Core.Managers;

namespace RiotGalaxy.Core.Weapons
{
    /// <summary>
    /// Оружие — единый настраиваемый класс (data-driven). Поведение задаётся <see cref="WeaponDef"/>:
    /// спрайт/пробивание снаряда, разброс (jitter), веер (fan), параметры по уровням. Подклассов нет.
    /// Стреляет очередями (burst) с интервалом, между очередями — перезарядка. У врагов оружие без
    /// Def — простой одиночный снаряд (дефолтные Options).
    /// </summary>
    public class Weapon
    {
        private static readonly Random _rnd = new Random();

        public WeaponDef Def { get; private set; }   // null у врагов
        public int Level { get; private set; } = 1;   // 1-based уровень оружия
        public WeaponOptions Options { get; private set; }
        public bool Safe { get; set; }                // предохранитель

        protected GameObject _owner;
        private float _aimAngle = 0f;                  // базовый угол прицеливания (0 = вверх)
        private int _burstRemaining = 0;
        private float _burstTimer = 0f;
        private float _reloadTimer = 0f;

        // Множители игрока (апгрейды × временные баффы; у врагов — 1).
        private float OwnerDamageMult => (_owner is PlayerShip ps) ? ps.EffectiveDamageMult : 1f;
        private float OwnerFireRateMult => (_owner is PlayerShip ps) ? ps.EffectiveFireRateMult : 1f;

        public Weapon(GameObject owner)
        {
            _owner = owner;
            Options = new WeaponOptions(1, 0.25f, 1.2f, 10f, 250f); // дефолт для врагов
        }

        /// <summary>Настроить оружие игрока по описанию и (1-based) уровню.</summary>
        public void SetWeapon(WeaponDef def, int level)
        {
            Def = def;
            Level = def.MaxLevel > 0 ? Math.Clamp(level, 1, def.MaxLevel) : 1;
            Options = def.OptionsForLevel(Level);
        }

        public void Aim(float angleRad) => _aimAngle = angleRad;

        private static Vector2 DirFromAngle(float a) =>
            new Vector2((float)Math.Sin(a), -(float)Math.Cos(a));

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_reloadTimer > 0f) _reloadTimer -= dt;

            if (_burstRemaining > 0)
            {
                _burstTimer -= dt;
                if (_burstTimer <= 0f)
                {
                    FireOnce();
                    _burstRemaining--;
                    _burstTimer = Options.burstInterval;
                }
            }
        }

        public void Fire()
        {
            if (Safe || Options == null) return;
            if (_reloadTimer > 0f || _burstRemaining > 0) return;

            _burstRemaining = Math.Max(1, Options.burst);
            _reloadTimer = Options.reloadSpeed / OwnerFireRateMult; // апгрейд/бафф темпа — короче перезарядка
            if (_owner is PlayerShip)
                AudioManager.Instance.PlayEffect("fire1");

            FireOnce();
            _burstRemaining--;
            _burstTimer = Options.burstInterval;
        }

        /// <summary>Один «тик» стрельбы: веер (если задан) или одиночный снаряд (с разбросом).</summary>
        private void FireOnce()
        {
            if (Def != null && Def.FanCount > 1)
            {
                int pellets = Def.FanCount + (Level - 1) * Def.FanPerLevel;
                float step = MathHelper.ToRadians(Def.FanStepDeg);
                float start = _aimAngle - step * (pellets - 1) / 2f;
                for (int i = 0; i < pellets; i++)
                    SpawnShell(start + step * i);
            }
            else
            {
                float angle = _aimAngle;
                int jitter = Def?.JitterDeg ?? 0;
                if (jitter > 0)
                    angle += MathHelper.ToRadians(_rnd.Next(-jitter, jitter + 1));
                SpawnShell(angle);
            }
        }

        private void SpawnShell(float angle)
        {
            Vector2 dir = DirFromAngle(angle);
            Vector2 spawn = _owner.Position + dir * (_owner.Height * 0.5f);

            Shell shell = new Shell(spawn, Def?.Sprite ?? "Images/bullet", Def?.Piercing ?? false);
            shell.Speed = Options.shellSpeed;
            shell.Damage = (int)(Options.damage * OwnerDamageMult);
            shell.Direction = dir;
            shell.Rotation = angle;
            shell.PlayerSide = (_owner is PlayerShip);

            // Дульная вспышка (цвет по стороне стрелка).
            Color muzzleColor = (_owner is PlayerShip) ? new Color(180, 220, 255) : new Color(255, 170, 120);
            GameManager.Instance.Particles.Explosion(spawn, muzzleColor, Utils.EffectsConfig.MuzzleFlash);

            // Визуальный рост снаряда с уровнем оружия.
            float lvScale = 1f + (Level - 1) * Utils.EffectsConfig.ShellLevelScaleStep;
            shell.Scale = new Vector2(lvScale);
            float glow = MathHelper.Clamp((Level - 1) * 0.22f, 0f, 0.8f);
            shell.Tint = Color.Lerp(Color.White, new Color(255, 235, 150), glow);

            GameManager.Instance.GameObjects.Add(shell);
        }
    }
}
