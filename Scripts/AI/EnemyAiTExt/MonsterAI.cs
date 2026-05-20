using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterAI : MonoBehaviour
{
    [Header("鐩爣璁剧疆")]
    public Transform player;
    [Header("AI鍙傛暟")]
    public float attackRange = 2f;
    public int attackDamage = 10;
    [Header("寮曠敤")]
    public Animator monsterAnim;

    private BehaviorTreeBuilder builder;
    private BehaviorTree aiTree;

    private void Awake()
    {
        builder = new BehaviorTreeBuilder();
        print("sss");
    }

    private void Start()
    {
        aiTree = builder
            .Selector()

            .Sequence()
            .Add(new CheckPlayerInAttackRange(transform, player, attackRange,monsterAnim))
            .Add(new AttackPlayer(monsterAnim, attackDamage))
            .Back()
            .End();
    }

    private void Update()
    {
        aiTree?.Tick();
        print("kais");
    }
}


