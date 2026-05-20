using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public enum ComboType
{
   NormalCombat,
   WeaponCombat,
   FileCombat
}

public enum decideType
{
   Box,
   Sphere
}


[CreateAssetMenu(menuName = "Configs/ComboData")]
public class CombatData : ScriptableObject
{
   public Color branchAttackColor;

   public ComboType comboType;

   public decideType decideType;

   public float decideLength;

   public float decideBreadth;

   public Vector3 decideOffset;

   public float decideAngle;


   public Animator animator;

   public List<NormalAttackData> normalAttackDates;


   public SkillData skill1;

   public SkillData skill2;

   public SkillData dodgeSkill;
   public SkillData defenseSkill;

}


