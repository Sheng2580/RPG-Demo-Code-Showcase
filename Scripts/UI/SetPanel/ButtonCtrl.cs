using UnityEngine;
using UnityEngine.UI;

public class ButtonCtrl : MonoBehaviour
{
   [SerializeField] private Color selectedButtonColor = new Color(0.45f, 0.45f, 0.45f, 1f);

   private Button _propertyButton;
   private Button _buffButton;
   private Button _propButton;
   private Button _setButton;

   private Color _propertyNormalColor;
   private Color _buffNormalColor;
   private Color _propNormalColor;
   private Color _setNormalColor;
   private bool _hasCachedButtonColors;

   private GameObject _playerProperty;
   private GameObject _buffKnapsack;
   private GameObject _prop;
   private GameObject _set;

   private void Awake()
   {
      Bind();
      AddListeners();
   }

   private void OnEnable()
   {
      Bind();
      ShowProperty();
   }

   private void Bind()
   {
      Transform root = FindPanelRoot();
      _propertyButton = FindButton(root, "PropertyButton");
      _buffButton = FindButton(root, "BuffButton");
      _propButton = FindButton(root, "PropButton");
      _setButton = FindButton(root, "SetButton");

      Transform center = root.Find("Center");
      if (center == null)
      {
         center = root;
      }

      _playerProperty = FindDirectChild(center, "PlayerProperty");
      _buffKnapsack = FindDirectChild(center, "BuffKnapsack");
      _prop = FindDirectChild(center, "Prop");
      _set = FindDirectChild(center, "Set");

      CacheButtonColors();
   }

   private Transform FindPanelRoot()
   {
      Transform current = transform;
      while (current != null)
      {
         if (current.Find("Center") != null)
         {
            return current;
         }

         current = current.parent;
      }

      Debug.LogWarning("[ButtonCtrl] SetPanel root not found, fallback to current transform.");
      return transform;
   }

   private void AddListeners()
   {
      _propertyButton?.onClick.RemoveListener(ShowProperty);
      _buffButton?.onClick.RemoveListener(ShowBuff);
      _propButton?.onClick.RemoveListener(ShowProp);
      _setButton?.onClick.RemoveListener(ShowSet);

      _propertyButton?.onClick.AddListener(ShowProperty);
      _buffButton?.onClick.AddListener(ShowBuff);
      _propButton?.onClick.AddListener(ShowProp);
      _setButton?.onClick.AddListener(ShowSet);
   }

   public void ShowProperty()
   {
      ShowOnly(_playerProperty);
   }

   public void ShowBuff()
   {
      ShowOnly(_buffKnapsack);
   }

   public void ShowProp()
   {
      ShowOnly(_prop);
   }

   public void ShowSet()
   {
      ShowOnly(_set);
   }

   private void ShowOnly(GameObject target)
   {
      SetActive(_playerProperty, target == _playerProperty);
      SetActive(_buffKnapsack, target == _buffKnapsack);
      SetActive(_prop, target == _prop);
      SetActive(_set, target == _set);
      SetSelectedButton(GetButtonByTarget(target));
   }

   private Button GetButtonByTarget(GameObject target)
   {
      if (target == _playerProperty)
      {
         return _propertyButton;
      }

      if (target == _buffKnapsack)
      {
         return _buffButton;
      }

      if (target == _prop)
      {
         return _propButton;
      }

      if (target == _set)
      {
         return _setButton;
      }

      return null;
   }

   private void CacheButtonColors()
   {
      if (_hasCachedButtonColors)
      {
         return;
      }

      _propertyNormalColor = GetGraphicColor(_propertyButton);
      _buffNormalColor = GetGraphicColor(_buffButton);
      _propNormalColor = GetGraphicColor(_propButton);
      _setNormalColor = GetGraphicColor(_setButton);
      _hasCachedButtonColors = true;
   }

   private Color GetGraphicColor(Button button)
   {
      return button != null && button.targetGraphic != null ? button.targetGraphic.color : Color.white;
   }

   private void SetSelectedButton(Button selectedButton)
   {
      ApplyButtonState(_propertyButton, selectedButton, _propertyNormalColor);
      ApplyButtonState(_buffButton, selectedButton, _buffNormalColor);
      ApplyButtonState(_propButton, selectedButton, _propNormalColor);
      ApplyButtonState(_setButton, selectedButton, _setNormalColor);
   }

   private void ApplyButtonState(Button button, Button selectedButton, Color normalColor)
   {
      if (button == null)
      {
         return;
      }

      bool selected = button == selectedButton;
      button.interactable = !selected;
      if (button.targetGraphic != null)
      {
         button.targetGraphic.color = selected ? selectedButtonColor : normalColor;
      }
   }

   private void SetActive(GameObject obj, bool active)
   {
      if (obj != null && obj.activeSelf != active)
      {
         obj.SetActive(active);
      }
   }

   private Button FindButton(Transform root, string buttonName)
   {
      Transform buttonTrans = FindChildRecursive(root, buttonName);
      return buttonTrans != null ? buttonTrans.GetComponent<Button>() : null;
   }

   private GameObject FindDirectChild(Transform parent, string childName)
   {
      Transform child = parent != null ? parent.Find(childName) : null;
      return child != null ? child.gameObject : null;
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
