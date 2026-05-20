using System.Collections.Generic;
using UnityEngine;

public class SlashProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifeTime = 0.8f;
    [SerializeField] private bool destroyOnFirstHit;

    private PlayerContorller owner;
    private float damage;
    private LayerMask enemyLayerMask;
    private readonly HashSet<Transform> hitTargets = new HashSet<Transform>();

    public void Init(PlayerContorller owner, float damage, LayerMask enemyLayerMask)
    {
        this.owner = owner;
        this.damage = damage;
        this.enemyLayerMask = enemyLayerMask;
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || ((1 << other.gameObject.layer) & enemyLayerMask.value) == 0)
        {
            return;
        }

        EnemyBase enemy = other.GetComponentInParent<EnemyBase>();
        Transform target = enemy != null ? enemy.transform : other.transform;
        if (!hitTargets.Add(target))
        {
            return;
        }

        Debug.Log($"触发伤害+{target.name}");

        DamageResult damageResult = CalculateOwnerDamage(damage);
        enemy?.TakeDamage(damageResult.damage, owner != null ? owner.transform : transform, damageResult.isCrit);
        PlayerCombatStats stats = owner != null ? owner.GetComponent<PlayerCombatStats>() : null;
        stats?.ApplyLifeStealFromDamage(damageResult.damage);

        if (destroyOnFirstHit)
        {
            Destroy(gameObject);
        }
    }

    private DamageResult CalculateOwnerDamage(float attackMultiplier)
    {
        float safeMultiplier = Mathf.Max(0f, attackMultiplier);
        PlayerCombatStats stats = owner != null ? owner.GetComponent<PlayerCombatStats>() : null;
        if (stats != null)
        {
            return stats.CalculateDamage(safeMultiplier);
        }

        return new DamageResult
        {
            damage = safeMultiplier,
            isCrit = false,
            attackMultiplier = safeMultiplier,
            critRate = 0f,
            critDamage = 1f
        };
    }
}
