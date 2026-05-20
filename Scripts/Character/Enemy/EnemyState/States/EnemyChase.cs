using UnityEngine;

public class EnemyChase : EnemyStateBase
{
    public override void Enter()
    {
        PlayAnimation(enemy.runAnimationName);
        enemy.BeginRootMotionMove();
    }

    public override void Update()
    {
        if (enemy.target == null)
        {
            enemy.ChangeState(EnemyStateType.Idle);
            return;
        }

        float distance = enemy.DistanceToTarget();
        if (distance <= enemy.attackStartRange)
        {
            enemy.ChangeState(EnemyStateType.Attack);
            return;
        }

        if (distance > enemy.detectRange * 1.5f)
        {
            enemy.ChangeState(EnemyStateType.Idle);
            return;
        }

        enemy.MoveTo(enemy.target.position, enemy.chaseSpeed);
    }

    public override void Exit()
    {
        enemy.EndRootMotionMove();
    }
}


