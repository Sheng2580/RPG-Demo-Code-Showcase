using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(CombatFormController))]
[RequireComponent(typeof(PlayerCombatStats))]
public class PlayerContorller : CharacterBase ,IStateMachineOwner
{
    private const string PerfectDodgeSoundName = "PP";

    private StateMachine _stateMachine;
    [HideInInspector]
    public PlayerStateType currentState;
    [HideInInspector]
    public PlayerStateType previousState;
    [HideInInspector]
    public FreeLookLeftShoulderFinal sitFreeLookCam;

    [Header("Combat Form")]
    public CombatFormController combatFormController;
    [HideInInspector]
    public bool isCanNextCombat;
    [HideInInspector]
    public bool isCanMoveInAttack;
    private bool isCombatInputEnabled = true;

    public LayerMask enemyLayerMask;

    [Header("受击 / 闪避")]
    public float perfectDodgeWindow = 0.25f;
    public float perfectDodgeAttackWindow = 0.6f;
    public float dodgeInvincibleDuration = 0.35f;
    public float slideCancelNormalizedTime = 0.55f;
    public float hitCancelNormalizedTime = 0.55f;
    public float slideEndNormalizedTime = 0.9f;
    [HideInInspector] public bool isInvincible;
    [HideInInspector] public bool isHitStunImmune;
    [HideInInspector] public bool hasPerfectDodgeAttackWindow;
    private bool _hasPendingEnemyDamage;
    private bool _slideAttackHitDone;
    private float _pendingEnemyDamageApplyTime;
    private float _pendingEnemyDamage;
    private float _invincibleEndTime;
    private float _perfectDodgeAttackEndTime;
    private int _actionInvincibleCount;
    private int _hitStunImmuneCount;
    private Vector3 _lastDamageSourcePosition;
    private Transform _pendingEnemyDamageSource;
    private Coroutine _unscaledAnimatorCoroutine;
    private AnimatorUpdateMode _cachedAnimatorUpdateMode;
    private bool _hasCachedAnimatorUpdateMode;

    [Header("锁定参数")]
    public float lockEnemyRadius = 18f;
    public float lockBreakDistance = 24f;
    public float lockCameraTargetHeight = 1.2f;
    public float lockFallbackForwardAngle = 70f;
    public float lockAttackTurnSmoothTime = 0.08f;
    public bool isDebugLockEnemy = true;
    [HideInInspector]
    public bool isLockingEnemy;
    [HideInInspector]
    public Transform lockEnemyTarget;
    private float _lockAttackTurnVelocity;

     public PlayerTimeLineController playerTimeLineController;


    private void Awake()
    {
        if (sitFreeLookCam == null)
        {
            sitFreeLookCam = transform.GetChild(0).GetComponent<FreeLookLeftShoulderFinal>();
        }
        GameManager.Instance.Player = this.gameObject;
        _stateMachine = new StateMachine();
        _stateMachine.Init(this);
        if (model == null)
        {
            model= transform.GetChild(0).GetComponent<PlayerModel>();
        }
        if (combatFormController == null)
        {
            combatFormController = GetComponent<CombatFormController>();
        }
        if (combatFormController == null)
        {
            combatFormController = gameObject.AddComponent<CombatFormController>();
        }

        playerTimeLineController = transform.Find("PlayerTimeLineController").GetComponent<PlayerTimeLineController>();
    }

    private void OnEnable()
    {
        EventCenter.Instance.AddEventListener<PlayerStateType>((GameEvent)4, ChangeState);
        EventCenter.Instance.AddEventListener<bool>(GameEvent.角色战斗控制, SetCombatInputEnabled);
    }

    private void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener<bool>(GameEvent.角色战斗控制, SetCombatInputEnabled);
        EventCenter.Instance.RemoveEventListener<PlayerStateType>((GameEvent)4, ChangeState);
    }

    private void OnDestroy()
    {
        if (_stateMachine != null)
        {
            _stateMachine.Stop(false);
        }
    }

    protected override void Start()
    {
        base.Start();
        ChangeState(PlayerStateType.Idle);
        InitializeCombatData();
        ApplyPlayerNumericData();
        InitializePlayerHud();
    }

    private void ApplyPlayerNumericData()
    {
        PlayerCombatStats stats = combatFormController != null ? combatFormController.Stats : GetComponent<PlayerCombatStats>();
        PlayerNumericManager.Instance.ApplyTo(stats);
    }

    private void InitializeCombatData()
    {
        if (combatFormController == null)
        {
            Debug.LogWarning("[PlayerContorller] No CombatFormController configured.");
            return;
        }

        if (!combatFormController.InitializeDefaultForm())
        {
            Debug.LogWarning("[PlayerContorller] CombatFormController is enabled, but no valid default form was applied.");
        }
    }

    public void ChangeState(PlayerStateType newState)
    {
        previousState = currentState;
        currentState = newState;
        switch (newState)
        {
            case PlayerStateType.Idle:
                _stateMachine.ChangeState<PlayerIdle>();
                break;
            case PlayerStateType.Move:
                _stateMachine.ChangeState<PlayerMove>();
                break;
            case PlayerStateType.MoveStop:
                _stateMachine.ChangeState<PlayerStopMove>();
                break;
            case PlayerStateType.Sit:
                _stateMachine.ChangeState<PlayerSit>();
                break;
            case PlayerStateType.CombatAttack:
                _stateMachine.ChangeState<PlayerCombatAttack>();
                break;
            case PlayerStateType.Skill:
                _stateMachine.ChangeState<PlayerSkill>();
                break;

            case PlayerStateType.Fall:
                _stateMachine.ChangeState<PlayerFall>();
                break;
            case PlayerStateType.Transfiguration :
                _stateMachine.ChangeState<PlayerTransfiguration>();
                break;
            case PlayerStateType.Hit:
                _stateMachine.ReChangeState<PlayerHit>();
                break;
            case PlayerStateType.Slide:
                _stateMachine.ChangeState<PlayerSlide>();
                break;
            case PlayerStateType.SlideAttack:
                _stateMachine.ReChangeState<PlayerSlideAttack>();
                break;
        }
    }

    protected override void Update()
    {
        base.Update();
        PlayerLockCamera();
        PlayerUseCombatForm();
        PlayerMove();
        PlayerCombatInput();
        PlayerFall();
        TickHitAndDodgeWindows();

        if (model.animator.HasParameter("IsGround"))
        {
            model.animator.SetBool("IsGround", characterIsGrounded);      
        }

    }
    #region 角色攻击


    private void PlayerUseCombatForm()
    {
        if (!isCombatInputEnabled)
        {
            return;
        }

        if (combatFormController == null || !GameInputManger.Instance.UseTheCombat)
        {
            return;
        }

        if (currentState == PlayerStateType.Transfiguration ||
            currentState == PlayerStateType.Hit ||
            currentState == PlayerStateType.Skill ||
            currentState == PlayerStateType.Slide ||
            currentState == PlayerStateType.SlideAttack)
        {
            return;
        }

        if (currentState == PlayerStateType.CombatAttack)
        {
            if (combatFormController.CurrentForm == null ||
                combatFormController.CurrentForm.FormType != CombatFormType.Normal ||
                !combatFormController.CanSwitchToTransform())
            {
                return;
            }

            ChangeState(PlayerStateType.Transfiguration);
            return;
        }

        if (combatFormController.CurrentForm != null &&
            combatFormController.CurrentForm.FormType == CombatFormType.Transform)
        {
            combatFormController.ToggleTransformForm();
            return;
        }

        if (!combatFormController.CanSwitchToTransform())
        {
            return;
        }

        ChangeState(PlayerStateType.Transfiguration);
    }

    private void PlayerCombatInput()
    {
        if (!isCombatInputEnabled)
        {
            return;
        }

        if (currentState == PlayerStateType.Transfiguration)
        {
            return;
        }

        if (currentState == PlayerStateType.Hit)
        {
            if (GameInputManger.Instance.Slide)
            {
                TryStartDodge();
            }

            return;
        }

        if (currentState == PlayerStateType.Skill ||
            currentState == PlayerStateType.Slide ||
            currentState == PlayerStateType.SlideAttack)
        {
            return;
        }

        if (combatFormController == null || combatFormController.CurrentForm == null)
        {
            return;
        }

        if (currentState == PlayerStateType.CombatAttack)
        {
            if (GameInputManger.Instance.Slide)
            {
                TryStartDodge();
            }

            return;
        }

        if (GameInputManger.Instance.LAttack)
        {
            combatFormController.TryLightAttack();
        }

        if (GameInputManger.Instance.RAttack)
        {
            combatFormController.TryHeavyAttack();
        }

        if (GameInputManger.Instance.Skill )
        {
            combatFormController.TryUseSkill(SkillSlot.Skill1);
        }

        if (GameInputManger.Instance.Tab)
        {
            combatFormController.TryUseSkill(SkillSlot.Skill2);
        }

        if (GameInputManger.Instance.Slide)
        {
            TryStartDodge();
        }
    }

    private void SetCombatInputEnabled(bool isEnabled)
    {
        isCombatInputEnabled = isEnabled;
    }


    public void CombatAttack()
    {
    }

    public void DetectionEnemy(TriggerHit triggerHit = null)
    {
        CombatData currentCombatData = GetCurrentCombatData();
        if (currentCombatData == null)
        {
            return;
        }

        ComboType comboType = currentCombatData.comboType;
        Transform detectionTransform = transform;
        bool needAngleLimit = comboType == ComboType.NormalCombat;

        if (comboType == ComboType.NormalCombat)
        {
            detectionTransform = transform;
        }
        else if (comboType == ComboType.WeaponCombat)
        {
            Transform weaponTransform = combatFormController != null ? combatFormController.CurrentWeaponTransform : null;
            if (weaponTransform != null)
            {
                detectionTransform = weaponTransform;
            }
        }
        else
        {
            detectionTransform = transform;
        }

        PlayAttackFeedback(triggerHit);

        Collider[] hits = GetAttackDetectionHits(detectionTransform);
        if (hits == null || hits.Length == 0)
        {
            return;
        }

        HashSet<Transform> damagedTargets = new HashSet<Transform>();
        foreach (Collider hit in hits)
        {
            Transform target = GetDamageTarget(hit);
            if (target == null || damagedTargets.Contains(target))
            {
                continue;
            }

            if (needAngleLimit && !IsTargetInAttackAngle(target))
            {
                continue;
            }

            damagedTargets.Add(target);
            EnemyBase enemy = hit.GetComponentInParent<EnemyBase>();
            if (enemy != null)
            {
                DamageResult damageResult = CalculatePlayerDamage(triggerHit);
                Debug.Log($"[PlayerDamage] Hit {enemy.name}, multiplier={damageResult.attackMultiplier}, damage={damageResult.damage}, crit={damageResult.isCrit}");
                float hitStunDuration = GetEnemyHitStunDuration(triggerHit);
                enemy.TakeDamage(damageResult.damage, transform, damageResult.isCrit, hitStunDuration);
                PlayerCombatStats stats = combatFormController != null ? combatFormController.Stats : GetComponent<PlayerCombatStats>();
                stats?.ApplyLifeStealFromDamage(damageResult.damage);
            }

            PlayHitFeedback(triggerHit, target);
            if (combatFormController != null)
            {
                combatFormController.AddEnergyFromNormalHit();
            }
        }
    }

    private float GetEnemyHitStunDuration(TriggerHit triggerHit)
    {
        CombatFormBase currentForm = combatFormController != null ? combatFormController.CurrentForm as CombatFormBase : null;
        return currentForm != null ? currentForm.GetEnemyHitStunDuration(triggerHit) : -1f;
    }

    private DamageResult CalculatePlayerDamage(TriggerHit triggerHit)
    {
        float attackMultiplier = triggerHit != null ? triggerHit.damage : 1f;
        PlayerCombatStats stats = combatFormController != null ? combatFormController.Stats : GetComponent<PlayerCombatStats>();
        if (stats != null)
        {
            return stats.CalculateDamage(attackMultiplier);
        }

        return new DamageResult
        {
            damage = Mathf.Max(0f, attackMultiplier),
            isCrit = false,
            attackMultiplier = Mathf.Max(0f, attackMultiplier),
            critRate = 0f,
            critDamage = 1f
        };
    }

    public void RegisterEnemyDamage(Transform damageSource)
    {
        RegisterEnemyDamage(damageSource, 10f);
    }

    public void RegisterEnemyDamage(Transform damageSource, float damage)
    {
        _lastDamageSourcePosition = damageSource != null ? damageSource.position : transform.position - transform.forward;

        _pendingEnemyDamageSource = damageSource;
        _pendingEnemyDamage = Mathf.Max(0f, damage);
        _hasPendingEnemyDamage = true;
        _pendingEnemyDamageApplyTime = Time.time + Mathf.Max(0f, perfectDodgeWindow);
        ShowCombatTipForPendingDamage();
    }

    public bool TryStartDodge()
    {
        if (combatFormController != null && combatFormController.CurrentForm is TransformCombatForm transformForm)
        {
            return transformForm.TryTransformDodgeTeleport();
        }

        if (!CanStartDodge())
        {
            return false;
        }

        StartDodgeInvincible();
        ChangeState(PlayerStateType.Slide);
        return true;
    }

    public bool TryTriggerPerfectDodgeInSlide()
    {
        if (currentState != PlayerStateType.Slide ||
            !_hasPendingEnemyDamage ||
            Time.time > _pendingEnemyDamageApplyTime)
        {
            return false;
        }

        TriggerPerfectDodge();
        return true;
    }

    public bool TryConsumePerfectDodgeSlideAttack()
    {
        if (!hasPerfectDodgeAttackWindow || Time.unscaledTime > _perfectDodgeAttackEndTime)
        {
            return false;
        }

        hasPerfectDodgeAttackWindow = false;
        _perfectDodgeAttackEndTime = 0f;
        _slideAttackHitDone = false;
        HideCombatTip();
        ChangeState(PlayerStateType.SlideAttack);
        ActionPostProcessManager.Instance?.RestorePerfectDodgeEffect();
        return true;
    }

    public bool TryTriggerSlideAttackHit(string stateName, float normalizedTime)
    {
        if (_slideAttackHitDone)
        {
            return false;
        }

        if (!IsCurrentAnimatorTagFinished("Attack", normalizedTime) &&
            !IsCurrentAnimatorStateFinished(stateName, normalizedTime))
        {
            return false;
        }

        _slideAttackHitDone = true;
        return true;
    }

    public void ClearSlideAttackRuntime()
    {
        _slideAttackHitDone = false;
    }

    public bool IsLastDamageFromFront()
    {
        Vector3 direction = _lastDamageSourcePosition - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return true;
        }

        return Vector3.Dot(transform.forward, direction.normalized) >= 0f;
    }

    public bool IsCurrentAnimatorTagFinished(string tag, float normalizedTime)
    {
        if (model == null || model.animator == null)
        {
            return false;
        }

        AnimatorStateInfo nextInfo = model.animator.GetNextAnimatorStateInfo(0);
        if (nextInfo.IsTag(tag))
        {
            return nextInfo.normalizedTime >= normalizedTime;
        }

        AnimatorStateInfo info = model.animator.GetCurrentAnimatorStateInfo(0);
        return info.IsTag(tag) && info.normalizedTime >= normalizedTime;
    }

    public bool IsCurrentAnimatorStateFinished(string stateName, float normalizedTime)
    {
        if (model == null || model.animator == null || string.IsNullOrEmpty(stateName))
        {
            return false;
        }

        AnimatorStateInfo nextInfo = model.animator.GetNextAnimatorStateInfo(0);
        if (nextInfo.IsName(stateName))
        {
            return nextInfo.normalizedTime >= normalizedTime;
        }

        AnimatorStateInfo info = model.animator.GetCurrentAnimatorStateInfo(0);
        return info.IsName(stateName) && info.normalizedTime >= normalizedTime;
    }

    private bool CanStartDodge()
    {
        switch (currentState)
        {
            case PlayerStateType.Idle:
            case PlayerStateType.Move:
            case PlayerStateType.MoveStop:
                return true;
            case PlayerStateType.CombatAttack:
                return true;
            case PlayerStateType.Hit:
                return IsCurrentAnimatorTagFinished("Hit", hitCancelNormalizedTime);
            case PlayerStateType.Slide:
                return IsCurrentAnimatorTagFinished("Slide", slideCancelNormalizedTime);
            default:
                return false;
        }
    }

    private void TriggerPerfectDodge()
    {
        _hasPendingEnemyDamage = false;
        _pendingEnemyDamageSource = null;
        HideCombatTip();

        if (combatFormController != null && !combatFormController.TryConsumeDodgeSkillCooldown())
        {
            return;
        }

        hasPerfectDodgeAttackWindow = true;
        _perfectDodgeAttackEndTime = Time.unscaledTime + Mathf.Max(0f, perfectDodgeAttackWindow);
        MusicManager.Instance.PlaySoundForAB(PerfectDodgeSoundName, transform.position);
        ActionPostProcessManager.Instance?.PlayPerfectDodgeEffect(this);
        EventCenter.Instance.EventTrigger((GameEvent)9);
    }

    private void ShowCombatTipForPendingDamage()
    {
        if (GameManager.Instance == null || !GameManager.Instance.CombatTipEnabled)
        {
            return;
        }

        CombatTipPanel.ShowForPlayer(this, GetCombatTipType());
    }

    private CombatTipType GetCombatTipType()
    {
        if (combatFormController != null && combatFormController.CurrentForm is TransformCombatForm)
        {
            return CombatTipType.PerfectDodgeUnavailableInTransform;
        }

        float cooldownRemaining = combatFormController != null ? combatFormController.GetDodgeSkillCooldownRemaining() : 0f;
        if (cooldownRemaining > 0f)
        {
            return CombatTipType.PerfectDodgeCooldown;
        }

        return CombatTipType.PerfectDodgeReady;
    }

    private void HideCombatTip()
    {
        CombatTipPanel.HideOpened();
    }

    public void UseUnscaledAnimatorFor(float duration)
    {
        if (model == null || model.animator == null)
        {
            return;
        }

        if (_unscaledAnimatorCoroutine != null)
        {
            StopCoroutine(_unscaledAnimatorCoroutine);
        }

        _unscaledAnimatorCoroutine = StartCoroutine(UnscaledAnimatorRoutine(Mathf.Max(0f, duration)));
    }

    private IEnumerator UnscaledAnimatorRoutine(float duration)
    {
        Animator animator = model.animator;
        if (animator == null)
        {
            yield break;
        }

        if (!_hasCachedAnimatorUpdateMode)
        {
            _cachedAnimatorUpdateMode = animator.updateMode;
            _hasCachedAnimatorUpdateMode = true;
        }

        animator.updateMode = AnimatorUpdateMode.UnscaledTime;

        if (duration > 0f)
        {
            yield return new WaitForSecondsRealtime(duration);
        }

        if (animator != null)
        {
            animator.updateMode = _cachedAnimatorUpdateMode;
        }

        _hasCachedAnimatorUpdateMode = false;
        _unscaledAnimatorCoroutine = null;
    }

    private void StartDodgeInvincible()
    {
        _invincibleEndTime = Time.time + Mathf.Max(0f, dodgeInvincibleDuration);
        RefreshInvincibleState();
    }

    public void StartDodgeInvincibleForTransform()
    {
        StartDodgeInvincible();
    }

    public void BeginActionInvincible()
    {
        _actionInvincibleCount++;
        RefreshInvincibleState();
    }

    public void EndActionInvincible()
    {
        _actionInvincibleCount = Mathf.Max(0, _actionInvincibleCount - 1);
        RefreshInvincibleState();
    }

    public void BeginHitStunImmune()
    {
        _hitStunImmuneCount++;
        RefreshHitStunImmuneState();
    }

    public void EndHitStunImmune()
    {
        _hitStunImmuneCount = Mathf.Max(0, _hitStunImmuneCount - 1);
        RefreshHitStunImmuneState();
    }

    private void TickHitAndDodgeWindows()
    {
        if (_invincibleEndTime > 0f && Time.time >= _invincibleEndTime)
        {
            _invincibleEndTime = 0f;
            RefreshInvincibleState();
        }

        if (hasPerfectDodgeAttackWindow && Time.unscaledTime > _perfectDodgeAttackEndTime)
        {
            hasPerfectDodgeAttackWindow = false;
            _perfectDodgeAttackEndTime = 0f;
        }

        if (_hasPendingEnemyDamage && Time.time > _pendingEnemyDamageApplyTime)
        {
            ApplyPendingEnemyDamage();
        }
    }

    private void ApplyPendingEnemyDamage()
    {
        if (!_hasPendingEnemyDamage)
        {
            return;
        }

        _hasPendingEnemyDamage = false;
        HideCombatTip();
        if (isInvincible)
        {
            _pendingEnemyDamage = 0f;
            _pendingEnemyDamageSource = null;
            return;
        }

        Transform source = _pendingEnemyDamageSource;
        _pendingEnemyDamageSource = null;
        _lastDamageSourcePosition = source != null ? source.position : _lastDamageSourcePosition;
        if (isHitStunImmune)
        {
            ApplyPlayerDamage(_pendingEnemyDamage);
            return;
        }

        ApplyPlayerDamage(_pendingEnemyDamage);
        ChangeState(PlayerStateType.Hit);
    }

    private void ApplyPlayerDamage(float damage)
    {
        PlayerCombatStats combatStats = combatFormController != null ? combatFormController.Stats : GetComponent<PlayerCombatStats>();
        combatStats?.TakeDamage(damage);
        _pendingEnemyDamage = 0f;
    }

    private void RefreshInvincibleState()
    {
        isInvincible = _actionInvincibleCount > 0 || (_invincibleEndTime > 0f && Time.time < _invincibleEndTime);
    }

    private void RefreshHitStunImmuneState()
    {
        isHitStunImmune = _hitStunImmuneCount > 0;
    }

    private void PlayerFall()
    {
        if (currentState == PlayerStateType.Hit ||
            currentState == PlayerStateType.Skill ||
            currentState == PlayerStateType.Slide ||
            currentState == PlayerStateType.SlideAttack ||
            currentState == PlayerStateType.Transfiguration)
        {
            return;
        }

        if (isEnableGravity && !characterIsGrounded)
        {
            ChangeState(PlayerStateType.Fall);
        }
    }


    private void PlayAttackFeedback(TriggerHit triggerHit)
    {
        if (triggerHit == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(triggerHit.attackSoundName))
        {
            MusicManager.Instance.PlaySoundForAB(triggerHit.attackSoundName, transform.position);
        }

        SpawnCombatEffect(triggerHit.attackEffect, transform);
    }

    private void PlayHitFeedback(TriggerHit triggerHit, Transform target)
    {
        if (triggerHit == null || target == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(triggerHit.hitSoundName))
        {
            MusicManager.Instance.PlaySoundForAB(triggerHit.hitSoundName, target.position);
        }

        SpawnCombatEffect(triggerHit.effects, target);
    }

    private void SpawnCombatEffect(Effects effectData, Transform origin)
    {
        if (effectData == null || origin == null || string.IsNullOrEmpty(effectData.effectsName))
        {
            return;
        }

        EffectManager.Instance.PlayEffectForAB(effectData, origin);
    }

    private Collider[] GetAttackDetectionHits(Transform detectionTransform)
    {
        CombatData currentCombatData = GetCurrentCombatData();
        if (detectionTransform == null || currentCombatData == null)
        {
            return null;
        }

        Vector3 center = detectionTransform.position + detectionTransform.TransformDirection(currentCombatData.decideOffset);
        if (currentCombatData.decideType == decideType.Box)
        {
            float length = Mathf.Max(0f, currentCombatData.decideLength);
            float breadth = Mathf.Max(0f, currentCombatData.decideBreadth);
            Vector3 halfExtents = new Vector3(breadth, breadth, length) * 0.5f;
            return Physics.OverlapBox(center, halfExtents, detectionTransform.rotation, enemyLayerMask, QueryTriggerInteraction.Collide);
        }

        float radius = Mathf.Max(0f, currentCombatData.decideLength);
        return Physics.OverlapSphere(center, radius, enemyLayerMask, QueryTriggerInteraction.Collide);
    }

    private Transform GetDamageTarget(Collider hit)
    {
        if (hit == null)
        {
            return null;
        }

        EnemyBase enemy = hit.GetComponentInParent<EnemyBase>();
        if (enemy != null)
        {
            if (enemy.isDead)
            {
                return null;
            }

            return enemy.transform;
        }

        return hit.attachedRigidbody != null ? hit.attachedRigidbody.transform : hit.transform;
    }

    private bool IsTargetInAttackAngle(Transform target)
    {
        CombatData currentCombatData = GetCurrentCombatData();
        if (target == null || currentCombatData == null)
        {
            return false;
        }

        Vector3 playerToTarget = target.position - transform.position;
        playerToTarget.y = 0f;
        if (playerToTarget.sqrMagnitude < 0.0001f)
        {
            return true;
        }

        float decideAngle = currentCombatData.decideAngle;
        if (decideAngle <= 0f || decideAngle >= 360f)
        {
            return true;
        }

        float angle = Vector3.Angle(transform.forward, playerToTarget.normalized);
        return angle <= decideAngle * 0.5f;
    }

    private CombatData GetCurrentCombatData()
    {
        return combatFormController != null ? combatFormController.CurrentCombatData : null;
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        DrawAttackDetectionGizmos();
    }

    private void DrawAttackDetectionGizmos()
    {
        CombatData gizmoCombatData = GetCurrentCombatData();
        if (gizmoCombatData == null)
        {
            return;
        }

        Transform detectionTransform = transform;
        Transform weaponTransform = combatFormController != null ? combatFormController.CurrentWeaponTransform : null;
        if (gizmoCombatData.comboType == ComboType.WeaponCombat && weaponTransform != null)
        {
            detectionTransform = weaponTransform;
        }

        Vector3 center = detectionTransform.position + detectionTransform.TransformDirection(gizmoCombatData.decideOffset);
        Gizmos.color = gizmoCombatData.branchAttackColor == default ? Color.red : gizmoCombatData.branchAttackColor;

        Matrix4x4 oldMatrix = Gizmos.matrix;
        if (gizmoCombatData.decideType == decideType.Box)
        {
            float length = Mathf.Max(0f, gizmoCombatData.decideLength);
            float breadth = Mathf.Max(0f, gizmoCombatData.decideBreadth);
            Gizmos.matrix = Matrix4x4.TRS(center, detectionTransform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(breadth, breadth, length));
            Gizmos.matrix = oldMatrix;
        }
        else
        {
            Gizmos.DrawWireSphere(center, Mathf.Max(0f, gizmoCombatData.decideLength));
        }

        if (gizmoCombatData.comboType == ComboType.NormalCombat)
        {
            DrawAttackAngleGizmos(gizmoCombatData, center);
        }
    }

    private void DrawAttackAngleGizmos(CombatData gizmoCombatData, Vector3 center)
    {
        float decideAngle = gizmoCombatData.decideAngle;
        if (decideAngle <= 0f || decideAngle >= 360f)
        {
            return;
        }

        float radius = Mathf.Max(0f, gizmoCombatData.decideLength);
        if (radius <= 0f)
        {
            return;
        }

        Vector3 leftDir = Quaternion.AngleAxis(-decideAngle * 0.5f, Vector3.up) * transform.forward;
        Vector3 rightDir = Quaternion.AngleAxis(decideAngle * 0.5f, Vector3.up) * transform.forward;

        Gizmos.DrawLine(transform.position, transform.position + leftDir.normalized * radius);
        Gizmos.DrawLine(transform.position, transform.position + rightDir.normalized * radius);
        Gizmos.DrawLine(transform.position, center);
    }


    #endregion


    private void PlayerMove()
    {
        if (currentState == PlayerStateType.Transfiguration ||
            currentState == PlayerStateType.Hit ||
            currentState == PlayerStateType.Skill ||
            currentState == PlayerStateType.Slide ||
            currentState == PlayerStateType.SlideAttack)
        {
            return;
        }

        if (currentState == PlayerStateType.CombatAttack && isCanNextCombat && GameInputManger.Instance.LAttack)
        {
            return;
        }

        if (currentState == PlayerStateType.CombatAttack && !isCanMoveInAttack)
        {
            return;
        }

        if (GameInputManger.Instance.Movement != Vector2.zero)
        {
            ChangeState(PlayerStateType.Move);
        }
    }

    #region 初始

    public void InitializePlayerHud()
    {
        if (GameSceneManager.Instance.GetCurrSceneName() == "hall")
        {
            return;
        }
        if (UIManager.Instance == null)
        {
            return;
        }

        UIManager.Instance.OpenPanelAsync<PlayerPnael>(UILayer.Dynamic, panel =>
        {
            panel?.Bind(this, combatFormController != null ? combatFormController.Stats : GetComponent<PlayerCombatStats>());
        });

    }

    private PlayerModel PlayerModel => model as PlayerModel;

    private void PlayerLockCamera()
    {

        if (GameInputManger.Instance.LockCamera)
        {

            if (isLockingEnemy)
            {
                CancelLockEnemy();
            }
            else
            {
                TryLockEnemy();
            }
        }

        if (!isLockingEnemy)
        {
            return;
        }

        if (IsLockTargetInvalid() ||
            Vector3.Distance(transform.position, lockEnemyTarget.position) > lockBreakDistance)
        {
            CancelLockEnemy();
            return;
        }

        UpdateLockCameraTarget();
    }

    private void TryLockEnemy()
    {
        Transform target = FindBestLockEnemy();
        if (target == null)
        {
            if (isDebugLockEnemy)
            {
                Debug.Log("[LockEnemy] 没有找到可锁定目标，请检查敌人 Collider 所在 Layer 是否包含在 enemyLayerMask 里。");
            }

            return;
        }

        LockEnemy(target);
    }

    private Transform FindBestLockEnemy()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            if (isDebugLockEnemy)
            {
                Debug.LogWarning("[LockEnemy] Camera.main 为空，无法根据摄像机画面选择锁定目标。");
            }

            return null;
        }

        if (enemyLayerMask.value == 0 && isDebugLockEnemy)
        {
            Debug.LogWarning("[LockEnemy] enemyLayerMask 没有配置任何 Layer，OverlapSphere 不会找到敌人。");
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, lockEnemyRadius, enemyLayerMask);
        if (isDebugLockEnemy)
        {
            Debug.Log($"[LockEnemy] 鎼滅储鍗婂緞={lockEnemyRadius}锛宔nemyLayerMask={enemyLayerMask.value}锛屽懡涓瑿ollider鏁伴噺={hits.Length}");
        }

        Transform bestTarget = null;
        float bestScore = Mathf.Infinity;
        Transform fallbackTarget = null;
        float fallbackScore = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            if (hit == null)
            {
                continue;
            }

            EnemyBase enemy = hit.GetComponentInParent<EnemyBase>();
            if (enemy == null || enemy.isDead)
            {
                continue;
            }

            Transform target = enemy.transform;
            Vector3 lockPoint = GetLockPoint(target);
            Vector3 viewportPos = mainCamera.WorldToViewportPoint(lockPoint);
            Vector3 playerToTarget = target.position - transform.position;
            playerToTarget.y = 0f;

            if (playerToTarget.sqrMagnitude > 0.0001f)
            {
                float forwardAngle = Vector3.Angle(transform.forward, playerToTarget.normalized);
                if (forwardAngle <= lockFallbackForwardAngle)
                {
                    float fallbackDistance = playerToTarget.magnitude / lockEnemyRadius;
                    float fallbackAngleScore = forwardAngle / lockFallbackForwardAngle;
                    float forwardScore = fallbackAngleScore * 0.7f + fallbackDistance * 0.3f;
                    if (forwardScore < fallbackScore)
                    {
                        fallbackScore = forwardScore;
                        fallbackTarget = target;
                    }
                }
            }

            if (viewportPos.z <= 0f ||
                viewportPos.x < 0f || viewportPos.x > 1f ||
                viewportPos.y < 0f || viewportPos.y > 1f)
            {
                if (isDebugLockEnemy)
                {
                    Debug.Log($"[LockEnemy] 璺宠繃鐩爣 {target.name}锛屼笉鍦ㄦ憚鍍忔満鐢婚潰鍐咃紝viewport={viewportPos}");
                }

                continue;
            }

            float screenCenterDistance = Vector2.Distance(
                new Vector2(viewportPos.x, viewportPos.y),
                new Vector2(0.5f, 0.5f)
            );
            float playerDistance = Vector3.Distance(transform.position, target.position) / lockEnemyRadius;

            float score = screenCenterDistance * 0.75f + playerDistance * 0.25f;
            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = target;
            }
        }

        return bestTarget != null ? bestTarget : fallbackTarget;
    }

    private bool IsLockTargetInvalid()
    {
        if (lockEnemyTarget == null)
        {
            return true;
        }

        if (!lockEnemyTarget.gameObject.activeInHierarchy)
        {
            return true;
        }

        EnemyBase enemy = lockEnemyTarget.GetComponentInParent<EnemyBase>();
        return enemy == null || enemy.isDead;
    }

    private void LockEnemy(Transform target)
    {
        FreeLookLeftShoulderFinal freeLookCamera = GetFreeLookCamera();
        if (freeLookCamera == null || freeLookCamera.freeLookCam == null)
        {
            return;
        }

        isLockingEnemy = true;
        lockEnemyTarget = target;
        if (isDebugLockEnemy)
        {
            Debug.Log($"[LockEnemy] 锁定目标：{lockEnemyTarget.name}");
        }

        UpdateLockCameraTarget();
        freeLookCamera.EnterLockCamera(transform, lockEnemyTarget);
        UIManager.Instance.OpenPanelAsync<lockPanel>(UILayer.Top, panel =>
        {
            if (panel == null)
            {
                return;
            }

            if (lockEnemyTarget != null)
            {
                panel.SetTarget(lockEnemyTarget.gameObject);
            }
            else
            {
                panel.PlayCloseAnimation();
            }
        });
    }

    public void CancelLockEnemy()
    {
        FreeLookLeftShoulderFinal freeLookCamera = GetFreeLookCamera();
        if (freeLookCamera != null && freeLookCamera.freeLookCam != null)
        {
            freeLookCamera.ExitLockCamera();
        }

        isLockingEnemy = false;
        lockEnemyTarget = null;
        lockPanel panel = UIManager.Instance.GetPanel<lockPanel>();
        if (panel != null)
        {
            panel.PlayCloseAnimation();
        }
    }

    private void UpdateLockCameraTarget()
    {
        if (lockEnemyTarget == null)
        {
            return;
        }

        FreeLookLeftShoulderFinal freeLookCamera = GetFreeLookCamera();
        if (freeLookCamera != null)
        {
            freeLookCamera.RefreshLockCameraTargets(transform, lockEnemyTarget);
        }
    }

    public bool TryFaceLockEnemy()
    {
        if (!isLockingEnemy || lockEnemyTarget == null)
        {
            return false;
        }

        Vector3 dir = lockEnemyTarget.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        float targetAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        float smoothAngle = Mathf.SmoothDampAngle(
            transform.eulerAngles.y,
            targetAngle,
            ref _lockAttackTurnVelocity,
            lockAttackTurnSmoothTime,
            Mathf.Infinity,
            Time.deltaTime
        );
        transform.eulerAngles = Vector3.up * smoothAngle;
        return true;
    }

    public bool FaceLockEnemyImmediate()
    {
        if (!isLockingEnemy || lockEnemyTarget == null)
        {
            return false;
        }

        Vector3 dir = lockEnemyTarget.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        float targetAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        transform.eulerAngles = Vector3.up * targetAngle;
        _lockAttackTurnVelocity = 0f;
        return true;
    }

    private Vector3 GetLockPoint(Transform target)
    {
        return target.position + Vector3.up * lockCameraTargetHeight;
    }

    private FreeLookLeftShoulderFinal GetFreeLookCamera()
    {
        PlayerModel playerModel = PlayerModel;
        if (playerModel != null && playerModel.freeLookCamera != null)
        {
            return playerModel.freeLookCamera;
        }

        return sitFreeLookCam;
    }

    #endregion


}


