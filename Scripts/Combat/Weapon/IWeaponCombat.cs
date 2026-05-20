public interface IWeaponCombat
{
    void Equip(PlayerContorller owner, CombatFormController formController);
    void Unequip();
    void Tick();
    void HandleLightAttack();
    void HandleHeavyAttack();
    void DetectHit(WeaponAttackData attackData, WeaponTriggerHit triggerHit);
}


