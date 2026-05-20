using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//控制根运动
public class CharacterModelBase : MonoBehaviour
{
   public Animator animator;
   protected virtual void Awake()
   {
      if (animator == null)
      {
         animator = GetComponent<Animator>();
      }
   }
   
   #region 动画根运动
   private Action<Vector3,Quaternion> RootMotionAction;
   // 赋值时加日志，确认委托被绑定
   public void SetRootMotionAction(Action<Vector3, Quaternion> rootMotionAction)
   {
      this.RootMotionAction = rootMotionAction;
   }

   public void ClearRootMotionAction()
   {
      this.RootMotionAction = null;
   }

   public void OnAnimatorMove()
   {
      if (RootMotionAction != null)
      {
         RootMotionAction.Invoke(animator.deltaPosition, animator.deltaRotation);
      }
   }
   #endregion

   
}
