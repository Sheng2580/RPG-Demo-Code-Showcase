using UnityEngine;

public class PlayerSlideAttack : PlayerState
{
    public override void Enter()
    {
        Player.combatFormController?.CurrentForm?.OnSlideAttackEnter();
    }

    public override void Update()
    {
        Player.combatFormController?.CurrentForm?.OnSlideAttackUpdate();
    }

    public override void Exit()
    {
        Player.combatFormController?.CurrentForm?.OnSlideAttackExit();
    }
}
