using System;
using System.Collections.Generic;
using UnityEngine;

public enum SkillSlot
{


}

public enum SkillEffectType
{


}

[CreateAssetMenu(menuName = "Configs/CombatSkillSetData")]
public class CombatSkillSetData : ScriptableObject
{
    public List<CombatSkillData> skills = new List<CombatSkillData>();

    public CombatSkillData GetSkill(SkillSlot slot)
    {
        if (skills == null)
        {
            return null;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            CombatSkillData skill = skills[i];
            if (skill != null && skill.slot == slot)
            {
                return skill;
            }
        }

        return null;
    }
}

[Serializable]
public class CombatSkillData
{
    public SkillSlot slot;

    public string animationName;

    public float energyCost;

    public float cooldown;

    public float endTime = 0.95f;

    public TriggerHit[] triggerHits;

    public Effects castEffect;

    public string castSoundName;

    public string hitSoundName;

    public SkillEffectType effectType;
}


