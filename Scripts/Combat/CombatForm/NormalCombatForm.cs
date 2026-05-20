using UnityEngine;

public class NormalCombatForm : CombatFormBase
{
    private const string BranchAttackSoundName = "PP";

    private NormalAttackData currentNormalAttackData;

    private BranchAttackData currentBranchAttackData;

    private bool isBranchAttack;

    private int branchMoveVersion;

    private float branchMoveRemainTime;

    private float branchMoveSpeed;

    private Vector3 branchMoveDirection;
    private bool branchMoveHitStunImmuneActive;
    private const string PacManPoolName = "Summon/PacMan";
    private bool skillPacManSpawned;
    private string currentSkillAnimationName;

    public override CombatFormType FormType => CombatFormType.Normal;

    public override void EnterForm(CombatContext context, CombatFormData formData)
    {
        base.EnterForm(context, formData);
        currentNormalAttackData = null;
        currentBranchAttackData = null;
        isBranchAttack = false;
        skillPacManSpawned = false;
        currentSkillAnimationName = null;
    }

    public override bool TryUseSkill(SkillSlot slot)
    {
        CombatSkillData skillData = skillSetData != null ? skillSetData.GetSkill(slot) : null;
        if (skillData == null || string.IsNullOrEmpty(skillData.animationName))
        {
            return false;
        }

        if (!base.TryUseSkill(slot))
        {
            return false;
        }

        skillPacManSpawned = false;
        currentSkillAnimationName = skillData.animationName;
        return true;
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

        if (controller.CurrentCombatData.normalAttackDates == null || controller.CurrentCombatData.normalAttackDates.Count == 0)
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
        currentNormalAttackData = attackData;
        currentBranchAttackData = null;
        isBranchAttack = false;

        int version = attackVersion + 1;
        StartAttack(attackData.attackAnimationName, attackData.endTime);
        RegisterAttackDetectionTimers(attackData.triggerHits, version);
        OpenNextAttackWindow(attackData.nextAttackTime, version);
        OpenMoveCancelWindow(attackData.cdTime, version);
        return true;
    }

    public override bool TryHeavyAttack()
    {
        if (player == null || currentNormalAttackData == null || currentNormalAttackData.branchAttackData == null)
        {
            return false;
        }

        if (isBranchAttack || !canNextAttack)
        {
            return false;
        }

        BranchAttackData branchData = currentNormalAttackData.branchAttackData;
        if (string.IsNullOrEmpty(branchData.attackAnimationName))
        {
            return false;
        }

        currentBranchAttackData = branchData;
        isBranchAttack = true;

        int version = attackVersion + 1;
        StartAttack(branchData.attackAnimationName, branchData.endTime);
        RegisterAttackDetectionTimers(branchData.triggerHits, version);
        OpenNextAttackWindow(branchData.nextAttackTime, version);
        OpenMoveCancelWindow(branchData.cdTime, version);
        MusicManager.Instance.PlaySoundForAB(BranchAttackSoundName, player.transform.position);
        StartBranchMoveHitStunImmune();
        StartBranchDisplacement(branchData, version);
        return true;
    }

    public override float GetEnemyHitStunDuration(TriggerHit triggerHit)
    {
        if (!isBranchAttack || currentBranchAttackData == null || currentBranchAttackData.enemyHitStunDuration <= 0f)
        {
            return base.GetEnemyHitStunDuration(triggerHit);
        }

        return currentBranchAttackData.enemyHitStunDuration;
    }

    public override void OnAttackUpdate()
    {
        if (GameInputManger.Instance.Slide && player != null && player.TryStartDodge())
        {
            return;
        }

        FaceAttackDirection();
        TickBranchDisplacement();

        if (GameInputManger.Instance.LAttack)
        {
            TryLightAttack();
            return;
        }

        if (GameInputManger.Instance.RAttack)
        {
            TryHeavyAttack();
            return;
        }

        if (isAttacking && IsCurrentAttackAnimationEnd())
        {
            EndAttackToIdle();
        }
    }

    public override void OnSkillUpdate()
    {
        TickPacManSkillSpawn();
        base.OnSkillUpdate();
    }

    public override void OnAttackExit()
    {
        StopBranchMoveHitStunImmune();
        base.OnAttackExit();
        currentNormalAttackData = null;
        currentBranchAttackData = null;
        isBranchAttack = false;
        skillPacManSpawned = false;
        currentSkillAnimationName = null;
        branchMoveVersion++;
        branchMoveRemainTime = 0f;
    }

    private void TickPacManSkillSpawn()
    {
        if (skillPacManSpawned || string.IsNullOrEmpty(currentSkillAnimationName))
        {
            return;
        }

        bool inSkillAnimation =
            CurrAnimationStateName(currentSkillAnimationName, out float normalizedTime) ||
            CurrAnimationStateTag("Skill", out normalizedTime);

        if (!inSkillAnimation || normalizedTime < 0.5f)
        {
            return;
        }

        skillPacManSpawned = true;
        SpawnPacMan();
    }

    private void SpawnPacMan()
    {
        if (player == null || PoolManager.Instance == null)
        {
            return;
        }

        GameObject obj = PoolManager.Instance.getObj(PacManPoolName);
        PacMan pacMan = obj.GetComponent<PacMan>();
        if (pacMan == null)
        {
            pacMan = obj.AddComponent<PacMan>();
        }

        pacMan.SpawnFromPlayerFront(player);
    }

    private void StartBranchDisplacement(BranchAttackData branchData, int version)
    {
        if (player == null || branchData == null || branchData.displacement <= 0f || branchData.displacementTime <= 0f)
        {
            return;
        }

        branchMoveVersion = version;
        branchMoveRemainTime = branchData.displacementTime;
        branchMoveSpeed = branchData.displacement / branchData.displacementTime;
        branchMoveDirection = GetBranchMoveDirection();
    }

    private void TickBranchDisplacement()
    {
        if (player == null || player.characterController == null || branchMoveVersion != attackVersion)
        {
            return;
        }

        if (branchMoveRemainTime <= 0f)
        {
            return;
        }

        float deltaTime = Mathf.Min(Time.deltaTime, branchMoveRemainTime);
        player.characterController.Move(branchMoveDirection * branchMoveSpeed * deltaTime);
        branchMoveRemainTime -= deltaTime;
        if (branchMoveRemainTime <= 0f)
        {
        }
    }

    private void StartBranchMoveHitStunImmune()
    {
        if (branchMoveHitStunImmuneActive || player == null)
        {
            return;
        }

        branchMoveHitStunImmuneActive = true;
        player.BeginHitStunImmune();
    }

    private void StopBranchMoveHitStunImmune()
    {
        if (!branchMoveHitStunImmuneActive)
        {
            return;
        }

        branchMoveHitStunImmuneActive = false;
        player?.EndHitStunImmune();
    }

    private Vector3 GetBranchMoveDirection()
    {
        if (player != null && player.isLockingEnemy && player.lockEnemyTarget != null)
        {
            Vector3 targetDirection = player.lockEnemyTarget.position - player.transform.position;
            targetDirection.y = 0f;
            if (targetDirection.sqrMagnitude > 0.0001f)
            {
                return targetDirection.normalized;
            }
        }

        return player != null ? player.transform.forward : Vector3.forward;
    }


}


