using System;
using System.Collections.Generic;

public enum PlayerStatType
{
    Attack,
    Defense,
    MaxHp,
    CritRate,
    CritDamage,
    EnergyGainEfficiency,
    EnergyCostReduction,
    LifeSteal,
    DamageBonus
}

public enum PlayerStatAddType
{
    Flat,
    Percent
}

[Serializable]
public class PlayerPropertyConfig
{
    public int id;
    public PlayerStatType statType;
    public string name;
    public string iconName;
    public float baseValue;
    public string displayFormat;
    public int sortOrder;
}

[Serializable]
public class PropertyUpgradeConfig
{
    public int id;
    public PlayerStatType statType;
    public int level;
    public PlayerStatAddType addType;
    public float addValue;
    public int costGold;
    public int maxLevel;
}

[Serializable]
public class BuffConfig
{
    public int id;
    public string name;
    public string iconName;
    public string desc;
    public PlayerStatType statType;
    public PlayerStatAddType addType;
    public float value;
    public float weight;
    public int price;
    public int maxStack;
}

[Serializable]
public class PropConfig
{
    public int id;
    public string name;
    public string iconName;
    public string desc;
    public string effectType;
    public float value;
    public int maxCount;
    public float weight;
    public int price;
}

[Serializable]
public class PlayerBuffRuntime
{
    public int buffId;
    public int stack;

    public PlayerBuffRuntime()
    {
    }

    public PlayerBuffRuntime(int buffId, int stack)
    {
        this.buffId = buffId;
        this.stack = stack;
    }
}

[Serializable]
public class PlayerPropRuntime
{
    public int propId;
    public int count;

    public PlayerPropRuntime()
    {
    }

    public PlayerPropRuntime(int propId, int count)
    {
        this.propId = propId;
        this.count = count;
    }
}

public class PlayerStatSnapshot
{
    public PlayerPropertyConfig property;
    public float baseValue;
    public float upgradeValue;
    public float buffValue;

    public float FinalValue => baseValue + upgradeValue + buffValue;
}

public class PlayerFinalStats
{
    private readonly Dictionary<PlayerStatType, PlayerStatSnapshot> snapshots;

    public PlayerFinalStats(Dictionary<PlayerStatType, PlayerStatSnapshot> snapshots)
    {
        this.snapshots = snapshots ?? new Dictionary<PlayerStatType, PlayerStatSnapshot>();
    }

    public IReadOnlyDictionary<PlayerStatType, PlayerStatSnapshot> Snapshots => snapshots;

    public float GetValue(PlayerStatType statType)
    {
        return snapshots.TryGetValue(statType, out PlayerStatSnapshot snapshot) ? snapshot.FinalValue : 0f;
    }
}
