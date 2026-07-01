using System;
using Microsoft.Xna.Framework;
using RiotGalaxy.Core.GameObjects;
using RiotGalaxy.Core.Managers;

namespace RiotGalaxy.Core.AI
{
    /// <summary>
    /// Босс с фазами по HP и телеграфом атак. Полностью ведёт движение и стрельбу владельца
    /// (Enemy.ShootSafe=true, Movement=null), таймерная стрельба Enemy отключена.
    ///
    /// Влёт сверху → зависание у верхней кромки + горизонтальный свип. Фазы по доле HP:
    ///   P1 (>66%) — прицельные очереди (3 снаряда);
    ///   P2 (>33%) — веер вниз (7) + прицельный;
    ///   P3 (≤33%) — радиальный залп (12) + подмога, всё быстрее.
    /// Перед каждым залпом — ТЕЛЕГРАФ: ~0.6с вспышка + почти остановка (игроку видно атаку).
    /// </summary>
    public class BossAI : EnemyAI
    {
        private readonly float _hoverY;
        private readonly float _centerX;
        private readonly float _amp;

        private float _t;             // общее время (свип/покачивание)
        private float _attackTimer;
        private bool _telegraph;
        private float _telegraphT;
        private int _phase = 1;
        private bool _arrived;
        private bool _addsSpawned;    // подмога в фазе 3 — один раз

        public BossAI(Enemy owner) : base(owner)
        {
            owner.ShootSafe = true;   // стрельбу ведёт BossAI, не таймер Enemy
            owner.Movement = null;    // движением тоже рулим вручную
            _hoverY  = GameManager.Instance.ScreenHeight * 0.18f;
            _centerX = GameManager.Instance.ScreenWidth * 0.5f;
            _amp     = GameManager.Instance.ScreenWidth * 0.32f;
            _attackTimer = 1.2f;      // пауза перед первой атакой
        }

        private float HpFrac => owner.MaxHp > 0 ? (float)owner.Hp / owner.MaxHp : 0f;
        private int PhaseForHp => HpFrac > 0.66f ? 1 : (HpFrac > 0.33f ? 2 : 3);
        private float AttackInterval => _phase == 1 ? 2.2f : (_phase == 2 ? 1.8f : 1.4f);
        private float SweepSpeed => _phase == 1 ? 0.8f : (_phase == 2 ? 1.2f : 1.7f);

        public override void Update(float dt)
        {
            _t += dt;

            // ── влёт сверху к точке зависания ──────────────────────────────
            if (!_arrived)
            {
                var p = owner.Position;
                p.X = _centerX;
                p.Y += 90f * dt;
                owner.Position = p;
                if (p.Y >= _hoverY) _arrived = true;
                return;
            }

            // ── смена фазы по HP ───────────────────────────────────────────
            int ph = PhaseForHp;
            if (ph != _phase) { _phase = ph; OnEnterPhase(ph); }

            // ── движение: свип по X (во время телеграфа почти стоим) + покачивание ─
            float sweep = _telegraph ? 0.15f : 1f;
            var pos = owner.Position;
            pos.X = _centerX + _amp * (float)Math.Sin(_t * SweepSpeed) * sweep;
            pos.Y = _hoverY + (float)Math.Sin(_t * 0.6f) * 18f;
            owner.Position = pos;

            // ── телеграф → залп ────────────────────────────────────────────
            if (_telegraph)
            {
                _telegraphT -= dt;
                float k = 0.5f + 0.5f * (float)Math.Sin(_telegraphT * 30f); // пульс
                owner.Tint = Color.Lerp(Color.White, new Color(255, 60, 40), k);
                if (_telegraphT <= 0f)
                {
                    _telegraph = false;
                    owner.Tint = Color.White;
                    FirePattern(_phase);
                }
                return;
            }

            _attackTimer -= dt;
            if (_attackTimer <= 0f)
            {
                _attackTimer = AttackInterval;
                _telegraph = true;
                _telegraphT = 0.6f;
            }
        }

        private void OnEnterPhase(int ph)
        {
            MessageLog.Add(ph == 3 ? "Босс в ярости!" : "Босс меняет тактику…",
                           ph == 3 ? Color.OrangeRed : Color.Gold);
            GameManager.Instance.Shake(ph == 3 ? 8f : 4f);
            if (ph == 3 && !_addsSpawned) { _addsSpawned = true; SpawnAdds(2); }
            _attackTimer = 0.6f; // быстрее перейти к атаке новой фазы
        }

        private void FirePattern(int ph)
        {
            var gun = owner.Gun;
            if (gun == null) return;
            var player = GameManager.Instance.Player;

            switch (ph)
            {
                case 1: // прицельная очередь из 3 (узкий веер вокруг направления на игрока)
                    {
                        float a = AimAngle(player);
                        for (int i = -1; i <= 1; i++)
                            gun.FireShell(a + MathHelper.ToRadians(10f * i));
                        break;
                    }
                case 2: // веер вниз (7) + 1 прицельный
                    {
                        const int n = 7;
                        float spread = MathHelper.ToRadians(110f);
                        float start = MathHelper.Pi - spread / 2f; // π = вниз
                        for (int i = 0; i < n; i++)
                            gun.FireShell(start + spread * i / (n - 1));
                        gun.FireShell(AimAngle(player));
                        break;
                    }
                default: // радиальный залп (12 по кругу)
                    {
                        const int n = 12;
                        for (int i = 0; i < n; i++)
                            gun.FireShell(MathHelper.TwoPi * i / n);
                        break;
                    }
            }
            GameManager.Instance.Shake(3f);
        }

        private float AimAngle(PlayerShip player)
        {
            if (player == null) return MathHelper.Pi; // вниз
            Vector2 d = player.Position - owner.Position;
            float angle = (float)Math.Atan2(d.X, -d.Y); // конвенция Weapon.Aim: 0=вверх, π=вниз
            // Тот же конус вниз, что и у обычных врагов (радиальный залп фазы 3 не через этот метод).
            float maxRad = MathHelper.ToRadians(Utils.GameOptions.EnemyAimMaxDeg);
            float delta = MathHelper.Clamp(MathHelper.WrapAngle(angle - MathHelper.Pi), -maxRad, maxRad);
            return MathHelper.Pi + delta;
        }

        private void SpawnAdds(int count)
        {
            for (int i = 0; i < count; i++)
            {
                float dx = (i == 0) ? -70f : 70f;
                var add = new Enemy(EnemyType.SM_SCOUT, new Vector2(owner.Position.X + dx, owner.Position.Y + 30f));
                GameManager.Instance.GameObjects.Add(add);
            }
        }
    }
}
