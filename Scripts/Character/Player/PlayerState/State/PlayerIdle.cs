public class PlayerIdle : PlayerState
{
    public override void Enter()
    {
        Player.combatFormController?.CurrentForm?.OnIdleEnter();
    }

    public override void Update()
    {
        Player.combatFormController?.CurrentForm?.OnIdleUpdate();
    }

    public override void Exit()
    {
        Player.combatFormController?.CurrentForm?.OnIdleExit();
    }
}
