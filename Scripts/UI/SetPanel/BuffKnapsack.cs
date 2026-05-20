using UnityEngine;

public class BuffKnapsack : MonoBehaviour
{
    private const string ItemAbName = "uiitem";
    private const string ItemName = "BuffItem";
    private const string EditorItemPath = "Assets/AB_Resources/UI/item/SetPanel/BuffItem.prefab";

    [SerializeField] private RectTransform buffCenter;

    private void Awake()
    {
        BindCenter();
    }

    private void OnEnable()
    {
        PlayerNumericManager.Instance.OnNumericChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        PlayerNumericManager.Instance.OnNumericChanged -= Refresh;
    }

    public void Refresh()
    {
        BindCenter();
        if (buffCenter == null)
        {
            Debug.LogWarning("[BuffKnapsack] Buff item parent not found.");
            return;
        }

        if (!buffCenter.gameObject.activeSelf)
        {
            buffCenter.gameObject.SetActive(true);
        }

        foreach (Transform child in buffCenter)
        {
            Destroy(child.gameObject);
        }

        foreach (PlayerBuffRuntime runtime in PlayerNumericManager.Instance.RuntimeBuffs)
        {
            GameObject itemObj = SetPanelItemLoader.Load(ItemAbName, ItemName, EditorItemPath);
            if (itemObj == null)
            {
                Debug.LogWarning("[BuffKnapsack] Load BuffItem failed.");
                return;
            }

            itemObj.transform.SetParent(buffCenter, false);
            BuffItem item = itemObj.GetComponent<BuffItem>();
            if (item != null)
            {
                item.Init(runtime);
            }
        }
    }

    private void BindCenter()
    {
        if (buffCenter != null)
        {
            return;
        }

        Transform center = transform.Find("BuffCenter");
        if (center == null)
        {
            center = transform.Find("Content");
        }

        if (center == null && transform.childCount > 0)
        {
            center = transform.GetChild(0);
        }

        buffCenter = center != null ? center.GetComponent<RectTransform>() : GetComponent<RectTransform>();
    }
}
