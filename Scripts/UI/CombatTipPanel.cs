using UnityEngine;
using UnityEngine.UI;

public enum CombatTipType
{
    PerfectDodgeReady,
    PerfectDodgeCooldown,
    PerfectDodgeUnavailableInTransform
}

public class CombatTipPanel : BasePanel
{
    [Header("Follow")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.9f, 0f);
    [SerializeField] private Vector2 screenOffset = new Vector2(110f, 45f);

    [Header("Visual")]
    [SerializeField] private Image slideTip;
    [SerializeField] private Color readyColor = Color.white;
    [SerializeField] private Color cooldownColor = new Color(0.65f, 0.65f, 0.65f, 0.85f);
    [SerializeField] private Color unavailableColor = new Color(0.75f, 0.45f, 1f, 0.85f);
    [SerializeField] private float autoHideDelay = 0.8f;

    [Header("Sound")]
    [SerializeField] private string soundABName = "sound";
    [SerializeField] private string slideTipSoundName = "SlideTip";

    private Transform followTarget;
    private RectTransform rectTransform;
    private RectTransform canvasRectTransform;
    private CombatTipType currentTipType;
    private float autoHideTime;
    private static int tipRequestVersion;

    public override void Awake()
    {
        base.Awake();
        rectTransform = transform as RectTransform;
        if (slideTip == null)
        {
            Transform slideTipTransform = transform.Find("SlideTip");
            slideTip = slideTipTransform != null ? slideTipTransform.GetComponent<Image>() : GetComponentInChildren<Image>(true);
        }
    }

    private void Update()
    {
        UpdateFollowPosition();
        TickAutoHide();
    }

    public void ShowSlideTip(Transform target, CombatTipType tipType)
    {
        followTarget = target;
        currentTipType = tipType;
        autoHideTime = autoHideDelay > 0f ? Time.unscaledTime + autoHideDelay : 0f;
        ApplyTipVisual(tipType);
        PlaySlideTipSound();

        Show();
        UpdateFollowPosition();
    }

    public void HideSlideTip()
    {
        followTarget = null;
        autoHideTime = 0f;
        Hide();
    }

    private void TickAutoHide()
    {
        if (!isShow || autoHideTime <= 0f || Time.unscaledTime < autoHideTime)
        {
            return;
        }

        HideSlideTip();
    }

    private void ApplyTipVisual(CombatTipType tipType)
    {
        if (slideTip == null)
        {
            return;
        }

        switch (tipType)
        {
            case CombatTipType.PerfectDodgeCooldown:
                slideTip.color = cooldownColor;
                break;
            case CombatTipType.PerfectDodgeUnavailableInTransform:
                slideTip.color = unavailableColor;
                break;
            default:
                slideTip.color = readyColor;
                break;
        }
    }

    private void UpdateFollowPosition()
    {
        if (!isShow || followTarget == null || rectTransform == null)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        Vector3 screenPosition = mainCamera.WorldToScreenPoint(followTarget.position + worldOffset);
        if (screenPosition.z <= 0f)
        {
            return;
        }

        RectTransform parentRect = GetCanvasRectTransform();
        if (parentRect == null)
        {
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPosition, null, out Vector2 localPoint))
        {
            rectTransform.anchoredPosition = localPoint + screenOffset;
        }
    }

    private RectTransform GetCanvasRectTransform()
    {
        if (canvasRectTransform != null)
        {
            return canvasRectTransform;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        canvasRectTransform = canvas != null ? canvas.transform as RectTransform : transform.parent as RectTransform;
        return canvasRectTransform;
    }

    private void PlaySlideTipSound()
    {
        if (string.IsNullOrEmpty(slideTipSoundName) || MusicManager.Instance == null)
        {
            return;
        }

        MusicManager.Instance.PlaySoundForAB(slideTipSoundName, soundABName);
    }

    public static void ShowForPlayer(PlayerContorller player, CombatTipType tipType, UILayer layer = UILayer.Top)
    {
        if (player == null || UIManager.Instance == null)
        {
            return;
        }

        int requestVersion = ++tipRequestVersion;
        UIManager.Instance.OpenPanelAsync<CombatTipPanel>(layer, panel =>
        {
            if (requestVersion != tipRequestVersion)
            {
                return;
            }

            panel?.ShowSlideTip(player.transform, tipType);
        });
    }

    public static void HideOpened()
    {
        if (UIManager.Instance == null)
        {
            return;
        }

        tipRequestVersion++;
        UIManager.Instance.ClosePanel<CombatTipPanel>();
    }
}


