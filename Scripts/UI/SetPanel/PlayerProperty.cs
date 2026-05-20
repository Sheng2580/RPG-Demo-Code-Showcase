using UnityEngine;

public class PlayerProperty : MonoBehaviour
{
   private const string ItemAbName = "uiitem";
   private const string ItemName = "PropertyItem";
   private const string EditorItemPath = "Assets/AB_Resources/UI/item/SetPanel/PropertyItem.prefab";

   private RectTransform _propertyGroup;

   private void Awake()
   {
      Transform group = transform.Find("PropertyGroup");
      if (group != null)
      {
         _propertyGroup = group.GetComponent<RectTransform>();
      }
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
      if (_propertyGroup == null)
      {
         Transform group = transform.Find("PropertyGroup");
         if (group != null)
         {
            _propertyGroup = group.GetComponent<RectTransform>();
         }
      }

      if (_propertyGroup == null)
      {
         Debug.LogWarning("[PlayerProperty] PropertyGroup not found.");
         return;
      }

      if (!_propertyGroup.gameObject.activeSelf)
      {
         _propertyGroup.gameObject.SetActive(true);
      }

      foreach (Transform child in _propertyGroup)
      {
         Destroy(child.gameObject);
      }

      PlayerFinalStats finalStats = PlayerNumericManager.Instance.GetFinalStats();
      foreach (var pair in finalStats.Snapshots)
      {
         GameObject itemObj = SetPanelItemLoader.Load(ItemAbName, ItemName, EditorItemPath);
         if (itemObj == null)
         {
            Debug.LogWarning("[PlayerProperty] Load PropertyItem failed.");
            return;
         }

         itemObj.transform.SetParent(_propertyGroup, false);
         PropertyItem item = itemObj.GetComponent<PropertyItem>();
         if (item != null)
         {
            item.Init(pair.Value);
         }
      }
   }
}


