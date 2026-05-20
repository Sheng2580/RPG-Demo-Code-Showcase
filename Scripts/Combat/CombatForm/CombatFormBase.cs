using System.Collections.Generic;
using UnityEngine;

public abstract class CombatFormBase : ICombatForm
{
    private const string SlideSkillSwordPoolName = "Summon/Sword";

    protected CombatFormController controller;

    protected CombatContext context;

    protected PlayerContorller player;

    protected PlayerCombatStats stats;

    protected CombatFormData formData;

    protected CombatSkillSetData skillSetData;

    protected WeaponComboData weaponComboData;

    protected int attackVersion;

    protected bool isAttacking;

    protected bool canNextAttack;

    protected bool canMoveCancel;

    protected float currentAttackEndTime = 0.95f;

    protected readonly Dictionary<SkillSlot, float> skillCooldownEndTimes = new Dictionary<SkillSlot, float>();

    public abstract CombatFormType FormType { get; }

    public virtual void EnterForm(CombatContext context, CombatFormData formData)
    {
        this.context = context;
        player = context != null ? context.Player : null;
        stats = context != null ? context.Stats : null;
        this.formData = formData;
        controller = player != null ? player.combatFormController : null;
        skillSetData = formData != null ? formData.skillSetData : null;
        weaponComboData = formData != null ? formData.formLogicData as WeaponComboData : null;
        ResetAttackRuntime();
    }

    public virtual void ExitForm()
    {
        attackVersion++;
        ResetAttackRuntime();
    }

    public virtual void TickForm()
    {
        controller?.CurrentWeaponCombat?.Tick();
    }

    public virtual void OnIdleEnter()
    {
        PlayAnimation(GetIdleAnimationName());
    }

    public virtual void OnIdleUpdate()
    {
    }

    public virtual void OnIdleExit()
    {
    }

    public virtual void OnMoveEnter()
    {
        PlayAnimation("Move");
        player?.model?.SetRootMotionAction(OnRootMotionMove);
    }

    public virtual void OnMoveUpdate()
    {
        DefaultGroundMoveUpdate();
    }

    public virtual void OnMoveExit()
    {
        player?.model?.ClearRootMotionAction();
    }

    public void OnFallEnter()
    {
        PlayAnimation("AirLoop");
        player?.model?.SetRootMotionAction(OnRootMotionMove);
    }

    public void OnFallUpdate()
    {
        if (CurrAnimationStateName("AirEnd", out float normalizedTime))
        {
            if (normalizedTime > 0.9f)
            {
                player.ChangeState(PlayerStateType.Idle);
            }
        }
    }

    public void OnFallExit()
    {
        player?.model?.ClearRootMotionAction();
    }


    public virtual void OnAttackEnter()
    {
        player?.model?.SetRootMotionAction(OnRootMotionMove);
    }

    public virtual void OnAttackUpdate()
    {
        if (GameInputManger.Instance.Slide && player != null && player.TryStartDodge())
        {
            return;
        }

        FaceAttackDirection();

        if (isAttacking && IsCurrentAttackAnimationEnd())
        {
            EndAttackToIdle();
        }
    }

    public virtual void OnAttackExit()
    {
        attackVersion++;
        ResetAttackRuntime();
        player?.model?.ClearRootMotionAction();
    }

    public virtual void OnSkillEnter()
    {
        player?.BeginActionInvincible();
        player?.model?.SetRootMotionAction(OnRootMotionMove);
    }

    public virtual void OnSkillUpdate()
    {
        if (player != null && IsCurrentAttackAnimationEnd())
        {
            player.ChangeState(PlayerStateType.Idle);
        }
    }

    public virtual void OnSkillExit()
    {
        attackVersion++;
        ResetAttackRuntime();
        player?.EndActionInvincible();
        player?.model?.ClearRootMotionAction();
    }

    public virtual bool TryLightAttack()
    {
        return TryPlayWeaponComboAttack();
    }

    public virtual bool TryHeavyAttack()
    {
        controller?.CurrentWeaponCombat?.HandleHeavyAttack();
        return controller != null && controller.CurrentWeaponCombat != null;
    }

    public virtual bool TryUseSkill(SkillSlot slot)
    {
        CombatSkillData skillData = skillSetData != null ? skillSetData.GetSkill(slot) : null;
        if (skillData == null || string.IsNullOrEmpty(skillData.animationName))
        {
            return false;
        }

        if (isAttacking && !canNextAttack)
        {
            return false;
        }

        if (Time.time < GetSkillCooldownEndTime(slot))
        {
            return false;
        }

        if (stats != null && !stats.TryConsumeEnergy(skillData.energyCost))
        {
            return false;
        }

        skillCooldownEndTimes[slot] = Time.time + Mathf.Max(0f, skillData.cooldown);
        int version = attackVersion + 1;
        StartSkill(skillData.animationName, skillData.endTime);
        PlaySkillFeedback(skillData);
        RegisterSkillHitTimers(skillData, version);
        return true;
    }

    public virtual bool TryConsumeDodgeSkillCooldown()
    {
        CombatSkillData skillData = skillSetData != null ? skillSetData.GetSkill(SkillSlot.Dodge) : null;
        if (skillData == null)
        {
            return true;
        }

        if (Time.time < GetSkillCooldownEndTime(SkillSlot.Dodge))
        {
            return false;
        }

        skillCooldownEndTimes[SkillSlot.Dodge] = Time.time + Mathf.Max(0f, skillData.cooldown);
        return true;
    }

    public virtual float GetDodgeSkillCooldownRemaining()
    {
        CombatSkillData skillData = skillSetData != null ? skillSetData.GetSkill(SkillSlot.Dodge) : null;
        if (skillData == null)
        {
            return 0f;
        }

        return Mathf.Max(0f, GetSkillCooldownEndTime(SkillSlot.Dodge) - Time.time);
    }

    public virtual float GetEnemyHitStunDuration(TriggerHit triggerHit)
    {
        return -1f;
    }

    public virtual void OnHitEnter()
    {
        PlayAnimation(player != null && player.IsLastDamageFromFront() ? GetHitForwardAnimationName() : GetHitBackwardAnimationName());
        player?.model?.SetRootMotionAction(OnRootMotionMove);
    }

    public virtual void OnHitUpdate()
    {
        if (player != null && player.IsCurrentAnimatorTagFinished("Hit", 0.9f))
        {
            player.ChangeState(PlayerStateType.Idle);
        }
    }

    public virtual void OnHitExit()
    {
        player?.model?.ClearRootMotionAction();
    }

    public virtual void OnSlideEnter()
    {
        player?.BeginActionInvincible();
        PlayAnimation(GetSlideAnimationName());
        player?.model?.SetRootMotionAction(OnRootMotionMove);
    }

    public virtual void OnSlideUpdate()
    {
        if (player == null)
        {
            return;
        }

        if (GameInputManger.Instance.LAttack && player.TryConsumePerfectDodgeSlideAttack())
        {
            return;
        }

        if (GameInputManger.Instance.Slide && player.TryStartDodge())
        {
            return;
        }

        if (player.IsCurrentAnimatorTagFinished("Slide", player.slideEndNormalizedTime))
        {
            player.ChangeState(GameInputManger.Instance.Movement == Vector2.zero ? PlayerStateType.Idle : PlayerStateType.Move);
        }
    }

    public virtual void OnSlideExit()
    {
        player?.EndActionInvincible();
        player?.model?.ClearRootMotionAction();
    }

    public virtual void OnSlideAttackEnter()
    {
        player?.BeginHitStunImmune();
        player?.FaceLockEnemyImmediate();
        PlayAnimation(GetSlideAttackAnimationName());
        player?.model?.SetRootMotionAction(OnRootMotionMove);
    }

    public virtual void OnSlideAttackUpdate()
    {
        if (player == null)
        {
            return;
        }

        if (player.TryTriggerSlideAttackHit(GetSlideAttackAnimationName(), GetSlideAttackHitTime()))
        {
            SpawnSlideSkillSword();
        }

        if (player.IsCurrentAnimatorTagFinished("Attack", GetSlideAttackEndTime()) ||
            player.IsCurrentAnimatorStateFinished(GetSlideAttackAnimationName(), GetSlideAttackEndTime()))
        {
            player.ChangeState(PlayerStateType.Idle);
        }
    }

    public virtual void OnSlideAttackExit()
    {
        player?.EndHitStunImmune();
        player?.model?.ClearRootMotionAction();
        player?.ClearSlideAttackRuntime();
    }

    protected virtual void SpawnSlideSkillSword()
    {
        if (player == null || PoolManager.Instance == null)
        {
            return;
        }

        player.FaceLockEnemyImmediate();

        GameObject obj = PoolManager.Instance.getObj(SlideSkillSwordPoolName);
        obj.transform.position = player.transform.position + Vector3.up * 0.9f + player.transform.forward * 1.1f;
        obj.transform.rotation = Quaternion.LookRotation(player.transform.forward, Vector3.up);

        Sword sword = obj.GetComponent<Sword>();
        if (sword == null)
        {
            sword = obj.AddComponent<Sword>();
        }

        sword.Init(player, SlideSkillSwordPoolName);
    }

    protected virtual string GetIdleAnimationName()
    {
        if (formData != null && !string.IsNullOrEmpty(formData.idleStateName))
        {
            return formData.idleStateName;
        }

        return "Idle";
    }

    protected virtual string GetHitForwardAnimationName()
    {
        return formData != null && !string.IsNullOrEmpty(formData.hitForwardStateName) ? formData.hitForwardStateName : "HitF";
    }

    protected virtual string GetHitBackwardAnimationName()
    {
        return formData != null && !string.IsNullOrEmpty(formData.hitBackwardStateName) ? formData.hitBackwardStateName : "HitB";
    }

    protected virtual string GetSlideAnimationName()
    {
        if (player != null && GameInputManger.Instance.Movement != Vector2.zero)
        {
            return formData != null && !string.IsNullOrEmpty(formData.slideForwardStateName) ? formData.slideForwardStateName : "SlideF";
        }

        return formData != null && !string.IsNullOrEmpty(formData.slideBackwardStateName) ? formData.slideBackwardStateName : "SlideB";
    }

    protected virtual string GetSlideAttackAnimationName()
    {
        return formData != null && !string.IsNullOrEmpty(formData.slideAttackStateName) ? formData.slideAttackStateName : "SlideAttack";
    }

    protected virtual float GetSlideAttackHitTime()
    {
        return formData != null && formData.slideAttackHitTime > 0f ? formData.slideAttackHitTime : 0.35f;
    }

    protected virtual float GetSlideAttackEndTime()
    {
        return formData != null && formData.slideAttackEndTime > 0f ? formData.slideAttackEndTime : 0.9f;
    }

    protected void StartAttack(string animationName, float endTime)
    {
        if (player == null || string.IsNullOrEmpty(animationName))
        {
            return;
        }

        attackVersion++;
        isAttacking = true;
        canNextAttack = false;
        canMoveCancel = false;
        player.isCanNextCombat = false;
        player.isCanMoveInAttack = false;
        currentAttackEndTime = endTime > 0f ? endTime : 0.95f;
        player.ChangeState(PlayerStateType.CombatAttack);
        PlayAnimation(animationName);
    }

    protected void StartSkill(string animationName, float endTime)
    {
        if (player == null || string.IsNullOrEmpty(animationName))
        {
            return;
        }

        attackVersion++;
        isAttacking = true;
        canNextAttack = false;
        canMoveCancel = false;
        player.isCanNextCombat = false;
        player.isCanMoveInAttack = false;
        currentAttackEndTime = endTime > 0f ? endTime : 0.95f;
        player.ChangeState(PlayerStateType.Skill);
        PlayAnimation(animationName);
    }

    protected void EndAttackToIdle()
    {
        if (player != null && player.currentState == PlayerStateType.CombatAttack)
        {
            player.ChangeState(PlayerStateType.Idle);
        }
    }

    protected void PlayAnimation(string stateName)
    {
        if (player == null || string.IsNullOrEmpty(stateName))
        {
            return;
        }

        player.PlayAnimation(stateName);
    }

    protected bool IsCurrentAttackAnimationEnd()
    {
        if (player == null || player.model == null || player.model.animator == null)
        {
            return false;
        }

        AnimatorStateInfo nextInfo = player.model.animator.GetNextAnimatorStateInfo(0);
        if (nextInfo.IsTag("Attack") || nextInfo.IsTag("Skill"))
        {
            return nextInfo.normalizedTime > currentAttackEndTime;
        }

        AnimatorStateInfo info = player.model.animator.GetCurrentAnimatorStateInfo(0);
        return (info.IsTag("Attack") || info.IsTag("Skill")) && info.normalizedTime > currentAttackEndTime;
    }

    protected void RegisterAttackDetectionTimers(TriggerHit[] triggerHits, int version)
    {
        if (triggerHits == null || triggerHits.Length == 0)
        {
            return;
        }

        foreach (TriggerHit triggerHit in triggerHits)
        {
            if (triggerHit == null)
            {
                continue;
            }

            TriggerHit cachedTriggerHit = triggerHit;
            RegisterTimer(cachedTriggerHit.startTriggerTime, version, () => player?.DetectionEnemy(cachedTriggerHit));
        }
    }

    protected void RegisterTimer(float delay, int version, System.Action callback)
    {
        if (callback == null)
        {
            return;
        }

        if (delay <= 0f)
        {
            if (IsTimerOwnerValid() && version == attackVersion)
            {
                callback.Invoke();
            }

            return;
        }
        MultiTimerManager.Instance.AddOneShotTimer(delay,
            () =>
            {
                if (!IsTimerOwnerValid())
                {
                    return;
                }

                if (version != attackVersion)
                {
                    return;
                }

                callback.Invoke();
            }
        );
    }

    private bool IsTimerOwnerValid()
    {
        return player != null && player.isActiveAndEnabled;
    }

    protected bool TryPlayWeaponComboAttack()
    {
        if (weaponComboData == null || weaponComboData.attacks == null || weaponComboData.attacks.Count == 0)
        {
            controller?.CurrentWeaponCombat?.HandleLightAttack();
            return controller != null && controller.CurrentWeaponCombat != null;
        }

        if (isAttacking && !canNextAttack)
        {
            return false;
        }

        int index = Mathf.Clamp(controller != null ? controller.CurrentCombatIndex : 0, 0, weaponComboData.attacks.Count - 1);
        WeaponAttackData attackData = weaponComboData.attacks[index];
        if (attackData == null || string.IsNullOrEmpty(attackData.animationName))
        {
            return false;
        }

        if (controller != null)
        {
            controller.CurrentCombatIndex = index + 1 >= weaponComboData.attacks.Count ? 0 : index + 1;
        }

        int version = attackVersion + 1;
        StartAttack(attackData.animationName, attackData.endTime);
        RegisterWeaponAttackHitTimers(attackData, version);
        OpenNextAttackWindow(attackData.nextAttackTime, version);
        OpenMoveCancelWindow(attackData.moveCancelTime, version);
        return true;
    }

    protected void OpenNextAttackWindow(float delay, int version)
    {
        RegisterTimer(delay, version, () =>
        {
            canNextAttack = true;
            if (player != null)
            {
                player.isCanNextCombat = true;
            }
        });
    }

    protected void OpenMoveCancelWindow(float delay, int version)
    {
        RegisterTimer(delay, version, () =>
        {
            canMoveCancel = true;
            if (player != null)
            {
                player.isCanMoveInAttack = true;
            }
        });
    }

    protected void ResetAttackRuntime()
    {
        isAttacking = false;
        canNextAttack = false;
        canMoveCancel = false;
        currentAttackEndTime = 0.95f;
        if (controller != null)
        {
            controller.ResetCombatIndex();
        }

        if (player != null)
        {
            player.isCanNextCombat = false;
            player.isCanMoveInAttack = false;
        }
    }

    protected void DefaultGroundMoveUpdate()
    {
        if (player == null)
        {
            return;
        }

        SetDefaultMoveBlendTree();
        DefaultMoveRotate();

        if (GameInputManger.Instance.Movement == Vector2.zero)
        {
            player.ChangeState(PlayerStateType.MoveStop);
        }
    }

    protected void FaceAttackDirection()
    {
        if (player == null)
        {
            return;
        }

        if (player.TryFaceLockEnemy())
        {
            return;
        }

        DefaultMoveRotate();
    }

    protected virtual void OnRootMotionMove(Vector3 dir, Quaternion rot)
    {
        if (player != null && player.characterController != null)
        {
            player.characterController.Move(dir);
        }
    }

    private void RegisterWeaponAttackHitTimers(WeaponAttackData attackData, int version)
    {
        if (attackData.triggerHits == null || attackData.triggerHits.Length == 0)
        {
            return;
        }

        foreach (WeaponTriggerHit triggerHit in attackData.triggerHits)
        {
            if (triggerHit == null)
            {
                continue;
            }

            WeaponTriggerHit cachedTriggerHit = triggerHit;
            RegisterTimer(cachedTriggerHit.startTriggerTime, version,
                () => controller?.CurrentWeaponCombat?.DetectHit(attackData, cachedTriggerHit));
        }
    }

    private void RegisterSkillHitTimers(CombatSkillData skillData, int version)
    {
        RegisterAttackDetectionTimers(skillData.triggerHits, version);
    }

    private void PlaySkillFeedback(CombatSkillData skillData)
    {
        if (skillData == null || player == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(skillData.castSoundName))
        {
            MusicManager.Instance.PlaySoundForAB(skillData.castSoundName, player.transform.position);
        }

        if (skillData.castEffect != null && !string.IsNullOrEmpty(skillData.castEffect.effectsName))
        {
            EffectManager.Instance.PlayEffectForAB(skillData.castEffect, player.transform);
        }
    }

    private float GetSkillCooldownEndTime(SkillSlot slot)
    {
        return skillCooldownEndTimes.TryGetValue(slot, out float endTime) ? endTime : 0f;
    }

    private void SetDefaultMoveBlendTree()
    {
        if (player == null || player.model == null || player.model.animator == null)
        {
            return;
        }

        float targetSpeed = GameInputManger.Instance.Run ? 1f : 0f;
        float currentSpeed = player.model.animator.GetFloat("Speed");
        float speed = Mathf.MoveTowards(currentSpeed, targetSpeed, Time.deltaTime * 8f);
        player.model.animator.SetFloat("Speed", speed);
    }

    private void DefaultMoveRotate()
    {
        if (player == null || player.sitFreeLookCam == null || GameInputManger.Instance.Movement == Vector2.zero)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        Vector2 input = GameInputManger.Instance.Movement;
        Vector3 worldDir = player.sitFreeLookCam.GetCorrectedCameraRelativeMoveDirection(input, mainCamera);
        if (worldDir.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float targetAngle = Mathf.Atan2(worldDir.x, worldDir.z) * Mathf.Rad2Deg;
        player.transform.eulerAngles = Vector3.up * Mathf.LerpAngle(
            player.transform.eulerAngles.y,
            targetAngle,
            Time.deltaTime * 12f
        );
    }


    protected virtual bool CurrAnimationStateName(string stateName , out float normalizedTime ,int layer = 0)
    {
        AnimatorStateInfo nextInfo = player.model.animator.GetNextAnimatorStateInfo(layer);
        if (nextInfo.IsName(stateName))
        {
            normalizedTime = nextInfo.normalizedTime;
            return true;
        }
        AnimatorStateInfo info =player.model.animator.GetCurrentAnimatorStateInfo(layer);
        normalizedTime = info.normalizedTime;
        return info.IsName(stateName);
    }

    protected virtual bool CurrAnimationStateName(string stateName ,int layer = 0)
    {
        AnimatorStateInfo nextInfo = player.model.animator.GetNextAnimatorStateInfo(layer);
        if (nextInfo.IsName(stateName))
        {
            return true;
        }
        AnimatorStateInfo info =player.model.animator.GetCurrentAnimatorStateInfo(layer);
        return info.IsName(stateName);
    }

    protected virtual bool CurrAnimationStateTag(string tag, out float normalizedTime)
    {
        AnimatorStateInfo nextInfo = player.model.animator.GetNextAnimatorStateInfo(0);
        if (nextInfo.IsTag(tag))
        {
            normalizedTime = nextInfo.normalizedTime;
            return true;
        }
        AnimatorStateInfo info = player.model.animator.GetCurrentAnimatorStateInfo(0);
        normalizedTime = info.normalizedTime;
        return info.IsTag(tag);
    }
}


