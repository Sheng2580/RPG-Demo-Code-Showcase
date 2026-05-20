
public class PlayerStopMove : PlayerState
{
    public override void Enter()
    {
        if (!TryGetAnimator(out UnityEngine.Animator animator))
        {
            return;
        }

        if (animator.GetFloat("Speed") > 0.5f)
        {
            Player.PlayAnimation("SStop");
        }
        else if(animator.GetFloat("Speed") > -0.1f)
        {
            Player.PlayAnimation("FStop");
        }
        else
        {
            Player.PlayAnimation("WStop");
        }
        Player.model.SetRootMotionAction(OnRootMotionAction);
    }
    
    public override void Update()
    {
        if (CurrAnimationStateTag("Stop", out float time))
        {
            if (!CurrAnimationStateName("Wstop"))
            {
                if (time >= 0.8f)
                {
                    Player.ChangeState(PlayerStateType.Idle);
                }   
            }
            else
            {
                if (time >= 0.7f)
                {
                    Player.ChangeState(PlayerStateType.Idle);
                }
            }
            
        }
    }

    public override void Exit()
    {
        if (TryGetAnimator(out UnityEngine.Animator animator))
        {
            animator.SetFloat("Speed", 0);
        }

        if (Player != null && Player.model != null)
        {
            Player.model.ClearRootMotionAction();
        }
    }
    
}
