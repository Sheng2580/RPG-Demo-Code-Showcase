using UnityEngine;

public class EnemyPatrol : EnemyStateBase
{
    private Vector3 _targetPos;
    private bool _hasTarget;

    public override void Enter()
    {
        PlayAnimation(enemy.walkAnimationName);
        enemy.BeginRootMotionMove();
        SetRandomPatrolPoint();
    }

    public override void Update()
    {
        if (!_hasTarget)
        {
            enemy.ChangeState(EnemyStateType.Idle);
            return;
        }

        float distance = Vector3.Distance(enemy.transform.position, _targetPos);
        if (distance < 0.5f)
        {
            enemy.ChangeState(EnemyStateType.Idle);
            return;
        }

        enemy.MoveTo(_targetPos, enemy.moveSpeed);
    }

    private void SetRandomPatrolPoint()
    {
        for (int i = 0; i < enemy.patrolPointSampleCount; i++)
        {
            Vector3 randomOffset = Random.insideUnitSphere * enemy.patrolRadius;
            randomOffset.y = 0f;

            Vector3 randomPoint = enemy.StartPosition + randomOffset;
            if (enemy.TryGetReachableNavMeshPoint(randomPoint, enemy.patrolNavMeshSampleRadius, out Vector3 navPoint))
            {
                _targetPos = navPoint;
                _hasTarget = true;
                return;
            }
        }

        _hasTarget = false;
    }

    public override void Exit()
    {
        enemy.EndRootMotionMove();
        _hasTarget = false;
    }
}
