using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerPnael : BasePanel
{
    [Header("HP")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Text hpText;

    [Header("Energy")]
    [SerializeField] private Image energyImage;
    [SerializeField] private TMP_Text energyText;
    [SerializeField] private Color energyNotFullColor = new Color(0.55f, 0.55f, 0.55f, 0.8f);
    [SerializeField] private Color energyFullColor = Color.white;
    [SerializeField] private float energyColorLerpSpeed = 6f;

    [Header("Transfiguration Layout")]
    [SerializeField] private float transfigurationOffsetY;
    [SerializeField] private float layoutTweenDuration = 0.2f;
    [SerializeField] private Ease layoutTweenEase = Ease.OutCubic;

    private PlayerContorller player;
    private PlayerCombatStats stats;
    private RectTransform hpSliderRect;
    private RectTransform energyImageRect;
    private Vector2 defaultHpPosition;
    private bool hasCachedDefaultLayout;
    private Color targetEnergyColor;
    private Tweener hpLayoutTweener;
    private bool isTransfigurationLayoutActive;
    private float lastAppliedTransfigurationOffsetY;

    public override void Awake()
    {
        base.Awake();
        CacheReferences();
        targetEnergyColor = energyNotFullColor;
    }

    private void OnDestroy()
    {
        UnbindStats();
        KillLayoutTweens();
    }

    private void Update()
    {
        if (energyImage != null)
        {
            energyImage.color = Color.Lerp(energyImage.color, targetEnergyColor, Time.unscaledDeltaTime * energyColorLerpSpeed);
        }

        if (isTransfigurationLayoutActive && !Mathf.Approximately(lastAppliedTransfigurationOffsetY, transfigurationOffsetY))
        {
            ApplyCurrentLayout();
        }
    }

    public void Bind(PlayerContorller player, PlayerCombatStats stats)
    {
        this.player = player;

        if (this.stats == stats)
        {
            RefreshAll();
            return;
        }

        UnbindStats();
        this.stats = stats;
        if (this.stats != null)
        {
            this.stats.OnHpChanged += RefreshHp;
            this.stats.OnEnergyChanged += RefreshEnergy;
        }

        RefreshAll();
    }

    private void CacheReferences()
    {
        if (hpSlider == null)
        {
            Transform hpSliderTransform = transform.Find("HPSlider");
            hpSlider = hpSliderTransform != null ? hpSliderTransform.GetComponent<Slider>() : GetComponentInChildren<Slider>(true);
        }

        if (hpSlider != null && hpSliderRect == null)
        {
            hpSliderRect = hpSlider.transform as RectTransform;
        }

        if (hpText == null)
        {
            Transform hpTextTransform = transform.Find("HPSlider/HPText (Legacy)");
            hpText = hpTextTransform != null ? hpTextTransform.GetComponent<Text>() : GetComponentInChildren<Text>(true);
        }

        if (energyImage == null)
        {
            Transform energyImageTransform = transform.Find("EnergyImage");
            energyImage = energyImageTransform != null ? energyImageTransform.GetComponent<Image>() : null;
        }

        if (energyImage != null && energyImageRect == null)
        {
            energyImageRect = energyImage.transform as RectTransform;
        }

        if (energyText == null)
        {
            Transform energyTextTransform = transform.Find("EnergyImage/EnergyText (TMP)");
            energyText = energyTextTransform != null ? energyTextTransform.GetComponent<TMP_Text>() : GetComponentInChildren<TMP_Text>(true);
        }

        CacheDefaultLayout();
    }

    private void CacheDefaultLayout()
    {
        if (hasCachedDefaultLayout || hpSliderRect == null)
        {
            return;
        }

        defaultHpPosition = hpSliderRect.anchoredPosition;
        hasCachedDefaultLayout = true;
    }

    public void SetTransfigurationLayout(bool isTransfiguration)
    {
        CacheReferences();
        if (!hasCachedDefaultLayout)
        {
            return;
        }

        isTransfigurationLayoutActive = isTransfiguration;
        ApplyCurrentLayout();
    }

    public static void SetSceneTransfigurationLayout(bool isTransfiguration)
    {
        PlayerPnael panel = FindObjectOfType<PlayerPnael>(true);
        panel?.SetTransfigurationLayout(isTransfiguration);
    }

    private void ApplyCurrentLayout()
    {
        Vector2 targetHpPosition = defaultHpPosition;
        if (isTransfigurationLayoutActive)
        {
            targetHpPosition.y += transfigurationOffsetY;
        }

        lastAppliedTransfigurationOffsetY = transfigurationOffsetY;
        TweenLayout(targetHpPosition);
    }

    private void RefreshAll()
    {
        CacheReferences();
        if (stats == null)
        {
            return;
        }

        RefreshHp(stats.CurrentHp, stats.MaxHp);
        RefreshEnergy(stats.CurrentEnergy, stats.MaxEnergy);
    }

    private void RefreshHp(float currentHp, float maxHp)
    {
        float safeMaxHp = Mathf.Max(1f, maxHp);
        float normalized = Mathf.Clamp01(currentHp / safeMaxHp);
        if (hpSlider != null)
        {
            hpSlider.minValue = 0f;
            hpSlider.maxValue = 1f;
            hpSlider.value = normalized;
        }

        if (hpText != null)
        {
            hpText.text = $"HP:{Mathf.CeilToInt(currentHp)}/{Mathf.CeilToInt(safeMaxHp)}";
        }
    }

    private void RefreshEnergy(float currentEnergy, float maxEnergy)
    {
        float safeMaxEnergy = Mathf.Max(1f, maxEnergy);
        float normalized = Mathf.Clamp01(currentEnergy / safeMaxEnergy);
        if (energyText != null)
        {
            energyText.text = $"{normalized * 100f:0.0}%";
        }

        targetEnergyColor = currentEnergy >= safeMaxEnergy - 0.001f ? energyFullColor : energyNotFullColor;
    }

    private void TweenLayout(Vector2 targetHpPosition)
    {
        KillLayoutTweens();

        if (hpSliderRect != null)
        {
            hpLayoutTweener = hpSliderRect.DOAnchorPos(targetHpPosition, layoutTweenDuration)
                .SetEase(layoutTweenEase)
                .SetUpdate(true);
        }
    }

    private void KillLayoutTweens()
    {
        if (hpLayoutTweener != null)
        {
            hpLayoutTweener.Kill();
            hpLayoutTweener = null;
        }

    }

    private void UnbindStats()
    {
        if (stats == null)
        {
            return;
        }

        stats.OnHpChanged -= RefreshHp;
        stats.OnEnergyChanged -= RefreshEnergy;
        stats = null;
    }
}


