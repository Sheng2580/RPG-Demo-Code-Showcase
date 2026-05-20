using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class MonoManager : UnitySingleTonMono<MonoManager>
{
    private Action updateAction;
    private Action lateUpdateAction;
    private Action FixedUpdateAction;
    private Action OnAnimatorIKAction;

    public void AddUpdateListener(Action action)
    {
        updateAction += action;
    }

    public void RemoveUpdateListener(Action action)
    {
        updateAction -= action;
    }

    public void AddFixedUpdateListener(Action action)
    {
        FixedUpdateAction += action;
    }
    public void RemoveFixedUpdateListener(Action action)
    {
        FixedUpdateAction -= action;
    }
    public void AddLateUpdateListener(Action action)
    {
        lateUpdateAction += action;
    }

    public void AddOnAnimatorIKListener(Action action)
    {
        OnAnimatorIKAction += action;
    }

    public void RemoveLateUpdateListener(Action action)
    {
        lateUpdateAction -= action;
    }

    public void RemoveOnAnimatorIKListener(Action action)
    {
        OnAnimatorIKAction -= action;
    }

    private void Update()
    {
        updateAction?.Invoke();
    }

    private void LateUpdate()
    {
        lateUpdateAction?.Invoke();
    }

    private void FixedUpdate()
    {
        FixedUpdateAction?.Invoke();    
    }


}


