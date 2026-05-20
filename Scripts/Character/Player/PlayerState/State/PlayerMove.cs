public class PlayerMove : PlayerState
{
   public override void Enter()
   {
      Player.combatFormController?.CurrentForm?.OnMoveEnter();
   }

   public override void Update()
   {
      Player.combatFormController?.CurrentForm?.OnMoveUpdate();
   }

   public override void Exit()
   {
      Player.combatFormController?.CurrentForm?.OnMoveExit();
   }
}


