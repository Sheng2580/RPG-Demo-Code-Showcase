using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ScenePreloadAssetInfo
{
    public string abName;
    public string assetName;
}

public abstract class SceneResourceManager<T> : UnitySingleTon<T> where T : MonoBehaviour
{
    [Header("场景开始时需要预加载的资源")]
    [SerializeField] private List<ScenePreloadAssetInfo> preloadAssets = new List<ScenePreloadAssetInfo>();

    [Header("场景销毁时需要卸载的AB包")]
    [SerializeField] private List<string> unloadAbNames = new List<string>();

    [Header("卸载AB包时是否同时卸载已加载对象")]
    [SerializeField] private bool unloadAllLoadedObjects;

    private readonly HashSet<string> loadedAssetKeys = new HashSet<string>();

    protected virtual void Start()
    {
        StartCoroutine(LoadSceneResources());
        
    }

    private IEnumerator LoadSceneResources()
    {
        yield return StartCoroutine(PreloadAssets());
        OnSceneResourcesLoaded();
    }

    private IEnumerator PreloadAssets()
    {
        for (int i = 0; i < preloadAssets.Count; i++)
        {
            ScenePreloadAssetInfo loadInfo = preloadAssets[i];
            if (!IsValidLoadInfo(loadInfo))
            {
                continue;
            }

            string cacheKey = GetCacheKey(loadInfo.abName, loadInfo.assetName);
            if (loadedAssetKeys.Contains(cacheKey))
            {
                continue;
            }

            bool isDone = false;
            Object loadedAsset = null;

            ABManager.Instance.LoadResAsync<Object>(loadInfo.abName, loadInfo.assetName, asset =>
            {
                loadedAsset = asset;
                isDone = true;
            });

            yield return new WaitUntil(() => isDone);

            if (loadedAsset == null)
            {
                Debug.LogError($"[{typeof(T).Name}] Preload failed: {loadInfo.abName}/{loadInfo.assetName}");
                continue;
            }

            loadedAssetKeys.Add(cacheKey);
        }
    }

    protected virtual void OnSceneResourcesLoaded()
    {
    }

    protected virtual void OnBeforeSceneResourcesUnload()
    {
    }

    protected override void OnDestroy()
    {
        OnBeforeSceneResourcesUnload();
        UnloadSceneAssetBundles();
        base.OnDestroy();
    }

    private void UnloadSceneAssetBundles()
    {
        if (ABManager.Instance == null)
        {
            return;
        }

        for (int i = 0; i < unloadAbNames.Count; i++)
        {
            string abName = unloadAbNames[i];
            if (string.IsNullOrEmpty(abName))
            {
                continue;
            }

            ABManager.Instance.UnloadRes(abName, unloadAllLoadedObjects);
        }
    }

    private bool IsValidLoadInfo(ScenePreloadAssetInfo loadInfo)
    {
        return loadInfo != null &&
               !string.IsNullOrEmpty(loadInfo.abName) &&
               !string.IsNullOrEmpty(loadInfo.assetName);
    }

    private string GetCacheKey(string abName, string assetName)
    {
        return $"{abName}/{assetName}";
    }
}
