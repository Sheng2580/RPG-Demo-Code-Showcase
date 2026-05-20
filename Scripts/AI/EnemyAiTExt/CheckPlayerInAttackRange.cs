using UnityEngine;

public class CheckPlayerInAttackRange : Behavior
{
    private readonly Transform monster;
    private readonly Transform player;
    private readonly float attackRange;
    private readonly Animator animator;

    public CheckPlayerInAttackRange(Transform monster, Transform player, float attackRange, Animator animator)
    {
        this.monster = monster;
        this.player = player;
        this.attackRange = attackRange;
        this.animator = animator;
    }

    protected override EStatus OnUpdate()
    {
        if (player == null || monster == null) return EStatus.Failure;

        float distance = Vector3.Distance(monster.position, player.position);
        return distance <= attackRange ? EStatus.Success : EStatus.Failure;
    }

    private void PlayAnimation(string animationName, int layer = 0, float fixedTransitionTime = 0.25f)
    {
        animator?.CrossFadeInFixedTime(animationName, fixedTransitionTime, layer);
    }
}

public class AttackPlayer : Behavior
{
    private readonly Animator anim;
    private readonly int damage;
    private float timer;

    public AttackPlayer(Animator anim, int damage)
    {
        this.anim = anim;
        this.damage = damage;
    }

    protected override void OnInitialize()
    {
        timer = 0f;
        PlayAnimation("Attack1");
        Debug.Log($"寮€濮嬫敾鍑伙紝閫犳垚{damage}鐐逛激瀹?);
    }

    private void PlayAnimation(string animationName, int layer = 0, float fixedTransitionTime = 0.25f)
    {
        anim?.CrossFadeInFixedTime(animationName, fixedTransitionTime, layer);
    }

    protected override EStatus OnUpdate()
    {
        timer += Time.deltaTime;
        return timer < 1f ? EStatus.Running : EStatus.Success;
    }
}


