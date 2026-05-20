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
    [Header("鍦烘櫙寮€濮嬫椂闇€瑕侀鍔犺浇鐨勮祫婧?)]
    [SerializeField] private List<ScenePreloadAssetInfo> preloadAssets = new List<ScenePreloadAssetInfo>();

    [Header("鍦烘櫙閿€姣佹椂闇€瑕佸嵏杞界殑AB鍖?)]
    [SerializeField] private List<string> unloadAbNames = new List<string>();

    [Header("鍗歌浇AB鍖呮椂鏄惁鍚屾椂鍗歌浇宸插姞杞藉璞?)]
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


