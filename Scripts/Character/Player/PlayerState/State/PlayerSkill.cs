public class PlayerSkill : PlayerState
{
    public override void Enter()
    {
        Player.combatFormController?.CurrentForm?.OnSkillEnter();
    }

    public override void Update()
    {
        Player.combatFormController?.CurrentForm?.OnSkillUpdate();
    }

    public override void Exit()
    {
        Player.combatFormController?.CurrentForm?.OnSkillExit();
    }
}


