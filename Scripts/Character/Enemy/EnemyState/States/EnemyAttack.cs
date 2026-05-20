using UnityEngine;

public class EnemyAttack : EnemyStateBase
{
    private int _comboIndex;
    private string _currentAttackAnimationName;

    public override void Enter()
    {
        enemy.BeginRootMotionMove();
        PlayCurrentComboAttack();
    }

    public override void Update()
    {
        if (enemy.target == null)
        {
            enemy.ChangeState(EnemyStateType.Idle);
            return;
        }

        FaceTarget();

        if (CheckAnimationState(_currentAttackAnimationName, out float normalizedTime))
        {
            if (normalizedTime >= enemy.attackEndNormalizedTime)
            {
                float distance = enemy.DistanceToTarget();
                if (distance <= enemy.AttackExitRange)
                {
                    _comboIndex++;
                    PlayCurrentComboAttack();
                }
                else
                {
                    enemy.ChangeState(EnemyStateType.Chase);
                }
            }
        }
    }

    private void PlayCurrentComboAttack()
    {
        enemy.EndWeaponAttackDetection();
        _currentAttackAnimationName = enemy.GetNormalAttackComboName(_comboIndex);
        PlayAnimation(_currentAttackAnimationName);
        enemy.BeginWeaponAttackDetection();
    }

    private void FaceTarget()
    {
        Vector3 direction = enemy.target.position - enemy.transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        enemy.transform.rotation = Quaternion.Slerp(
            enemy.transform.rotation,
            Quaternion.LookRotation(direction),
            12f * Time.deltaTime);
    }

    public override void Exit()
    {
        enemy.EndRootMotionMove();
        enemy.EndWeaponAttackDetection();
        _comboIndex = 0;
        _currentAttackAnimationName = null;
    }
}
