using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PropertyItem : MonoBehaviour
{
  //该属性对应的图片
  private Image _propertyImage;
  //该属性的名字 （攻击力）
  private Text _propertyName;
  
  //该属性的总数值(基础加上加成Buff的结果)
  private Text _sumValue;
  
  //该属性的附加数值（buff提供的）
  private Text _buffValue;


  private void Awake()
  {
    _propertyImage = GetChildComponent<Image>("PropertyImage");
    _propertyName = GetChildComponent<Text>("PropertyName");
    _sumValue = GetChildComponent<Text>("SumValue");
    _buffValue = GetChildComponent<Text>("BuffValue");
  }

  public void Init(PlayerStatSnapshot snapshot)
  {
    if (snapshot == null || snapshot.property == null)
    {
      return;
    }

    if (_propertyName != null)
    {
      _propertyName.text = snapshot.property.name;
    }

    if (_sumValue != null)
    {
      _sumValue.text = FormatValue(snapshot.property, snapshot.FinalValue);
    }

    if (_buffValue != null)
    {
      _buffValue.text = snapshot.buffValue > 0f ? "(+" + FormatValue(snapshot.property, snapshot.buffValue)+")" : string.Empty;
    }

    LoadIcon(_propertyImage, snapshot.property.iconName);
  }

  private string FormatValue(PlayerPropertyConfig property, float value)
  {
    if (property != null && property.displayFormat == "P0")
    {
      return value.ToString("P0");
    }

    return value.ToString("0");
  }

  private void LoadIcon(Image image, string iconName)
  {
    if (image == null || string.IsNullOrEmpty(iconName) || ABManager.Instance == null)
    {
      return;
    }

    Sprite sprite = ABManager.Instance.LoadRes<Sprite>("icon", iconName);
    if (sprite != null)
    {
      image.sprite = sprite;
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
