using UnityEngine;

public class EnemyStateBase : StateBase
{
    protected EnemyBase enemy;
    
    public override void Init(IStateMachineOwner owner)
    {
        enemy = owner as EnemyBase;
    }
    
    protected void PlayAnimation(string animationName, int layer = 0, float fixedTransitionTime = 0.25f)
    {
        if (enemy == null)
        {
            return;
        }

        enemy.PlayAnimation(animationName, layer, fixedTransitionTime);
    }
    
    protected bool CheckAnimationState(string stateName, out float normalizedTime, int layer = 0)
    {
        if (enemy == null || enemy.model == null || enemy.model.animator == null)
        {
            normalizedTime = 0f;
            return false;
        }

        AnimatorStateInfo nextInfo = enemy.model.animator.GetNextAnimatorStateInfo(layer);
        if (nextInfo.IsName(stateName))
        {
            normalizedTime = nextInfo.normalizedTime;
            return true;
        }
        AnimatorStateInfo info = enemy.model.animator.GetCurrentAnimatorStateInfo(layer);
        normalizedTime = info.normalizedTime;
        return info.IsName(stateName);
    }
}
