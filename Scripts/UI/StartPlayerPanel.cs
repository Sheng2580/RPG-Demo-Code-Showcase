using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class StartPlayerPanel : BasePanel
{
    private const int MinRenderTextureSize = 64;
    private static readonly int KeyColorId = Shader.PropertyToID("_KeyColor");
    private static readonly Color PreviewClearColor = new Color(1f, 0f, 1f, 0f);

    private Material materialInstance;
    private RawImage rawImage;
    private RectTransform rawImageRect;
    private Camera previewCamera;
    private UniversalAdditionalCameraData previewCameraData;
    private Texture originalTexture;
    private RenderTexture originalTargetTexture;
    private RenderTexture runtimeRenderTexture;
    private bool originalRenderPostProcessing;
    private bool hasOriginalRenderPostProcessing;
    private CameraClearFlags originalClearFlags;
    private Color originalBackgroundColor;
    private bool hasOriginalCameraState;

    private int lastTextureWidth = -1;
    private int lastTextureHeight = -1;
    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;

    private Coroutine silhouetteCoroutine;
    private Coroutine previewSetupCoroutine;
    private bool pendingForcePreviewSetup;

    private int PreviewAntiAliasing
    {
        get
        {
            int qualityAntiAliasing = QualitySettings.antiAliasing;
            return qualityAntiAliasing >= 2 ? qualityAntiAliasing : 4;
        }
    }

    public override void Awake()
    {
        base.Awake();

        StretchToParent(transform as RectTransform);
        rawImage = transform.GetChild(0).GetComponent<RawImage>();
        rawImageRect = rawImage != null ? rawImage.rectTransform : null;
        StretchToParent(rawImageRect);

        if (rawImage != null)
        {
            materialInstance = rawImage.material;
            originalTexture = rawImage.texture;

            if (materialInstance != null && materialInstance.HasProperty(KeyColorId))
            {
                materialInstance.SetColor(KeyColorId, new Color(PreviewClearColor.r, PreviewClearColor.g, PreviewClearColor.b, 1f));
            }
        }
    }

    private void OnEnable()
    {
        EventCenter.Instance.AddEventListener(GameEvent.鍓奖鍖? SetFactor);
        Canvas.willRenderCanvases += HandleWillRenderCanvases;
        QueuePreviewSetup(true);
    }

    private void Start()
    {
        if (materialInstance != null)
        {
            materialInstance.SetFloat("_SilhouetteFactor", 0f);
        }

        QueuePreviewSetup(true);
    }

    private void LateUpdate()
    {
        EnsurePreviewSetup();
    }

    private void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener(GameEvent.鍓奖鍖? SetFactor);

        if (silhouetteCoroutine != null)
        {
            StopCoroutine(silhouetteCoroutine);
            silhouetteCoroutine = null;
        }

        Canvas.willRenderCanvases -= HandleWillRenderCanvases;
        StopPreviewSetupCoroutine();
        RestoreOriginalPreviewTarget();
        ReleaseRuntimeRenderTexture();
        lastTextureWidth = -1;
        lastTextureHeight = -1;
        lastScreenWidth = -1;
        lastScreenHeight = -1;
    }

    private void OnDestroy()
    {
        Canvas.willRenderCanvases -= HandleWillRenderCanvases;
        StopPreviewSetupCoroutine();
        RestoreOriginalPreviewTarget();
        ReleaseRuntimeRenderTexture();
    }

    public override void Show()
    {
        base.Show();
        QueuePreviewSetup(true);
    }

    private void OnTransformParentChanged()
    {
        if (isActiveAndEnabled)
        {
            QueuePreviewSetup(true);
        }
    }

    private void SetFactor()
    {
        if (silhouetteCoroutine != null)
        {
            StopCoroutine(silhouetteCoroutine);
        }

        silhouetteCoroutine = StartCoroutine(SetSilhouetteFactor());
    }

    private IEnumerator SetSilhouetteFactor()
    {
        if (materialInstance == null)
        {
            yield break;
        }

        float n = 0f;
        while (materialInstance.GetFloat("_SilhouetteFactor") < 0.98f)
        {
            n += 10f * Time.deltaTime;
            materialInstance.SetFloat("_SilhouetteFactor", n);
            yield return null;
        }

        materialInstance.SetFloat("_SilhouetteFactor", 1f);
        silhouetteCoroutine = null;
    }

    private void HandleWillRenderCanvases()
    {
        EnsurePreviewSetup();
    }

    private void QueuePreviewSetup(bool force)
    {
        pendingForcePreviewSetup |= force;

        if (previewSetupCoroutine == null)
        {
            previewSetupCoroutine = StartCoroutine(DelayedPreviewSetup());
        }
    }

    private IEnumerator DelayedPreviewSetup()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();

        bool force = pendingForcePreviewSetup;
        pendingForcePreviewSetup = false;
        EnsurePreviewSetup(force);

        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        EnsurePreviewSetup(force);

        previewSetupCoroutine = null;

        if (pendingForcePreviewSetup)
        {
            QueuePreviewSetup(false);
        }
    }

    private void StopPreviewSetupCoroutine()
    {
        pendingForcePreviewSetup = false;

        if (previewSetupCoroutine != null)
        {
            StopCoroutine(previewSetupCoroutine);
            previewSetupCoroutine = null;
        }
    }

    private void EnsurePreviewSetup(bool force = false)
    {
        if (!TryResolvePreviewCamera())
        {
            return;
        }

        Vector2Int targetSize = GetRenderTextureSize();
        if (targetSize.x <= 0 || targetSize.y <= 0)
        {
            return;
        }

        bool sizeChanged = force
            || targetSize.x != lastTextureWidth
            || targetSize.y != lastTextureHeight
            || Screen.width != lastScreenWidth
            || Screen.height != lastScreenHeight;

        if (!sizeChanged)
        {
            return;
        }

        lastTextureWidth = targetSize.x;
        lastTextureHeight = targetSize.y;
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        RebuildRenderTexture(targetSize.x, targetSize.y);
    }

    private bool TryResolvePreviewCamera()
    {
        if (previewCamera != null)
        {
            return true;
        }

        if (ShootPlayerManager.Instance != null && ShootPlayerManager.Instance.startPlayer != null)

        if (previewCamera == null)
        {
            StartPlayer startPlayer = FindObjectOfType<StartPlayer>();
            if (startPlayer != null)
            {
                previewCamera = startPlayer.camera;
            }
        }

        if (previewCamera == null)
        {
            return false;
        }

        originalTargetTexture = previewCamera.targetTexture;
        if (!hasOriginalCameraState)
        {
            originalClearFlags = previewCamera.clearFlags;
            originalBackgroundColor = previewCamera.backgroundColor;
            hasOriginalCameraState = true;
        }

        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = PreviewClearColor;

        previewCameraData = previewCamera.GetComponent<UniversalAdditionalCameraData>();
        if (previewCameraData != null && !hasOriginalRenderPostProcessing)
        {
            originalRenderPostProcessing = previewCameraData.renderPostProcessing;
            hasOriginalRenderPostProcessing = true;
        }

        if (previewCameraData != null)
        {
            previewCameraData.renderPostProcessing = false;
        }

        return true;
    }

    private Vector2Int GetRenderTextureSize()
    {
        if (rawImageRect == null || rawImage == null)
        {
            return new Vector2Int(0, 0);
        }

        Canvas canvas = rawImage.canvas;
        Rect pixelRect = canvas != null
            ? RectTransformUtility.PixelAdjustRect(rawImageRect, canvas)
            : rawImageRect.rect;

        float rectWidth = pixelRect.width;
        float rectHeight = pixelRect.height;

        if (rectWidth <= 1f || rectHeight <= 1f)
        {
            Rect localRect = rawImageRect.rect;
            rectWidth = localRect.width;
            rectHeight = localRect.height;
        }

        if (rectWidth <= 1f || rectHeight <= 1f)
        {
            rectWidth = Screen.width;
            rectHeight = Screen.height;
        }

        int width = Mathf.Max(MinRenderTextureSize, Mathf.RoundToInt(rectWidth));
        int height = Mathf.Max(MinRenderTextureSize, Mathf.RoundToInt(rectHeight));

        return new Vector2Int(width, height);
    }

    private void RebuildRenderTexture(int width, int height)
    {
        ReleaseRuntimeRenderTexture();

        runtimeRenderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
        {
            name = $"StartPlayerPreview_{width}x{height}",
            useMipMap = false,
            autoGenerateMips = false,
            antiAliasing = PreviewAntiAliasing,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        runtimeRenderTexture.Create();

        previewCamera.targetTexture = runtimeRenderTexture;
        previewCamera.aspect = width / (float)height;
        previewCamera.enabled = true;
        previewCamera.Render();

        if (rawImage != null)
        {
            rawImage.texture = runtimeRenderTexture;
        }
    }

    private void RestoreOriginalPreviewTarget()
    {
        if (previewCamera != null)
        {
            previewCamera.targetTexture = originalTargetTexture;

            if (hasOriginalCameraState)
            {
                previewCamera.clearFlags = originalClearFlags;
                previewCamera.backgroundColor = originalBackgroundColor;
            }
        }

        if (previewCameraData != null && hasOriginalRenderPostProcessing)
        {
            previewCameraData.renderPostProcessing = originalRenderPostProcessing;
        }

        if (rawImage != null)
        {
            rawImage.texture = originalTexture;
        }
    }

    private void ReleaseRuntimeRenderTexture()
    {
        if (runtimeRenderTexture == null)
        {
            return;
        }

        if (previewCamera != null && previewCamera.targetTexture == runtimeRenderTexture)
        {
            previewCamera.targetTexture = null;
        }

        runtimeRenderTexture.Release();
        Destroy(runtimeRenderTexture);
        runtimeRenderTexture = null;
    }

    private void StretchToParent(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}


