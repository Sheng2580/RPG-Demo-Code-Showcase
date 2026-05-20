using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcStateBase : StateBase
{
    protected NPCBase npc;
    protected NPCModle npcModle;
    
    public override void Init(IStateMachineOwner owner)
    {
        base.Init(owner);
        npc=(NPCBase)owner;
        npcModle = npc.model as NPCModle;
    }
    
    protected virtual bool CurrAnimationStateName(string stateName , out float normalizedTime ,int layer = 0)
    {
        AnimatorStateInfo nextInfo =npc.model.animator.GetNextAnimatorStateInfo(layer);
        if (nextInfo.IsName(stateName))
        {
            normalizedTime = nextInfo.normalizedTime;
            return true;
        }
        AnimatorStateInfo info =npc.model.animator.GetCurrentAnimatorStateInfo(layer);
        normalizedTime = info.normalizedTime;
        return info.IsName(stateName);
    }
    
    protected virtual bool CurrAnimationStateTag(string tag, out float normalizedTime)
    {
        AnimatorStateInfo nextInfo = npc.model.animator.GetNextAnimatorStateInfo(0);
        if (nextInfo.IsTag(tag))
        {
            normalizedTime = nextInfo.normalizedTime;
            return true;
        }
        AnimatorStateInfo info = npc.model.animator.GetCurrentAnimatorStateInfo(0);
        normalizedTime = info.normalizedTime;
        return info.IsTag(tag);
    }

    
    
    protected virtual void OnRootMotionAction(Vector3 dir, Quaternion rot)
    {
        npc.characterController.Move(dir);
    }
    
}
