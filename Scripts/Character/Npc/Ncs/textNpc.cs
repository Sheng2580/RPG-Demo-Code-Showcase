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
        AddAction("升级", () =>
        {
            UIManager.Instance.OpenPanel<UpGradePanel>();
            EventCenter.Instance.EventTrigger(GameEvent.设置玩家输入状态,false);
        } );
    }
}
