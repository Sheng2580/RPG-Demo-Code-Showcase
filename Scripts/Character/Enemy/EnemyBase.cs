using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

/// <summary>
/// 敌人基类：保存所有敌人共用的数据、状态机入口和基础移动能力。
/// 行为树负责“决定要做什么”，状态机负责“执行具体动作”。
/// </summary>
public abstract class EnemyBase : CharacterBase, IStateMachineOwner
{
    [Header("敌人基础数据")]
    public float moveSpeed = 3f;
    public float chaseSpeed = 5f;
    public float maxHp = 100f;
    [FormerlySerializedAs("attackRange")]
    [Tooltip("AI 开始攻击的距离，不是伤害判定范围。真正命中范围由 EnemyWeapon 控制。")]
    public float attackStartRange = 2f;
    public float attackExitRangeOffset = 0.75f;
    public float detectRange = 10f;
    public float patrolRadius = 5f;
    public float patrolNavMeshSampleRadius = 2f;
    public int patrolPointSampleCount = 12;

    [Header("Gizmos")]
    public bool drawAiGizmos = true;
    public Color detectRangeGizmoColor = new Color(0.2f, 0.65f, 1f, 0.9f);
    public Color attackRangeGizmoColor = new Color(1f, 0.15f, 0.1f, 0.95f);
    public Color attackExitRangeGizmoColor = new Color(1f, 0.65f, 0.1f, 0.75f);
    public Color patrolRangeGizmoColor = new Color(0.2f, 1f, 0.25f, 0.55f);

    [Header("Root Motion")]
    public bool useRootMotionMovement = true;
    public float rootMotionSpeedScale = 1f;

    [Header("动画状态名")]
    public string idleAnimationName = "Idle";
    public string walkAnimationName = "Walk";
    public string runAnimationName = "Run";
    public string attackAnimationName = "Attack1";
    public string[] normalAttackComboNames = { "Attack1", "Attack2", "Attack3" };
    public float attackHitNormalizedTime = 0.5f;
    public float attackEndNormalizedTime = 0.9f;
    public string hitAnimationName = "Hit";
    public string deadAnimationName = "Die";

    [Header("受击")]
    public float hitStunDuration = 0.35f;
    public float hitKnockbackDistance = 0.6f;
    public float hitKnockbackDuration = 0.12f;

    [Header("目标")]
    public Transform target;

    [Header("武器判定")]
    public EnemyWeapon enemyWeapon;

    [Header("状态")]
    public EnemyStateType currentStateType;
    public bool isHit;
    public bool isDead;
    public float currentHp;

    public event Action<EnemyBase, float, float> OnHpChanged;
    public event Action<EnemyBase, float, bool> OnDamaged;
    public event Action<EnemyBase> OnDead;

    protected StateMachine stateMachine;
    protected BehaviorTree behaviorTree;
    protected NavMeshAgent navMeshAgent;
    private Vector3 startPosition;
    private bool rootMotionMoving;
    private Vector3 hitKnockbackDirection;
    private float hitStunEndTime;

    public Vector3 StartPosition => startPosition;
    public Vector3 HitKnockbackDirection => hitKnockbackDirection;
    public bool IsHitStunActive => Time.time < hitStunEndTime;
    public float AttackExitRange => attackStartRange + attackExitRangeOffset;
    public bool IsActionLocked =>
        currentStateType == EnemyStateType.Attack ||
        currentStateType == EnemyStateType.Hit ||
        currentStateType == EnemyStateType.Dead;

    protected override void Start()
    {
        base.Start();

        navMeshAgent = GetComponent<NavMeshAgent>();
        InitNavMeshAgent();
        InitEnemyWeapon();

        currentHp = Mathf.Max(1f, maxHp);
        startPosition = transform.position;
        stateMachine = new StateMachine();
        stateMachine.Init(this);

        InitBehaviorTree();
        RegisterHud();
        ChangeState(EnemyStateType.Idle);
    }

    public virtual void ResetForSpawn(Transform newTarget)
    {
        target = newTarget;
        isDead = false;
        isHit = false;
        hitStunEndTime = 0f;
        currentHp = Mathf.Max(1f, maxHp);
        startPosition = transform.position;
        velocityY = 0f;
        EndWeaponAttackDetection();
        EnableDamageColliders();

        if (navMeshAgent == null)
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }

        InitNavMeshAgent();
        SyncNavMeshAgentPosition();

        if (stateMachine != null)
        {
            ChangeState(EnemyStateType.Idle);
        }

        RegisterHud();
        OnHpChanged?.Invoke(this, currentHp, maxHp);
    }

    private void RegisterHud()
    {
        if (UIManager.Instance == null)
        {
            return;
        }

        UIManager.Instance.OpenPanelAsync<EnemyHudPanel>(UILayer.Dynamic, panel =>
        {
            panel?.RegisterEnemy(this);
        });
    }

    private void InitEnemyWeapon()
    {
        if (enemyWeapon == null)
        {
            enemyWeapon = GetComponentInChildren<EnemyWeapon>();
        }
    }

    public void BeginWeaponAttackDetection()
    {
        enemyWeapon?.BeginAttackDetection(transform);
    }

    public void EndWeaponAttackDetection()
    {
        enemyWeapon?.EndAttackDetection();
    }

    protected override void Update()
    {
        base.Update();

        // 行为树每帧只做决策，真正的移动和动画由当前状态处理。
        behaviorTree?.Tick();
    }

    /// <summary>
    /// 子类在这里组装自己的行为树，例如近战、远程、Boss 可以有不同决策。
    /// </summary>
    protected abstract void InitBehaviorTree();

    /// <summary>
    /// 子类决定每个 EnemyStateType 对应哪一个状态类。
    /// </summary>
    public abstract void ChangeState(EnemyStateType newState);

    public float DistanceToTarget()
    {
        if (target == null)
        {
            return float.MaxValue;
        }

        return Vector3.Distance(transform.position, target.position);
    }

    private void InitNavMeshAgent()
    {
        if (navMeshAgent == null)
        {
            return;
        }

        navMeshAgent.updatePosition = false;
        navMeshAgent.updateRotation = false;
        navMeshAgent.nextPosition = transform.position;
    }

    /// <summary>
    /// 第一版使用 CharacterController 做直线移动；后续接 NavMeshAgent 时只需要替换这里。
    /// </summary>
    public virtual void MoveTo(Vector3 targetPos, float speed)
    {
        if (characterController == null)
        {
            return;
        }

        Vector3 moveTargetPos = targetPos;
        if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.speed = speed;
            navMeshAgent.SetDestination(targetPos);

            if (!navMeshAgent.pathPending && navMeshAgent.hasPath)
            {
                moveTargetPos = navMeshAgent.steeringTarget;
            }
        }

        Vector3 direction = moveTargetPos - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        direction.Normalize();
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);

        if (rootMotionMoving)
        {
            return;
        }

        characterController.Move(direction * speed * Time.deltaTime);
        SyncNavMeshAgentPosition();
    }

    public virtual void BeginRootMotionMove()
    {
        if (!useRootMotionMovement || model == null || model.animator == null)
        {
            rootMotionMoving = false;
            return;
        }

        rootMotionMoving = true;
        model.animator.applyRootMotion = true;
        model.SetRootMotionAction(OnRootMotionAction);
    }

    public virtual void EndRootMotionMove()
    {
        rootMotionMoving = false;

        if (model == null || model.animator == null)
        {
            return;
        }

        model.ClearRootMotionAction();
        model.animator.applyRootMotion = false;
    }

    protected virtual void OnRootMotionAction(Vector3 dir, Quaternion rot)
    {
        if (characterController == null || !characterController.enabled)
        {
            return;
        }

        // Same idea as PlayerState: animation drives displacement; scale is used only when tuning speed.
        characterController.Move(dir * rootMotionSpeedScale);
        SyncNavMeshAgentPosition();
    }

    public void MoveByKnockback(Vector3 direction, float distance)
    {
        if (characterController == null || !characterController.enabled || distance <= 0f)
        {
            return;
        }

        characterController.Move(direction * distance);
        SyncNavMeshAgentPosition();
    }

    protected void SyncNavMeshAgentPosition()
    {
        if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.nextPosition = transform.position;
        }
    }

    public string GetNormalAttackComboName(int comboIndex)
    {
        if (normalAttackComboNames == null || normalAttackComboNames.Length == 0)
        {
            return attackAnimationName;
        }

        int index = Mathf.Abs(comboIndex) % normalAttackComboNames.Length;
        return normalAttackComboNames[index];
    }

    public bool TryGetReachableNavMeshPoint(Vector3 point, float sampleRadius, out Vector3 navPoint)
    {
        navPoint = point;

        if (navMeshAgent == null || !navMeshAgent.enabled || !navMeshAgent.isOnNavMesh)
        {
            return true;
        }

        if (!NavMesh.SamplePosition(point, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
        {
            return false;
        }

        NavMeshPath path = new NavMeshPath();
        if (!navMeshAgent.CalculatePath(hit.position, path))
        {
            return false;
        }

        if (path.status != NavMeshPathStatus.PathComplete)
        {
            return false;
        }

        navPoint = hit.position;
        return true;
    }

    public bool TryFindTarget()
    {
        if (target != null)
        {
            return true;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        target = player != null ? player.transform : null;
        return target != null;
    }

    /// <summary>
    /// 敌人受击入口。后续接血量系统时，可以在这里扣血并判断是否死亡。
    /// </summary>
    public virtual void TakeHit()
    {
        TakeHit(null, -1f);
    }

    public virtual void TakeHit(Transform attacker)
    {
        TakeHit(attacker, -1f);
    }

    public virtual void TakeHit(Transform attacker, float stunDuration)
    {
        EndWeaponAttackDetection();

        if (isDead)
        {
            return;
        }

        hitKnockbackDirection = GetHitKnockbackDirection(attacker);
        hitStunEndTime = Time.time + Mathf.Max(0f, stunDuration > 0f ? stunDuration : hitStunDuration);
        isHit = true;
        ChangeState(EnemyStateType.Hit);
    }

    public virtual void TakeDamage(float damage, Transform attacker, bool isCrit = false)
    {
        TakeDamage(damage, attacker, isCrit, -1f);
    }

    public virtual void TakeDamage(float damage, Transform attacker, bool isCrit, float stunDuration)
    {
        if (isDead)
        {
            return;
        }

        float finalDamage = Mathf.Max(0f, damage);
        currentHp = Mathf.Max(0f, currentHp - finalDamage);
        Debug.Log($"[Enemy] {name} TakeDamage damage={finalDamage}, hp={currentHp}/{maxHp}");
        OnHpChanged?.Invoke(this, currentHp, maxHp);
        OnDamaged?.Invoke(this, finalDamage, isCrit);

        if (currentHp <= 0f)
        {
            Die();
            return;
        }

        TakeHit(attacker, stunDuration);
    }

    private Vector3 GetHitKnockbackDirection(Transform attacker)
    {
        Vector3 direction = attacker != null
            ? transform.position - attacker.position
            : -transform.forward;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = -transform.forward;
            direction.y = 0f;
        }

        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.back;
    }

    public virtual void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        isHit = false;
        hitStunEndTime = 0f;
        EndWeaponAttackDetection();
        CancelPlayerLockIfNeeded();
        DisableDamageColliders();
        OnDead?.Invoke(this);
        ChangeState(EnemyStateType.Dead);
    }

    public override void PlayAnimation(string animationName, int layer = 0, float fixedTransitionTime = 0.25f)
    {
        if (isDead && animationName != deadAnimationName)
        {
            return;
        }

        base.PlayAnimation(animationName, layer, fixedTransitionTime);
    }

    private void DisableDamageColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null)
            {
                continue;
            }

            colliders[i].enabled = false;
        }

        if (characterController != null)
        {
            characterController.enabled = false;
        }
    }

    private void EnableDamageColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null)
            {
                continue;
            }

            colliders[i].enabled = true;
        }

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (characterController != null)
        {
            characterController.enabled = true;
        }
    }

    private void CancelPlayerLockIfNeeded()
    {
        PlayerContorller player = target != null ? target.GetComponent<PlayerContorller>() : null;
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            player = playerObject != null ? playerObject.GetComponent<PlayerContorller>() : null;
        }

        if (player == null || !IsPlayerLockingThisEnemy(player))
        {
            return;
        }

        player.CancelLockEnemy();
    }

    private bool IsPlayerLockingThisEnemy(PlayerContorller player)
    {
        if (player == null || player.lockEnemyTarget == null)
        {
            return false;
        }

        if (player.lockEnemyTarget == transform)
        {
            return true;
        }

        EnemyBase lockedEnemy = player.lockEnemyTarget.GetComponentInParent<EnemyBase>();
        return lockedEnemy == this;
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (!drawAiGizmos)
        {
            return;
        }

        Vector3 center = transform.position;
        DrawRangeGizmo(center, detectRange, detectRangeGizmoColor);
        DrawRangeGizmo(center, attackStartRange, attackRangeGizmoColor);
        DrawRangeGizmo(center, AttackExitRange, attackExitRangeGizmoColor);

        Vector3 patrolCenter = Application.isPlaying ? StartPosition : transform.position;
        DrawRangeGizmo(patrolCenter, patrolRadius, patrolRangeGizmoColor);

        if (target != null)
        {
            Gizmos.color = attackRangeGizmoColor;
            Gizmos.DrawLine(center, target.position);
        }
    }

    private void DrawRangeGizmo(Vector3 center, float radius, Color color)
    {
        if (radius <= 0f)
        {
            return;
        }

        Gizmos.color = color;
        Gizmos.DrawWireSphere(center, radius);
    }
}
