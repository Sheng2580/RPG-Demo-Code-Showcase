using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Configs/NormalAttackData")]
public class NormalAttackData : ScriptableObject
{
    // 后摇时间，超过后允许移动打断攻击。
    public float cdTime;

    // 攻击名称，只用于在配置面板中识别。
    public string attackName;

    // 对应 Animator 里的动画状态名。
    public string attackAnimationName;

    // 连招窗口开启时间，超过后可以接下一段普通攻击或分支攻击。
    public float nextAttackTime;

    // 攻击结束时间，动画 normalizedTime 超过后没有输入就回 Idle。
    public float endTime = 0.95f;

    // 该普通攻击段的命中判定时间点。
    public TriggerHit[] triggerHits;

    // 该普通攻击段可衔接的分支攻击数据。
    public BranchAttackData branchAttackData;

    // 分支攻击位移距离，有锁定目标时朝目标突进，否则朝角色前方位移。
    public float displacement;

    // 分支攻击位移持续时间。
    public float displacementTime;
}

[Serializable]
public class TriggerHit
{
    // 从动画开始到触发该伤害判定需要等待的时间。
    public float startTriggerTime;

    // 判定结束时间，后续做持续判定时使用。
    public float endTriggerTime;

    // 攻击挥出时播放的特效。
    public Effects attackEffect;

    // 攻击挥出时播放的音效名称。
    public string attackSoundName;

    // 该判定的基础伤害。
    // Attack multiplier. Kept as damage to preserve existing Unity asset data.
    public float damage;

    // 命中目标时播放的音效名称。
    public string hitSoundName;

    // 命中目标时播放的特效。
    public Effects effects;

}

[Serializable]
public class Effects
{
    // 特效资源名称。
    public string effectsName;

    // 特效相对生成位置。
    public Vector3 effectsPos;

    // 特效相对旋转角。
    public Vector3 effectRot;
}
