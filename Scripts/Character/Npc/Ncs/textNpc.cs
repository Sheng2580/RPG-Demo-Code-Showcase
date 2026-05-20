using UnityEngine;

public class textNpc : NPCBase
{
    private NPCModle _npcModle;

    protected override void Start()
    {
        base.Start();
        ChangeState(NPCStateType.idle);
        InitActions();
        _npcModle = transform.GetChild(0).gameObject.GetComponent<NPCModle>();
    }

    protected override void Update()
    {
        base.Update();
    }

    public void InitActions()
    {
        AddAction("鍗囩骇", () =>
        {
            UIManager.Instance.OpenPanel<UpGradePanel>();
            EventCenter.Instance.EventTrigger(GameEvent.璁剧疆鐜╁杈撳叆鐘舵€?false);
        } );
    }
}


