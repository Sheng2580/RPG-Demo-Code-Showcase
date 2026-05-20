using System;
using System.Collections.Generic;

public class StateMachine
{
    private IStateMachineOwner owner;
    private readonly Dictionary<Type, StateBase> stateDic = new Dictionary<Type, StateBase>();

    public StateBase currentState;

    private Type CurrentStateType => currentState.GetType();

    public void Init(IStateMachineOwner owner)
    {
        this.owner = owner;
    }

    private void ExitCurrentState()
    {
        if (currentState == null)
        {
            return;
        }

        MonoManager monoManager = MonoManager.Instance;
        currentState.Exit();

        if (monoManager == null)
        {
            return;
        }

        monoManager.RemoveUpdateListener(currentState.Update);
        monoManager.RemoveFixedUpdateListener(currentState.FixedUpdate);
        monoManager.RemoveLateUpdateListener(currentState.LateUpdate);
    }

    public void Stop(bool callExit = true)
    {
        if (currentState == null)
        {
            return;
        }

        MonoManager monoManager = MonoManager.Instance;
        if (monoManager != null)
        {
            monoManager.RemoveUpdateListener(currentState.Update);
            monoManager.RemoveFixedUpdateListener(currentState.FixedUpdate);
            monoManager.RemoveLateUpdateListener(currentState.LateUpdate);
        }

        if (callExit)
        {
            currentState.Exit();
        }

        currentState = null;
    }

    private void EnterNewState<T>() where T : StateBase, new()
    {
        MonoManager monoManager = MonoManager.Instance;
        currentState = GetTypr<T>();
        currentState.Enter();

        if (monoManager == null)
        {
            return;
        }

        monoManager.AddUpdateListener(currentState.Update);
        monoManager.AddFixedUpdateListener(currentState.FixedUpdate);
        monoManager.AddLateUpdateListener(currentState.LateUpdate);
    }

    public bool ChangeState<T>(bool isRe = false) where T : StateBase, new()
    {
        if (!isRe && currentState != null && CurrentStateType == typeof(T))
        {
            return false;
        }

        ExitCurrentState();
        EnterNewState<T>();
        return false;
    }

    public bool ReChangeState<T>(bool refreshState = false) where T : StateBase, new()
    {
        MonoManager monoManager = MonoManager.Instance;

        if (currentState != null)
        {
            currentState.Exit();

            if (monoManager != null)
            {
                monoManager.RemoveUpdateListener(currentState.Update);
                monoManager.RemoveFixedUpdateListener(currentState.FixedUpdate);
                monoManager.RemoveLateUpdateListener(currentState.LateUpdate);
            }
        }

        currentState = GetTypr<T>();
        currentState.Enter();

        if (monoManager != null)
        {
            monoManager.AddUpdateListener(currentState.Update);
            monoManager.AddFixedUpdateListener(currentState.FixedUpdate);
            monoManager.AddLateUpdateListener(currentState.LateUpdate);
        }

        return false;
    }

    public StateBase GetTypr<T>() where T : StateBase, new()
    {
        if (!stateDic.TryGetValue(typeof(T), out StateBase state))
        {
            state = new T();
            state.Init(owner);
            stateDic.Add(typeof(T), state);
        }

        return state;
    }
}


