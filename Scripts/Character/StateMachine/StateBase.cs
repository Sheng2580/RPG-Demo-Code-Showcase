using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IStateMachineOwner
{

}

public abstract class StateBase : IStateMachineOwner
{
   public virtual void Init(IStateMachineOwner owner)
   {

   }


   public virtual void Unit()
   {

   }

   public virtual void Enter()
   {

   }

   public virtual void Exit()
   {

   }

   public virtual void Update()
   {

   }

   public virtual void FixedUpdate()
   {

   }

   public virtual void LateUpdate()
   {

   }

   public virtual void OnAnimatorIK()
   {

   }
}


