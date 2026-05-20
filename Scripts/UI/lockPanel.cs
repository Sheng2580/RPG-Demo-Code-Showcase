using DG.Tweening;
using UnityEngine;

public class lockPanel :BasePanel
{
    private RectTransform _lockImage;
    private RectTransform _parentRect;
    private Canvas _canvas;
    private Camera _mainCamera;
    private Vector2 _positionVelocity;
    private bool _hasPosition;
    private int _lastUpdatedFrame = -1;
    private Tweener _scaleTweener;
    private bool _closeWithoutAnimation;
    private GameObject _targetObject;

    public Vector3 targetOffset = Vector3.zero;
    [Range(0f, 0.2f)] public float followSmoothTime = 0.03f;
    [Range(0.01f, 1f)] public float showAnimTime = 0.18f;
    [Range(0.01f, 1f)] public float hideAnimTime = 0.12f;
    public bool hideWhenTargetBehindCamera = true;

    public override void Awake()
    {
        base.Awake();
        _lockImage = transform.Find("lockImage").GetComponent<RectTransform>();
        _parentRect = _lockImage != null ? _lockImage.parent as RectTransform : null;
        _canvas = GetComponentInParent<Canvas>();
    }

    private void OnEnable()
    {
        Canvas.willRenderCanvases += UpdateLockPositionBeforeRender;
    }

    private void OnDisable()
    {
        Canvas.willRenderCanvases -= UpdateLockPositionBeforeRender;
        _positionVelocity = Vector2.zero;
        _hasPosition = false;
        KillScaleTween();
    }

    private void UpdateLockPositionBeforeRender()
    {
        if (_lastUpdatedFrame == Time.frameCount)
        {
            return;
        }

        _lastUpdatedFrame = Time.frameCount;
        LockObject(_targetObject);
    }

    private void LockObject(GameObject obj)
    {
        if (_lockImage == null || _parentRect == null || obj == null)
        {
            SetLockImageVisible(false);
            return;
        }

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        if (_mainCamera == null)
        {
            SetLockImageVisible(false);
            return;
        }

        Vector3 worldPos = obj.transform.position + targetOffset;
        Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);
        if (hideWhenTargetBehindCamera && screenPos.z <= 0f)
        {
            SetLockImageVisible(false);
            _hasPosition = false;
            return;
        }

        Camera uiCamera = null;
        if (_canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            uiCamera = _canvas.worldCamera;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRect, screenPos, uiCamera, out Vector2 localPos))
        {
            SetLockImageVisible(false);
            return;
        }

        SetLockImageVisible(true);
        if (!_hasPosition || followSmoothTime <= 0f)
        {
            _lockImage.anchoredPosition = localPos;
            _positionVelocity = Vector2.zero;
            _hasPosition = true;
            return;
        }

        _lockImage.anchoredPosition = Vector2.SmoothDamp(
            _lockImage.anchoredPosition,
            localPos,
            ref _positionVelocity,
            followSmoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime
        );
    }

    private void SetLockImageVisible(bool visible)
    {
        if (_lockImage != null && _lockImage.gameObject.activeSelf != visible)
        {
            _lockImage.gameObject.SetActive(visible);
        }
    }

    public override void Hide()
    {
        if (!_closeWithoutAnimation)
        {
            PlayCloseAnimation();
            return;
        }

        _closeWithoutAnimation = false;
        base.Hide();
        _positionVelocity = Vector2.zero;
        _hasPosition = false;
        KillScaleTween();
        if (_lockImage != null)
        {
            _lockImage.localScale = Vector3.one*0.6f;
        }
    }

    public override void Show()
    {
        base.Show();
        if (_lockImage == null)
        {
            _lockImage = transform.Find("lockImage").GetComponent<RectTransform>();
        }

        if (_parentRect == null && _lockImage != null)
        {
            _parentRect = _lockImage.parent as RectTransform;
        }

        if (_canvas == null)
        {
            _canvas = GetComponentInParent<Canvas>();
        }

        _mainCamera = Camera.main;
        _positionVelocity = Vector2.zero;
        _hasPosition = false;
        transform.localScale = Vector3.one*0.6f;
    }

    public void SetTarget(GameObject target)
    {
        _closeWithoutAnimation = false;
        _targetObject = target;
        _positionVelocity = Vector2.zero;
        _hasPosition = false;
        LockObject(_targetObject);
        PlayShowAnimation();
    }

    public void PlayCloseAnimation()
    {
        KillScaleTween();
        Transform tweenTarget = _lockImage != null ? _lockImage.transform : transform;
        _scaleTweener = tweenTarget.DOScale(Vector3.zero, hideAnimTime)
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _closeWithoutAnimation = true;
                UIManager.Instance.ClosePanel<lockPanel>();
            });
    }

    private void PlayShowAnimation()
    {
        KillScaleTween();
        transform.localScale = Vector3.one*0.6f;
        Transform tweenTarget = _lockImage != null ? _lockImage.transform : transform;
        tweenTarget.localScale = Vector3.zero;
        _scaleTweener = tweenTarget.DOScale(Vector3.one, showAnimTime)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
    }

    private void KillScaleTween()
    {
        if (_scaleTweener != null)
        {
            _scaleTweener.Kill();
            _scaleTweener = null;
        }
    }

    private void OnValidate()
    {
        if (followSmoothTime < 0f)
        {
            followSmoothTime = 0f;
        }
    }
}


