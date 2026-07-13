using System;
using Microsoft.Xna.Framework;
using RiotGalaxy.Core.GameObjects;
using RiotGalaxy.Core.Managers;

namespace RiotGalaxy.Core.Components
{
    /// <summary>Тактика пике при вылете из улья (задаётся списком в enemies.yaml).</summary>
    public enum SortieTactic
    {
        Random,    // прямой курс вниз с уклоном + отскок от боков
        Snake,     // змейкой (синус по X) с уходом вниз
        Ram,       // таран — прямо в точку, где был игрок на момент вылета
        Ellipse,   // петля/дуга (Galaga-стиль) с уходом вниз
        Zigzag,    // резкий зигзаг (треугольная волна по X) с уходом вниз
        Spiral,    // расширяющийся штопор (радиус растёт) с уходом вниз
        Swoop,     // дайв-бомба: разгон вниз, боковой замах гаснет
        Strafe,    // спуск → проход поперёк экрана (штурмовка) → снова вниз
        Homing,    // самонаведение: непрерывно доворачивает на игрока
        Boomerang  // ныряет вниз, тормозит и возвращается вверх (уход через верх)
    }

    /// <summary>
    /// Вылет из улья (Galaga-стиль): враг пикирует (стреляя) по выбранной тактике, уходит за
    /// границу экрана, затем возвращается в свою ячейку улья (снова FormationMovement).
    /// Управляется ульем (см. <see cref="GameObjects.Hive"/>).
    /// </summary>
    public class SortieMovement : MovementComponent
    {
        private enum Phase { Dive, Return }

        private readonly Hive _hive;
        private readonly int _cx, _cy;
        private readonly SortieTactic _tactic;
        private Phase _phase = Phase.Dive;

        // Старт пике (для траекторных тактик) и накопленное время
        private readonly float _startX, _startY;
        private float _t;

        // Прямолинейные/скоростные тактики (Random/Ram/Swoop/Homing/Boomerang)
        private float _vx, _vy;
        // Параметры траекторных тактик (Snake/Ellipse/Zigzag/Spiral)
        private float _amp, _omega, _radius, _descend, _dir = 1f, _grow;
        // Ускорение/поворот/штурмовка
        private float _accel, _turn, _strafeY;
        private int _strafeStage;

        // Страховка от «зависания»: максимум времени в пике до принудительного возврата (ai.yaml).
        private static float MaxDiveTime => Utils.AiConfig.Sortie.MaxDiveTime;

        public SortieMovement(GameObject owner, float speed, Hive hive, int cx, int cy, SortieTactic tactic)
            : base(owner, speed)
        {
            _hive = hive;
            _cx = cx;
            _cy = cy;
            _tactic = tactic;
            _startX = owner.Position.X;
            _startY = owner.Position.Y;

            var rnd = (owner as Enemy)?.AiRandom;
            double R() => rnd?.NextDouble() ?? 0.5;
            float side() => R() < 0.5 ? -1f : 1f;

            var gm = GameManager.Instance;
            var cfg = Utils.AiConfig.Sortie; // форма тактик — ai.yaml (секция sortie)

            switch (tactic)
            {
                case SortieTactic.Ram:
                {
                    var player = gm.Player;
                    Vector2 dir = (player != null) ? player.Position - owner.Position : new Vector2(0, speed);
                    if (dir.LengthSquared() < 0.001f) dir = new Vector2(0, speed);
                    dir.Normalize();
                    _vx = dir.X * speed;
                    _vy = dir.Y * speed;
                    break;
                }
                case SortieTactic.Snake:
                    _amp = cfg.SnakeAmplitude;
                    _descend = speed * cfg.SnakeDescendFrac;
                    _omega = (speed * cfg.SnakeWaveSpeedFrac) / _amp;
                    break;
                case SortieTactic.Ellipse:
                    _radius = cfg.EllipseRadius;
                    _omega = speed / _radius;
                    _descend = speed * cfg.EllipseDescendFrac;
                    _dir = side(); // петля влево/вправо
                    break;
                case SortieTactic.Zigzag:
                    // Треугольная волна по X (острее змейки) + равномерный спуск.
                    _amp = cfg.ZigzagAmplitude;
                    _descend = speed * cfg.ZigzagDescendFrac;
                    _omega = (speed * cfg.ZigzagWaveSpeedFrac) / _amp;
                    break;
                case SortieTactic.Spiral:
                    // Штопор с растущим радиусом.
                    _radius = cfg.SpiralStartRadius;
                    _grow = cfg.SpiralRadiusGrow;               // px/сек прирост радиуса
                    _omega = speed / cfg.SpiralOmegaDivisor;
                    _descend = speed * cfg.SpiralDescendFrac;
                    _dir = side();
                    break;
                case SortieTactic.Swoop:
                {
                    // Дайв-бомба: боковой замах в сторону игрока, вертикаль разгоняется.
                    var player = gm.Player;
                    float sx = (player != null && player.Position.X < owner.Position.X) ? -1f : 1f;
                    _vx = sx * speed * cfg.SwoopSideFrac;
                    _vy = speed * cfg.SwoopDownFrac;
                    _accel = speed * cfg.SwoopAccelFrac; // ускорение вниз
                    break;
                }
                case SortieTactic.Strafe:
                    _descend = speed;
                    _strafeY = gm.ScreenHeight * cfg.StrafeTurnYFrac; // до этой высоты спускаемся, потом штурмуем
                    _dir = side();
                    _strafeStage = 0;
                    break;
                case SortieTactic.Homing:
                    _vx = 0f;
                    _vy = speed;                 // старт вниз, дальше доворот на игрока
                    _turn = cfg.HomingTurnSpeed; // рад/сек — скорость доворота
                    break;
                case SortieTactic.Boomerang:
                    _vx = side() * speed * cfg.BoomerangSideFrac;
                    _vy = speed * cfg.BoomerangDownFrac;         // вниз
                    _accel = -(speed * cfg.BoomerangBrakeFrac);  // торможение → разворот вверх
                    break;
                default: // Random
                {
                    var dir = Utils.MathUtil.DirFromAngleDeg(
                        cfg.RandomCourseMinDeg + (float)(R() * (cfg.RandomCourseMaxDeg - cfg.RandomCourseMinDeg))) * speed;
                    _vx = dir.X;
                    _vy = dir.Y;
                    break;
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var gm = GameManager.Instance;

            if (_phase == Phase.Dive)
            {
                _t += dt;
                Vector2 p = _owner.Position;

                switch (_tactic)
                {
                    case SortieTactic.Snake:
                        p.X = _startX + _amp * (float)Math.Sin(_omega * _t);
                        p.Y = _startY + _descend * _t;
                        break;

                    case SortieTactic.Ellipse:
                    {
                        float theta = _omega * _t;
                        p.X = _startX + _dir * _radius * (float)Math.Sin(theta);
                        p.Y = _startY + _descend * _t - _radius * (float)Math.Cos(theta) + _radius;
                        break;
                    }

                    case SortieTactic.Zigzag:
                    {
                        p.Y = _startY + _descend * _t;
                        double ph = (_omega * _t) % (2.0 * Math.PI);
                        if (ph < 0) ph += 2.0 * Math.PI;
                        // треугольная волна [-1..1]
                        float tri = ph < Math.PI ? (float)(-1.0 + 2.0 * ph / Math.PI)
                                                 : (float)(3.0 - 2.0 * ph / Math.PI);
                        p.X = _startX + _amp * tri;
                        break;
                    }

                    case SortieTactic.Spiral:
                        p.X = _startX + _dir * (_radius + _grow * _t) * (float)Math.Sin(_omega * _t);
                        p.Y = _startY + _descend * _t;
                        break;

                    case SortieTactic.Swoop:
                        _vy += _accel * dt;   // разгон вниз
                        _vx *= 0.96f;         // боковой замах гаснет
                        p += new Vector2(_vx, _vy) * dt;
                        break;

                    case SortieTactic.Strafe:
                        if (_strafeStage == 0) // спуск до высоты штурмовки
                        {
                            p.Y += _descend * dt;
                            if (p.Y >= _strafeY) _strafeStage = 1;
                        }
                        else if (_strafeStage == 1) // проход поперёк
                        {
                            p.X += _dir * _speed * dt;
                            p.Y += _descend * 0.15f * dt;
                            float b = gm.ScreenWidth * 0.08f;
                            if ((_dir > 0 && p.X > gm.ScreenWidth - b - _owner.Width) || (_dir < 0 && p.X < b))
                                _strafeStage = 2;
                        }
                        else // снова вниз
                        {
                            p.Y += _descend * dt;
                        }
                        break;

                    case SortieTactic.Homing:
                    {
                        var player = gm.Player;
                        Vector2 v = new Vector2(_vx, _vy);
                        if (player != null)
                        {
                            Vector2 des = player.Position - p;
                            if (des.LengthSquared() > 1f)
                            {
                                float ang = (float)Math.Atan2(v.Y, v.X);
                                float tang = (float)Math.Atan2(des.Y, des.X);
                                float diff = MathHelper.WrapAngle(tang - ang);
                                float step = _turn * dt;
                                ang += Math.Abs(diff) < step ? diff : Math.Sign(diff) * step;
                                v = new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * _speed;
                                _vx = v.X; _vy = v.Y;
                            }
                        }
                        p += v * dt;
                        break;
                    }

                    case SortieTactic.Boomerang:
                        p += new Vector2(_vx, _vy) * dt;
                        _vy += _accel * dt;   // тормозит и уходит вверх
                        break;

                    case SortieTactic.Ram:
                        p += new Vector2(_vx, _vy) * dt;
                        break;

                    default: // Random — курс вниз с отскоком от боков
                        p += new Vector2(_vx, _vy) * dt;
                        float border = gm.ScreenWidth * 0.10f;
                        if (p.X < border) { _vx = -_vx; p.X = border + 1; }
                        else if (p.X > gm.ScreenWidth - border - _owner.Width)
                        {
                            _vx = -_vx;
                            p.X = gm.ScreenWidth - border - _owner.Width - 1;
                        }
                        break;
                }

                // Удержим в пределах экрана по X
                p.X = MathHelper.Clamp(p.X, 0f, gm.ScreenWidth - _owner.Width);

                // Ушёл за низ → появляется сверху и заходит на возврат; ушёл за верх (бумеранг) → сразу возврат.
                if (p.Y > gm.ScreenHeight + 50f)
                {
                    p.Y = -50f;
                    _phase = Phase.Return;
                }
                else if (p.Y < -80f)
                {
                    _phase = Phase.Return;
                }
                else if (_t > MaxDiveTime)
                {
                    _phase = Phase.Return; // страховка: слишком долго в пике
                }

                _owner.Position = p;
            }
            else // Return — летим к своей ячейке
            {
                Vector2 pos = _owner.Position;
                bool arrived = Utils.MathUtil.MoveTowards(ref pos, _hive.CellWorldPos(_cx, _cy), _speed * dt);
                _owner.Position = pos;
                if (arrived)
                {
                    // Единая скорость строя (как при JoinFormation), не sortie-скорость.
                    float formSpeed = (_owner is Enemy en) ? en.FormationSpeed : _speed;
                    _owner.Movement = new FormationMovement(_owner, formSpeed, _hive, _cx, _cy);
                    if (_owner is Enemy e)
                    {
                        e.ShootSafe = true;
                        _hive.NotifyReturned(e);
                    }
                }
            }
        }

        /// <summary>Разобрать строку тактики (из enemies.yaml). Неизвестное → Random.</summary>
        public static SortieTactic ParseTactic(string s)
        {
            switch (s?.Trim().ToLowerInvariant())
            {
                case "snake": return SortieTactic.Snake;
                case "ram": return SortieTactic.Ram;
                case "ellipse": return SortieTactic.Ellipse;
                case "zigzag": return SortieTactic.Zigzag;
                case "spiral": return SortieTactic.Spiral;
                case "swoop": return SortieTactic.Swoop;
                case "strafe": return SortieTactic.Strafe;
                case "homing": return SortieTactic.Homing;
                case "boomerang": return SortieTactic.Boomerang;
                default: return SortieTactic.Random;
            }
        }
    }
}
