using System.Collections.Generic;

public abstract class Composite : Behavior
{
    protected LinkedList<Behavior> children;
    public Composite()
    {
        children = new LinkedList<Behavior>();
    }
    public virtual void RemoveChild(Behavior child)
    {
        children.Remove(child);
    }
    public void ClearChildren()
    {
        children.Clear();
    }
    public override void AddChild(Behavior child)
    {
        children.AddLast(child);
    }
}

