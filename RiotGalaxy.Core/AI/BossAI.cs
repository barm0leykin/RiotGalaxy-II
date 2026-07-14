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

        private float _t;             // общее время (покачивание)
        private float _movePhase;     // фаза паттерна движения (аккумулируется — смена скорости без скачков)
        private Vector2 _roamTarget;  // текущая цель блуждания (movement: roam)
        private float _roamTimer;
        private float _attackTimer;
        private bool _telegraph;
        private float _telegraphT;
        private int _phase;           // индекс текущей фазы в _def.Phases
        private bool _arrived;
        private readonly bool[] _addsSpawned; // подмога каждой фазы — один раз

        /// <summary>Отложенный выстрел «волны» (shotDelay в атаке): таймер + параметры снаряда.</summary>
        private struct PendingShot
        {
            public float Delay;
            public float Angle;
            public string Sprite;   // null — обычный вражеский снаряд
            public bool Piercing;
        }
        private readonly System.Collections.Generic.List<PendingShot> _pending
            = new System.Collections.Generic.List<PendingShot>();

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

            // ── отложенные выстрелы «волны» (идут независимо от телеграфа) ──
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                var shot = _pending[i];
                shot.Delay -= dt;
                if (shot.Delay <= 0f)
                {
                    owner.Gun?.FireShell(shot.Angle, shot.Sprite, shot.Piercing);
                    _pending.RemoveAt(i);
                }
                else
                {
                    _pending[i] = shot;
                }
            }

            // ── движение: паттерн фазы даёт ЦЕЛЬ, босс плывёт к ней с ограниченной скоростью ──
            // (никаких телепортов: смена фазы/паттерна — плавный перелёт; телеграф замедляет)
            float slow = _telegraph ? 0.15f : 1f;
            _movePhase += Phase.SweepSpeed * slow * dt;
            Vector2 target = MovementTarget(dt);
            target.Y += (float)Math.Sin(_t * _def.BobSpeed) * _def.BobAmplitude; // лёгкое покачивание поверх
            Vector2 pos = owner.Position;
            Utils.MathUtil.MoveTowards(ref pos, target, _def.MoveSpeed * slow * dt);
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

        /// <summary>Целевая точка паттерна движения текущей фазы (movement в ai.yaml).</summary>
        private Vector2 MovementTarget(float dt)
        {
            float vertAmp = GameManager.Instance.ScreenHeight * _def.VertAmpFrac;
            switch (Phase.Movement.Trim().ToLowerInvariant())
            {
                case "static": // висит в точке зависания (крепкий босс-«крепость»)
                    return new Vector2(_centerX, _hoverY);

                case "figure8": // горизонтальная восьмёрка (Лиссажу 1:2)
                    return new Vector2(
                        _centerX + _amp * (float)Math.Sin(_movePhase),
                        _hoverY + vertAmp * (float)Math.Sin(_movePhase * 2f));

                case "circle": // круг: вниз-в сторону-вверх
                    return new Vector2(
                        _centerX + _amp * (float)Math.Sin(_movePhase),
                        _hoverY + vertAmp * (1f - (float)Math.Cos(_movePhase)));

                case "roam": // «роится»: случайные точки в верхней зоне, смена раз в ~1.5с/по прибытии
                {
                    _roamTimer -= dt;
                    bool near = Vector2.DistanceSquared(owner.Position, _roamTarget) < 24f * 24f;
                    if (_roamTimer <= 0f || near || _roamTarget == Vector2.Zero)
                    {
                        _roamTimer = 1.5f;
                        var r = owner.AiRandom;
                        _roamTarget = new Vector2(
                            _centerX + _amp * (float)(r.NextDouble() * 2 - 1),
                            _hoverY + vertAmp * (float)(r.NextDouble() * 2 - 1) + vertAmp);
                    }
                    return _roamTarget;
                }

                default: // sweep — классический синус по X на высоте зависания
                    return new Vector2(_centerX + _amp * (float)Math.Sin(_movePhase), _hoverY);
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

        /// <summary>
        /// Исполнить все паттерны залпа текущей фазы. Паттерн собирает список углов (и опц.
        /// спрайт/пробивание снаряда для type: weapon); shotDelay > 0 превращает залп в «волну» —
        /// снаряды выходят поочерёдно (очередь _pending), иначе выпускаются разом.
        /// </summary>
        private void FirePhaseAttacks(Utils.AiConfig.PhaseDef phase)
        {
            var gun = owner.Gun;
            if (gun == null) return;
            var player = GameManager.Instance.Player;
            var angles = new System.Collections.Generic.List<float>();

            foreach (var a in phase.Attacks)
            {
                angles.Clear();
                string sprite = null;
                bool piercing = false;

                switch (a.Type.Trim().ToLowerInvariant())
                {
                    case "aimedburst": // очередь вокруг направления на игрока (шаг spreadDeg)
                    case "aimedfan":
                    {
                        float aim = AimAngle(player);
                        float step = MathHelper.ToRadians(a.SpreadDeg);
                        float start = aim - step * (a.Count - 1) / 2f;
                        for (int i = 0; i < a.Count; i++)
                            angles.Add(start + step * i);
                        break;
                    }
                    case "fandown": // веер вниз полной шириной spreadDeg (с shotDelay — «волна» слева направо)
                    {
                        int n = Math.Max(2, a.Count);
                        float spread = MathHelper.ToRadians(a.SpreadDeg);
                        float start = MathHelper.Pi - spread / 2f; // π = вниз
                        for (int i = 0; i < n; i++)
                            angles.Add(start + spread * i / (n - 1));
                        break;
                    }
                    case "radial": // залп по кругу (с shotDelay — «вертушка»)
                    {
                        int n = Math.Max(1, a.Count);
                        for (int i = 0; i < n; i++)
                            angles.Add(MathHelper.TwoPi * i / n);
                        break;
                    }
                    case "spiral": // вращающаяся серия: угол крутится на spreadDeg за снаряд (задать shotDelay!)
                    {
                        float step = MathHelper.ToRadians(a.SpreadDeg);
                        for (int i = 0; i < Math.Max(1, a.Count); i++)
                            angles.Add(MathHelper.Pi + step * i); // старт вниз, дальше по кругу
                        break;
                    }
                    case "weapon": // стреляем «оружием игрока» из weapons.yaml (спрайт/пирсинг/джиттер/веер)
                    {
                        var def = Weapons.WeaponConfig.Get(a.WeaponId);
                        sprite = def?.Sprite;
                        piercing = def?.Piercing ?? false;
                        float aim = AimAngle(player);
                        for (int i = 0; i < Math.Max(1, a.Count); i++)
                        {
                            if (def != null && def.FanCount > 1)
                            {
                                // Веерное оружие (spread): каждый «выстрел» — веер целиком.
                                float fstep = MathHelper.ToRadians(def.FanStepDeg);
                                float fstart = aim - fstep * (def.FanCount - 1) / 2f;
                                for (int f = 0; f < def.FanCount; f++)
                                    angles.Add(fstart + fstep * f);
                            }
                            else
                            {
                                // Одиночный ствол: разброс оружия (пулемёт) на каждый снаряд.
                                int jitter = def?.JitterDeg ?? 0;
                                float jr = jitter > 0
                                    ? MathHelper.ToRadians(owner.AiRandom.Next(-jitter, jitter + 1)) : 0f;
                                angles.Add(aim + jr);
                            }
                        }
                        break;
                    }
                    default: // aimedShot — одиночный прицельный
                        angles.Add(AimAngle(player));
                        break;
                }

                if (a.ShotDelay > 0f)
                {
                    // «Волна»: поочерёдный выпуск с интервалом shotDelay.
                    for (int i = 0; i < angles.Count; i++)
                        _pending.Add(new PendingShot
                        {
                            Delay = a.ShotDelay * (i + 1),
                            Angle = angles[i],
                            Sprite = sprite,
                            Piercing = piercing,
                        });
                }
                else
                {
                    foreach (float ang in angles)
                        gun.FireShell(ang, sprite, piercing);
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
