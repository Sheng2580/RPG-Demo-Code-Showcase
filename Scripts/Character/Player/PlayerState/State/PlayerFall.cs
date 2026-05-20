using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFall : PlayerState
{
    public override void Enter()
    {
        Player.combatFormController?.CurrentForm?.OnFallEnter();
    }

    public override void Update()
    {
        Player.combatFormController?.CurrentForm?.OnFallUpdate();
    }

    public override void Exit()
    {
        Player.combatFormController?.CurrentForm?.OnFallExit();
    }
}
