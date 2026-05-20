using System;
using UnityEngine;

public struct DamageResult
{
    public float damage;
    public bool isCrit;
    public float attackMultiplier;
    public float critRate;
    public float critDamage;
}

public class PlayerCombatStats : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] private float baseAttack = 10f;
    [SerializeField] private float baseDefense;
    [SerializeField] private float baseMaxHp = 100f;
    [SerializeField] [Range(0f, 1f)] private float baseCritRate = 0.1f;
    [SerializeField] private float baseCritDamage = 1.5f;
    [SerializeField] private float critOverflowToCritDamageRate = 1f;
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float energyGainEfficiency;
    [SerializeField] private float energyCostReduction;
    [SerializeField] private float lifeSteal;

    [Header("Run Buff Stats")]
    [SerializeField] private float runAttackAdd;
    [SerializeField] private float runDefenseAdd;
    [SerializeField] private float runMaxHpAdd;
    [SerializeField] private float runCritRateAdd;
    [SerializeField] private float runCritDamageAdd;
    [SerializeField] private float runDamageBonusAdd;

    [Header("Runtime")]
    [SerializeField] private float currentHp;
    [SerializeField] private float currentEnergy;
    [SerializeField] private bool isDebugEnergy = true;

    public float BaseAttack => baseAttack;
    public float BaseDefense => baseDefense;
    public float BaseMaxHp => baseMaxHp;
    public float BaseCritRate => baseCritRate;
    public float BaseCritDamage => baseCritDamage;

    public float RunAttackAdd => runAttackAdd;
    public float RunDefenseAdd => runDefenseAdd;
    public float RunMaxHpAdd => runMaxHpAdd;
    public float RunCritRateAdd => runCritRateAdd;
    public float RunCritDamageAdd => runCritDamageAdd;
    public float RunDamageBonusAdd => runDamageBonusAdd;

    public float Attack => Mathf.Max(0f, baseAttack + runAttackAdd);
    public float Defense => Mathf.Max(0f, baseDefense + runDefenseAdd);
    public float MaxHp => Mathf.Max(1f, baseMaxHp + runMaxHpAdd);
    public float CurrentHp => currentHp;
    public float RawCritRate => baseCritRate + runCritRateAdd;
    public float CritRate => Mathf.Clamp01(RawCritRate);
    public float CritRateOverflow => Mathf.Max(0f, RawCritRate - 1f);
    public float CritDamage =>
        Mathf.Max(1f, baseCritDamage + runCritDamageAdd + CritRateOverflow * critOverflowToCritDamageRate);
    public float DamageBonusAdd => runDamageBonusAdd;
    public float EnergyGainEfficiency => energyGainEfficiency;
    public float EnergyCostReduction => energyCostReduction;
    public float LifeSteal => lifeSteal;

    public float CurrentEnergy => currentEnergy;
    public float MaxEnergy => maxEnergy;

    public bool IsDebugEnergy
    {
        get => isDebugEnergy;
        set => isDebugEnergy = value;
    }

    public event Action<float, float> OnEnergyChanged;
    public event Action<float, float> OnHpChanged;

    private void Awake()
    {
        if (currentHp <= 0f)
        {
            currentHp = MaxHp;
        }

        currentHp = Mathf.Clamp(currentHp, 0f, MaxHp);
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);
    }

    public void Init(float attack, float critRate, float critDamage, float maxEnergy, float currentEnergy)
    {
        baseAttack = Mathf.Max(0f, attack);
        baseCritRate = Mathf.Max(0f, critRate);
        baseCritDamage = Mathf.Max(1f, critDamage);
        this.maxEnergy = Mathf.Max(0f, maxEnergy);
        currentHp = MaxHp;
        this.currentEnergy = Mathf.Clamp(currentEnergy, 0f, this.maxEnergy);
        OnHpChanged?.Invoke(this.currentHp, MaxHp);
        OnEnergyChanged?.Invoke(this.currentEnergy, this.maxEnergy);
    }

    public void InitBaseStats(
        float attack,
        float defense,
        float maxHp,
        float critRate,
        float critDamage,
        float maxEnergy,
        float currentEnergy)
    {
        baseAttack = Mathf.Max(0f, attack);
        baseDefense = Mathf.Max(0f, defense);
        baseMaxHp = Mathf.Max(1f, maxHp);
        baseCritRate = Mathf.Max(0f, critRate);
        baseCritDamage = Mathf.Max(1f, critDamage);
        this.maxEnergy = Mathf.Max(0f, maxEnergy);
        currentHp = Mathf.Clamp(currentHp <= 0f ? MaxHp : currentHp, 0f, MaxHp);
        this.currentEnergy = Mathf.Clamp(currentEnergy, 0f, this.maxEnergy);
        OnHpChanged?.Invoke(this.currentHp, MaxHp);
        OnEnergyChanged?.Invoke(this.currentEnergy, this.maxEnergy);
    }

    public void ApplyNumericStats(
        float attack,
        float defense,
        float maxHp,
        float critRate,
        float critDamage,
        float energyGainEfficiency,
        float energyCostReduction,
        float lifeSteal,
        float damageBonusAdd)
    {
        float oldMaxHp = MaxHp;
        baseAttack = Mathf.Max(0f, attack);
        baseDefense = Mathf.Max(0f, defense);
        baseMaxHp = Mathf.Max(1f, maxHp);
        baseCritRate = Mathf.Max(0f, critRate);
        baseCritDamage = Mathf.Max(1f, critDamage);
        this.energyGainEfficiency = Mathf.Max(0f, energyGainEfficiency);
        this.energyCostReduction = Mathf.Clamp01(energyCostReduction);
        this.lifeSteal = Mathf.Max(0f, lifeSteal);
        runDamageBonusAdd = Mathf.Max(0f, damageBonusAdd);

        if (currentHp <= 0f)
        {
            currentHp = MaxHp;
        }
        else if (!Mathf.Approximately(oldMaxHp, MaxHp))
        {
            currentHp = Mathf.Clamp(currentHp + (MaxHp - oldMaxHp), 0f, MaxHp);
        }
        else
        {
            currentHp = Mathf.Clamp(currentHp, 0f, MaxHp);
        }

        currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);
        OnHpChanged?.Invoke(currentHp, MaxHp);
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }

    public void AddRunBuffStats(
        float attackAdd = 0f,
        float defenseAdd = 0f,
        float maxHpAdd = 0f,
        float critRateAdd = 0f,
        float critDamageAdd = 0f,
        float damageBonusAdd = 0f)
    {
        runAttackAdd += attackAdd;
        runDefenseAdd += defenseAdd;
        runMaxHpAdd += maxHpAdd;
        runCritRateAdd += critRateAdd;
        runCritDamageAdd += critDamageAdd;
        runDamageBonusAdd += damageBonusAdd;
        currentHp = Mathf.Clamp(currentHp, 0f, MaxHp);
        OnHpChanged?.Invoke(currentHp, MaxHp);
    }

    public void ClearRunBuffStats()
    {
        runAttackAdd = 0f;
        runDefenseAdd = 0f;
        runMaxHpAdd = 0f;
        runCritRateAdd = 0f;
        runCritDamageAdd = 0f;
        runDamageBonusAdd = 0f;
        currentHp = Mathf.Clamp(currentHp, 0f, MaxHp);
        OnHpChanged?.Invoke(currentHp, MaxHp);
    }

    public void TakeDamage(float damage)
    {
        float finalDamage = Mathf.Max(0f, damage - Defense);
        if (finalDamage <= 0f)
        {
            return;
        }

        float oldHp = currentHp;
        currentHp = Mathf.Clamp(currentHp - finalDamage, 0f, MaxHp);
        if (!Mathf.Approximately(oldHp, currentHp))
        {
            OnHpChanged?.Invoke(currentHp, MaxHp);
        }
    }

    public void Heal(float value)
    {
        float oldHp = currentHp;
        currentHp = Mathf.Clamp(currentHp + Mathf.Max(0f, value), 0f, MaxHp);
        if (!Mathf.Approximately(oldHp, currentHp))
        {
            OnHpChanged?.Invoke(currentHp, MaxHp);
        }
    }

    public void AddEnergy(float value)
    {
        AddEnergy(value, true);
    }

    public void AddEnergy(float value, bool logDebug)
    {
        float oldEnergy = currentEnergy;
        float finalValue = value > 0f ? value * (1f + energyGainEfficiency) : value;
        currentEnergy = Mathf.Clamp(currentEnergy + finalValue, 0f, maxEnergy);

        if (isDebugEnergy && logDebug)
        {
            Debug.Log($"[Energy] AddEnergy value={value}, final={finalValue}, {oldEnergy} -> {currentEnergy}, Max={maxEnergy}");
        }

        if (!Mathf.Approximately(oldEnergy, currentEnergy))
        {
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        }
    }

    public bool TryConsumeEnergy(float value)
    {
        float cost = Mathf.Max(0f, value) * (1f - energyCostReduction);
        if (currentEnergy < cost)
        {
            if (isDebugEnergy)
            {
                Debug.Log($"[Energy] TryConsumeEnergy failed. Cost={cost}, Current={currentEnergy}");
            }

            return false;
        }

        AddEnergy(-cost);
        return true;
    }

    public void ApplyLifeStealFromDamage(float damage)
    {
        if (lifeSteal <= 0f || damage <= 0f)
        {
            return;
        }

        Heal(damage * lifeSteal);
    }

    public DamageResult CalculateDamage(float attackMultiplier)
    {
        float safeMultiplier = Mathf.Max(0f, attackMultiplier);
        float damage = Attack * safeMultiplier;
        damage *= 1f + DamageBonusAdd;

        bool isCrit = UnityEngine.Random.value < CritRate;
        if (isCrit)
        {
            damage *= CritDamage;
        }

        return new DamageResult
        {
            damage = damage,
            isCrit = isCrit,
            attackMultiplier = safeMultiplier,
            critRate = CritRate,
            critDamage = CritDamage
        };
    }

    public float CalculateDamage(float attackMultiplier, out bool isCrit)
    {
        DamageResult result = CalculateDamage(attackMultiplier);
        isCrit = result.isCrit;
        return result.damage;
    }
}
