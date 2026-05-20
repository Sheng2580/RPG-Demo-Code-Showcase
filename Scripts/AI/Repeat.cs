using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Repeat : Decorator
{
    private int conunter;
    private int limit;
    public Repeat(int limit)
    {
        this.limit = limit;
    }
    protected override void OnInitialize()
    {
        conunter = 0;
    }
    protected override EStatus OnUpdate()
    {
        while (true)
        {
            child.Tick();
            if(child.IsRunning)
                return EStatus.Running;
            if(child.IsFailure)
                return EStatus.Failure;
            if(++conunter >= limit)
                return EStatus.Success;
        }
    }
}

