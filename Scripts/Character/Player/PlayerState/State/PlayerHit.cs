using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHit : PlayerState
{
    public override void Enter()
    {
        Player.combatFormController?.CurrentForm?.OnHitEnter();
    }

    public override void Update()
    {
        Player.combatFormController?.CurrentForm?.OnHitUpdate();
    }

    public override void Exit()
    {
        Player.combatFormController?.CurrentForm?.OnHitExit();
    }
}


