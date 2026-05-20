using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public abstract class Decorator : Behavior
{
    protected Behavior child;
    public override void AddChild(Behavior child)
    {
        this.child = child;
    }
}

