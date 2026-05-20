using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TabControlItem : MonoBehaviour
{
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
                ? "浜や簰"
                : "瀵硅瘽";
            _tabImage.sprite = LoadIcon(iconName);
        }

        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            if (data != null && clickAction != null)
            {
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

        if (sprite != null)
        {
            IconCache[iconName] = sprite;
        }
        else
        {
            Debug.LogWarning($"[TabControlItem] 鏃犳硶鍔犺浇鍥炬爣: {iconName}");
        }

        return sprite;
    }
}


