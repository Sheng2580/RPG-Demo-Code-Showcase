using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Configs/WeaponComboData")]
public class WeaponComboData : ScriptableObject
{
    public List<WeaponAttackData> attacks = new List<WeaponAttackData>();
}

[Serializable]
public class WeaponAttackData
{
    public string attackName;

    public string animationName;

    public float nextAttackTime;

    public float endTime = 0.95f;

    public float moveCancelTime;

    public GameObject hitboxPrefab;

    public GameObject projectilePrefab;

    public WeaponTriggerHit[] triggerHits;
}

[Serializable]
public class WeaponTriggerHit
{
    public float startTriggerTime;

    public float endTriggerTime;

    public float damage;

    public string attackSoundName;

    public string hitSoundName;

    public string attackEffectName;

    public string hitEffectName;
}


