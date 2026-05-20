/// <summary>
/// 普通近战敌人：巡逻、发现玩家后追击、进入攻击距离后攻击。
/// </summary>
public class OrdinaryEnemy : EnemyBase
{
    private bool hasCurrentState;

    protected override void InitBehaviorTree()
    {
        var builder = new BehaviorTreeBuilder();

        behaviorTree = builder
            .Selector()
                .Sequence()
                    .Add(new EnemyCheckDead(this))
                    .Add(new SetDeadState(this))
                    .Back()
                .Sequence()
                    .Add(new EnemyCheckHit(this))
                    .Add(new SetHitState(this))
                    .Back()
                .Sequence()
                    .Add(new EnemyCheckDetectRange(this))
                    .Add(new EnemyCheckAttackRange(this))
                    .Add(new SetAttackState(this))
                    .Back()
                .Sequence()
                    .Add(new EnemyCheckDetectRange(this))
                    .Add(new SetChaseState(this))
                    .Back()
                .Add(new SetPatrolState(this))
                .Back()
            .End();
    }

    public override void ChangeState(EnemyStateType newState)
    {
        if (isDead && newState != EnemyStateType.Dead)
        {
            return;
        }

        if (hasCurrentState && currentStateType == newState)
        {
            if (newState == EnemyStateType.Hit)
            {
                stateMachine.ReChangeState<EnemyHit>();
            }

            return;
        }

        currentStateType = newState;
        hasCurrentState = true;

        switch (newState)
        {
            case EnemyStateType.Idle:
                stateMachine.ChangeState<EnemyIdle>();
                break;
            case EnemyStateType.Patrol:
                stateMachine.ChangeState<EnemyPatrol>();
                break;
            case EnemyStateType.Chase:
                stateMachine.ChangeState<EnemyChase>();
                break;
            case EnemyStateType.Hit:
                stateMachine.ChangeState<EnemyHit>();
                break;
            case EnemyStateType.Attack:
                stateMachine.ChangeState<EnemyAttack>();
                break;
            case EnemyStateType.Dead:
                stateMachine.ChangeState<EnemyDead>();
                break;
        }
    }
}
