using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSit : PlayerState
{
  public override void Enter()
  {
  
    Player.PlayAnimation("SitLoop");
    Debug.Log("sss");
  }
  
  public override void Update()
  {
    if (CurrAnimationStateName("SitEnd", out var time))
    {
      if (time > 0.98f)
      {
        Player.ChangeState(PlayerStateType.Idle);
      }
    }
  }

  
}
