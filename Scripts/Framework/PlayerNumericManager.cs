using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerNumericManager : SingleTon<PlayerNumericManager>
{
    private const string PlayerPropertyTableName = "tbplayer_property";
    private const string PropertyUpgradeTableName = "tbproperty_upgrade";
    private const string BuffTableName = "tbbuff";
    private const string PropTableName = "tbprop";
    private const int DefaultRandomBuffCount = 3;

    private bool isLoaded;
    private readonly List<PlayerPropertyConfig> propertyConfigs = new List<PlayerPropertyConfig>();
    private readonly List<PropertyUpgradeConfig> upgradeConfigs = new List<PropertyUpgradeConfig>();
    private readonly List<BuffConfig> buffConfigs = new List<BuffConfig>();
    private readonly List<PropConfig> propConfigs = new List<PropConfig>();
    private readonly List<PlayerBuffRuntime> runtimeBuffs = new List<PlayerBuffRuntime>();

    private readonly Dictionary<PlayerStatType, PlayerPropertyConfig> propertyByType =
        new Dictionary<PlayerStatType, PlayerPropertyConfig>();

    private readonly Dictionary<PlayerStatType, List<PropertyUpgradeConfig>> upgradesByType =
        new Dictionary<PlayerStatType, List<PropertyUpgradeConfig>>();

    private readonly Dictionary<int, BuffConfig> buffById = new Dictionary<int, BuffConfig>();
    private readonly Dictionary<int, PropConfig> propById = new Dictionary<int, PropConfig>();

    public event Action OnNumericChanged;

    public IReadOnlyList<PlayerPropertyConfig> PropertyConfigs
    {
        get
        {
            EnsureLoaded();
            return propertyConfigs;
        }
    }

    public IReadOnlyList<PlayerBuffRuntime> RuntimeBuffs
    {
        get
        {
            EnsureRuntimeInitialized();
            return runtimeBuffs;
        }
    }

    public IReadOnlyList<BuffConfig> BuffConfigs
    {
        get
        {
            EnsureLoaded();
            return buffConfigs;
        }
    }

    public IReadOnlyList<PropConfig> PropConfigs
    {
        get
        {
            EnsureLoaded();
            return propConfigs;
        }
    }

    public void Reload()
    {
        isLoaded = false;
        propertyConfigs.Clear();
        upgradeConfigs.Clear();
        buffConfigs.Clear();
        propConfigs.Clear();
        propertyByType.Clear();
        upgradesByType.Clear();
        buffById.Clear();
        propById.Clear();
        runtimeBuffs.Clear();
        EnsureLoaded();
    }

    public void EnsureRuntimeInitialized()
    {
        EnsureLoaded();
        EnsurePlayerData();

        if (runtimeBuffs.Count == 0)
        {
            InitRandomBuffs(DefaultRandomBuffCount);
        }
    }

    public PlayerFinalStats GetFinalStats()
    {
        EnsureRuntimeInitialized();

        Dictionary<PlayerStatType, PlayerStatSnapshot> snapshots = new Dictionary<PlayerStatType, PlayerStatSnapshot>();
        foreach (PlayerPropertyConfig property in propertyConfigs.OrderBy(config => config.sortOrder))
        {
            PlayerStatSnapshot snapshot = new PlayerStatSnapshot
            {
                property = property,
                baseValue = property.baseValue,
                upgradeValue = GetUpgradeValue(property.statType),
                buffValue = GetBuffValue(property.statType)
            };

            snapshots[property.statType] = snapshot;
        }

        return new PlayerFinalStats(snapshots);
    }

    public bool TryUpgrade(PlayerStatType statType)
    {
        EnsureRuntimeInitialized();

        PlayerData playerData = GameManager.Instance.PlayerData;
        string key = statType.ToString();
        int currentLevel = GetPropertyLevel(statType);
        int nextLevel = currentLevel + 1;
        PropertyUpgradeConfig nextConfig = GetUpgradeConfig(statType, nextLevel);
        if (nextConfig == null)
        {
            Debug.Log($"[PlayerNumericManager] {statType} already at max level.");
            return false;
        }

        if (playerData.gold < nextConfig.costGold)
        {
            Debug.Log($"[PlayerNumericManager] Not enough gold for {statType}. Need={nextConfig.costGold}, Current={playerData.gold}");
            return false;
        }

        playerData.gold -= nextConfig.costGold;
        playerData.propertyLevels[key] = nextLevel;
        ApplyToCurrentPlayer();
        OnNumericChanged?.Invoke();
        return true;
    }

    public void ApplyTo(PlayerCombatStats stats)
    {
        if (stats == null)
        {
            return;
        }

        PlayerFinalStats finalStats = GetFinalStats();
        stats.ApplyNumericStats(
            finalStats.GetValue(PlayerStatType.Attack),
            finalStats.GetValue(PlayerStatType.Defense),
            finalStats.GetValue(PlayerStatType.MaxHp),
            finalStats.GetValue(PlayerStatType.CritRate),
            finalStats.GetValue(PlayerStatType.CritDamage),
            finalStats.GetValue(PlayerStatType.EnergyGainEfficiency),
            finalStats.GetValue(PlayerStatType.EnergyCostReduction),
            finalStats.GetValue(PlayerStatType.LifeSteal),
            finalStats.GetValue(PlayerStatType.DamageBonus));
    }

    public void ApplyToCurrentPlayer()
    {
        GameObject player = GameManager.Instance != null ? GameManager.Instance.Player : null;
        PlayerCombatStats stats = player != null ? player.GetComponent<PlayerCombatStats>() : null;
        ApplyTo(stats);
    }

    public int GetPropertyLevel(PlayerStatType statType)
    {
        EnsurePlayerData();
        string key = statType.ToString();
        return GameManager.Instance.PlayerData.propertyLevels.TryGetValue(key, out int level) ? Mathf.Max(0, level) : 0;
    }

    public int GetMaxLevel(PlayerStatType statType)
    {
        EnsureLoaded();
        return upgradesByType.TryGetValue(statType, out List<PropertyUpgradeConfig> configs) && configs.Count > 0
            ? configs.Max(config => config.level)
            : 0;
    }

    public PropertyUpgradeConfig GetNextUpgradeConfig(PlayerStatType statType)
    {
        return GetUpgradeConfig(statType, GetPropertyLevel(statType) + 1);
    }

    public BuffConfig GetBuffConfig(int buffId)
    {
        EnsureLoaded();
        buffById.TryGetValue(buffId, out BuffConfig config);
        return config;
    }

    public PropConfig GetPropConfig(int propId)
    {
        EnsureLoaded();
        propById.TryGetValue(propId, out PropConfig config);
        return config;
    }

    public bool TryAddRuntimeBuff(int buffId, int stack = 1)
    {
        EnsureRuntimeInitialized();
        if (!buffById.TryGetValue(buffId, out BuffConfig config))
        {
            return false;
        }

        int addStack = Mathf.Max(1, stack);
        int maxStack = Mathf.Max(1, config.maxStack);
        PlayerBuffRuntime runtime = runtimeBuffs.FirstOrDefault(buff => buff.buffId == buffId);
        if (runtime == null)
        {
            runtimeBuffs.Add(new PlayerBuffRuntime(buffId, Mathf.Min(addStack, maxStack)));
        }
        else
        {
            if (runtime.stack >= maxStack)
            {
                return false;
            }

            runtime.stack = Mathf.Min(maxStack, runtime.stack + addStack);
        }

        ApplyToCurrentPlayer();
        OnNumericChanged?.Invoke();
        return true;
    }

    public bool TryAddProp(int propId, int count = 1)
    {
        EnsureLoaded();
        EnsurePlayerData();
        if (!propById.TryGetValue(propId, out PropConfig config))
        {
            return false;
        }

        int addCount = Mathf.Max(1, count);
        int currentCount = GameManager.Instance.PlayerData.props.TryGetValue(propId, out int savedCount)
            ? Mathf.Max(0, savedCount)
            : 0;
        int maxCount = config.maxCount > 0 ? config.maxCount : int.MaxValue;
        if (currentCount >= maxCount)
        {
            return false;
        }

        GameManager.Instance.PlayerData.props[propId] = Mathf.Min(maxCount, currentCount + addCount);
        OnNumericChanged?.Invoke();
        return true;
    }

    public IReadOnlyDictionary<int, int> GetProps()
    {
        EnsurePlayerData();
        return GameManager.Instance.PlayerData.props;
    }

    private void EnsureLoaded()
    {
        if (isLoaded)
        {
            return;
        }

        LoadConfigs();
        BuildIndexes();
        isLoaded = true;
    }

    private void LoadConfigs()
    {
        propertyConfigs.AddRange(LoadTable<PlayerPropertyConfig>(PlayerPropertyTableName));
        upgradeConfigs.AddRange(LoadTable<PropertyUpgradeConfig>(PropertyUpgradeTableName));
        buffConfigs.AddRange(LoadTable<BuffConfig>(BuffTableName));
        propConfigs.AddRange(LoadTable<PropConfig>(PropTableName));

        if (propertyConfigs.Count == 0)
        {
            AddFallbackConfigs();
        }

        if (buffConfigs.Count == 0)
        {
            AddFallbackBuffConfigs();
        }

        if (propConfigs.Count == 0)
        {
            AddFallbackPropConfigs();
        }
    }

    private List<T> LoadTable<T>(string tableName) where T : new()
    {
        List<T> list = JsonManager.Instance.LoadData<List<T>>(tableName);
        return list ?? new List<T>();
    }

    private void BuildIndexes()
    {
        foreach (PlayerPropertyConfig config in propertyConfigs.OrderBy(config => config.sortOrder))
        {
            propertyByType[config.statType] = config;
        }

        foreach (PropertyUpgradeConfig config in upgradeConfigs.OrderBy(config => config.level))
        {
            if (!upgradesByType.TryGetValue(config.statType, out List<PropertyUpgradeConfig> configs))
            {
                configs = new List<PropertyUpgradeConfig>();
                upgradesByType[config.statType] = configs;
            }

            configs.Add(config);
        }

        foreach (BuffConfig config in buffConfigs)
        {
            buffById[config.id] = config;
        }

        foreach (PropConfig config in propConfigs)
        {
            propById[config.id] = config;
        }
    }

    private void InitRandomBuffs(int count)
    {
        runtimeBuffs.Clear();
        List<BuffConfig> pool = buffConfigs.Where(config => config.weight > 0f).ToList();
        int takeCount = Mathf.Min(count, pool.Count);
        for (int i = 0; i < takeCount; i++)
        {
            int index = GetWeightedRandomIndex(pool);
            BuffConfig selected = pool[index];
            runtimeBuffs.Add(new PlayerBuffRuntime(selected.id, 1));
            pool.RemoveAt(index);
        }

        OnNumericChanged?.Invoke();
    }

    private int GetWeightedRandomIndex(List<BuffConfig> pool)
    {
        float totalWeight = pool.Sum(config => Mathf.Max(0f, config.weight));
        if (totalWeight <= 0f)
        {
            return UnityEngine.Random.Range(0, pool.Count);
        }

        float roll = UnityEngine.Random.value * totalWeight;
        float current = 0f;
        for (int i = 0; i < pool.Count; i++)
        {
            current += Mathf.Max(0f, pool[i].weight);
            if (roll <= current)
            {
                return i;
            }
        }

        return pool.Count - 1;
    }

    private float GetUpgradeValue(PlayerStatType statType)
    {
        int level = GetPropertyLevel(statType);
        if (level <= 0 || !upgradesByType.TryGetValue(statType, out List<PropertyUpgradeConfig> configs))
        {
            return 0f;
        }

        float value = 0f;
        foreach (PropertyUpgradeConfig config in configs)
        {
            if (config.level <= level)
            {
                value += config.addValue;
            }
        }

        return value;
    }

    private float GetBuffValue(PlayerStatType statType)
    {
        float value = 0f;
        foreach (PlayerBuffRuntime runtime in runtimeBuffs)
        {
            if (!buffById.TryGetValue(runtime.buffId, out BuffConfig config) || config.statType != statType)
            {
                continue;
            }

            value += config.value * Mathf.Max(1, runtime.stack);
        }

        if (statType == PlayerStatType.EnergyGainEfficiency)
        {
            value = Mathf.Min(value, 0.8f);
        }

        return value;
    }

    private PropertyUpgradeConfig GetUpgradeConfig(PlayerStatType statType, int level)
    {
        EnsureLoaded();
        if (!upgradesByType.TryGetValue(statType, out List<PropertyUpgradeConfig> configs))
        {
            return null;
        }

        return configs.FirstOrDefault(config => config.level == level);
    }

    private void EnsurePlayerData()
    {
        GameManager.Instance.PlayerData.EnsureCollections();
    }

    private void AddFallbackConfigs()
    {
        propertyConfigs.Add(new PlayerPropertyConfig { id = 1, statType = PlayerStatType.Attack, name = "Attack", iconName = "Attack", baseValue = 10f, displayFormat = "0", sortOrder = 1 });
        propertyConfigs.Add(new PlayerPropertyConfig { id = 2, statType = PlayerStatType.Defense, name = "Defense", iconName = "Defense", baseValue = 0f, displayFormat = "0", sortOrder = 2 });
        propertyConfigs.Add(new PlayerPropertyConfig { id = 3, statType = PlayerStatType.MaxHp, name = "HP", iconName = "HP", baseValue = 100f, displayFormat = "0", sortOrder = 3 });
        propertyConfigs.Add(new PlayerPropertyConfig { id = 4, statType = PlayerStatType.CritRate, name = "Crit Rate", iconName = "CritRate", baseValue = 0.1f, displayFormat = "P0", sortOrder = 4 });
        propertyConfigs.Add(new PlayerPropertyConfig { id = 5, statType = PlayerStatType.CritDamage, name = "Crit Damage", iconName = "CritDamage", baseValue = 1.5f, displayFormat = "P0", sortOrder = 5 });
        propertyConfigs.Add(new PlayerPropertyConfig { id = 6, statType = PlayerStatType.EnergyGainEfficiency, name = "Energy Gain", iconName = "EnergyGain", baseValue = 0f, displayFormat = "P0", sortOrder = 6 });
        propertyConfigs.Add(new PlayerPropertyConfig { id = 7, statType = PlayerStatType.EnergyCostReduction, name = "Energy Cost Down", iconName = "EnergyGain", baseValue = 0f, displayFormat = "P0", sortOrder = 7 });
        propertyConfigs.Add(new PlayerPropertyConfig { id = 8, statType = PlayerStatType.LifeSteal, name = "Life Steal", iconName = "Attack", baseValue = 0f, displayFormat = "P0", sortOrder = 8 });
        propertyConfigs.Add(new PlayerPropertyConfig { id = 9, statType = PlayerStatType.DamageBonus, name = "Damage Bonus", iconName = "Attack", baseValue = 0f, displayFormat = "P0", sortOrder = 9 });

        int id = 1;
        foreach (PlayerPropertyConfig property in propertyConfigs.Take(6))
        {
            for (int level = 1; level <= 10; level++)
            {
                float addValue = property.statType == PlayerStatType.CritRate ? 0.05f :
                    property.statType == PlayerStatType.CritDamage ? 0.1f :
                    property.statType == PlayerStatType.EnergyGainEfficiency ? 0.08f : 10f;

                upgradeConfigs.Add(new PropertyUpgradeConfig
                {
                    id = id++,
                    statType = property.statType,
                    level = level,
                    addType = PlayerStatAddType.Flat,
                    addValue = addValue,
                    costGold = 100 * level,
                    maxLevel = 10
                });
            }
        }

        AddFallbackBuffConfigs();
    }

    private void AddFallbackPropConfigs()
    {
        propConfigs.Add(new PropConfig { id = 1, name = "Potion", iconName = "Potion", desc = "Heal 10% HP", effectType = "HealPercent", value = 0.1f, maxCount = 3, weight = 1f, price = 50 });
        propConfigs.Add(new PropConfig { id = 2, name = "Energy Potion", iconName = "EnergyPotion", desc = "Recover energy", effectType = "EnergyFull", value = 1f, maxCount = 3, weight = 1f, price = 80 });
    }

    private void AddFallbackBuffConfigs()
    {
        buffConfigs.Add(new BuffConfig { id = 1, name = "Attack", iconName = "Attack", desc = "Attack +10", statType = PlayerStatType.Attack, addType = PlayerStatAddType.Flat, value = 10f, weight = 1f, price = 100, maxStack = 1 });
        buffConfigs.Add(new BuffConfig { id = 2, name = "Crit Rate", iconName = "CritRate", desc = "Crit Rate +10%", statType = PlayerStatType.CritRate, addType = PlayerStatAddType.Flat, value = 0.1f, weight = 1f, price = 100, maxStack = 1 });
        buffConfigs.Add(new BuffConfig { id = 3, name = "Life Steal", iconName = "Attack", desc = "Life Steal +1%", statType = PlayerStatType.LifeSteal, addType = PlayerStatAddType.Flat, value = 0.01f, weight = 1f, price = 100, maxStack = 1 });
        buffConfigs.Add(new BuffConfig { id = 4, name = "Energy Gain", iconName = "EnergyGain", desc = "Energy Gain +20%", statType = PlayerStatType.EnergyGainEfficiency, addType = PlayerStatAddType.Flat, value = 0.2f, weight = 1f, price = 100, maxStack = 1 });
    }
}


