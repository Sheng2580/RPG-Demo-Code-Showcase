using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class LoadCanvas : UnitySingleTonMono<LoadCanvas>
{
    [SerializeField] private float showSpeed = 5f;
    [SerializeField] private float hideSpeed = 1f;

    private CanvasGroup _canvasGroup;
    private Coroutine _fadeRoutine;
    private Coroutine _loadRoutine;

    public override void Awake()
    {
        base.Awake();
        if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        EnsureCanvasGroup();
    }


    public void Hied(UnityAction callBack = null)
    {
        StartFade(HiedCanvas(callBack));
    }

    public void Show()
    {
        StartFade(ShowCanvas());
    }

    public void LoadScene(string sceneName, UnityAction beforeLoad = null, UnityAction afterLoad = null)
    {
        if (_loadRoutine != null)
        {
            StopCoroutine(_loadRoutine);
        }

        _loadRoutine = StartCoroutine(LoadSceneWithTransition(sceneName, beforeLoad, afterLoad));
    }

    private IEnumerator LoadSceneWithTransition(string sceneName, UnityAction beforeLoad, UnityAction afterLoad)
    {
        yield return StartFade(ShowCanvas());

        beforeLoad?.Invoke();

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        if (operation == null)
        {
            Debug.LogError($"[LoadCanvas] Load scene failed: {sceneName}");
            _loadRoutine = null;
            yield break;
        }

        yield return operation;

        afterLoad?.Invoke();
        _loadRoutine = null;
    }

    private Coroutine StartFade(IEnumerator routine)
    {
        EnsureCanvasGroup();

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
        }

        _fadeRoutine = StartCoroutine(routine);
        return _fadeRoutine;
    }

    private IEnumerator HiedCanvas(UnityAction callBack = null)
    {
        EnsureCanvasGroup();
        _canvasGroup.blocksRaycasts = true;

        float alpha = _canvasGroup.alpha;
        while (alpha > 0f)
        {
            alpha -= Time.deltaTime * hideSpeed;
            _canvasGroup.alpha = alpha;
            yield return null;
        }

        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        callBack?.Invoke();
        _fadeRoutine = null;
    }

    private IEnumerator ShowCanvas()
    {
        EnsureCanvasGroup();
        _canvasGroup.blocksRaycasts = true;

        float alpha = _canvasGroup.alpha;
        while (alpha < 1f)
        {
            alpha += Time.deltaTime * showSpeed;
            _canvasGroup.alpha = alpha;
            yield return null;
        }

        _canvasGroup.alpha = 1f;
        _fadeRoutine = null;
    }

    private void EnsureCanvasGroup()
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            Debug.LogWarning("[LoadCanvas] CanvasGroup missing, added automatically.");
        }
    }
}


