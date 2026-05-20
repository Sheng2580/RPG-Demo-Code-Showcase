public enum EStatus
{
    Failure, Success, Running, Aborted, Invalid
}

public abstract class Behavior
{
    public bool IsTerminated => IsSuccess || IsFailure;
    public bool IsSuccess => status == EStatus.Success;
    public bool IsFailure => status == EStatus.Failure;
    public bool IsRunning => status == EStatus.Running;
    protected EStatus status;
    public Behavior()
    {
        status = EStatus.Invalid;
    }
    protected virtual void OnInitialize() {}

    protected abstract EStatus OnUpdate();

    protected virtual void OnTerminate() {}

    public EStatus Tick()
    {
        if(!IsRunning)
            OnInitialize();
        status = OnUpdate();
        if(!IsRunning)
            OnTerminate();
        return status;
    }

    public virtual void AddChild(Behavior child) {}

    public void Reset()
    {
        status = EStatus.Invalid;
    }

    public void Abort()
    {
        OnTerminate();
        status = EStatus.Aborted;
    }
}

