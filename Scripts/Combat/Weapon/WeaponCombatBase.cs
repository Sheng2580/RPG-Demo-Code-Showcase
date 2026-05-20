using UnityEngine;
using System.Collections.Generic;

public abstract class WeaponCombatBase : WeaponBase, IWeaponCombat
{
    protected PlayerContorller owner;
    protected CombatFormController formController;

    public virtual void Equip(PlayerContorller owner, CombatFormController formController)
    {
        this.owner = owner;
        this.formController = formController;
    }

    public virtual void Unequip()
    {
    }

    public virtual void Tick()
    {
    }

    public virtual void HandleLightAttack()
    {
    }

    public virtual void HandleHeavyAttack()
    {
    }

    public virtual void DetectHit(WeaponAttackData attackData, WeaponTriggerHit triggerHit)
    {
        if (attackData == null || triggerHit == null)
        {
            return;
        }

        Debug.Log($"[WeaponCombat] DetectHit {attackData.attackName}, attackMultiplier={triggerHit.damage}");
        Collider[] hits = DetectColliders();
        HashSet<Transform> damagedTargets = new HashSet<Transform>();
        for (int i = 0; i < hits.Length; i++)
        {
            Transform target = GetDamageTarget(hits[i]);
            if (target != null && damagedTargets.Add(target))
            {
                ApplyHitTarget(target, triggerHit.damage);
            }
        }
    }

    protected override Transform GetDamageTarget(Collider hit)
    {
        EnemyBase enemy = hit != null ? hit.GetComponentInParent<EnemyBase>() : null;
        return enemy != null && !enemy.isDead ? enemy.transform : null;
    }

    protected override void OnHitTarget(Transform target, Collider hit)
    {
        ApplyHitTarget(target, 1f);
    }

    private void ApplyHitTarget(Transform target, float attackMultiplier)
    {
        EnemyBase enemy = target != null ? target.GetComponentInParent<EnemyBase>() : null;
        if (enemy == null)
        {
            return;
        }

        Transform attacker = owner != null ? owner.transform : transform;
        PlayerCombatStats stats = formController != null ? formController.Stats : null;
        if (stats != null)
        {
            DamageResult damageResult = stats.CalculateDamage(attackMultiplier);
            Debug.Log($"[WeaponDamage] Hit {enemy.name}, multiplier={damageResult.attackMultiplier}, damage={damageResult.damage}, crit={damageResult.isCrit}");
            enemy.TakeDamage(damageResult.damage, attacker, damageResult.isCrit);
            stats.ApplyLifeStealFromDamage(damageResult.damage);
            return;
        }

        enemy.TakeDamage(Mathf.Max(0f, attackMultiplier), attacker);
    }
}


