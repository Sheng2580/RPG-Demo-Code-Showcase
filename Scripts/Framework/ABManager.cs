using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

public class ABManager : UnitySingleTonMono<ABManager>
{
    private readonly Dictionary<string, AssetBundle> assetBundlesDictionary = new Dictionary<string, AssetBundle>();
    // 同一个 AB 正在加载时合并回调，避免并发请求重复读包。
    private readonly Dictionary<string, List<UnityAction>> loadingCallbacks = new Dictionary<string, List<UnityAction>>();

    private AssetBundle mainAb;
    private AssetBundleManifest manifest;
    private bool isMainBundleLoading;
    private readonly List<UnityAction> mainBundleCallbacks = new List<UnityAction>();

    private string Pathur => Application.streamingAssetsPath;

    private string MainName
    {
        get
        {
#if UNITY_IOS
            return "iOS";
#elif UNITY_ANDROID
            return "Android";
#else
            return "PC";
#endif
        }
    }

    private string NormalizeABName(string abName)
    {
        if (string.IsNullOrEmpty(abName))
        {
            return abName;
        }

        return Path.GetFileName(abName.Replace("\\", "/"));
    }

    public void LoadMainAndManifestOfAB(string abName)
    {
        abName = NormalizeABName(abName);
        EnsureMainBundleLoaded();

        if (manifest == null)
        {
            Debug.LogError($"[ABManager] Manifest is null, cannot load AB: {abName}");
            return;
        }

        // 先按 Manifest 补齐依赖，再加载目标包。
        string[] dependencies = manifest.GetAllDependencies(abName);
        for (int i = 0; i < dependencies.Length; i++)
        {
            LoadBundleSync(NormalizeABName(dependencies[i]));
        }

        LoadBundleSync(abName);
    }

    private void EnsureMainBundleLoaded()
    {
        if (mainAb != null)
        {
            return;
        }

        string mainPath = Path.Combine(Pathur, MainName);
        mainAb = AssetBundle.LoadFromFile(mainPath);
        if (mainAb == null)
        {
            Debug.LogError($"[ABManager] Main bundle load failed: {mainPath}");
            return;
        }

        manifest = mainAb.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        if (manifest == null)
        {
            Debug.LogError($"[ABManager] Main bundle manifest missing: {MainName}");
        }
    }

    private void LoadBundleSync(string abName)
    {
        abName = NormalizeABName(abName);
        if (string.IsNullOrEmpty(abName) || assetBundlesDictionary.ContainsKey(abName))
        {
            return;
        }

        AssetBundle loadedBundle = GetLoadedAssetBundle(abName);
        if (loadedBundle == null)
        {
            loadedBundle = AssetBundle.LoadFromFile(Path.Combine(Pathur, abName));
        }

        if (loadedBundle != null)
        {
            assetBundlesDictionary[abName] = loadedBundle;
        }
        else
        {
            Debug.LogError($"[ABManager] AB load failed: {abName}");
        }
    }

    private AssetBundle GetLoadedAssetBundle(string abName)
    {
        string normalizedName = NormalizeABName(abName);
        foreach (AssetBundle loadedBundle in AssetBundle.GetAllLoadedAssetBundles())
        {
            if (loadedBundle == null)
            {
                continue;
            }

            if (string.Equals(NormalizeABName(loadedBundle.name), normalizedName, StringComparison.OrdinalIgnoreCase))
            {
                return loadedBundle;
            }
        }

        return null;
    }

    public object LoadRes(string abName, string resName)
    {
        abName = NormalizeABName(abName);
        LoadMainAndManifestOfAB(abName);
        if (!assetBundlesDictionary.TryGetValue(abName, out AssetBundle ab))
        {
            Debug.LogError($"[ABManager] Sync load failed, AB not loaded: {abName}");
            return null;
        }

        Object obj = ab.LoadAsset(resName);
        return InstantiateIfGameObject(obj, resName);
    }

    public object LoadRes(string abName, string resName, Type type)
    {
        abName = NormalizeABName(abName);
        LoadMainAndManifestOfAB(abName);
        if (!assetBundlesDictionary.TryGetValue(abName, out AssetBundle ab))
        {
            Debug.LogError($"[ABManager] Sync load failed, AB not loaded: {abName}");
            return null;
        }

        Object obj = ab.LoadAsset(resName, type);
        return InstantiateIfGameObject(obj, resName);
    }

    public T LoadRes<T>(string abName, string resName) where T : Object
    {
        abName = NormalizeABName(abName);
        LoadMainAndManifestOfAB(abName);
        if (!assetBundlesDictionary.TryGetValue(abName, out AssetBundle ab))
        {
            Debug.LogError($"[ABManager] Sync load failed, AB not loaded: {abName}");
            return null;
        }

        T obj = ab.LoadAsset<T>(resName);
        return InstantiateIfGameObject(obj, resName) as T;
    }

    private Object InstantiateIfGameObject(Object obj, string resName)
    {
        if (obj == null)
        {
            Debug.LogError($"[ABManager] Asset load failed: {resName}");
            return null;
        }

        if (obj is GameObject prefab)
        {
            GameObject newObj = Instantiate(prefab);
            newObj.name = resName;
            return newObj;
        }

        return obj;
    }

    public void LoadResAsync(string abName, string resName, UnityAction<Object> callback)
    {
        StartCoroutine(Really(abName, resName, callback));
    }

    public void LoadResAsync(string abName, string resName, Type type, UnityAction<Object> callback)
    {
        StartCoroutine(Really(abName, resName, type, callback));
    }

    public void LoadResAsync<T>(string abName, string resName, UnityAction<T> callback) where T : Object
    {
        StartCoroutine(Really(abName, resName, callback));
    }

    private void LoadABAsync(string abName, UnityAction onLoaded)
    {
        abName = NormalizeABName(abName);
        if (assetBundlesDictionary.ContainsKey(abName))
        {
            onLoaded?.Invoke();
            return;
        }

        AssetBundle loadedBundle = GetLoadedAssetBundle(abName);
        if (loadedBundle != null)
        {
            assetBundlesDictionary[abName] = loadedBundle;
            onLoaded?.Invoke();
            return;
        }

        if (loadingCallbacks.ContainsKey(abName))
        {
            loadingCallbacks[abName].Add(onLoaded);
            return;
        }

        loadingCallbacks[abName] = new List<UnityAction> { onLoaded };
        StartCoroutine(ReallyLoadABAsync(abName));
    }

    private IEnumerator ReallyLoadABAsync(string abName)
    {
        abName = NormalizeABName(abName);

        bool mainLoaded = false;
        LoadMainBundleAsync(() => mainLoaded = true);
        yield return new WaitUntil(() => mainLoaded);

        if (manifest == null)
        {
            Debug.LogError($"[ABManager] Manifest is null, cannot load AB: {abName}");
            ExecuteCallbacks(abName);
            yield break;
        }

        LoadBundleSync(abName);

        string[] dependencies = manifest.GetAllDependencies(abName);
        for (int i = 0; i < dependencies.Length; i++)
        {
            string depName = NormalizeABName(dependencies[i]);
            bool depLoaded = false;
            LoadABAsync(depName, () => depLoaded = true);
            yield return new WaitUntil(() => depLoaded);
        }

        ExecuteCallbacks(abName);
    }

    private void LoadMainBundleAsync(UnityAction onLoaded)
    {
        if (mainAb != null)
        {
            onLoaded?.Invoke();
            return;
        }

        if (isMainBundleLoading)
        {
            mainBundleCallbacks.Add(onLoaded);
            return;
        }

        isMainBundleLoading = true;
        mainBundleCallbacks.Add(onLoaded);
        StartCoroutine(LoadMainBundleCoroutine());
    }

    private IEnumerator LoadMainBundleCoroutine()
    {
        string mainPath = Path.Combine(Pathur, MainName);
        AssetBundleCreateRequest mainRequest = AssetBundle.LoadFromFileAsync(mainPath);
        yield return mainRequest;

        mainAb = mainRequest.assetBundle;
        if (mainAb != null)
        {
            manifest = mainAb.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        }
        else
        {
            Debug.LogError($"[ABManager] Main bundle load failed: {mainPath}");
        }

        isMainBundleLoading = false;
        for (int i = 0; i < mainBundleCallbacks.Count; i++)
        {
            mainBundleCallbacks[i]?.Invoke();
        }
        mainBundleCallbacks.Clear();
    }

    private void ExecuteCallbacks(string abName)
    {
        abName = NormalizeABName(abName);
        if (!loadingCallbacks.TryGetValue(abName, out List<UnityAction> callbacks))
        {
            return;
        }

        loadingCallbacks.Remove(abName);
        for (int i = 0; i < callbacks.Count; i++)
        {
            callbacks[i]?.Invoke();
        }
    }

    private IEnumerator Really(string abName, string resName, UnityAction<Object> callback)
    {
        abName = NormalizeABName(abName);
        bool abLoaded = false;
        LoadABAsync(abName, () => abLoaded = true);
        yield return new WaitUntil(() => abLoaded);

        if (!assetBundlesDictionary.TryGetValue(abName, out AssetBundle ab))
        {
            Debug.LogError($"[ABManager] Async load failed, AB not loaded: {abName}");
            callback?.Invoke(null);
            yield break;
        }

        AssetBundleRequest request = ab.LoadAssetAsync(resName);
        yield return request;
        callback?.Invoke(InstantiateIfGameObject(request.asset, resName));
    }

    private IEnumerator Really<T>(string abName, string resName, UnityAction<T> callback) where T : Object
    {
        abName = NormalizeABName(abName);
        bool abLoaded = false;
        LoadABAsync(abName, () => abLoaded = true);
        yield return new WaitUntil(() => abLoaded);

        if (!assetBundlesDictionary.TryGetValue(abName, out AssetBundle ab))
        {
            Debug.LogError($"[ABManager] Async load failed, AB not loaded: {abName}");
            callback?.Invoke(null);
            yield break;
        }

        AssetBundleRequest request = ab.LoadAssetAsync<T>(resName);
        yield return request;
        callback?.Invoke(InstantiateIfGameObject(request.asset, resName) as T);
    }

    private IEnumerator Really(string abName, string resName, Type type, UnityAction<Object> callback)
    {
        abName = NormalizeABName(abName);
        bool abLoaded = false;
        LoadABAsync(abName, () => abLoaded = true);
        yield return new WaitUntil(() => abLoaded);

        if (!assetBundlesDictionary.TryGetValue(abName, out AssetBundle ab))
        {
            Debug.LogError($"[ABManager] Async load failed, AB not loaded: {abName}");
            callback?.Invoke(null);
            yield break;
        }

        AssetBundleRequest request = ab.LoadAssetAsync(resName, type);
        yield return request;
        callback?.Invoke(InstantiateIfGameObject(request.asset, resName));
    }

    public void UnloadRes(string abName, bool unloadAllLoadedObjects = false)
    {
        abName = NormalizeABName(abName);
        if (assetBundlesDictionary.TryGetValue(abName, out AssetBundle ab))
        {
            ab.Unload(unloadAllLoadedObjects);
            assetBundlesDictionary.Remove(abName);
            Debug.Log($"[ABManager] AB unloaded: {abName}");
        }
    }

    public void UnloadAllRes(bool unloadAllLoadedObjects = false)
    {
        AssetBundle.UnloadAllAssetBundles(unloadAllLoadedObjects);
        assetBundlesDictionary.Clear();
        loadingCallbacks.Clear();
        mainBundleCallbacks.Clear();
        mainAb = null;
        manifest = null;
        isMainBundleLoading = false;
        Debug.Log("[ABManager] All AB unloaded");
    }
}


