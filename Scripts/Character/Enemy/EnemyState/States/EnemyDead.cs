public class EnemyDead : EnemyStateBase
{
    public override void Enter()
    {
        enemy.isHit = false;
        enemy.EndWeaponAttackDetection();
        PlayAnimation(enemy.deadAnimationName);
    }
    
    public override void Update()
    {
    }

    public override void Exit()
    {
    }
}
