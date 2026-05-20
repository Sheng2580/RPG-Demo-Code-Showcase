using UnityEngine;

public class TransformCombatForm : CombatFormBase
{
    private const string MotorcyclePoolName = "Summon/moT";
    private const string TransformLaserEffectABName = "effects";
    private const string TransformLaserEffectName = "Blue laser";
    private const float TransformLaserEnergyDrainMultiplier = 2f;
    private const float TransformLaserDamageMultiplier = 1.2f;
    private const float TransformLaserDamageInterval = 0.18f;
    private const float TransformLaserDamageRange = 10f;
    private const float TransformLaserDamageRadius = 1.15f;
    private const float TransformLaserRecycleDelay = 0.45f;
    private static readonly Vector3 TransformLaserLocalPosition = new Vector3(0f, 1.25f, 1.25f);
    private static readonly Vector3 TransformLaserLocalEuler = Vector3.zero;

    private const float FlyMoveSpeed = 7f;

    private const float CameraFaceSmoothTime = 0.08f;

    private const float FlyMoveDirectionLerpSpeed = 8f;

    private const float FlyAngleSmoothTime = 0.1f;

    private const float FlySpeedDampTime = 0.08f;

    private const float TransformAttackRecoilDistance = 0.75f;
    private const float TransformDodgeDistance = 3f;
    private const float TransformDodgeDuration = 0.16f;
    private const float TransformDodgeCooldown = 0.28f;

    private const float DefaultTransformAttackLungeDuration = 0.12f;

    private float cameraFaceVelocity;

    private Vector3 smoothedMoveDirection;

    private float currentFlyAngle;

    private float flyAngleVelocity;

    private NormalAttackData currentTransformAttackData;

    private bool hasSpawnedMotorcycleRush;

    private bool isMotorcycleRushPending;

    private int transformAttackLungeVersion;

    private float transformAttackLungeRemainTime;

    private float transformAttackLungeSpeed;

    private Vector3 transformAttackLungeDirection;
    private int lastTransformDodgeFrame = -1;
    private int transformDodgeVersion;
    private float transformDodgeCooldownEndTime;
    private float transformDodgeRemainTime;
    private float transformDodgeSpeed;
    private Vector3 transformDodgeDirection;
    private GameObject transformLaserObject;
    private TransformLaserSkill transformLaserSkill;
    private bool isTransformLaserSkillActive;
    private int transformLaserSpawnVersion;
    private float transformLaserCooldownEndTime;

    public override CombatFormType FormType => CombatFormType.Transform;

    public override void EnterForm(CombatContext context, CombatFormData formData)
    {
        base.EnterForm(context, formData);
        Debug.Log("杩涘叆鍙樿韩褰㈡€?);

        currentTransformAttackData = null;
        hasSpawnedMotorcycleRush = false;
        isMotorcycleRushPending = false;
        smoothedMoveDirection = Vector3.zero;
        currentFlyAngle = 0f;
        flyAngleVelocity = 0f;

        if (player != null)
        {
            if (player.isLockingEnemy)
            {
                player.CancelLockEnemy();
            }

            player.sitFreeLookCam?.ForceUnlockLookInput();
            player.sitFreeLookCam?.EnterTransformFormCamera();
            LaserGunCon.StartTransformSpawner(player);
            player.isEnableGravity = false;
            player.velocityY = 0f;
        }
    }

    public override void ExitForm()
    {
        base.ExitForm();

        EventCenter.Instance.EventTrigger(
            GameEvent.澶栨弿杈瑰彂鍏?
            new OutlineGlowEventData(false, Color.blue)
        );

        currentTransformAttackData = null;
        transformDodgeVersion++;
        transformDodgeRemainTime = 0f;
        StopTransformLaserSkill();

        if (player != null)
        {
            if (player.isLockingEnemy)
            {
                player.CancelLockEnemy();
            }

            player.sitFreeLookCam?.ForceUnlockLookInput();
            LaserGunCon.StopTransformSpawner(player);
            player.sitFreeLookCam?.ExitTransformFormCamera();
            ActionPostProcessManager.Instance?.Restore(0.25f);
            PlayerPnael.SetSceneTransfigurationLayout(false);

            if (player.model is PlayerModel playerModel)
            {
                playerModel.CloseTransfiguration();
            }

            player.isEnableGravity = true;
            player.velocityY = 0f;
            PlayerModel pM  =  player.model as PlayerModel;
            pM?.wing.SetActive(false);
        }
    }

    public override void OnIdleEnter()
    {
        base.OnIdleEnter();
        ResetFlyingBlendTree();
    }

    public override void OnIdleUpdate()
    {
        if (GameInputManger.Instance.Slide && TryTransformDodgeTeleport())
        {
            return;
        }

        TickTransformDodgeMove();
        FaceCameraForward();
    }

    public override void OnMoveEnter()
    {
        PlayAnimation("Move");
        player?.model?.ClearRootMotionAction();
    }

    public override void OnMoveUpdate()
    {
        if (GameInputManger.Instance.Slide && TryTransformDodgeTeleport())
        {
            return;
        }

        if (TickTransformDodgeMove())
        {
            return;
        }

        HandleFlyMove();
    }

    public override void OnMoveExit()
    {
        ResetFlyingBlendTree();
    }

    public override bool TryLightAttack()
    {
        if (player == null || controller == null || controller.CurrentCombatData == null)
        {
            return false;
        }

        if (isAttacking && !canNextAttack)
        {
            return false;
        }

        if (controller.CurrentCombatData.normalAttackDates == null ||
            controller.CurrentCombatData.normalAttackDates.Count == 0)
        {
            return false;
        }

        int index = Mathf.Clamp(controller.CurrentCombatIndex, 0, controller.CurrentCombatData.normalAttackDates.Count - 1);
        NormalAttackData attackData = controller.CurrentCombatData.normalAttackDates[index];
        if (attackData == null || string.IsNullOrEmpty(attackData.attackAnimationName))
        {
            return false;
        }

        controller.AdvanceNormalCombatIndex();
        currentTransformAttackData = attackData;
        hasSpawnedMotorcycleRush = false;
        isMotorcycleRushPending = true;

        int version = attackVersion + 1;
        StartAttack(attackData.attackAnimationName, attackData.endTime);
        TriggerTransformAttackGhost();
        StartTransformAttackLunge(attackData, version);
        RegisterAttackDetectionTimers(attackData.triggerHits, version);
        OpenNextAttackWindow(attackData.nextAttackTime, version);
        OpenMoveCancelWindow(attackData.cdTime, version);
        return true;
    }

    public override bool TryHeavyAttack()
    {
        return false;
    }

    public override bool TryUseSkill(SkillSlot slot)
    {
        if (slot != SkillSlot.Skill1 || isTransformLaserSkillActive || player == null)
        {
            return false;
        }

        CombatSkillData skillData = skillSetData != null ? skillSetData.GetSkill(slot) : null;
        if (Time.time < transformLaserCooldownEndTime)
        {
            return false;
        }

        if (skillData != null && stats != null && !stats.TryConsumeEnergy(skillData.energyCost))
        {
            return false;
        }

        transformLaserCooldownEndTime = Time.time + Mathf.Max(0f, skillData != null ? skillData.cooldown : 0f);
        string animationName = skillData != null && !string.IsNullOrEmpty(skillData.animationName) ? skillData.animationName : "Skill";
        StartSkill(animationName, 999f);
        StartTransformLaserSkill();
        return true;
    }

    public override void OnSkillEnter()
    {
        base.OnSkillEnter();
        player?.model?.ClearRootMotionAction();
        PlayerModel pM  =  player.model as PlayerModel;
        pM?.wing.SetActive(true);
    }

    public override void OnSkillUpdate()
    {
        FaceCameraForward();
        TickTransformLaserMove();
    }

    public override void OnSkillExit()
    {
        base.OnSkillExit();
    }

    public override void OnAttackUpdate()
    {
        if (GameInputManger.Instance.Slide && TryTransformDodgeTeleport())
        {
            return;
        }

        FaceCameraForward();
        TickTransformDodgeMove();
        TickTransformAttackLunge();
        TrySpawnMotorcycleRushFromAttackProgress();

        if (GameInputManger.Instance.LAttack)
        {
            TryLightAttack();
            return;
        }

        if (isAttacking && IsCurrentAttackAnimationEnd())
        {
            EndAttackToIdle();
        }
    }

    public override void OnAttackExit()
    {
        base.OnAttackExit();
        currentTransformAttackData = null;
        hasSpawnedMotorcycleRush = false;
        isMotorcycleRushPending = false;
        transformAttackLungeVersion++;
        transformAttackLungeRemainTime = 0f;
    }

    private void TrySpawnMotorcycleRushFromAttackProgress()
    {
        if (hasSpawnedMotorcycleRush)
        {
            return;
        }

        if (isMotorcycleRushPending && CurrAnimationStateName(currentTransformAttackData.attackAnimationName, out float normalizedTime))
        {
            if (normalizedTime >= 0.08f)
            {
                SpawnMotorcycleRush();
            }

            return;
        }

    }

    private void SpawnMotorcycleRush()
    {
        if (hasSpawnedMotorcycleRush || player == null)
        {
            return;
        }

        hasSpawnedMotorcycleRush = true;
        isMotorcycleRushPending = false;
        Debug.Log("[TransformCombatForm] Spawn motorcycle rush.");
        ActionPostProcessManager.Instance?.PlayRushEffect();
        Motorcycle.SpawnRush(player, MotorcyclePoolName);
    }

    private void StartTransformLaserSkill()
    {
        if (player == null || PoolManager.Instance == null)
        {
            return;
        }

        isTransformLaserSkillActive = true;
        transformLaserSpawnVersion++;
        if (controller != null)
        {
            controller.TransformEnergyDrainMultiplier = TransformLaserEnergyDrainMultiplier;
        }

        int spawnVersion = transformLaserSpawnVersion;
        PoolManager.Instance.GetObjForAB(TransformLaserEffectABName, TransformLaserEffectName, obj =>
        {
            if (!isTransformLaserSkillActive || spawnVersion != transformLaserSpawnVersion || player == null || obj == null)
            {
                if (obj != null)
                {
                    PoolManager.Instance.pushObj(TransformLaserEffectName, obj);
                }

                return;
            }

            transformLaserObject = obj;
            transformLaserObject.transform.SetParent(player.transform, false);
            transformLaserObject.transform.localPosition = TransformLaserLocalPosition;
            transformLaserObject.transform.localRotation = Quaternion.Euler(TransformLaserLocalEuler);
            transformLaserObject.transform.localScale = Vector3.one;

            transformLaserSkill = transformLaserObject.GetComponent<TransformLaserSkill>();
            if (transformLaserSkill == null)
            {
                transformLaserSkill = transformLaserObject.AddComponent<TransformLaserSkill>();
            }

            transformLaserSkill.Init(
                player,
                stats,
                TransformLaserEffectName,
                TransformLaserDamageMultiplier,
                TransformLaserDamageInterval,
                TransformLaserDamageRange,
                TransformLaserDamageRadius,
                TransformLaserRecycleDelay,
                TransformLaserLocalPosition,
                Quaternion.Euler(TransformLaserLocalEuler));
        });
    }

    private void StopTransformLaserSkill()
    {
        isTransformLaserSkillActive = false;
        transformLaserSpawnVersion++;
        if (controller != null)
        {
            controller.TransformEnergyDrainMultiplier = 1f;
        }

        if (player != null && player.currentState == PlayerStateType.Skill)
        {
            player.EndActionInvincible();
        }

        if (transformLaserSkill != null)
        {
            transformLaserSkill.StopSkill();
        }
        else if (transformLaserObject != null && PoolManager.Instance != null)
        {
            PoolManager.Instance.pushObj(TransformLaserEffectName, transformLaserObject);
        }

        transformLaserSkill = null;
        transformLaserObject = null;
    }

    private void TickTransformLaserMove()
    {
        if (player == null || player.characterController == null)
        {
            return;
        }

        Vector2 input = GameInputManger.Instance.Movement;
        bool hasInput = input.sqrMagnitude > 0.0001f;
        SetFlyingBlendTree(input, hasInput);
        if (!hasInput)
        {
            smoothedMoveDirection = Vector3.zero;
            return;
        }

        Vector3 targetMoveDirection = GetCameraRelativeMoveDirection(input);
        if (targetMoveDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        targetMoveDirection.Normalize();
        float lerpValue = 1f - Mathf.Exp(-FlyMoveDirectionLerpSpeed * Time.deltaTime);
        smoothedMoveDirection = smoothedMoveDirection.sqrMagnitude < 0.0001f
            ? targetMoveDirection
            : Vector3.Slerp(smoothedMoveDirection, targetMoveDirection, lerpValue).normalized;
        player.characterController.Move(smoothedMoveDirection * FlyMoveSpeed * Time.deltaTime);
    }

    private void HandleFlyMove()
    {
        if (player == null || player.characterController == null)
        {
            return;
        }

        FaceCameraForward();

        Vector2 input = GameInputManger.Instance.Movement;
        bool hasInput = input.sqrMagnitude > 0.0001f;
        SetFlyingBlendTree(input, hasInput);

        if (!hasInput)
        {
            smoothedMoveDirection = Vector3.zero;
            player.ChangeState(PlayerStateType.Idle);
            return;
        }

        Vector3 targetMoveDirection = GetCameraRelativeMoveDirection(input);
        if (targetMoveDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        targetMoveDirection.Normalize();
        if (smoothedMoveDirection.sqrMagnitude < 0.0001f)
        {
            smoothedMoveDirection = targetMoveDirection;
        }
        else
        {
            float lerpValue = 1f - Mathf.Exp(-FlyMoveDirectionLerpSpeed * Time.deltaTime);
            smoothedMoveDirection = Vector3.Slerp(smoothedMoveDirection, targetMoveDirection, lerpValue).normalized;
        }

        player.characterController.Move(smoothedMoveDirection * FlyMoveSpeed * Time.deltaTime);
    }

    public bool TryTransformDodgeTeleport()
    {
        if (player == null || player.characterController == null)
        {
            return false;
        }

        if (lastTransformDodgeFrame == Time.frameCount)
        {
            return true;
        }

        if (Time.time < transformDodgeCooldownEndTime)
        {
            return false;
        }

        Vector3 dodgeDirection = GetTransformDodgeDirection();
        if (dodgeDirection.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        EventCenter.Instance.EventTrigger(GameEvent.鐢熸垚娈嬪奖, new Color(0.145f, 0.612f, 0.91f, 1f));
        transformDodgeVersion = attackVersion;
        transformDodgeDirection = dodgeDirection.normalized;
        transformDodgeRemainTime = TransformDodgeDuration;
        transformDodgeSpeed = TransformDodgeDistance / Mathf.Max(0.001f, TransformDodgeDuration);
        transformDodgeCooldownEndTime = Time.time + TransformDodgeCooldown;
        player.StartDodgeInvincibleForTransform();
        lastTransformDodgeFrame = Time.frameCount;
        return true;
    }

    private bool TickTransformDodgeMove()
    {
        if (player == null ||
            player.characterController == null ||
            transformDodgeRemainTime <= 0f ||
            transformDodgeVersion != attackVersion)
        {
            return false;
        }

        float deltaTime = Mathf.Min(Time.deltaTime, transformDodgeRemainTime);
        float progress = Mathf.Clamp01(1f - transformDodgeRemainTime / TransformDodgeDuration);
        float easeSpeed = Mathf.SmoothStep(1.35f, 0.45f, progress);
        player.characterController.Move(transformDodgeDirection * transformDodgeSpeed * easeSpeed * deltaTime);
        transformDodgeRemainTime -= deltaTime;
        return transformDodgeRemainTime > 0f;
    }

    private Vector3 GetTransformDodgeDirection()
    {
        Vector2 input = GameInputManger.Instance.Movement;
        if (input.sqrMagnitude > 0.0001f)
        {
            Vector3 inputDirection = GetCameraRelativeMoveDirection(input);
            inputDirection.y = 0f;
            if (inputDirection.sqrMagnitude > 0.0001f)
            {
                return inputDirection.normalized;
            }
        }

        Vector3 backward = player != null ? -player.transform.forward : -Vector3.forward;
        backward.y = 0f;
        return backward.sqrMagnitude > 0.0001f ? backward.normalized : -Vector3.forward;
    }

    private void FaceCameraForward()
    {
        if (player == null)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        Vector3 cameraForward = mainCamera.transform.forward;
        cameraForward.y = 0f;
        if (cameraForward.sqrMagnitude < 0.0001f)
        {
            return;
        }

        cameraForward.Normalize();
        float targetAngle = Mathf.Atan2(cameraForward.x, cameraForward.z) * Mathf.Rad2Deg;
        if (player.sitFreeLookCam != null)
        {
            targetAngle -= player.sitFreeLookCam.RotateFixAngle;
        }

        float smoothAngle = Mathf.SmoothDampAngle(
            player.transform.eulerAngles.y,
            targetAngle,
            ref cameraFaceVelocity,
            CameraFaceSmoothTime
        );

        player.transform.eulerAngles = Vector3.up * smoothAngle;
    }

    private Vector3 GetCameraRelativeMoveDirection(Vector2 input)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return Vector3.zero;
        }

        Vector3 cameraForward = mainCamera.transform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        Vector3 cameraRight = mainCamera.transform.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();

        return cameraForward * input.y + cameraRight * input.x;
    }

    private void SetFlyingBlendTree(Vector2 input, bool hasInput)
    {
        if (player == null || player.model == null || player.model.animator == null)
        {
            return;
        }

        player.model.animator.SetFloat("Speed", hasInput ? 1f : 0f, FlySpeedDampTime, Time.deltaTime);
        if (!hasInput)
        {
            currentFlyAngle = Mathf.SmoothDampAngle(currentFlyAngle, 0f, ref flyAngleVelocity, FlyAngleSmoothTime);
            player.model.animator.SetFloat("Angle", currentFlyAngle);
            return;
        }

        float targetAngle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg;
        currentFlyAngle = Mathf.SmoothDampAngle(currentFlyAngle, targetAngle, ref flyAngleVelocity, FlyAngleSmoothTime);
        player.model.animator.SetFloat("Angle", currentFlyAngle);
    }

    private void TriggerTransformAttackGhost()
    {
        EventCenter.Instance.EventTrigger(GameEvent.鐢熸垚娈嬪奖, new Color(0.145f, 0.612f, 0.91f, 1f));
    }

    private void StartTransformAttackLunge(NormalAttackData attackData, int version)
    {
        if (player == null || player.characterController == null || attackData == null)
        {
            return;
        }

        float lungeDuration = GetTransformAttackLungeDuration(attackData);
        if (lungeDuration <= 0f)
        {
            return;
        }

        transformAttackLungeVersion = version;
        transformAttackLungeRemainTime = lungeDuration;
        transformAttackLungeSpeed = TransformAttackRecoilDistance / lungeDuration;
        transformAttackLungeDirection = GetTransformAttackRecoilDirection();
    }

    private void TickTransformAttackLunge()
    {
        if (player == null ||
            player.characterController == null ||
            transformAttackLungeRemainTime <= 0f ||
            transformAttackLungeVersion != attackVersion)
        {
            return;
        }

        float deltaTime = Mathf.Min(Time.deltaTime, transformAttackLungeRemainTime);
        player.characterController.Move(transformAttackLungeDirection * transformAttackLungeSpeed * deltaTime);
        transformAttackLungeRemainTime -= deltaTime;
    }

    private float GetTransformAttackLungeDuration(NormalAttackData attackData)
    {
        float firstTriggerTime = float.MaxValue;
        if (attackData.triggerHits != null)
        {
            foreach (TriggerHit triggerHit in attackData.triggerHits)
            {
                if (triggerHit == null)
                {
                    continue;
                }

                firstTriggerTime = Mathf.Min(firstTriggerTime, triggerHit.startTriggerTime);
            }
        }

        if (firstTriggerTime != float.MaxValue && firstTriggerTime > 0f)
        {
            return firstTriggerTime * 0.85f;
        }

        return DefaultTransformAttackLungeDuration;
    }

    private Vector3 GetTransformAttackRecoilDirection()
    {
        Vector3 forward = player != null ? player.transform.forward : Vector3.forward;
        forward.y = 0f;
        return forward.sqrMagnitude > 0.0001f ? -forward.normalized : -Vector3.forward;
    }

    private Transform GetTransformAttackTarget()
    {
        if (player == null)
        {
            return null;
        }

        if (player.isLockingEnemy && player.lockEnemyTarget != null)
        {
            return player.lockEnemyTarget;
        }

        return null;
    }

    private void ResetFlyingBlendTree()
    {
        if (player == null || player.model == null || player.model.animator == null)
        {
            return;
        }

        smoothedMoveDirection = Vector3.zero;
        currentFlyAngle = 0f;
        flyAngleVelocity = 0f;
        player.model.animator.SetFloat("Speed", 0f);
        player.model.animator.SetFloat("Angle", 0f);
    }
}


