using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public enum ComboType
{
   //近战普通攻击判定绑定在角色身上，有角度限制
   NormalCombat,
   //武器攻击攻击绑定在武器上，无角度限制
   WeaponCombat,
   //飞行攻击判定绑定在角色身上，无角度限制,特殊状态自身为球心检测
   FileCombat
}

//判定的类型
public enum decideType
{
   Box,
   Sphere
}


//一中武器的连招数据表
[CreateAssetMenu(menuName = "Configs/ComboData")]
public class CombatData : ScriptableObject
{
   //该系列攻击的分支攻击触发残影的颜色攻击
   public Color branchAttackColor;
   
   public ComboType comboType;
   
   public decideType decideType;
  
   //判定的半径(如果是Box用做长度)
   public float decideLength;

   //球形状无意义
   public float decideBreadth;
  
   //判定的偏移值
   public Vector3 decideOffset;
   
   //角度限制仅NormalCombat
   public float decideAngle;
   
   
   //该武器对应的动画控制器(后面加载现在不用)
   //public Animator animator;
   
   //该武器对应的普通攻击连招
   public List<NormalAttackData> normalAttackDates;
   
   
   //技能
   public SkillData skill1;
   
   public SkillData skill2;

   //完美闪避技能
   public SkillData dodgeSkill;
   //完美防御技能
   public SkillData defenseSkill;

}
