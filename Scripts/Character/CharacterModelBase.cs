using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

   #region 鍔ㄧ敾鏍硅繍鍔?
   private Action<Vector3,Quaternion> RootMotionAction;
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


