public class EnemyCheckDetectRange : Behavior
{
    private EnemyBase enemy;

    public EnemyCheckDetectRange(EnemyBase enemy)
    {
        this.enemy = enemy;
    }

    protected override EStatus OnUpdate()
    {
        if (enemy == null || !enemy.TryFindTarget()) return EStatus.Failure;

        float distance = enemy.DistanceToTarget();
        return distance <= enemy.detectRange ? EStatus.Success : EStatus.Failure;
    }
}

public class EnemyCheckAttackRange : Behavior
{
    private EnemyBase enemy;

    public EnemyCheckAttackRange(EnemyBase enemy)
    {
        this.enemy = enemy;
    }

    protected override EStatus OnUpdate()
    {
        if (enemy == null || enemy.target == null) return EStatus.Failure;

        if (enemy.currentStateType == EnemyStateType.Attack)
        {
            return EStatus.Success;
        }

        float distance = enemy.DistanceToTarget();
        return distance <= enemy.attackStartRange ? EStatus.Success : EStatus.Failure;
    }
}

public class EnemyCheckDead : Behavior
{
    private EnemyBase enemy;

    public EnemyCheckDead(EnemyBase enemy)
    {
        this.enemy = enemy;
    }

    protected override EStatus OnUpdate()
    {
        return enemy != null && enemy.isDead ? EStatus.Success : EStatus.Failure;
    }
}

public class EnemyCheckHit : Behavior
{
    private EnemyBase enemy;

    public EnemyCheckHit(EnemyBase enemy)
    {
        this.enemy = enemy;
    }

    protected override EStatus OnUpdate()
    {
        return enemy != null && enemy.isHit ? EStatus.Success : EStatus.Failure;
    }
}

public class SetChaseState : Behavior
{
    private EnemyBase enemy;

    public SetChaseState(EnemyBase enemy)
    {
        this.enemy = enemy;
    }

    protected override EStatus OnUpdate()
    {
        if (enemy == null) return EStatus.Failure;

        if (enemy.IsActionLocked)
        {
            return EStatus.Success;
        }

        if (enemy.currentStateType != EnemyStateType.Chase)
        {
            enemy.ChangeState(EnemyStateType.Chase);
        }

        return EStatus.Success;
    }
}

public class SetHitState : Behavior
{
    private EnemyBase enemy;

    public SetHitState(EnemyBase enemy)
    {
        this.enemy = enemy;
    }

    protected override EStatus OnUpdate()
    {
        if (enemy == null) return EStatus.Failure;

        if (enemy.currentStateType != EnemyStateType.Hit)
        {
            enemy.ChangeState(EnemyStateType.Hit);
        }

        return EStatus.Success;
    }
}

public class SetAttackState : Behavior
{
    private EnemyBase enemy;

    public SetAttackState(EnemyBase enemy)
    {
        this.enemy = enemy;
    }

    protected override EStatus OnUpdate()
    {
        if (enemy == null) return EStatus.Failure;

        if (enemy.currentStateType != EnemyStateType.Attack)
        {
            enemy.ChangeState(EnemyStateType.Attack);
        }

        return EStatus.Success;
    }
}

public class SetPatrolState : Behavior
{
    private EnemyBase enemy;

    public SetPatrolState(EnemyBase enemy)
    {
        this.enemy = enemy;
    }

    protected override EStatus OnUpdate()
    {
        if (enemy == null) return EStatus.Failure;

        if (enemy.IsActionLocked)
        {
            return EStatus.Success;
        }

        if (enemy.currentStateType != EnemyStateType.Patrol &&
            enemy.currentStateType != EnemyStateType.Idle)
        {
            enemy.ChangeState(EnemyStateType.Idle);
        }

        return EStatus.Success;
    }
}

public class SetDeadState : Behavior
{
    private EnemyBase enemy;

    public SetDeadState(EnemyBase enemy)
    {
        this.enemy = enemy;
    }

    protected override EStatus OnUpdate()
    {
        if (enemy == null) return EStatus.Failure;

        if (enemy.currentStateType != EnemyStateType.Dead)
        {
            enemy.ChangeState(EnemyStateType.Dead);
        }

        return EStatus.Success;
    }
}


