using UnityEngine;

[CreateAssetMenu(menuName = "Configs/CombatFormData")]
public class CombatFormData : ScriptableObject
{
    [Header("Combat")]
    public CombatFormType formType;

    public CombatData combatData;

    public ScriptableObject formLogicData;

    public CombatSkillSetData skillSetData;

    public RuntimeAnimatorController animatorController;

    public GameObject weaponPrefab;

    [Header("Energy")]
    public float energyCost;

    public float duration;

    public bool returnToNormalWhenEnd = true;

    [Header("Animation")]
    public string idleStateName = "Idle";

    public float switchCrossFadeTime = 0.1f;

    [Header("鍙楀嚮 / 闂伩")]
    public string hitForwardStateName = "HitF";
    public string hitBackwardStateName = "HitB";
    public string slideForwardStateName = "SlideF";
    public string slideBackwardStateName = "SlideB";
    public string slideAttackStateName = "SlideAttack";
    public float slideAttackHitTime = 0.35f;
    public float slideAttackEndTime = 0.9f;
}


