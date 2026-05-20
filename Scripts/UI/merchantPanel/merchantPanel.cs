using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class merchantPanel : BasePanel
{
    private const string ItemAbName = "uipanel";
    private const string ItemName = "merchantItem";
    private const int BuffCommodityType = 1;
    private const int PropCommodityType = 2;
    private const int DefaultBuffPrice = 100;
    private const int DefaultPropPrice = 50;

    [SerializeField] private Text gelo;
    public merchantDescribe merchantDescribe;
    public GameObject content;
    [SerializeField] private int stockCount = 5;

    public event System.Action OnPanelClosed;

    private static string stockedSceneName;
    private static readonly List<commodityClass> sceneStock = new List<commodityClass>();

    private readonly List<merchantItem> _itemList = new List<merchantItem>();
    private merchantItem _selectItem;

    public override void Awake()
    {
        base.Awake();
        if (closeBtn != null)
        {
            closeBtn.onClick.RemoveListener(Hide);
            closeBtn.onClick.AddListener(CloseSelf);
        }
    }

    private void Start()
    {
        RebuildFromSceneStock();
    }

    private void Update()
    {
        if (GameInputManger.Instance.Esc)
        {
            UIManager.Instance.ClosePanel<merchantPanel>();
        }
    }

    public override void Show()
    {
        base.Show();
        specialTipPanel.Open("鍟嗗簵");
        RebuildFromSceneStock();
        if (GameSceneManager.Instance.GetCurrSceneName() != "hall")
        {
            UIManager.Instance.ClosePanel<PlayerPnael>();
        }
    }

    public override void Hide()
    {
        UIManager.Instance.ClosePanel<specialTipPanel>();
        base.Hide();
        OnPanelClosed?.Invoke();
        if (GameSceneManager.Instance.GetCurrSceneName() != "hall")
        {
            UIManager.Instance.OpenPanel<PlayerPnael>();
            EventCenter.Instance.EventTrigger(GameEvent.璁剧疆鐜╁杈撳叆鐘舵€? true);
            EventCenter.Instance.EventTrigger(GameEvent.瑙掕壊鎴樻枟鎺у埗,true);
        }
    }

    private void RebuildFromSceneStock()
    {
        EnsureSceneStock();
        CreateItemsFromCommodities(sceneStock);
        RefreshGoldText();
    }

    private void EnsureSceneStock()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (stockedSceneName == sceneName)
        {
            return;
        }

        stockedSceneName = sceneName;
        sceneStock.Clear();
        sceneStock.AddRange(GetRandomMerchantGoods(stockCount));
        Debug.Log($"[merchantPanel] reset stock scene={sceneName}, count={sceneStock.Count}");
    }

    private List<commodityClass> GetRandomMerchantGoods(int count)
    {
        List<commodityClass> pool = BuildMerchantGoodsPool();
        List<commodityClass> result = new List<commodityClass>();
        int take = Mathf.Min(Mathf.Max(0, count), pool.Count);

        for (int i = 0; i < take; i++)
        {
            int index = GetWeightedRandomIndex(pool);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result;
    }

    private List<commodityClass> BuildMerchantGoodsPool()
    {
        PlayerNumericManager numericManager = PlayerNumericManager.Instance;
        List<commodityClass> pool = new List<commodityClass>();

        foreach (BuffConfig config in numericManager.BuffConfigs)
        {
            if (config == null || config.weight <= 0f)
            {
                continue;
            }

            pool.Add(new commodityClass
            {
                CommodityID = config.id,
                CommodityType = BuffCommodityType,
                CommodityPrice = config.price > 0 ? config.price : DefaultBuffPrice,
                CommodityName = config.name,
                CommodityImageName = config.iconName,
                CommodityDetailedInformationText = config.desc,
                weight = config.weight
            });
        }

        foreach (PropConfig config in numericManager.PropConfigs)
        {
            if (config == null)
            {
                continue;
            }

            pool.Add(new commodityClass
            {
                CommodityID = config.id,
                CommodityType = PropCommodityType,
                CommodityPrice = config.price > 0 ? config.price : DefaultPropPrice,
                CommodityName = config.name,
                CommodityImageName = config.iconName,
                CommodityDetailedInformationText = config.desc,
                weight = config.weight > 0f ? config.weight : 1f
            });
        }

        return pool;
    }

    private int GetWeightedRandomIndex(List<commodityClass> pool)
    {
        float totalWeight = pool.Sum(item => Mathf.Max(0f, item.weight));
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

    private void CreateItemsFromCommodities(List<commodityClass> list)
    {
        if (content == null)
        {
            Debug.LogWarning("[merchantPanel] Content is missing.");
            return;
        }

        foreach (Transform child in content.transform)
        {
            Destroy(child.gameObject);
        }

        _itemList.Clear();
        _selectItem = null;

        if (list == null || list.Count == 0)
        {
            return;
        }

        GameObject prefab = ABManager.Instance != null
            ? ABManager.Instance.LoadRes<GameObject>(ItemAbName, ItemName)
            : null;
        if (prefab == null)
        {
            Debug.LogWarning($"[merchantPanel] Load item failed: {ItemAbName}/{ItemName}");
            return;
        }

        foreach (commodityClass commodity in list)
        {
            GameObject go = Instantiate(prefab, content.transform, false);
            go.name = $"MerchantItem_{commodity.CommodityType}_{commodity.CommodityID}";
            merchantItem item = go.GetComponent<merchantItem>();
            if (item == null)
            {
                continue;
            }

            item.InitMerchantItem(commodity);
            item.OnClicked = OnItemClicked;
            _itemList.Add(item);
        }

        Destroy(prefab);

        if (_itemList.Count > 0)
        {
            SetSelectedItem(_itemList[0]);
        }
    }

    private void OnItemClicked(merchantItem item)
    {
        if (item != null)
        {
            SetSelectedItem(item);
        }
    }

    private void SetSelectedItem(merchantItem item)
    {
        if (item == null)
        {
            return;
        }

        if (_selectItem != null && _selectItem != item)
        {
            _selectItem.CancelThisItem();
        }

        _selectItem = item;
        _selectItem.SelectThisItem();

        if (merchantDescribe != null)
        {
            merchantDescribe.SetDescribe(item.Commodity, () => OnBuyClicked(item.Commodity));
        }
    }

    private void OnBuyClicked(commodityClass commodity)
    {
        if (commodity == null)
        {
            return;
        }

        PlayerData playerData = GameManager.Instance.PlayerData;
        if (playerData.gold < commodity.CommodityPrice)
        {
            Debug.Log($"[merchantPanel] Not enough gold. Need={commodity.CommodityPrice}, Current={playerData.gold}");
            return;
        }

        bool effectAdded = false;
        bool success = TryBuyCommodity(commodity, out effectAdded);

        if (!success)
        {
            Debug.LogWarning($"[merchantPanel] Buy failed. Type={commodity.CommodityType}, ID={commodity.CommodityID}");
            return;
        }

        playerData.gold -= commodity.CommodityPrice;
        RemoveSoldItem(commodity);
        RefreshGoldText();
        PlayerNumericManager.Instance.ApplyToCurrentPlayer();
        Debug.Log($"[merchantPanel] Bought Type={commodity.CommodityType}, ID={commodity.CommodityID}, EffectAdded={effectAdded}, Gold={playerData.gold}");
    }

    private bool TryBuyCommodity(commodityClass commodity, out bool effectAdded)
    {
        effectAdded = false;
        if (commodity == null)
        {
            return false;
        }

        PlayerNumericManager numericManager = PlayerNumericManager.Instance;
        switch (commodity.CommodityType)
        {
            case BuffCommodityType:
                if (numericManager.GetBuffConfig(commodity.CommodityID) == null)
                {
                    return false;
                }

                effectAdded = numericManager.TryAddRuntimeBuff(commodity.CommodityID);
                return true;

            case PropCommodityType:
                if (numericManager.GetPropConfig(commodity.CommodityID) == null)
                {
                    return false;
                }

                effectAdded = numericManager.TryAddProp(commodity.CommodityID);
                return true;

            default:
                return false;
        }
    }

    private void RemoveSoldItem(commodityClass commodity)
    {
        sceneStock.RemoveAll(item => item != null &&
                                    item.CommodityType == commodity.CommodityType &&
                                    item.CommodityID == commodity.CommodityID);

        merchantItem soldItem = _selectItem != null && _selectItem.Commodity == commodity
            ? _selectItem
            : _itemList.FirstOrDefault(item => item != null && item.Commodity == commodity);

        if (soldItem != null)
        {
            _itemList.Remove(soldItem);
            Destroy(soldItem.gameObject);
        }

        _selectItem = null;
        if (_itemList.Count > 0)
        {
            SetSelectedItem(_itemList[0]);
        }
    }

    private void RefreshGoldText()
    {
        if (gelo != null && GameManager.Instance != null)
        {
            gelo.text = GameManager.Instance.PlayerData.gold.ToString();
        }
    }

    private void CloseSelf()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ClosePanel<merchantPanel>();
        }
    }
}


