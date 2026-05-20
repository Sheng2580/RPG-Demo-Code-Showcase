using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSlide : PlayerState
{
    public override void Enter()
    {
        Player.combatFormController?.CurrentForm?.OnSlideEnter();
    }

    public override void Update()
    {
        Player.TryTriggerPerfectDodgeInSlide();
        Player.combatFormController?.CurrentForm?.OnSlideUpdate();
    }

    public override void Exit()
    {
        Player.combatFormController?.CurrentForm?.OnSlideExit();
    }
}
