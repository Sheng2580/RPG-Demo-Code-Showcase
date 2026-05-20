using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InteractionPanel : BasePanel
{
    private GameObject center;
    private readonly List<InteractionItem> interactionItems = new List<InteractionItem>();
    private int currentInteractionIndex;

    private void Awake()
    {
        if (transform.childCount > 0)
        {
            center = transform.GetChild(0).gameObject;
        }
    }

    private void OnEnable()
    {
        currentInteractionIndex = 0;
    }

    private void OnDisable()
    {
        RemoveInteractionItem();
    }

    private void Update()
    {
        TabAction();
        UseAction();
    }

    public void RefreshInteractionItems(IReadOnlyDictionary<string, UnityAction> interactionActions)
    {
        RemoveInteractionItem();

        if (interactionActions == null || interactionActions.Count == 0)
        {
            return;
        }

        foreach (KeyValuePair<string, UnityAction> interactionAction in interactionActions)
        {
            AddInteractionItem(interactionAction.Key, interactionAction.Value);
        }

        currentInteractionIndex = 0;
        SetUseAction();
    }

    public void AddInteractionItem(string text, UnityAction callback)
    {
        InteractionItem interactionItem = GetOrCreateInteractionItem(interactionItems.Count);
        if (interactionItem == null)
        {
            Debug.LogError("[InteractionPanel] Failed to create InteractionItem.");
            return;
        }

        interactionItem.transform.SetParent(center.transform, false);
        interactionItem.transform.localScale = Vector3.one;
        interactionItem.gameObject.SetActive(true);
        interactionItem.InitInteraction(text, callback);
        interactionItem.StopAction();

        interactionItems.Add(interactionItem);
        HideUnusedItems();
    }

    public void RemoveInteractionItem()
    {
        for (int i = 0; i < interactionItems.Count; i++)
        {
            if (interactionItems[i] != null)
            {
                interactionItems[i].gameObject.SetActive(false);
            }
        }

        interactionItems.Clear();
        currentInteractionIndex = 0;
        HideUnusedItems();
    }

    private InteractionItem GetOrCreateInteractionItem(int desiredIndex)
    {
        if (center == null)
        {
            return null;
        }

        if (center.transform.childCount > desiredIndex)
        {
            GameObject child = center.transform.GetChild(desiredIndex).gameObject;
            InteractionItem existingItem = child.GetComponent<InteractionItem>();
            if (existingItem != null)
            {
                return existingItem;
            }
        }

        GameObject prefab = Resources.Load<GameObject>("UI/InteractionItem");
        if (prefab != null)
        {
            GameObject go = Instantiate(prefab);
            return go.GetComponent<InteractionItem>() ?? go.AddComponent<InteractionItem>();
        }

        if (ABManager.Instance != null)
        {
            GameObject go = ABManager.Instance.LoadRes<GameObject>("uiitem", "InteractionItem");
            if (go != null)
            {
                return go.GetComponent<InteractionItem>() ?? go.AddComponent<InteractionItem>();
            }
        }

        return null;
    }

    private void HideUnusedItems()
    {
        if (center == null)
        {
            return;
        }

        for (int i = interactionItems.Count; i < center.transform.childCount; i++)
        {
            GameObject child = center.transform.GetChild(i).gameObject;
            if (child != null)
            {
                child.SetActive(false);
            }
        }
    }

    private void TabAction()
    {
        if (GameInputManger.Instance.Tab && interactionItems.Count > 0)
        {
            if (currentInteractionIndex < interactionItems.Count - 1)
            {
                currentInteractionIndex++;
            }
            else
            {
                currentInteractionIndex = 0;
            }

            SetUseAction();
        }
    }

    private void UseAction()
    {
        if (GameInputManger.Instance.F)
        {
            if (interactionItems.Count == 0)
            {
                return;
            }

            currentInteractionIndex = Mathf.Clamp(currentInteractionIndex, 0, interactionItems.Count - 1);
            InteractionItem item = interactionItems[currentInteractionIndex];
            if (item != null)
            {
                item.CallInteraction();
                Debug.Log($"[InteractionPanel] Call interaction index {currentInteractionIndex}");
            }
        }
    }

    private void SetUseAction()
    {
        for (int i = 0; i < interactionItems.Count; i++)
        {
            if (i == currentInteractionIndex)
            {
                interactionItems[i].UseAction();
            }
            else
            {
                interactionItems[i].StopAction();
            }
        }
    }
}
