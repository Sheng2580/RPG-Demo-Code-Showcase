using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parallel : Composite
{
    protected Policy mSuccessPolicy;
    protected Policy mFailurePolicy;
    public enum Policy
    {
        RequireOne, RequireAll,
    }
    public Parallel(Policy mSuccessPolicy, Policy mFailurePolicy)
    {
        this.mSuccessPolicy = mSuccessPolicy;
        this.mFailurePolicy = mFailurePolicy;
    }
    protected override EStatus OnUpdate()
    {
        int successCount = 0, failureCount = 0;
        var b = children.First;
        var size = children.Count;
        for (int i = 0; i < size; ++i)
        {
            var bh = b.Value;
            if(!bh.IsTerminated)
                bh.Tick();
            b = b.Next;
            if(bh.IsSuccess)
            {
                ++successCount;
                if(mSuccessPolicy == Policy.RequireOne)
                    return EStatus.Success;
            }
            if(bh.IsFailure)
            {
                ++failureCount;
                if(mFailurePolicy == Policy.RequireOne)
                    return EStatus.Failure;
            }
        }
        if(mFailurePolicy == Policy.RequireAll && failureCount == size)
            return EStatus.Failure;
        if(mSuccessPolicy == Policy.RequireAll && successCount == size)
            return EStatus.Success;
        return EStatus.Running;
    }
    protected override void OnTerminate()
    {
        foreach(var b in children)
        {
            if(b.IsRunning)
                b.Abort();
        }
    }
}


