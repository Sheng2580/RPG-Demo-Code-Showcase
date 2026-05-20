using UnityEngine;

public class Prop : MonoBehaviour
{
   private const string ItemAbName = "uiitem";
   private const string ItemName = "PropItem";
   private const string EditorItemPath = "Assets/AB_Resources/UI/item/SetPanel/PropItem.prefab";

   [SerializeField] private RectTransform center;

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
      if (center == null)
      {
         Debug.LogWarning("[Prop] Prop item parent not found.");
         return;
      }

      if (!center.gameObject.activeSelf)
      {
         center.gameObject.SetActive(true);
      }

      foreach (Transform child in center)
      {
         Destroy(child.gameObject);
      }

      foreach (var pair in PlayerNumericManager.Instance.GetProps())
      {
         if (pair.Value <= 0)
         {
            continue;
         }

         GameObject itemObj = SetPanelItemLoader.Load(ItemAbName, ItemName, EditorItemPath);
         if (itemObj == null)
         {
            Debug.LogWarning("[Prop] Load PropItem failed.");
            return;
         }

         itemObj.transform.SetParent(center, false);
         PropItem item = itemObj.GetComponent<PropItem>();
         if (item != null)
         {
            item.Init(pair.Key, pair.Value);
         }
      }
   }

   private void BindCenter()
   {
      if (center != null)
      {
         return;
      }

      Transform centerTrans = transform.Find("Center");
      if (centerTrans == null)
      {
         centerTrans = transform.Find("PropContent");
      }

      if (centerTrans == null)
      {
         centerTrans = transform.Find("Content");
      }

      if (centerTrans == null && transform.childCount > 0)
      {
         centerTrans = transform.GetChild(0);
      }

      center = centerTrans != null ? centerTrans.GetComponent<RectTransform>() : GetComponent<RectTransform>();
   }
}


