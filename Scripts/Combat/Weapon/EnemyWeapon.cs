using UnityEngine;

public class EnemyWeapon : WeaponBase
{
    [Header("Enemy Damage")]
    [SerializeField] private float damage = 10f;

    protected override Transform GetDamageTarget(Collider hit)
    {
        PlayerContorller player = hit != null ? hit.GetComponentInParent<PlayerContorller>() : null;
        return player != null ? player.transform : null;
    }

    protected override void OnHitTarget(Transform target, Collider hit)
    {
        PlayerContorller player = target != null ? target.GetComponentInParent<PlayerContorller>() : null;
        if (player == null)
        {
            return;
        }

        player.RegisterEnemyDamage(attackOwner != null ? attackOwner : transform, damage);
    }
}
