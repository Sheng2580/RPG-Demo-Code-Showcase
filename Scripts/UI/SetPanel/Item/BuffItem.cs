using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuffItem : MonoBehaviour
{
    //buff对应的图片
    private Image _buffImage;
    //buff的描述文本 (攻击力++ 增加20%攻击)
    private Text _buffText;

    private void Awake()
    {
        _buffImage = GetChildComponent<Image>("BuffImage");
        _buffText = GetChildComponent<Text>("BuffText");
    }

    public void Init(PlayerBuffRuntime runtime)
    {
        if (runtime == null)
        {
            return;
        }

        BuffConfig config = PlayerNumericManager.Instance.GetBuffConfig(runtime.buffId);
        if (config == null)
        {
            return;
        }

        if (_buffText != null)
        {
            _buffText.text = runtime.stack > 1 ? $"{config.desc} x{runtime.stack}" : config.desc;
        }

        LoadIcon(config.iconName);
    }

    private void LoadIcon(string iconName)
    {
        if (_buffImage == null || string.IsNullOrEmpty(iconName) || ABManager.Instance == null)
        {
            return;
        }

        Sprite sprite = ABManager.Instance.LoadRes<Sprite>("icon", iconName);
        if (sprite != null)
        {
            _buffImage.sprite = sprite;
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
