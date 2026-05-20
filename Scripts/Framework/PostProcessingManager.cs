using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 全局后处理管理器（单例），负责 DepthOfField 的平滑过渡
/// </summary>
public class PostProcessingManager : UnitySingleTonMono<PostProcessingManager>
{
    private Volume _globalVolume;
    private DepthOfField _dof;
    private Coroutine _dofCoroutine;
    private float _dofVelocity;
    private float _initialFocusDistance = 10f;
    // 如果为 true，则在恢复到初始值后自动禁用 DOF
    private bool _disableAfterRestore = true;

    private void Start()
    {
        _globalVolume = FindGlobalVolume();
        if (_globalVolume != null && _globalVolume.profile != null)
        {
            _globalVolume.profile.TryGet<DepthOfField>(out _dof);
            if (_dof != null)
            {
                // 采用默认初始值 10，并确保 DOF 默认关闭
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

        // 如果 DOF 当前被禁用且我们要从初始值过渡，先把值设为初始并启用组件
        if (!_dof.active)
        {
            _dof.focusDistance.value = _initialFocusDistance;
            _dof.active = true;
            // 确保 Volume 本身启用
            if (_globalVolume != null && !_globalVolume.enabled) _globalVolume.enabled = true;
        }

        // 确保 focusDistance 的 overrideState 打开，这样修改 value 才生效
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
            // optional debug per frame (comment out to reduce spam)
            // Debug.Log($"[PostProcessingManager] DOF value={current}");
            elapsed += Time.deltaTime;
            yield return null;
        }

        dof.focusDistance.value = target;
        // 确保在目标为非初始值时 DOF 保持启用并参数 override 打开
        if (!Mathf.Approximately(target, _initialFocusDistance))
        {
            dof.focusDistance.overrideState = true;
            dof.active = true;
            if (_globalVolume != null && !_globalVolume.enabled) _globalVolume.enabled = true;
        }
        // 如果目标是初始值并且配置要求恢复后禁用，则禁用组件并关闭 override
        if (_disableAfterRestore && Mathf.Approximately(target, _initialFocusDistance))
        {
            dof.focusDistance.overrideState = false;
            dof.active = false;
        }
        _dofCoroutine = null;
    }
    
}

