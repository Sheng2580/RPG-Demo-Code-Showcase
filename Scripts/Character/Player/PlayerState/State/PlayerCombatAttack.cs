public class PlayerCombatAttack : PlayerState
{
    public override void Enter()
    {
        Player.combatFormController?.CurrentForm?.OnAttackEnter();
    }

    public override void Update()
    {
        Player.combatFormController?.CurrentForm?.OnAttackUpdate();
    }

    public override void Exit()
    {
        Player.combatFormController?.CurrentForm?.OnAttackExit();
    }
}
