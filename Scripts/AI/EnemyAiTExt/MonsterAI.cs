using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterAI : MonoBehaviour
{
    [Header("目标设置")]
    public Transform player;
    [Header("AI参数")]
    public float attackRange = 2f;
    public int attackDamage = 10;
    [Header("引用")]
    public Animator monsterAnim;

    private BehaviorTreeBuilder builder;
    private BehaviorTree aiTree;

    private void Awake()
    {
        // 初始化构建器
        builder = new BehaviorTreeBuilder();
        print("sss");
    }

    private void Start()
    {
        // 构建怪物AI行为树
        aiTree = builder
            // 根节点：选择器
            .Selector()
            
            // 分支1：攻击
            .Sequence()
            // 【修改点】这里用 .Add 代替 .AddBehavior
            .Add(new CheckPlayerInAttackRange(transform, player, attackRange,monsterAnim))
            .Add(new AttackPlayer(monsterAnim, attackDamage))
            .Back()
            .End();
    }

    private void Update()
    {
        // 每帧驱动行为树运行
        aiTree?.Tick();
        print("kais");
    }
}
