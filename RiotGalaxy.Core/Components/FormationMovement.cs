using Microsoft.Xna.Framework;
using RiotGalaxy.Core.GameObjects;

namespace RiotGalaxy.Core.Components
{
    /// <summary>
    /// Движение врага в составе формации (улья): летит к своей ячейке, затем «прилипает»
    /// к ней и барражирует вместе со всем ульем (ячейка движется вместе с Hive.Offset).
    /// </summary>
    public class FormationMovement : MovementComponent
    {
        private readonly Hive _hive;
        private readonly int _cx, _cy;

        public FormationMovement(GameObject owner, float speed, Hive hive, int cx, int cy)
            : base(owner, speed)
        {
            _hive = hive;
            _cx = cx;
            _cy = cy;
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            // Летим к своей ячейке; по достижении — «прилипаем» (ячейка движется с ульём).
            Vector2 pos = _owner.Position;
            Utils.MathUtil.MoveTowards(ref pos, _hive.CellWorldPos(_cx, _cy), _speed * dt);
            _owner.Position = pos;
        }
    }
}
