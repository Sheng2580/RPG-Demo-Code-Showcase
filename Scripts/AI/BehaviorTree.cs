using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BehaviorTree
{
    public bool HaveRoot => root != null;
    private Behavior root;
    public BehaviorTree(Behavior root)
    {
        this.root = root;
    }
    public void Tick()
    {
        root.Tick();
    }
    public void SetRoot(Behavior root)
    {
        this.root = root;
    }
}

