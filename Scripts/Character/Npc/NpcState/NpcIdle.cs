using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcIdle : NpcStateBase
{
    public override void Enter()
    {
        npc.PlayAnimation("idle");
        npcModle.SetRootMotionAction(OnRootMotionAction);
        npcModle.isHeadRota = true;
    }


    public override void Exit()
    {
        npcModle.ClearRootMotionAction();
    }
}


