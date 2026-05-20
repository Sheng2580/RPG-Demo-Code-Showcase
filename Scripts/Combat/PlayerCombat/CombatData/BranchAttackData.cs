using UnityEngine;

[CreateAssetMenu(menuName = "Configs/BranchAttackData")]
public class BranchAttackData : ScriptableObject
{
    /* 后摇时间，超过后允许移动打断攻击。 */ public float cdTime;
    /* 攻击名称，只用于在配置面板中识别。 */ public string attackName;
    /* 连招窗口开启时间，超过后可以接回下一段普通攻击。 */ public float nextAttackTime;
    /* 攻击结束时间，动画 normalizedTime 超过后没有输入就回 Idle。 */ public float endTime = 0.95f;
    /* 该分支攻击段的命中判定时间点。 */ public TriggerHit[] triggerHits;
    /* 对应 Animator 里的动画状态名。 */ public string attackAnimationName;
    /* 分支攻击位移距离，有锁定目标时朝目标突进，否则朝角色前方位移。 */ public float displacement;
    
    // 分支攻击位移持续时间
     public float displacementTime;
    //击退距离
    public float repelDistance;

    // Enemy hit-stun caused by this branch attack. Longer than normal hits so the enemy cannot trade attacks.
    public float enemyHitStunDuration = 0.8f;
}
