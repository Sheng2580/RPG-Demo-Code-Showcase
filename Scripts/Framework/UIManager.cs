using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public enum UILayer
{
    Static,

    Dynamic,

    Top
}

public class UIManager : UnitySingleTonMono<UIManager>
{
    private const string PanelAbName = "uipanel";
    private const string ResourcesPanelPath = "UI/";

    private GameObject rootCanvasObj;
    private Canvas staticCanvas;
    private Canvas lowFreqCanvas;
    private Canvas highFreqCanvas;

    private readonly Dictionary<string, Queue<BasePanel>> uiPool = new Dictionary<string, Queue<BasePanel>>();

    private readonly Dictionary<string, BasePanel> openedPanels = new Dictionary<string, BasePanel>();

    // 合并同一面板的异步加载请求，避免重复加载 AB 资源。
    private readonly Dictionary<string, List<UnityAction<BasePanel>>> loadingPanelCallbacks = new Dictionary<string, List<UnityAction<BasePanel>>>();

    private readonly Dictionary<string, UILayer> loadingPanelLayers = new Dictionary<string, UILayer>();

    // ClosePanel 可能发生在 AB 回调之前，用版本号丢弃过期加载结果。
    private readonly Dictionary<string, int> loadingPanelVersions = new Dictionary<string, int>();

    private readonly Vector2 designResolution = new Vector2(1920, 1080);
    private readonly CanvasScaler.ScaleMode scaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

    public override void Awake()
    {
        base.Awake();
        if (Instance != this)
        {
            return;
        }

        if (rootCanvasObj != null)
        {
            return;
        }

        rootCanvasObj = new GameObject("UI_Root", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas rootCanvas = rootCanvasObj.GetComponent<Canvas>();
        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        rootCanvas.pixelPerfect = true;
        DontDestroyOnLoad(rootCanvasObj);

        CanvasScaler rootScaler = rootCanvasObj.GetComponent<CanvasScaler>();
        SetupCanvasScaler(rootScaler);

        staticCanvas = CreateSubCanvas("Canvas_Static", rootCanvas.transform, false);
        lowFreqCanvas = CreateSubCanvas("Canvas_LowFreq", rootCanvas.transform, true);
        highFreqCanvas = CreateSubCanvas("Canvas_HighFreq", rootCanvas.transform, true);
        EnsureEventSystem();
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObj = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(eventSystemObj);
    }

    protected override void OnDestroy()
    {
        if (rootCanvasObj != null)
        {
            Destroy(rootCanvasObj);
            rootCanvasObj = null;
        }

        base.OnDestroy();
    }

    private Canvas CreateSubCanvas(string canvasName, Transform parent, bool needRaycaster)
    {
        GameObject subCanvasObj = new GameObject(canvasName);
        subCanvasObj.transform.SetParent(parent, false);

        RectTransform rect = subCanvasObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        Canvas subCanvas = subCanvasObj.AddComponent<Canvas>();
        subCanvas.overrideSorting = false;

        if (needRaycaster)
        {
            subCanvasObj.AddComponent<GraphicRaycaster>();
        }

        return subCanvas;
    }

    private void SetupCanvasScaler(CanvasScaler scaler)
    {
        scaler.uiScaleMode = scaleMode;
        scaler.referenceResolution = designResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        scaler.matchWidthOrHeight = 0;
        scaler.referencePixelsPerUnit = 100;
    }

    public T OpenPanel<T>(UILayer layer = UILayer.Dynamic) where T : BasePanel
    {
        BasePanel panel = GetOrOpenCachedPanel<T>(layer);
        if (panel != null)
        {
            return panel as T;
        }

        OpenPanelAsync<T>(layer);
        return null;
    }

    public T OpenPanel<T>(UILayer layer, bool useResManager) where T : BasePanel
    {
        return useResManager ? OpenPanelByRes<T>(layer) : OpenPanel<T>(layer);
    }

    public T OpenPanelByRes<T>(UILayer layer = UILayer.Dynamic) where T : BasePanel
    {
        string panelName = typeof(T).Name;

        BasePanel panel = GetOrOpenCachedPanel<T>(layer);
        if (panel != null)
        {
            return panel as T;
        }

        if (ResourceManager.Instance == null)
        {
            Debug.LogError("[UIManager] ResourceManager is null, cannot load panel: " + panelName);
            return null;
        }

        GameObject panelObj = ResourceManager.Instance.load<GameObject>(ResourcesPanelPath + panelName);
        if (panelObj == null)
        {
            Debug.LogError($"[UIManager] Load Resources panel failed: {ResourcesPanelPath}{panelName}");
            return null;
        }

        T loadedPanel = panelObj.GetComponent<T>();
        if (loadedPanel == null)
        {
            Debug.LogError($"[UIManager] Panel prefab missing component {typeof(T).Name}: {ResourcesPanelPath}{panelName}");
            Destroy(panelObj);
            return null;
        }

        loadedPanel.name = panelName;
        OpenLoadedPanel(panelName, loadedPanel, layer);
        return loadedPanel;
    }


    public void OpenPanelAsync<T>(UILayer layer = UILayer.Dynamic, UnityAction<T> callback = null) where T : BasePanel
    {
        string panelName = typeof(T).Name;

        BasePanel panel = GetOrOpenCachedPanel<T>(layer);
        if (panel != null)
        {
            callback?.Invoke(panel as T);
            return;
        }

        UnityAction<BasePanel> wrappedCallback = loadedPanel => callback?.Invoke(loadedPanel as T);
        if (loadingPanelCallbacks.TryGetValue(panelName, out List<UnityAction<BasePanel>> callbacks))
        {
            callbacks.Add(wrappedCallback);
            return;
        }

        int loadVersion = loadingPanelVersions.TryGetValue(panelName, out int currentVersion) ? currentVersion + 1 : 1;
        loadingPanelVersions[panelName] = loadVersion;
        loadingPanelCallbacks.Add(panelName, new List<UnityAction<BasePanel>> { wrappedCallback });
        loadingPanelLayers.Add(panelName, layer);

        if (ABManager.Instance == null)
        {
            Debug.LogError("[UIManager] ABManager is null, cannot load panel: " + panelName);
            InvokeLoadingCallbacks(panelName, null);
            return;
        }

        ABManager.Instance.LoadResAsync<GameObject>(PanelAbName, panelName, prefabObj =>
        {
            if (!loadingPanelCallbacks.ContainsKey(panelName) ||
                !loadingPanelVersions.TryGetValue(panelName, out int activeVersion) ||
                activeVersion != loadVersion)
            {
                if (prefabObj != null)
                {
                    Destroy(prefabObj);
                }

                return;
            }

            if (prefabObj == null)
            {
                Debug.LogError($"[UIManager] Load panel failed: {PanelAbName}/{panelName}");
                InvokeLoadingCallbacks(panelName, null);
                return;
            }

            T loadedPanel = prefabObj.GetComponent<T>();
            if (loadedPanel == null)
            {
                Debug.LogError($"[UIManager] Panel prefab missing component {typeof(T).Name}: {panelName}");
                Destroy(prefabObj);
                InvokeLoadingCallbacks(panelName, null);
                return;
            }

            loadedPanel.name = panelName;
            UILayer targetLayer = loadingPanelLayers.TryGetValue(panelName, out UILayer pendingLayer) ? pendingLayer : layer;
            OpenLoadedPanel(panelName, loadedPanel, targetLayer);
            InvokeLoadingCallbacks(panelName, loadedPanel);
        });
    }

    private BasePanel GetOrOpenCachedPanel<T>(UILayer layer) where T : BasePanel
    {
        string panelName = typeof(T).Name;

        if (openedPanels.TryGetValue(panelName, out BasePanel panel))
        {
            panel.transform.SetAsLastSibling();
            return panel;
        }

        if (uiPool.ContainsKey(panelName) && uiPool[panelName].Count > 0)
        {
            panel = uiPool[panelName].Dequeue();
            panel.gameObject.SetActive(true);
            OpenLoadedPanel(panelName, panel, layer);
            return panel;
        }

        return null;
    }

    private void OpenLoadedPanel(string panelName, BasePanel panel, UILayer layer)
    {
        Canvas targetCanvas = layer switch
        {
            UILayer.Static => staticCanvas,
            UILayer.Dynamic => lowFreqCanvas,
            UILayer.Top => highFreqCanvas,
            _ => lowFreqCanvas
        };

        panel.transform.SetParent(targetCanvas.transform, false);
        panel.transform.SetAsLastSibling();
        openedPanels[panelName] = panel;

        panel.Show();
    }

    private void InvokeLoadingCallbacks(string panelName, BasePanel panel)
    {
        if (loadingPanelCallbacks.TryGetValue(panelName, out List<UnityAction<BasePanel>> callbacks))
        {
            for (int i = 0; i < callbacks.Count; i++)
            {
                callbacks[i]?.Invoke(panel);
            }
        }

        loadingPanelCallbacks.Remove(panelName);
        loadingPanelLayers.Remove(panelName);
        loadingPanelVersions.Remove(panelName);
    }

    public void ClosePanel<T>() where T : BasePanel
    {
        string panelName = typeof(T).Name;

        if (loadingPanelCallbacks.ContainsKey(panelName))
        {
            InvokeLoadingCallbacks(panelName, null);
            return;
        }

        BasePanel panel = GetPanel<T>();
        if (panel == null)
        {
            return;
        }

        openedPanels.Remove(panelName);
        panel.Hide();
        if (!uiPool.ContainsKey(panelName))
        {
            uiPool.Add(panelName, new Queue<BasePanel>());
        }

        uiPool[panelName].Enqueue(panel);
    }

    public T GetPanel<T>() where T : BasePanel
    {
        string panelName = typeof(T).Name;
        if (openedPanels.TryGetValue(panelName, out BasePanel panel))
        {
            return panel as T;
        }

        return null;
    }

    public bool IsPanelOpened<T>() where T : BasePanel
    {
        return openedPanels.ContainsKey(typeof(T).Name);
    }

    public void CloseAllPanels()
    {
        ClosePanelsInCanvas(staticCanvas);
        ClosePanelsInCanvas(lowFreqCanvas);
        ClosePanelsInCanvas(highFreqCanvas);
        openedPanels.Clear();
    }

    public void DestroyAllPanels()
    {
        CloseAllPanels();

        foreach (var kvp in uiPool)
        {
            foreach (BasePanel panel in kvp.Value)
            {
                if (panel != null)
                {
                    Destroy(panel.gameObject);
                }
            }
        }

        uiPool.Clear();
    }

    private void ClosePanelsInCanvas(Canvas canvas)
    {
        if (canvas == null)
        {
            return;
        }

        List<BasePanel> panelsToClose = new List<BasePanel>();

        foreach (Transform child in canvas.transform)
        {
            BasePanel panel = child.GetComponent<BasePanel>();
            if (panel != null)
            {
                panelsToClose.Add(panel);
            }
        }

        foreach (BasePanel panel in panelsToClose)
        {
            string panelName = panel.GetType().Name;
            panel.Hide();

            if (!uiPool.ContainsKey(panelName))
            {
                uiPool.Add(panelName, new Queue<BasePanel>());
            }

            uiPool[panelName].Enqueue(panel);
        }
    }
}


