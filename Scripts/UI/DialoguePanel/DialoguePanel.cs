using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DialoguePanel : BasePanel
{
    private Text nameText;
    private Text typeText;
    private Text descriptionText;
    private Transform tabControlCenter;
    private Button endButton;
    private readonly List<TabControlItem> tabItems = new List<TabControlItem>();

    public NPCBase currentNpc;
    public DialogueData currentDialogue;
    public event Action OnPanelClosed;

    private Tweener descriptionTextTweener;
    private bool suppressInteractionRefreshOnClose;

    public override void Awake()
    {
        base.Awake();

        Transform center = transform.Find("Center");
        Transform dialogueCenter = center != null ? center.Find("DialogueCenter") : null;
        nameText = dialogueCenter != null ? dialogueCenter.Find("nameText")?.GetComponent<Text>() : null;
        typeText = dialogueCenter != null ? dialogueCenter.Find("typeText")?.GetComponent<Text>() : null;
        descriptionText = center != null ? center.Find("dialogueText")?.GetComponent<Text>() : null;
        tabControlCenter = center != null ? center.Find("TabControlCenter") : null;
        endButton = transform.Find("EndButton")?.GetComponent<Button>();

        if (endButton != null)
        {
            endButton.onClick.RemoveListener(OnEndButtonClicked);
            endButton.onClick.AddListener(OnEndButtonClicked);
        }

        if (closeBtn != null)
        {
            closeBtn.onClick.RemoveListener(Hide);
            closeBtn.onClick.AddListener(CloseSelf);
        }
    }

    private void OnEnable()
    {
        if (endButton != null)
        {
            endButton.gameObject.SetActive(false);
        }

        specialTipPanel.Open("瀵硅瘽");
        EventCenter.Instance.EventTrigger(GameEvent.璁剧疆鐜╁杈撳叆鐘舵€? false);
    }

    private void OnDisable()
    {
        descriptionTextTweener?.Kill();
        descriptionTextTweener = null;
        ClearTabControls();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ClosePanel<specialTipPanel>();
        }
        EventCenter.Instance.EventTrigger(GameEvent.璁剧疆鐜╁杈撳叆鐘舵€? true);
    }

    private void Update()
    {
        if (GameInputManger.Instance.Esc)
        {
            CloseSelf();
            return;
        }

        if (GameInputManger.Instance.F)
        {
            HandleConfirmInput();
        }
    }

    public override void Hide()
    {
        base.Hide();
        OnPanelClosed?.Invoke();

        if (!suppressInteractionRefreshOnClose)
        {
            EventCenter.Instance.EventTrigger(GameEvent.鐜╁妫€娴婲pc);
        }

        suppressInteractionRefreshOnClose = false;
    }

    public void OpenDialogue(NPCBase npc)
    {
        currentNpc = npc;
        if (currentNpc == null)
        {
            Debug.LogWarning("[DialoguePanel] currentNpc is null");
            CloseSelf();
            return;
        }

        DialogueData startDialogue = DialogueConfigManager.Instance.GetStartDialogue(currentNpc.npcID);
        if (startDialogue == null)
        {
            Debug.LogWarning($"[DialoguePanel] No start dialogue configured for npcID={currentNpc.npcID}");
            CloseSelf();
            return;
        }

        ShowDialogue(startDialogue);
    }

    public void SetDialoguePanel()
    {
        ShowDialogue(currentDialogue);
    }

    private void HandleConfirmInput()
    {
        if (currentDialogue == null)
        {
            return;
        }

        if (IsTyping())
        {
            CompleteTyping();
            return;
        }

        if (tabItems.Count > 0)
        {
            return;
        }

        if (!string.IsNullOrEmpty(currentDialogue.nextDialogueId))
        {
            JumpToDialogue(currentDialogue.nextDialogueId);
            return;
        }

        if (endButton != null && endButton.gameObject.activeSelf)
        {
            CloseSelf();
        }
    }

    private void ShowDialogue(DialogueData dialogueData)
    {
        currentDialogue = dialogueData;
        if (currentDialogue == null)
        {
            Debug.LogWarning("[DialoguePanel] Dialogue data is null");
            CloseSelf();
            return;
        }

        if (nameText != null)
        {
            nameText.text = currentDialogue.name ?? string.Empty;
        }

        if (typeText != null)
        {
            typeText.text = string.IsNullOrEmpty(currentDialogue.occupation)
                ? string.Empty
                : $"[{currentDialogue.occupation}]";
        }

        StartDialogueTextTween(currentDialogue.dialogue);
        RefreshTabControls();
    }

    private void StartDialogueTextTween(string content)
    {
        descriptionTextTweener?.Kill();
        descriptionTextTweener = null;

        if (descriptionText == null)
        {
            return;
        }

        string finalText = content ?? string.Empty;
        descriptionText.text = string.Empty;
        SetTabControlsInteractable(false);

        float duration = Mathf.Max(0.2f, finalText.Length * 0.04f);
        descriptionTextTweener = descriptionText.DOText(finalText, duration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                descriptionTextTweener = null;
                SetTabControlsInteractable(true);
                RefreshEndButtonState();
            });
    }

    private void RefreshTabControls()
    {
        ClearTabControls();
        if (tabControlCenter == null || currentDialogue == null)
        {
            RefreshEndButtonState();
            return;
        }

        List<DialogueTabControlData> tabControls =
            DialogueConfigManager.Instance.GetTabControls(currentDialogue.id, currentDialogue.DialogueTabControlNum);

        foreach (DialogueTabControlData tabControlData in tabControls)
        {
            TabControlItem item = CreateTabControlItem();
            if (item == null)
            {
                continue;
            }

            item.transform.SetParent(tabControlCenter, false);
            item.gameObject.SetActive(true);
            item.Init(tabControlData, OnClickTabControl);
            item.SetInteractable(!IsTyping());
            tabItems.Add(item);
        }

        RefreshEndButtonState();
    }

    private TabControlItem CreateTabControlItem()
    {
        if (ABManager.Instance == null)
        {
            Debug.LogWarning("[DialoguePanel] ABManager is null");
            return null;
        }

        GameObject go = ABManager.Instance.LoadRes<GameObject>("uiitem", "TabControlItem");
        if (go == null)
        {
            Debug.LogError("[DialoguePanel] Failed to load TabControlItem");
            return null;
        }

        TabControlItem item = go.GetComponent<TabControlItem>();
        if (item == null)
        {
            item = go.AddComponent<TabControlItem>();
        }

        return item;
    }

    private void ClearTabControls()
    {
        for (int i = 0; i < tabItems.Count; i++)
        {
            TabControlItem item = tabItems[i];
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }

        tabItems.Clear();
    }

    private void SetTabControlsInteractable(bool interactable)
    {
        for (int i = 0; i < tabItems.Count; i++)
        {
            if (tabItems[i] != null)
            {
                tabItems[i].SetInteractable(interactable);
            }
        }
    }

    private void OnClickTabControl(DialogueTabControlData tabControlData)
    {
        if (tabControlData == null)
        {
            return;
        }

        if (IsTyping())
        {
            CompleteTyping();
            return;
        }

        if (tabControlData.TabType == DialogueTabControlType.Dialogue)
        {
            string nextDialogueId = !string.IsNullOrEmpty(tabControlData.nextDialogueId)
                ? tabControlData.nextDialogueId
                : currentDialogue != null ? currentDialogue.nextDialogueId : string.Empty;

            if (string.IsNullOrEmpty(nextDialogueId))
            {
                Debug.LogWarning($"[DialoguePanel] Tab id={tabControlData.id} has no nextDialogueId");
                RefreshEndButtonState();
                return;
            }

            JumpToDialogue(nextDialogueId);
            return;
        }

        suppressInteractionRefreshOnClose = true;
        CloseSelf();
        ExecuteNpcAction(tabControlData.actionKey);
    }

    private void ExecuteNpcAction(string actionKey)
    {
        if (currentNpc == null)
        {
            Debug.LogWarning("[DialoguePanel] currentNpc is null, cannot execute action");
            return;
        }

        if (string.IsNullOrEmpty(actionKey))
        {
            Debug.LogWarning("[DialoguePanel] actionKey is empty");
            return;
        }

        if (!currentNpc.CurrNpcInteractionActions.TryGetValue(actionKey, out UnityAction action))
        {
            Debug.LogWarning($"[DialoguePanel] NPC has no action named {actionKey}");
            return;
        }

        action.Invoke();
    }

    private void JumpToDialogue(string dialogueId)
    {
        DialogueData nextDialogue = DialogueConfigManager.Instance.GetDialogue(dialogueId);
        if (nextDialogue == null)
        {
            Debug.LogWarning($"[DialoguePanel] Dialogue not found: {dialogueId}");
            RefreshEndButtonState();
            return;
        }

        ShowDialogue(nextDialogue);
    }

    private void CompleteTyping()
    {
        if (descriptionTextTweener == null)
        {
            return;
        }

        descriptionTextTweener.Complete();
        descriptionTextTweener = null;
        if (descriptionText != null && currentDialogue != null)
        {
            descriptionText.text = currentDialogue.dialogue ?? string.Empty;
        }

        SetTabControlsInteractable(true);
        RefreshEndButtonState();
    }

    private bool IsTyping()
    {
        return descriptionTextTweener != null && descriptionTextTweener.IsActive();
    }

    private void RefreshEndButtonState()
    {
        if (endButton == null)
        {
            return;
        }

        bool shouldShow = currentDialogue != null
            && !IsTyping()
            && tabItems.Count == 0
            && string.IsNullOrEmpty(currentDialogue.nextDialogueId);

        endButton.gameObject.SetActive(shouldShow);
    }

    private void OnEndButtonClicked()
    {
        if (IsTyping())
        {
            CompleteTyping();
            return;
        }

        CloseSelf();
    }

    private void CloseSelf()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ClosePanel<DialoguePanel>();
        }
    }
}


