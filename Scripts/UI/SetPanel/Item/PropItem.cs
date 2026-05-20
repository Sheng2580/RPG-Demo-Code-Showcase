using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PropItem : MonoBehaviour
{
   private Image _propImage;

   private Text _propName;

   //该道具的数量
   private Text _propSum;

   private void Awake()
   {
      _propImage = GetChildComponent<Image>("PropImage");
      _propName = GetChildComponent<Text>("PropName");
      _propSum = GetChildComponent<Text>("PropSum");
   }

   public void Init(int propId, int count)
   {
      PropConfig config = PlayerNumericManager.Instance.GetPropConfig(propId);
      if (config == null)
      {
         return;
      }

      if (_propName != null)
      {
         _propName.text = config.name;
      }

      if (_propSum != null)
      {
         _propSum.text = count.ToString();
      }

      LoadIcon(config.iconName);
   }

   private void LoadIcon(string iconName)
   {
      if (_propImage == null || string.IsNullOrEmpty(iconName) || ABManager.Instance == null)
      {
         return;
      }

      Sprite sprite = ABManager.Instance.LoadRes<Sprite>("icon", iconName);
      if (sprite != null)
      {
         _propImage.sprite = sprite;
      }
   }

   private T GetChildComponent<T>(string childName) where T : Component
   {
      Transform child = FindChildRecursive(transform, childName);
      return child != null ? child.GetComponent<T>() : null;
   }

   private Transform FindChildRecursive(Transform parent, string childName)
   {
      if (parent == null)
      {
         return null;
      }

      if (parent.name == childName)
      {
         return parent;
      }

      foreach (Transform child in parent)
      {
         Transform result = FindChildRecursive(child, childName);
         if (result != null)
         {
            return result;
         }
      }

      return null;
   }


}
