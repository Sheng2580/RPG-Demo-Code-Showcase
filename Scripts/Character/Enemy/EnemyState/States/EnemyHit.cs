using UnityEngine;

public class EnemyHit : EnemyStateBase
{
    private float _knockbackTimer;
    private float _knockbackSpeed;

    public override void Enter()
    {
        if (enemy.isDead)
        {
            return;
        }

        enemy.isHit = true;
        enemy.EndWeaponAttackDetection();
        PlayAnimation(enemy.hitAnimationName);
        enemy.BeginRootMotionMove();
        _knockbackTimer = enemy.hitKnockbackDuration;
        _knockbackSpeed = enemy.hitKnockbackDuration > 0f
            ? enemy.hitKnockbackDistance / enemy.hitKnockbackDuration
            : 0f;
    }

    public override void Update()
    {
        if (enemy.isDead)
        {
            return;
        }

        TickKnockback();

        if (!CheckAnimationState(enemy.hitAnimationName, out float normalizedTime))
        {
            return;
        }

        if (normalizedTime < 0.9f || enemy.IsHitStunActive)
        {
            return;
        }

        enemy.isHit = false;

        if (enemy.TryFindTarget() && enemy.DistanceToTarget() <= enemy.detectRange)
        {
            enemy.ChangeState(EnemyStateType.Chase);
        }
        else
        {
            enemy.ChangeState(EnemyStateType.Idle);
        }
    }

    private void TickKnockback()
    {
        if (_knockbackTimer <= 0f || _knockbackSpeed <= 0f)
        {
            return;
        }

        float deltaTime = Mathf.Min(Time.deltaTime, _knockbackTimer);
        enemy.MoveByKnockback(enemy.HitKnockbackDirection, _knockbackSpeed * deltaTime);
        _knockbackTimer -= deltaTime;
    }

    public override void Exit()
    {
        enemy.EndRootMotionMove();
        enemy.EndWeaponAttackDetection();
        enemy.isHit = false;
        _knockbackTimer = 0f;
        _knockbackSpeed = 0f;
    }
}


