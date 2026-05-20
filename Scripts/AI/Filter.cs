using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Filter : Sequence
{
    public void AddCondition(Behavior condition)
    {
        children.AddFirst(condition);
    }
    public void AddAction(Behavior action)
    {
        children.AddLast(action);
    }
}

