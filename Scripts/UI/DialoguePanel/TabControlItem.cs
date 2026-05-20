using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TabControlItem : MonoBehaviour
{
    // 图标只分“对话 / 交互”两种，缓存后避免重复从 AB 加载。
    private static readonly Dictionary<string, Sprite> IconCache = new Dictionary<string, Sprite>();

    private Image _tabImage;
    private Text _tabText;
    private Button _button;
    private DialogueTabControlData _currentData;

    private void Awake()
    {
        _tabImage = transform.GetChild(0).GetComponent<Image>();
        _tabText = transform.GetChild(1).GetComponent<Text>();
        _button = GetComponent<Button>();
    }

    public void Init(DialogueTabControlData data, UnityAction<DialogueTabControlData> clickAction)
    {
        _currentData = data;

        if (_tabText != null)
        {
            _tabText.text = data != null ? data.text : string.Empty;
        }

        if (_tabImage != null)
        {
            string iconName = data != null && data.TabType == DialogueTabControlType.Interaction
                ? "交互"
                : "对话";
            _tabImage.sprite = LoadIcon(iconName);
        }

        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            if (data != null && clickAction != null)
            {
                // 把整条选项数据回传给面板，面板再决定跳句还是执行交互。
                _button.onClick.AddListener(() => clickAction.Invoke(_currentData));
            }
        }
    }

    public void SetInteractable(bool interactable)
    {
        if (_button != null)
        {
            _button.interactable = interactable;
        }
    }

    private Sprite LoadIcon(string iconName)
    {
        if (string.IsNullOrEmpty(iconName))
        {
            return null;
        }

        if (IconCache.TryGetValue(iconName, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        // 选项 prefab 在 uiitem 包，图标资源在 icon 包。
        Sprite sprite = ABManager.Instance != null ? ABManager.Instance.LoadRes<Sprite>("icon", iconName) : null;
        if (sprite != null)
        {
            IconCache[iconName] = sprite;
        }
        else
        {
            Debug.LogWarning($"[TabControlItem] 无法加载图标: {iconName}");
        }

        return sprite;
    }
}
