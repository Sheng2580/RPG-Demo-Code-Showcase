using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Selector : Sequence
{
    protected override EStatus OnUpdate()
    {
        while(true)
        {
            var s = currentChild.Value.Tick();
            if( s != EStatus.Failure)
                return s;
            currentChild = currentChild.Next;
            if(currentChild == null)
                return EStatus.Failure;
        }
    }
}


