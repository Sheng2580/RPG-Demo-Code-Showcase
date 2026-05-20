using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Configs/WeaponComboData")]
public class WeaponComboData : ScriptableObject
{
    // 当前武器或变身形态的轻攻击连招段。
    public List<WeaponAttackData> attacks = new List<WeaponAttackData>();
}

[Serializable]
public class WeaponAttackData
{
    // 攻击段名称，只用于在配置面板中识别。
    public string attackName;

    // 对应 Animator 里的动画状态名。
    public string animationName;

    // 连招窗口开启时间，超过后可以接下一段轻攻击。
    public float nextAttackTime;

    // 攻击结束时间，动画 normalizedTime 超过后没有输入就回 Idle。
    public float endTime = 0.95f;

    // 移动取消时间，超过后允许移动打断攻击后摇。
    public float moveCancelTime;

    // 预留命中盒预制体，后续如果做独立 HitBox 可使用。
    public GameObject hitboxPrefab;

    // 预留投射物预制体，后续如果做远程攻击可使用。
    public GameObject projectilePrefab;

    // 该攻击段的命中判定时间点。
    public WeaponTriggerHit[] triggerHits;
}

[Serializable]
public class WeaponTriggerHit
{
    // 从动画开始到触发该命中判定需要等待的时间。
    public float startTriggerTime;

    // 命中判定结束时间，后续做持续判定时使用。
    public float endTriggerTime;

    // 该判定的基础伤害。
    // Attack multiplier. Kept as damage to preserve existing Unity asset data.
    public float damage;

    // 攻击挥出时播放的音效名称。
    public string attackSoundName;

    // 命中目标时播放的音效名称。
    public string hitSoundName;

    // 攻击挥出时播放的特效名称。
    public string attackEffectName;

    // 命中目标时播放的特效名称。
    public string hitEffectName;
}
