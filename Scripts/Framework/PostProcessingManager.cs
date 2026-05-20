using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessingManager : UnitySingleTonMono<PostProcessingManager>
{
    private Volume _globalVolume;
    private DepthOfField _dof;
    private Coroutine _dofCoroutine;
    private float _dofVelocity;
    private float _initialFocusDistance = 10f;
    private bool _disableAfterRestore = true;

    private void Start()
    {
        _globalVolume = FindGlobalVolume();
        if (_globalVolume != null && _globalVolume.profile != null)
        {
            _globalVolume.profile.TryGet<DepthOfField>(out _dof);
            if (_dof != null)
            {
                _initialFocusDistance = 10f;
                _dof.focusDistance.value = _initialFocusDistance;
                _dof.active = false;
            }
        }
    }

    private Volume FindGlobalVolume()
    {
        GameObject go = GameObject.Find("GlobalVolume");
        if (go != null)
        {
            var vol = go.GetComponent<Volume>();
            if (vol != null) return vol;
        }
        return FindObjectOfType<Volume>();
    }

    public void SetDepthImmediate(float value)
    {
        if (_dof == null)
        {
            if (_globalVolume == null) _globalVolume = FindGlobalVolume();
            if (_globalVolume == null || _globalVolume.profile == null) return;
            _globalVolume.profile.TryGet<DepthOfField>(out _dof);
            if (_dof == null) return;
        }
        _dof.focusDistance.value = value;
    }

    public void AnimateDepthOfFieldTo(float targetValue, float duration)
    {
        if (_dof == null)
        {
            _globalVolume = FindGlobalVolume();
            if (_globalVolume == null || _globalVolume.profile == null)
            {
                return;
            }
            _globalVolume.profile.TryGet<DepthOfField>(out _dof);
            if (_dof == null)
            {
                return;
            }
        }

        if (!_dof.active)
        {
            _dof.focusDistance.value = _initialFocusDistance;
            _dof.active = true;
            if (_globalVolume != null && !_globalVolume.enabled) _globalVolume.enabled = true;
        }

        _dof.focusDistance.overrideState = true;


        if (_dofCoroutine != null) StopCoroutine(_dofCoroutine);
        _dofCoroutine = StartCoroutine(DoAnimateDepth(_dof, targetValue, duration));
    }

    public void RestoreDepthOfField(float duration)
    {
        AnimateDepthOfFieldTo(_initialFocusDistance, duration);
    }

    private IEnumerator DoAnimateDepth(DepthOfField dof, float target, float duration)
    {
        float current = dof.focusDistance.value;
        float smoothTime = Mathf.Max(0.01f, duration / 3f);
        _dofVelocity = 0f;

        float elapsed = 0f;
        while (elapsed < duration && Mathf.Abs(current - target) > 0.0005f)
        {
            current = MathTools.SmoothTransition(current, target, ref _dofVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);
            dof.focusDistance.value = current;
            [PostProcessingManager] DOF value={current}");
            elapsed += Time.deltaTime;
            yield return null;
        }

        dof.focusDistance.value = target;
        if (!Mathf.Approximately(target, _initialFocusDistance))
        {
            dof.focusDistance.overrideState = true;
            dof.active = true;
            if (_globalVolume != null && !_globalVolume.enabled) _globalVolume.enabled = true;
        }
        if (_disableAfterRestore && Mathf.Approximately(target, _initialFocusDistance))
        {
            dof.focusDistance.overrideState = false;
            dof.active = false;
        }
        _dofCoroutine = null;
    }

}



