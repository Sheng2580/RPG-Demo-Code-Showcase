using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DialogueConfigManager : SingleTon<DialogueConfigManager>
{
    private const string DialogueTableName = "tbDialogue";
    private const string DialogueTabControlTableName = "tbDialogueTabControl";

    private bool isLoaded;
    private readonly Dictionary<string, DialogueData> dialogueDict = new Dictionary<string, DialogueData>();
    private readonly Dictionary<int, List<DialogueData>> npcDialogueDict = new Dictionary<int, List<DialogueData>>();
    private readonly Dictionary<string, List<DialogueTabControlData>> tabControlDict =
        new Dictionary<string, List<DialogueTabControlData>>();

    public void Reload()
    {
        isLoaded = false;
        dialogueDict.Clear();
        npcDialogueDict.Clear();
        tabControlDict.Clear();
        EnsureLoaded();
    }

    public DialogueData GetDialogue(string dialogueId)
    {
        EnsureLoaded();
        if (string.IsNullOrEmpty(dialogueId))
        {
            return null;
        }

        dialogueDict.TryGetValue(dialogueId, out DialogueData dialogueData);
        return dialogueData;
    }

    public DialogueData GetStartDialogue(int npcId)
    {
        EnsureLoaded();
        if (!npcDialogueDict.TryGetValue(npcId, out List<DialogueData> dialogueList) || dialogueList == null || dialogueList.Count == 0)
        {
            Debug.LogWarning($"[DialogueConfigManager] No dialogues found for npcID={npcId}");
            return null;
        }

        DialogueData startDialogue = dialogueList.FirstOrDefault(data => data != null && data.isStart);
        return startDialogue ?? dialogueList[0];
    }

    public List<DialogueTabControlData> GetTabControls(string dialogueId, int expectedCount = -1)
    {
        EnsureLoaded();
        if (string.IsNullOrEmpty(dialogueId))
        {
            return new List<DialogueTabControlData>();
        }

        if (!tabControlDict.TryGetValue(dialogueId, out List<DialogueTabControlData> tabList) || tabList == null)
        {
            return new List<DialogueTabControlData>();
        }

        List<DialogueTabControlData> result = tabList
            .Where(data => data != null)
            .ToList();

        if (expectedCount >= 0 && expectedCount != result.Count)
        {
            Debug.LogWarning(
                $"[DialogueConfigManager] dialogueID={dialogueId} tab count is {result.Count}, expected {expectedCount}");
        }

        return result;
    }

    private void EnsureLoaded()
    {
        if (isLoaded)
        {
            return;
        }

        LoadDialogues();
        LoadTabControls();
        isLoaded = true;
    }

    private void LoadDialogues()
    {
        List<DialogueData> dialogueList = JsonManager.Instance.LoadData<List<DialogueData>>(DialogueTableName);
        if (dialogueList == null)
        {
            Debug.LogWarning($"[DialogueConfigManager] {DialogueTableName}.json load result is null");
            return;
        }

        foreach (DialogueData dialogueData in dialogueList)
        {
            if (dialogueData == null || string.IsNullOrEmpty(dialogueData.id))
            {
                continue;
            }

            if (dialogueDict.ContainsKey(dialogueData.id))
            {
                Debug.LogWarning($"[DialogueConfigManager] Duplicate dialogue id={dialogueData.id}");
                continue;
            }

            dialogueDict.Add(dialogueData.id, dialogueData);
            if (!npcDialogueDict.TryGetValue(dialogueData.npcID, out List<DialogueData> npcDialogues) || npcDialogues == null)
            {
                npcDialogues = new List<DialogueData>();
                npcDialogueDict[dialogueData.npcID] = npcDialogues;
            }

            npcDialogues.Add(dialogueData);
        }
    }

    private void LoadTabControls()
    {
        List<DialogueTabControlData> tabControlList =
            JsonManager.Instance.LoadData<List<DialogueTabControlData>>(DialogueTabControlTableName);

        if (tabControlList == null)
        {
            Debug.LogWarning($"[DialogueConfigManager] {DialogueTabControlTableName}.json load result is null");
            return;
        }

        foreach (DialogueTabControlData tabControlData in tabControlList)
        {
            if (tabControlData == null || string.IsNullOrEmpty(tabControlData.dialogueID))
            {
                continue;
            }

            if (!tabControlDict.TryGetValue(tabControlData.dialogueID, out List<DialogueTabControlData> tabList) || tabList == null)
            {
                tabList = new List<DialogueTabControlData>();
                tabControlDict[tabControlData.dialogueID] = tabList;
            }

            tabList.Add(tabControlData);
        }
    }
}


