public interface ICombatForm
{
    CombatFormType FormType { get; }

    void EnterForm(CombatContext context, CombatFormData formData);
    void ExitForm();
    void TickForm();

    void OnIdleEnter();
    void OnIdleUpdate();
    void OnIdleExit();

    void OnMoveEnter();
    void OnMoveUpdate();
    void OnMoveExit();
    
    void OnFallEnter();
    void OnFallUpdate();
    void OnFallExit();


    void OnAttackEnter();
    void OnAttackUpdate();
    void OnAttackExit();

    void OnSkillEnter();
    void OnSkillUpdate();
    void OnSkillExit();

    bool TryLightAttack();
    bool TryHeavyAttack();
    bool TryUseSkill(SkillSlot slot);
    bool TryConsumeDodgeSkillCooldown();
    float GetDodgeSkillCooldownRemaining();

    void OnHitEnter();
    void OnHitUpdate();
    void OnHitExit();


    void OnSlideEnter();
    void OnSlideUpdate();
    void OnSlideExit();

    void OnSlideAttackEnter();
    void OnSlideAttackUpdate();
    void OnSlideAttackExit();
}
