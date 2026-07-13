using RiotGalaxy.Core.GameObjects;
using RiotGalaxy.Core.Managers;

namespace RiotGalaxy.Core.AI
{
    /// <summary>
    /// Контроллер ИИ врага — машина состояний. Порт BehAI из CocosSharp
    /// (Objects/ObjBehavior/BehAI.cs): держит текущее состояние, меняет его и раз в ~3 c
    /// генерирует «новую идею». Конкретные типы решают условия переходов.
    /// </summary>
    public abstract class EnemyAI
    {
        protected readonly Enemy owner;
        protected AIState state;
        protected float ideaTime;

        // Граница «зоны роения»: когда враг влетел в верхнюю долю экрана (ai.yaml: common.swarmZoneFrac).
        // (В оригинале swarmingPosition = 0.7*height при Y-вверх; в MonoGame Y-вниз.)
        protected float SwarmY => GameManager.Instance.ScreenHeight * Utils.AiConfig.SwarmZoneFrac;

        protected static float IdeaInterval => Utils.AiConfig.IdeaInterval;

        protected EnemyAI(Enemy owner) => this.owner = owner;

        public void ChangeState(AIState newState)
        {
            state?.Exit();
            state = newState;
            state.Enter();
        }

        public abstract void Update(float dt);
    }

    /// <summary>ИИ-«пустышка»: ничего не делает (порт ObjBehAIDumd).</summary>
    public class EnemyAIDumb : EnemyAI
    {
        public EnemyAIDumb(Enemy owner) : base(owner) { }
        public override void Update(float dt) { }
    }

    /// <summary>Красный: влетает (TakeOff) → роится и стреляет (интервалы — ai.yaml). Порт ObjBehAIEnemyRed.</summary>
    public class EnemyAIRed : EnemyAI
    {
        public EnemyAIRed(Enemy owner) : base(owner) => ChangeState(new AIStateTakeOff(owner));

        public override void Update(float dt)
        {
            state.Update(dt);

            ideaTime += dt;
            if (ideaTime > IdeaInterval)
            {
                state.NewIdea(dt);
                ideaTime = 0f;
            }

            // Влетел в зону роения → Swarming (стрельба реже)
            if (state is AIStateTakeOff && owner.Position.Y > SwarmY)
            {
                ChangeState(new AIStateSwarming(owner));
                owner.SetShootInterval(Utils.AiConfig.RedSwarmShootInterval);
            }
        }
    }

    /// <summary>
    /// Синий: TakeOff → Swarming → иногда Attack (интервалы/шанс — ai.yaml);
    /// улетел за верх — снова TakeOff. Порт ObjBehAIEnemyBlue.
    /// </summary>
    public class EnemyAIBlue : EnemyAI
    {
        public EnemyAIBlue(Enemy owner) : base(owner) => ChangeState(new AIStateTakeOff(owner));

        public override void Update(float dt)
        {
            state.Update(dt);

            ideaTime += dt;
            if (ideaTime > IdeaInterval)
            {
                state.NewIdea(dt);
                ideaTime = 0f;

                // Из роения иногда срываемся в атаку (стреляем чаще)
                if (state is AIStateSwarming && owner.AiRandom.NextDouble() < Utils.AiConfig.BlueAttackChance)
                {
                    ChangeState(new AIStateAttack(owner));
                    owner.SetShootInterval(Utils.AiConfig.BlueAttackShootInterval);
                }
            }

            // Влетел в зону роения → Swarming
            if (state is AIStateTakeOff && owner.Position.Y > SwarmY)
            {
                ChangeState(new AIStateSwarming(owner));
                owner.SetShootInterval(Utils.AiConfig.BlueSwarmShootInterval);
            }

            // Атакуя, улетел за верхнюю границу → снова заходим на взлёт
            if (state is AIStateAttack && owner.Position.Y < 0)
                ChangeState(new AIStateTakeOff(owner));
        }
    }
}
