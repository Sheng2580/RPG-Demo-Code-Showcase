using System.Collections;
using SCPE;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls short combat/action post process pulses on the current scene volume.
/// Scene-specific default values are captured as the baseline and restored after each effect.
/// </summary>
public class ActionPostProcessManager : UnitySingleTonMono<ActionPostProcessManager>
{
    [Header("Volume")]
    [SerializeField] private string globalVolumeName = "GlobalVolume";
    [SerializeField] private string fallbackGlobalVolumeName = "Global Volume";
    [SerializeField] private bool addComponentIfMissing = true;
    [SerializeField] private bool logVolumeBinding = true;

    [Header("Transfiguration")]
    [SerializeField] private float transfigurationFadeInTime = 0.12f;
    [SerializeField] private float transfigurationPostExposure = -1.15f;
    [SerializeField] private float transfigurationBloomIntensity = 4f;
    [SerializeField] private float transfigurationBlackBarsSize = 0.5f;
    [SerializeField] private float transfigurationContrast = 1.18f;
    [SerializeField] private float transfigurationDarken = 0.2f;
    [SerializeField] private float transfigurationShadowCrush = 0.08f;
    [SerializeField] private float transfigurationHighlightBoost = 0.18f;
    [SerializeField] private float transfigurationSaturation = 1.2f;
    [SerializeField] private float transfigurationShadowDesat = 0.1f;
    [SerializeField] private float transfigurationVibrance = 0.35f;
    [SerializeField] private float transfigurationGlowBoost = 0.85f;
    [SerializeField] private float transfigurationVignette = 0f;
    [SerializeField] private float transfigurationSharpen = 0.22f;
    [SerializeField] private float transfigurationColorTemp = -0.18f;
    [SerializeField] private Color transfigurationShadowTint = new Color(0.42f, 0.52f, 0.78f, 1f);
    [SerializeField] private Color transfigurationHighlightTint = new Color(0.72f, 0.96f, 1f, 1f);

    [Header("Rush")]
    [SerializeField] private float rushFadeInTime = 0.06f;
    [SerializeField] private float rushHoldTime = 0.16f;
    [SerializeField] private float rushFadeOutTime = 0.22f;
    [SerializeField] private float rushContrast = 1.22f;
    [SerializeField] private float rushSaturation = 1.22f;
    [SerializeField] private float rushGlowBoost = 0.26f;
    [SerializeField] private float rushVignette = 0.12f;
    [SerializeField] private float rushSharpen = 0.18f;

    [Header("Perfect Dodge")]
    [SerializeField] private float perfectDodgeRadialAmount = 0.1f;
    [SerializeField] private float perfectDodgeRadialFadeInTime = 0.06f;
    [SerializeField] private float perfectDodgeAutoRestoreTime = 1f;
    [SerializeField] private float perfectDodgeRestoreTime = 0.16f;
    [SerializeField, Range(0.01f, 1f)] private float perfectDodgeTimeScale = 0.22f;
    [SerializeField] private float perfectDodgeTimeFadeInTime = 0.03f;
    [SerializeField] private float perfectDodgePostExposure = -0.32f;
    [SerializeField] private float perfectDodgeBloomIntensity = 1.35f;
    [SerializeField] private float perfectDodgeBlackBarsSize = 0.16f;
    [SerializeField] private float perfectDodgeContrast = 1.08f;
    [SerializeField] private float perfectDodgeDarken = 0.08f;
    [SerializeField] private float perfectDodgeHighlightBoost = 0.08f;
    [SerializeField] private float perfectDodgeSaturation = 1.08f;
    [SerializeField] private float perfectDodgeVibrance = 0.12f;
    [SerializeField] private float perfectDodgeGlowBoost = 0.22f;
    [SerializeField] private float perfectDodgeVignette = 0.08f;
    [SerializeField] private float perfectDodgeSharpen = 0.08f;

    private Volume globalVolume;
    private ZzzPostProcessVolume actionVolume;
    private ColorAdjustments colorAdjustments;
    private Bloom bloom;
    private BlackBars blackBars;
    private RadialBlur radialBlur;
    private ActionPostProcessSnapshot baseline;
    private RadialBlurSnapshot radialBlurBaseline;
    private Coroutine effectCoroutine;
    private Coroutine perfectDodgeRadialCoroutine;
    private Coroutine perfectDodgeTimeCoroutine;
    private Coroutine perfectDodgeAutoRestoreCoroutine;
    private bool isTransfigurationEffectActive;
    private float defaultFixedDeltaTime;

    public override void Awake()
    {
        base.Awake();
        defaultFixedDeltaTime = Time.fixedDeltaTime;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        BindSceneVolume();
    }

    protected override void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        base.OnDestroy();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindSceneVolume();
    }

    public void PlayTransfigurationEffect()
    {
        isTransfigurationEffectActive = true;
        var target = CreateTransfigurationTarget();
        PlaySustain(target, transfigurationFadeInTime);
    }

    public void PlayRushEffect()
    {
        var target = CreateRushTarget();
        var restoreTarget = isTransfigurationEffectActive ? CreateTransfigurationTarget() : baseline;
        PlayPulse(target, restoreTarget, rushFadeInTime, rushHoldTime, rushFadeOutTime);
    }

    public void PlayPerfectDodgeEffect(PlayerContorller player = null)
    {
        if (!EnsureVolume())
        {
            return;
        }

        player?.UseUnscaledAnimatorFor(Mathf.Max(0.05f, perfectDodgeAutoRestoreTime));

        StopPerfectDodgeCoroutines(false);

        PlaySustain(CreatePerfectDodgeTarget(), perfectDodgeRadialFadeInTime);
        perfectDodgeRadialCoroutine = StartCoroutine(BlendRadialBlurAmountTo(perfectDodgeRadialAmount, perfectDodgeRadialFadeInTime));
        perfectDodgeTimeCoroutine = StartCoroutine(BlendTimeScale(Time.timeScale, perfectDodgeTimeScale, perfectDodgeTimeFadeInTime));
        perfectDodgeAutoRestoreCoroutine = StartCoroutine(PerfectDodgeAutoRestoreRoutine());
    }

    public void RestorePerfectDodgeEffect()
    {
        if (!EnsureVolume())
        {
            RestoreTimeScale();
            return;
        }

        StopPerfectDodgeCoroutines(false);
        ActionPostProcessSnapshot restoreTarget = isTransfigurationEffectActive ? CreateTransfigurationTarget() : baseline;

        if (effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
        }

        effectCoroutine = StartCoroutine(BlendToSnapshot(restoreTarget, perfectDodgeRestoreTime));
        perfectDodgeRadialCoroutine = StartCoroutine(RestoreRadialBlurRoutine(perfectDodgeRestoreTime));
        perfectDodgeTimeCoroutine = StartCoroutine(RestoreTimeScaleRoutine(perfectDodgeRestoreTime));
    }

    public void Restore(float duration)
    {
        if (!EnsureVolume())
        {
            return;
        }

        if (effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
        }

        isTransfigurationEffectActive = false;
        effectCoroutine = StartCoroutine(BlendToBaseline(duration));
    }

    private void PlayPulse(
        ActionPostProcessSnapshot target,
        ActionPostProcessSnapshot restoreTarget,
        float fadeInTime,
        float holdTime,
        float fadeOutTime)
    {
        if (!EnsureVolume())
        {
            return;
        }

        if (effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
        }

        effectCoroutine = StartCoroutine(PulseRoutine(target, restoreTarget, fadeInTime, holdTime, fadeOutTime));
    }

    private void PlaySustain(ActionPostProcessSnapshot target, float fadeInTime)
    {
        if (!EnsureVolume())
        {
            return;
        }

        if (effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
        }

        effectCoroutine = StartCoroutine(SustainRoutine(target, fadeInTime));
    }

    private IEnumerator PulseRoutine(
        ActionPostProcessSnapshot target,
        ActionPostProcessSnapshot restoreTarget,
        float fadeInTime,
        float holdTime,
        float fadeOutTime)
    {
        actionVolume.active = true;
        if (colorAdjustments != null)
        {
            colorAdjustments.active = true;
            colorAdjustments.postExposure.overrideState = true;
        }
        if (bloom != null)
        {
            bloom.active = true;
            bloom.intensity.overrideState = true;
        }
        if (blackBars != null)
        {
            blackBars.active = true;
            blackBars.size.overrideState = true;
        }
        SetOverrideState(true);

        yield return BlendRoutine(ActionPostProcessSnapshot.Capture(actionVolume, colorAdjustments, bloom, blackBars), target, fadeInTime);

        if (holdTime > 0f)
        {
            yield return new WaitForSecondsRealtime(holdTime);
        }

        yield return BlendRoutine(ActionPostProcessSnapshot.Capture(actionVolume, colorAdjustments, bloom, blackBars), restoreTarget, fadeOutTime);
        effectCoroutine = null;
    }

    private IEnumerator SustainRoutine(ActionPostProcessSnapshot target, float fadeInTime)
    {
        actionVolume.active = true;
        if (colorAdjustments != null)
        {
            colorAdjustments.active = true;
            colorAdjustments.postExposure.overrideState = true;
        }
        if (bloom != null)
        {
            bloom.active = true;
            bloom.intensity.overrideState = true;
        }
        if (blackBars != null)
        {
            blackBars.active = true;
            blackBars.size.overrideState = true;
        }
        SetOverrideState(true);

        yield return BlendRoutine(ActionPostProcessSnapshot.Capture(actionVolume, colorAdjustments, bloom, blackBars), target, fadeInTime);
        effectCoroutine = null;
    }

    private IEnumerator BlendToBaseline(float duration)
    {
        if (actionVolume == null)
        {
            yield break;
        }

        yield return BlendRoutine(ActionPostProcessSnapshot.Capture(actionVolume, colorAdjustments, bloom, blackBars), baseline, duration);
        baseline.ApplyActiveAndOverride(actionVolume, colorAdjustments, bloom, blackBars);
        effectCoroutine = null;
    }

    private IEnumerator BlendToSnapshot(ActionPostProcessSnapshot target, float duration)
    {
        if (actionVolume == null)
        {
            yield break;
        }

        yield return BlendRoutine(ActionPostProcessSnapshot.Capture(actionVolume, colorAdjustments, bloom, blackBars), target, duration);
        effectCoroutine = null;
    }

    private IEnumerator BlendRoutine(
        ActionPostProcessSnapshot from,
        ActionPostProcessSnapshot to,
        float duration)
    {
        if (duration <= 0f)
        {
            to.ApplyValues(actionVolume, colorAdjustments, bloom, blackBars);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            ActionPostProcessSnapshot.Lerp(from, to, eased).ApplyValues(actionVolume, colorAdjustments, bloom, blackBars);
            yield return null;
        }

        to.ApplyValues(actionVolume, colorAdjustments, bloom, blackBars);
    }

    private bool EnsureVolume()
    {
        if (actionVolume != null)
        {
            return true;
        }

        BindSceneVolume();
        return actionVolume != null;
    }

    private void BindSceneVolume()
    {
        globalVolume = FindGlobalVolume();
        actionVolume = null;
        colorAdjustments = null;
        bloom = null;
        blackBars = null;
        radialBlur = null;

        if (globalVolume == null || globalVolume.profile == null)
        {
            Debug.LogWarning("[ActionPostProcessManager] Global Volume or VolumeProfile not found.");
            return;
        }

        if (logVolumeBinding)
        {
            Debug.Log($"[ActionPostProcessManager] Bind Volume: {globalVolume.name}, profile={globalVolume.profile.name}");
        }

        if (!globalVolume.profile.TryGet(out actionVolume) && addComponentIfMissing)
        {
            actionVolume = globalVolume.profile.Add<ZzzPostProcessVolume>(false);
        }

        if (actionVolume == null)
        {
            Debug.LogWarning("[ActionPostProcessManager] ZzzPostProcessVolume not found on current scene volume.");
            return;
        }

        if (!globalVolume.profile.TryGet(out colorAdjustments) && addComponentIfMissing)
        {
            colorAdjustments = globalVolume.profile.Add<ColorAdjustments>(false);
        }

        if (!globalVolume.profile.TryGet(out bloom) && addComponentIfMissing)
        {
            bloom = globalVolume.profile.Add<Bloom>(false);
        }

        if (!globalVolume.profile.TryGet(out blackBars) && addComponentIfMissing)
        {
            blackBars = globalVolume.profile.Add<BlackBars>(false);
        }

        if (!globalVolume.profile.TryGet(out radialBlur) && addComponentIfMissing)
        {
            radialBlur = globalVolume.profile.Add<RadialBlur>(false);
        }

        baseline = ActionPostProcessSnapshot.Capture(actionVolume, colorAdjustments, bloom, blackBars);
        radialBlurBaseline = RadialBlurSnapshot.Capture(radialBlur);
    }

    private Volume FindGlobalVolume()
    {
        GameObject go = GameObject.Find(globalVolumeName);
        if (go != null && go.TryGetComponent(out Volume volume))
        {
            return volume;
        }

        go = GameObject.Find(fallbackGlobalVolumeName);
        if (go != null && go.TryGetComponent(out volume))
        {
            return volume;
        }

        Volume[] volumes = FindObjectsOfType<Volume>(true);
        Volume fallback = null;
        for (int i = 0; i < volumes.Length; i++)
        {
            if (volumes[i] == null)
            {
                continue;
            }

            if (volumes[i].isGlobal && volumes[i].enabled && volumes[i].gameObject.activeInHierarchy)
            {
                return volumes[i];
            }

            fallback ??= volumes[i];
        }

        return fallback;
    }

    private ActionPostProcessSnapshot CreateTransfigurationTarget()
    {
        var target = baseline;
        target.active = true;
        target.colorAdjustmentsActive = true;
        target.bloomActive = true;
        target.blackBarsActive = true;
        target.postExposure = transfigurationPostExposure;
        target.bloomIntensity = Mathf.Max(baseline.bloomIntensity, transfigurationBloomIntensity);
        target.blackBarsSize = transfigurationBlackBarsSize;
        target.contrast = transfigurationContrast;
        target.darken = transfigurationDarken;
        target.shadowCrush = transfigurationShadowCrush;
        target.highlightBoost = transfigurationHighlightBoost;
        target.saturation = transfigurationSaturation;
        target.shadowDesat = transfigurationShadowDesat;
        target.vibrance = transfigurationVibrance;
        target.glowBoost = transfigurationGlowBoost;
        target.vignetteIntensity = transfigurationVignette;
        target.sharpenStrength = transfigurationSharpen;
        target.colorTemp = transfigurationColorTemp;
        target.shadowTint = transfigurationShadowTint;
        target.highlightTint = transfigurationHighlightTint;
        return target;
    }

    private ActionPostProcessSnapshot CreatePerfectDodgeTarget()
    {
        var target = isTransfigurationEffectActive ? CreateTransfigurationTarget() : baseline;
        target.active = true;
        target.colorAdjustmentsActive = true;
        target.bloomActive = true;
        target.blackBarsActive = true;
        target.postExposure = perfectDodgePostExposure;
        target.bloomIntensity = Mathf.Max(baseline.bloomIntensity, perfectDodgeBloomIntensity);
        target.blackBarsSize = perfectDodgeBlackBarsSize;
        target.contrast = perfectDodgeContrast;
        target.darken = perfectDodgeDarken;
        target.highlightBoost = perfectDodgeHighlightBoost;
        target.saturation = perfectDodgeSaturation;
        target.vibrance = perfectDodgeVibrance;
        target.glowBoost = perfectDodgeGlowBoost;
        target.vignetteIntensity = perfectDodgeVignette;
        target.sharpenStrength = perfectDodgeSharpen;
        target.shadowTint = transfigurationShadowTint;
        target.highlightTint = transfigurationHighlightTint;
        return target;
    }

    private IEnumerator PerfectDodgeAutoRestoreRoutine()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, perfectDodgeAutoRestoreTime));
        perfectDodgeAutoRestoreCoroutine = null;
        RestorePerfectDodgeEffect();
    }

    private IEnumerator BlendRadialBlurAmountTo(float targetAmount, float duration)
    {
        if (radialBlur == null)
        {
            yield break;
        }

        radialBlur.active = true;
        radialBlur.amount.overrideState = true;
        yield return BlendRadialBlurAmount(radialBlur.amount.value, targetAmount, duration);
        perfectDodgeRadialCoroutine = null;
    }

    private IEnumerator RestoreRadialBlurRoutine(float duration)
    {
        yield return BlendRadialBlurAmount(radialBlur != null ? radialBlur.amount.value : 0f, radialBlurBaseline.amount, duration);
        radialBlurBaseline.Apply(radialBlur);
        perfectDodgeRadialCoroutine = null;
    }

    private IEnumerator BlendRadialBlurAmount(float from, float to, float duration)
    {
        if (radialBlur == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            radialBlur.amount.value = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            radialBlur.amount.value = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        radialBlur.amount.value = to;
    }

    private IEnumerator RestoreTimeScaleRoutine(float duration)
    {
        yield return BlendTimeScale(Time.timeScale, 1f, duration);
        RestoreTimeScale();
        perfectDodgeTimeCoroutine = null;
    }

    private IEnumerator BlendTimeScale(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetTimeScale(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetTimeScale(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t)));
            yield return null;
        }

        SetTimeScale(to);
    }

    private void SetTimeScale(float value)
    {
        Time.timeScale = Mathf.Clamp(value, 0.01f, 1f);
        Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;
    }

    private void RestoreTimeScale()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
    }

    private void StopPerfectDodgeCoroutines(bool restoreTimeScale)
    {
        if (perfectDodgeRadialCoroutine != null)
        {
            StopCoroutine(perfectDodgeRadialCoroutine);
            perfectDodgeRadialCoroutine = null;
        }

        if (perfectDodgeTimeCoroutine != null)
        {
            StopCoroutine(perfectDodgeTimeCoroutine);
            perfectDodgeTimeCoroutine = null;
        }

        if (perfectDodgeAutoRestoreCoroutine != null)
        {
            StopCoroutine(perfectDodgeAutoRestoreCoroutine);
            perfectDodgeAutoRestoreCoroutine = null;
        }

        if (restoreTimeScale)
        {
            RestoreTimeScale();
        }
    }

    private ActionPostProcessSnapshot CreateRushTarget()
    {
        var target = isTransfigurationEffectActive ? CreateTransfigurationTarget() : baseline;
        target.active = true;
        target.contrast = rushContrast;
        target.saturation = rushSaturation;
        target.glowBoost = rushGlowBoost;
        target.vignetteIntensity = rushVignette;
        target.sharpenStrength = rushSharpen;
        return target;
    }

    private void SetOverrideState(bool value)
    {
        actionVolume.contrast.overrideState = value;
        actionVolume.darken.overrideState = value;
        actionVolume.shadowCrush.overrideState = value;
        actionVolume.highlightBoost.overrideState = value;
        actionVolume.saturation.overrideState = value;
        actionVolume.shadowDesat.overrideState = value;
        actionVolume.vibrance.overrideState = value;
        actionVolume.shadowTint.overrideState = value;
        actionVolume.midToneShift.overrideState = value;
        actionVolume.highlightTint.overrideState = value;
        actionVolume.colorTemp.overrideState = value;
        actionVolume.sharpenStrength.overrideState = value;
        actionVolume.sharpenEdge.overrideState = value;
        actionVolume.glowBoost.overrideState = value;
        actionVolume.glowThreshold.overrideState = value;
        actionVolume.vignetteIntensity.overrideState = value;
        actionVolume.vignetteSmoothness.overrideState = value;
        actionVolume.vignetteRoundness.overrideState = value;
        actionVolume.bandSteps.overrideState = value;
        actionVolume.bandingBlend.overrideState = value;

        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.overrideState = value;
        }

        if (bloom != null)
        {
            bloom.intensity.overrideState = value;
        }

        if (blackBars != null)
        {
            blackBars.size.overrideState = value;
        }
    }

    private struct RadialBlurSnapshot
    {
        public bool active;
        public bool amountOverride;
        public float amount;

        public static RadialBlurSnapshot Capture(RadialBlur radialBlur)
        {
            if (radialBlur == null)
            {
                return default;
            }

            return new RadialBlurSnapshot
            {
                active = radialBlur.active,
                amountOverride = radialBlur.amount.overrideState,
                amount = radialBlur.amount.value
            };
        }

        public void Apply(RadialBlur radialBlur)
        {
            if (radialBlur == null)
            {
                return;
            }

            radialBlur.active = active;
            radialBlur.amount.overrideState = amountOverride;
            radialBlur.amount.value = amount;
        }
    }

    private struct ActionPostProcessSnapshot
    {
        public bool active;
        public bool colorAdjustmentsActive;
        public bool bloomActive;
        public bool blackBarsActive;
        public bool contrastOverride;
        public bool postExposureOverride;
        public bool bloomIntensityOverride;
        public bool blackBarsSizeOverride;
        public bool darkenOverride;
        public bool shadowCrushOverride;
        public bool highlightBoostOverride;
        public bool saturationOverride;
        public bool shadowDesatOverride;
        public bool vibranceOverride;
        public bool shadowTintOverride;
        public bool midToneShiftOverride;
        public bool highlightTintOverride;
        public bool colorTempOverride;
        public bool sharpenStrengthOverride;
        public bool sharpenEdgeOverride;
        public bool glowBoostOverride;
        public bool glowThresholdOverride;
        public bool vignetteIntensityOverride;
        public bool vignetteSmoothnessOverride;
        public bool vignetteRoundnessOverride;
        public bool bandStepsOverride;
        public bool bandingBlendOverride;

        public float contrast;
        public float postExposure;
        public float bloomIntensity;
        public float blackBarsSize;
        public float darken;
        public float shadowCrush;
        public float highlightBoost;
        public float saturation;
        public float shadowDesat;
        public float vibrance;
        public Color shadowTint;
        public Color midToneShift;
        public Color highlightTint;
        public float colorTemp;
        public float sharpenStrength;
        public float sharpenEdge;
        public float glowBoost;
        public float glowThreshold;
        public float vignetteIntensity;
        public float vignetteSmoothness;
        public float vignetteRoundness;
        public int bandSteps;
        public float bandingBlend;

        public static ActionPostProcessSnapshot Capture(
            ZzzPostProcessVolume volume,
            ColorAdjustments colorAdjustments,
            Bloom bloom,
            BlackBars blackBars)
        {
            return new ActionPostProcessSnapshot
            {
                active = volume.active,
                colorAdjustmentsActive = colorAdjustments != null && colorAdjustments.active,
                bloomActive = bloom != null && bloom.active,
                blackBarsActive = blackBars != null && blackBars.active,
                contrastOverride = volume.contrast.overrideState,
                postExposureOverride = colorAdjustments != null && colorAdjustments.postExposure.overrideState,
                bloomIntensityOverride = bloom != null && bloom.intensity.overrideState,
                blackBarsSizeOverride = blackBars != null && blackBars.size.overrideState,
                darkenOverride = volume.darken.overrideState,
                shadowCrushOverride = volume.shadowCrush.overrideState,
                highlightBoostOverride = volume.highlightBoost.overrideState,
                saturationOverride = volume.saturation.overrideState,
                shadowDesatOverride = volume.shadowDesat.overrideState,
                vibranceOverride = volume.vibrance.overrideState,
                shadowTintOverride = volume.shadowTint.overrideState,
                midToneShiftOverride = volume.midToneShift.overrideState,
                highlightTintOverride = volume.highlightTint.overrideState,
                colorTempOverride = volume.colorTemp.overrideState,
                sharpenStrengthOverride = volume.sharpenStrength.overrideState,
                sharpenEdgeOverride = volume.sharpenEdge.overrideState,
                glowBoostOverride = volume.glowBoost.overrideState,
                glowThresholdOverride = volume.glowThreshold.overrideState,
                vignetteIntensityOverride = volume.vignetteIntensity.overrideState,
                vignetteSmoothnessOverride = volume.vignetteSmoothness.overrideState,
                vignetteRoundnessOverride = volume.vignetteRoundness.overrideState,
                bandStepsOverride = volume.bandSteps.overrideState,
                bandingBlendOverride = volume.bandingBlend.overrideState,
                contrast = volume.contrast.value,
                postExposure = colorAdjustments != null ? colorAdjustments.postExposure.value : 0f,
                bloomIntensity = bloom != null ? bloom.intensity.value : 0f,
                blackBarsSize = blackBars != null ? blackBars.size.value : 0f,
                darken = volume.darken.value,
                shadowCrush = volume.shadowCrush.value,
                highlightBoost = volume.highlightBoost.value,
                saturation = volume.saturation.value,
                shadowDesat = volume.shadowDesat.value,
                vibrance = volume.vibrance.value,
                shadowTint = volume.shadowTint.value,
                midToneShift = volume.midToneShift.value,
                highlightTint = volume.highlightTint.value,
                colorTemp = volume.colorTemp.value,
                sharpenStrength = volume.sharpenStrength.value,
                sharpenEdge = volume.sharpenEdge.value,
                glowBoost = volume.glowBoost.value,
                glowThreshold = volume.glowThreshold.value,
                vignetteIntensity = volume.vignetteIntensity.value,
                vignetteSmoothness = volume.vignetteSmoothness.value,
                vignetteRoundness = volume.vignetteRoundness.value,
                bandSteps = volume.bandSteps.value,
                bandingBlend = volume.bandingBlend.value
            };
        }

        public static ActionPostProcessSnapshot Lerp(
            ActionPostProcessSnapshot from,
            ActionPostProcessSnapshot to,
            float t)
        {
            return new ActionPostProcessSnapshot
            {
                active = true,
                colorAdjustmentsActive = true,
                bloomActive = true,
                blackBarsActive = true,
                contrast = Mathf.Lerp(from.contrast, to.contrast, t),
                postExposure = Mathf.Lerp(from.postExposure, to.postExposure, t),
                bloomIntensity = Mathf.Lerp(from.bloomIntensity, to.bloomIntensity, t),
                blackBarsSize = Mathf.Lerp(from.blackBarsSize, to.blackBarsSize, t),
                darken = Mathf.Lerp(from.darken, to.darken, t),
                shadowCrush = Mathf.Lerp(from.shadowCrush, to.shadowCrush, t),
                highlightBoost = Mathf.Lerp(from.highlightBoost, to.highlightBoost, t),
                saturation = Mathf.Lerp(from.saturation, to.saturation, t),
                shadowDesat = Mathf.Lerp(from.shadowDesat, to.shadowDesat, t),
                vibrance = Mathf.Lerp(from.vibrance, to.vibrance, t),
                shadowTint = Color.Lerp(from.shadowTint, to.shadowTint, t),
                midToneShift = Color.Lerp(from.midToneShift, to.midToneShift, t),
                highlightTint = Color.Lerp(from.highlightTint, to.highlightTint, t),
                colorTemp = Mathf.Lerp(from.colorTemp, to.colorTemp, t),
                sharpenStrength = Mathf.Lerp(from.sharpenStrength, to.sharpenStrength, t),
                sharpenEdge = Mathf.Lerp(from.sharpenEdge, to.sharpenEdge, t),
                glowBoost = Mathf.Lerp(from.glowBoost, to.glowBoost, t),
                glowThreshold = Mathf.Lerp(from.glowThreshold, to.glowThreshold, t),
                vignetteIntensity = Mathf.Lerp(from.vignetteIntensity, to.vignetteIntensity, t),
                vignetteSmoothness = Mathf.Lerp(from.vignetteSmoothness, to.vignetteSmoothness, t),
                vignetteRoundness = Mathf.Lerp(from.vignetteRoundness, to.vignetteRoundness, t),
                bandSteps = Mathf.RoundToInt(Mathf.Lerp(from.bandSteps, to.bandSteps, t)),
                bandingBlend = Mathf.Lerp(from.bandingBlend, to.bandingBlend, t)
            };
        }

        public void ApplyValues(
            ZzzPostProcessVolume volume,
            ColorAdjustments colorAdjustments,
            Bloom bloom,
            BlackBars blackBars)
        {
            volume.active = active;
            volume.contrast.value = contrast;
            volume.darken.value = darken;
            volume.shadowCrush.value = shadowCrush;
            volume.highlightBoost.value = highlightBoost;
            volume.saturation.value = saturation;
            volume.shadowDesat.value = shadowDesat;
            volume.vibrance.value = vibrance;
            volume.shadowTint.value = shadowTint;
            volume.midToneShift.value = midToneShift;
            volume.highlightTint.value = highlightTint;
            volume.colorTemp.value = colorTemp;
            volume.sharpenStrength.value = sharpenStrength;
            volume.sharpenEdge.value = sharpenEdge;
            volume.glowBoost.value = glowBoost;
            volume.glowThreshold.value = glowThreshold;
            volume.vignetteIntensity.value = vignetteIntensity;
            volume.vignetteSmoothness.value = vignetteSmoothness;
            volume.vignetteRoundness.value = vignetteRoundness;
            volume.bandSteps.value = bandSteps;
            volume.bandingBlend.value = bandingBlend;

            if (colorAdjustments != null)
            {
                colorAdjustments.active = colorAdjustmentsActive;
                colorAdjustments.postExposure.value = postExposure;
            }

            if (bloom != null)
            {
                bloom.active = bloomActive;
                bloom.intensity.value = bloomIntensity;
            }

            if (blackBars != null)
            {
                blackBars.active = blackBarsActive;
                blackBars.size.value = blackBarsSize;
            }
        }

        public void ApplyActiveAndOverride(
            ZzzPostProcessVolume volume,
            ColorAdjustments colorAdjustments,
            Bloom bloom,
            BlackBars blackBars)
        {
            volume.active = active;
            volume.contrast.overrideState = contrastOverride;
            volume.darken.overrideState = darkenOverride;
            volume.shadowCrush.overrideState = shadowCrushOverride;
            volume.highlightBoost.overrideState = highlightBoostOverride;
            volume.saturation.overrideState = saturationOverride;
            volume.shadowDesat.overrideState = shadowDesatOverride;
            volume.vibrance.overrideState = vibranceOverride;
            volume.shadowTint.overrideState = shadowTintOverride;
            volume.midToneShift.overrideState = midToneShiftOverride;
            volume.highlightTint.overrideState = highlightTintOverride;
            volume.colorTemp.overrideState = colorTempOverride;
            volume.sharpenStrength.overrideState = sharpenStrengthOverride;
            volume.sharpenEdge.overrideState = sharpenEdgeOverride;
            volume.glowBoost.overrideState = glowBoostOverride;
            volume.glowThreshold.overrideState = glowThresholdOverride;
            volume.vignetteIntensity.overrideState = vignetteIntensityOverride;
            volume.vignetteSmoothness.overrideState = vignetteSmoothnessOverride;
            volume.vignetteRoundness.overrideState = vignetteRoundnessOverride;
            volume.bandSteps.overrideState = bandStepsOverride;
            volume.bandingBlend.overrideState = bandingBlendOverride;

            if (colorAdjustments != null)
            {
                colorAdjustments.active = colorAdjustmentsActive;
                colorAdjustments.postExposure.overrideState = postExposureOverride;
            }

            if (bloom != null)
            {
                bloom.active = bloomActive;
                bloom.intensity.overrideState = bloomIntensityOverride;
            }

            if (blackBars != null)
            {
                blackBars.active = blackBarsActive;
                blackBars.size.overrideState = blackBarsSizeOverride;
            }
        }
    }
}
