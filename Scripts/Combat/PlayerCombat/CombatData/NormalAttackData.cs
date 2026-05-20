using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Configs/NormalAttackData")]
public class NormalAttackData : ScriptableObject
{
    public float cdTime;

    public string attackName;

    public string attackAnimationName;

    public float nextAttackTime;

    public float endTime = 0.95f;

    public TriggerHit[] triggerHits;

    public BranchAttackData branchAttackData;

    public float displacement;

    public float displacementTime;
}

[Serializable]
public class TriggerHit
{
    public float startTriggerTime;

    public float endTriggerTime;

    public Effects attackEffect;

    public string attackSoundName;

    public float damage;

    public string hitSoundName;

    public Effects effects;

}

[Serializable]
public class Effects
{
    public string effectsName;

    public Vector3 effectsPos;

    public Vector3 effectRot;
}


