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
    /// ПОЛНОСТЬЮ data-driven (ai.yaml): у каждого типа босса свой конфиг (bosses.&lt;тип&gt;,
    /// незаданное — из bossDefault): движение/телеграф + СПИСОК ФАЗ, у каждой фазы свои
    /// паттерны атак (aimedBurst/aimedFan/fanDown/radial/aimedShot), темп, скорость свипа,
    /// «оружие» (скорость/урон снаряда) и подмога (тип+количество).
    /// Перед каждым залпом — ТЕЛЕГРАФ: вспышка + почти остановка (игроку видно атаку).
    /// </summary>
    public class BossAI : EnemyAI
    {
        private readonly Utils.AiConfig.BossDef _def;
        private readonly float _hoverY;
        private readonly float _centerX;
        private readonly float _amp;

        private float _t;             // общее время (свип/покачивание)
        private float _attackTimer;
        private bool _telegraph;
        private float _telegraphT;
        private int _phase;           // индекс текущей фазы в _def.Phases
        private bool _arrived;
        private readonly bool[] _addsSpawned; // подмога каждой фазы — один раз

        public BossAI(Enemy owner) : base(owner)
        {
            owner.ShootSafe = true;   // стрельбу ведёт BossAI, не таймер Enemy
            owner.Movement = null;    // движением тоже рулим вручную
            _def = Utils.AiConfig.GetBoss(owner.Type); // пер-босс конфиг (ai.yaml)
            _hoverY  = GameManager.Instance.ScreenHeight * _def.HoverYFrac;
            _centerX = GameManager.Instance.ScreenWidth * _def.CenterXFrac;
            _amp     = GameManager.Instance.ScreenWidth * _def.SweepAmpFrac;
            _attackTimer = _def.FirstAttackDelay; // пауза перед первой атакой
            _addsSpawned = new bool[Math.Max(1, _def.Phases.Count)];
        }

        private float HpFrac => owner.MaxHp > 0 ? (float)owner.Hp / owner.MaxHp : 0f;

        private Utils.AiConfig.PhaseDef Phase =>
            _def.Phases[Math.Clamp(_phase, 0, _def.Phases.Count - 1)];

        /// <summary>Индекс фазы по доле HP: первая в списке, чей порог ниже текущего HP.</summary>
        private int PhaseIndexForHp()
        {
            for (int i = 0; i < _def.Phases.Count; i++)
                if (HpFrac > _def.Phases[i].HpAbove)
                    return i;
            return _def.Phases.Count - 1;
        }

        public override void Update(float dt)
        {
            _t += dt;

            // ── влёт сверху к точке зависания ──────────────────────────────
            if (!_arrived)
            {
                var p = owner.Position;
                p.X = _centerX;
                p.Y += _def.EntrySpeed * dt;
                owner.Position = p;
                if (p.Y >= _hoverY)
                {
                    _arrived = true;
                    ApplyPhaseWeapon(Phase); // «оружие» первой фазы
                    GameManager.Instance.ShowBossTaunt("intro"); // реплика босса — когда долетел и виден
                    Barks.Fire("bossAppear");                     // ответная реплика пилота
                }
                return;
            }

            // ── смена фазы по HP ───────────────────────────────────────────
            int ph = PhaseIndexForHp();
            if (ph != _phase) { _phase = ph; OnEnterPhase(ph); }

            // ── движение: свип по X (во время телеграфа почти стоим) + покачивание ─
            float sweep = _telegraph ? 0.15f : 1f;
            var pos = owner.Position;
            pos.X = _centerX + _amp * (float)Math.Sin(_t * Phase.SweepSpeed) * sweep;
            pos.Y = _hoverY + (float)Math.Sin(_t * _def.BobSpeed) * _def.BobAmplitude;
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
                    FirePhaseAttacks(Phase);
                }
                return;
            }

            _attackTimer -= dt;
            if (_attackTimer <= 0f)
            {
                _attackTimer = Phase.AttackInterval;
                _telegraph = true;
                _telegraphT = _def.TelegraphTime;
            }
        }

        private void OnEnterPhase(int idx)
        {
            // Теги реплик в диалогах миссий: phase2/phase3 (нумерация фаз 1-based).
            GameManager.Instance.ShowBossTaunt($"phase{idx + 1}");
            Barks.Fire("bossPhase");                                          // ответ пилота
            GameManager.Instance.Shake(idx >= _def.Phases.Count - 1 ? 8f : 4f);

            var phase = Phase;
            ApplyPhaseWeapon(phase);
            if (phase.AddsCount > 0 && idx < _addsSpawned.Length && !_addsSpawned[idx])
            {
                _addsSpawned[idx] = true;
                SpawnAdds(phase.AddsCount, phase.AddsType);
            }
            _attackTimer = _def.PhaseAttackDelay; // быстрее перейти к атаке новой фазы
        }

        /// <summary>«Оружие фазы»: скорость/урон снаряда босса (>0 в конфиге → применить).</summary>
        private void ApplyPhaseWeapon(Utils.AiConfig.PhaseDef phase)
        {
            var opts = owner.Gun?.Options;
            if (opts == null) return;
            if (phase.ShellSpeed > 0f) opts.shellSpeed = phase.ShellSpeed;
            if (phase.ShellDamage > 0f) opts.damage = phase.ShellDamage;
        }

        /// <summary>Исполнить все паттерны залпа текущей фазы.</summary>
        private void FirePhaseAttacks(Utils.AiConfig.PhaseDef phase)
        {
            var gun = owner.Gun;
            if (gun == null) return;
            var player = GameManager.Instance.Player;

            foreach (var a in phase.Attacks)
            {
                switch (a.Type.Trim().ToLowerInvariant())
                {
                    case "aimedburst": // очередь вокруг направления на игрока (шаг spreadDeg)
                    case "aimedfan":
                    {
                        float aim = AimAngle(player);
                        float step = MathHelper.ToRadians(a.SpreadDeg);
                        float start = aim - step * (a.Count - 1) / 2f;
                        for (int i = 0; i < a.Count; i++)
                            gun.FireShell(start + step * i);
                        break;
                    }
                    case "fandown": // веер вниз полной шириной spreadDeg
                    {
                        int n = Math.Max(2, a.Count);
                        float spread = MathHelper.ToRadians(a.SpreadDeg);
                        float start = MathHelper.Pi - spread / 2f; // π = вниз
                        for (int i = 0; i < n; i++)
                            gun.FireShell(start + spread * i / (n - 1));
                        break;
                    }
                    case "radial": // залп по кругу
                    {
                        int n = Math.Max(1, a.Count);
                        for (int i = 0; i < n; i++)
                            gun.FireShell(MathHelper.TwoPi * i / n);
                        break;
                    }
                    default: // aimedShot — одиночный прицельный
                        gun.FireShell(AimAngle(player));
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
            // Тот же конус вниз, что и у обычных врагов (радиальный залп — не через этот метод).
            float maxRad = MathHelper.ToRadians(Utils.GameOptions.EnemyAimMaxDeg);
            float delta = MathHelper.Clamp(MathHelper.WrapAngle(angle - MathHelper.Pi), -maxRad, maxRad);
            return MathHelper.Pi + delta;
        }

        /// <summary>Подмога: тип из конфига фазы (имя врага из enemies.yaml), веером от центра босса.</summary>
        private void SpawnAdds(int count, string typeName)
        {
            if (!Utils.EnemyConfig.TryParseType(typeName ?? "scout", out var type))
                type = EnemyType.SM_SCOUT;
            for (int i = 0; i < count; i++)
            {
                float dx = (i % 2 == 0 ? -1f : 1f) * _def.AddsOffsetX * (1 + i / 2);
                var add = new Enemy(type, new Vector2(owner.Position.X + dx, owner.Position.Y + 30f));
                GameManager.Instance.GameObjects.Add(add);
            }
        }
    }
}
