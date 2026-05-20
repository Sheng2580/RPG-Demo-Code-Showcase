using UnityEngine;

public class EnemyIdle : EnemyStateBase
{
    private float _idleTime;
    private float _waitTime;
    
    public override void Enter()
    {
        // 待机一小段随机时间，让巡逻节奏不那么机械。
        PlayAnimation(enemy.idleAnimationName);
        _waitTime = Random.Range(2f, 5f);
        _idleTime = 0f;
    }
    
    public override void Update()
    {
        _idleTime += Time.deltaTime;
        if (_idleTime >= _waitTime)
        {
            enemy.ChangeState(EnemyStateType.Patrol);
        }
    }
    
    public override void Exit()
    {
        _idleTime = 0f;
    }
}
