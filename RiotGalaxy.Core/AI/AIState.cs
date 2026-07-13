using RiotGalaxy.Core.GameObjects;

namespace RiotGalaxy.Core.AI
{
    /// <summary>
    /// Базовое состояние ИИ врага. Порт AIState из CocosSharp (Objects/ObjBehavior/AIState.cs).
    /// Состояние управляет движением/скоростью/стрельбой владельца; переходы решает <see cref="EnemyAI"/>.
    /// </summary>
    public abstract class AIState
    {
        protected readonly Enemy owner;

        /// <summary>Готовность состояния к смене (как ReadyToChange в оригинале).</summary>
        public bool ReadyToChange { get; protected set; } = true;

        protected AIState(Enemy owner) => this.owner = owner;

        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void NewIdea(float dt) { }
        public virtual void Update(float dt) { }
    }

    /// <summary>Взлёт/влёт на экран: полный ход, не стреляет, курс вниз (ai.yaml: states.takeoff). Порт AIStateTakeOff.</summary>
    public class AIStateTakeOff : AIState
    {
        public AIStateTakeOff(Enemy owner) : base(owner) { }

        public override void Enter()
        {
            ReadyToChange = false;
            owner.CurrentSpeed = owner.MaxSpeed; // полный вперёд
            owner.ShootSafe = true;              // не стреляем
            owner.UseBounceMovement();
            NewIdea(0);
        }

        public override void NewIdea(float dt)
        {
            float min = Utils.AiConfig.TakeoffCourseMinDeg, max = Utils.AiConfig.TakeoffCourseMaxDeg;
            owner.SetMoveDirection(min + (float)owner.AiRandom.NextDouble() * (max - min)); // курс вниз
        }
    }

    /// <summary>Роение: медленно (доля скорости — ai.yaml), стреляет, случайный курс. Порт AIStateSwarming.</summary>
    public class AIStateSwarming : AIState
    {
        public AIStateSwarming(Enemy owner) : base(owner) { }

        public override void Enter()
        {
            owner.CurrentSpeed = owner.MaxSpeed * Utils.AiConfig.SwarmingSpeedFrac; // летаем медленнее
            owner.ShootSafe = false;                  // можно стрелять
            owner.UseBounceMovement();
            NewIdea(0);
        }

        public override void NewIdea(float dt)
        {
            owner.SetMoveDirection((float)owner.AiRandom.NextDouble() * 360f); // куда угодно
            ReadyToChange = true;
        }
    }

    /// <summary>Атака: полный ход с отскоком, стреляет, курс вниз (ai.yaml: states.attack). Порт AIStateAttack.</summary>
    public class AIStateAttack : AIState
    {
        public AIStateAttack(Enemy owner) : base(owner) { }

        public override void Enter()
        {
            owner.CurrentSpeed = owner.MaxSpeed;
            owner.ShootSafe = false;
            owner.UseBounceMovement();
            NewIdea(0);
        }

        public override void NewIdea(float dt)
        {
            float min = Utils.AiConfig.AttackCourseMinDeg, max = Utils.AiConfig.AttackCourseMaxDeg;
            owner.SetMoveDirection(min + (float)owner.AiRandom.NextDouble() * (max - min)); // курс круче вниз
            ReadyToChange = true;
        }
    }
}
