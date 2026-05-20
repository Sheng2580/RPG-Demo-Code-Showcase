using UnityEngine;

[CreateAssetMenu(menuName = "Configs/CombatFormData")]
public class CombatFormData : ScriptableObject
{
    [Header("Combat")]
    // 形态类型，用于 CombatFormController 创建对应的形态逻辑。
    public CombatFormType formType;

    // 旧普通攻击判定和普通连招数据，第一版仍给普通形态复用。
    public CombatData combatData;

    // 形态专属逻辑数据，例如 WeaponComboData 或后续 Transform 专属数据。
    public ScriptableObject formLogicData;

    // 当前形态的技能表，切换形态时技能也随之切换。
    public CombatSkillSetData skillSetData;

    // 当前形态使用的 AnimatorController。
    public RuntimeAnimatorController animatorController;

    // 当前形态需要挂载的武器或形态对象预制件。
    public GameObject weaponPrefab;

    [Header("Energy")]
    // 切换到该形态需要消耗的能量。
    public float energyCost;

    // 形态持续时间，<=0 表示不按时间自动结束。
    public float duration;

    // 持续时间结束后是否自动回到普通形态。
    public bool returnToNormalWhenEnd = true;

    [Header("Animation")]
    // 切换到该形态后默认播放的待机动画状态名。
    public string idleStateName = "Idle";

    // 切换形态时播放待机动画的过渡时间，后续如果需要可接入 PlayAnimation。
    public float switchCrossFadeTime = 0.1f;

    [Header("受击 / 闪避")]
    // 每个战斗形态可以配置各自的受击、闪避和极限闪避反击动画。
    public string hitForwardStateName = "HitF";
    public string hitBackwardStateName = "HitB";
    public string slideForwardStateName = "SlideF";
    public string slideBackwardStateName = "SlideB";
    public string slideAttackStateName = "SlideAttack";
    public float slideAttackHitTime = 0.35f;
    public float slideAttackEndTime = 0.9f;
}
