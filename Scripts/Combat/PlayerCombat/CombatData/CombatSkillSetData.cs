using System;
using System.Collections.Generic;
using UnityEngine;

public enum SkillSlot
{
    // 技能槽 1，第一版默认绑定 F。
    Skill1,

    // 技能槽 2，第一版默认绑定 Tab。
    Skill2,

    // 闪避技能槽，第一版默认绑定 Slide。
    Dodge,

    // 防御技能槽，第一版预留，暂不绑定输入。
    Defense
}

public enum SkillEffectType
{
    // 范围伤害类技能。
    DamageArea,

    // 投射物类技能。
    Projectile,

    // 位移冲刺类技能。
    Dash,

    // 治疗或回复类技能。
    Heal,

    // 变身形态专属特殊技能。
    TransformSpecial
}

[CreateAssetMenu(menuName = "Configs/CombatSkillSetData")]
public class CombatSkillSetData : ScriptableObject
{
    // 当前形态拥有的技能列表，每个 slot 通常只配置一个技能。
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
    // 技能所属槽位，用于和输入按键对应。
    public SkillSlot slot;

    // 技能播放的 Animator 状态名。
    public string animationName;

    // 释放技能需要消耗的能量。
    public float energyCost;

    // 技能冷却时间，单位秒。
    public float cooldown;

    // 技能动画播放到该 normalizedTime 后回到 Idle。
    public float endTime = 0.95f;

    // 技能伤害判定点。
    public TriggerHit[] triggerHits;

    // 技能释放时播放的特效。
    public Effects castEffect;

    // 技能释放时播放的音效名。
    public string castSoundName;

    // 技能命中时播放的音效名。
    public string hitSoundName;

    // 技能效果类型，代码根据它选择具体执行逻辑。
    public SkillEffectType effectType;
}
