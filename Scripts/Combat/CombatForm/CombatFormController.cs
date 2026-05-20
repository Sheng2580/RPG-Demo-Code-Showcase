using UnityEngine;

public class CombatFormController : MonoBehaviour
{
    [Header("References")]
    // 玩家控制器，作为战斗形态系统的宿主。
    [SerializeField] private PlayerContorller player;

    // 玩家战斗属性，负责能量、攻击力、暴击和后续 Buff。
    [SerializeField] private PlayerCombatStats stats;

    // 武器或形态对象的挂载点。
    [SerializeField] private Transform weaponSocket;

    [Header("Forms")]
    // 普通形态配置。
    [SerializeField] private CombatFormData normalFormData;

    // 武器 A 形态配置。
    [SerializeField] private CombatFormData weaponAFormData;

    // 武器 B 形态配置。
    [SerializeField] private CombatFormData weaponBFormData;

    // 变身形态配置。
    [SerializeField] private CombatFormData transformFormData;

    [Header("Energy")]
    // 普通形态命中一个目标时增加的能量。
    [SerializeField] private float normalHitEnergyGain = 10f;

    // 变身形态每秒消耗的能量。能量耗尽时自动回到普通形态。
    [SerializeField] private float transformEnergyDrainPerSecond = 10f;

    // 是否输出能量获取和消耗日志。
    [SerializeField] private bool isDebugEnergy = true;

    // 当前形态逻辑对象。
    private ICombatForm currentForm;

    // 当前形态配置。
    private CombatFormData currentFormData;

    // 当前形态使用的旧 CombatData。
    private CombatData currentCombatData;

    // 当前实例化出的武器或形态对象。
    private GameObject currentWeaponObject;

    // 当前武器对象上的武器逻辑组件。
    private IWeaponCombat currentWeaponCombat;

    // 当前形态剩余持续时间。
    private float formTimer;

    // 当前连招段索引，由具体 CombatForm 使用。
    private int currentCombatIndex;

    // 当前形态上下文。
    private CombatContext context;

    // 当前宿主玩家。
    public PlayerContorller Player => player;

    // 当前正在运行的形态逻辑。
    public ICombatForm CurrentForm => currentForm;

    // 当前形态使用的旧 CombatData。
    public CombatData CurrentCombatData => currentCombatData;

    // 当前武器逻辑组件。
    public IWeaponCombat CurrentWeaponCombat => currentWeaponCombat;

    // 当前武器或形态对象的 Transform。
    public Transform CurrentWeaponTransform => currentWeaponObject != null ? currentWeaponObject.transform : null;

    // 当前玩家战斗属性组件。
    public PlayerCombatStats Stats => stats;

    // 当前能量值。
    public float CurrentEnergy => stats != null ? stats.CurrentEnergy : 0f;

    // 最大能量值。
    public float MaxEnergy => stats != null ? stats.MaxEnergy : 0f;

    // 当前是否输出能量调试日志。
    public bool IsDebugEnergy => isDebugEnergy;

    public float TransformEnergyDrainMultiplier { get; set; } = 1f;

    // 当前连招段索引。
    public int CurrentCombatIndex
    {
        get => currentCombatIndex;
        set => currentCombatIndex = Mathf.Max(0, value);
    }

    private void Awake()
    {
        if (player == null)
        {
            player = GetComponent<PlayerContorller>();
        }

        if (stats == null)
        {
            stats = GetComponent<PlayerCombatStats>();
        }

        if (stats == null)
        {
            stats = gameObject.AddComponent<PlayerCombatStats>();
        }

        stats.IsDebugEnergy = isDebugEnergy;
    }

    private void Start()
    {
        InitializeDefaultForm();
    }

    private void Update()
    {
        currentForm?.TickForm();
        TickTransformEnergyDrain();
        TickFormDuration();
    }

    public bool InitializeDefaultForm()
    {
        if (normalFormData == null)
        {
            return false;
        }

        if (currentFormData == normalFormData)
        {
            return true;
        }

        SwitchForm(normalFormData, false);
        return true;
    }

    public bool TryLightAttack()
    {
        return currentForm != null && currentForm.TryLightAttack();
    }

    public bool TryHeavyAttack()
    {
        return currentForm != null && currentForm.TryHeavyAttack();
    }

    public bool TryUseSkill(SkillSlot slot)
    {
        return currentForm != null && currentForm.TryUseSkill(slot);
    }

    public bool TryConsumeDodgeSkillCooldown()
    {
        return currentForm != null && currentForm.TryConsumeDodgeSkillCooldown();
    }

    public float GetDodgeSkillCooldownRemaining()
    {
        return currentForm != null ? currentForm.GetDodgeSkillCooldownRemaining() : 0f;
    }

    public void AddEnergy(float value)
    {
        stats?.AddEnergy(value);
    }

    public void AddEnergyFromNormalHit()
    {
        if (isDebugEnergy)
        {
            Debug.Log($"[Energy] Normal hit energy check. CurrentForm={currentForm?.FormType.ToString() ?? "None"}");
        }

        if (currentForm == null || currentForm.FormType != CombatFormType.Normal)
        {
            if (isDebugEnergy)
            {
                Debug.Log("[Energy] Skip normal hit energy gain because current form is not Normal.");
            }

            return;
        }

        AddEnergy(normalHitEnergyGain);
    }

    public void ResetCombatIndex()
    {
        currentCombatIndex = 0;
    }

    public void AdvanceNormalCombatIndex()
    {
        if (currentCombatData == null || currentCombatData.normalAttackDates == null || currentCombatData.normalAttackDates.Count == 0)
        {
            currentCombatIndex = 0;
            return;
        }

        currentCombatIndex = currentCombatIndex < currentCombatData.normalAttackDates.Count - 1 ? currentCombatIndex + 1 : 0;
    }

    public bool TrySwitchToWeaponA()
    {
        return TrySwitchForm(weaponAFormData);
    }

    public bool TrySwitchToWeaponB()
    {
        return TrySwitchForm(weaponBFormData);
    }

    public bool TrySwitchToTransform()
    {
        if (!CanSwitchToTransform())
        {
            return false;
        }

        SwitchForm(transformFormData, true);
        return true;
    }

    public bool CanSwitchToTransform()
    {
        if (transformFormData == null || stats == null)
        {
            return false;
        }

        float requiredEnergy = Mathf.Max(1f, stats.MaxEnergy);
        if (stats.CurrentEnergy < requiredEnergy - 0.001f)
        {
            if (isDebugEnergy)
            {
                Debug.Log($"[Energy] Transform requires full energy. Required={requiredEnergy}, Current={stats.CurrentEnergy}");
            }

            return false;
        }

        return true;
    }

    public void ToggleTransformForm()
    {
        if (player != null && player.currentState == PlayerStateType.CombatAttack)
        {
            return;
        }

        if (currentForm != null && currentForm.FormType == CombatFormType.Transform)
        {
            return;
        }

        TrySwitchToTransform();
    }

    public void ReturnToNormal()
    {
        if (normalFormData != null)
        {
            SwitchForm(normalFormData, false);
        }
    }

    public bool TrySwitchForm(CombatFormData formData)
    {
        if (formData == null)
        {
            return false;
        }

        if (player != null && player.currentState == PlayerStateType.CombatAttack)
        {
            return false;
        }

        if (stats == null)
        {
            if (isDebugEnergy)
            {
                Debug.LogWarning("[Energy] Cannot switch form because PlayerCombatStats is missing.");
            }

            return false;
        }

        if (!stats.TryConsumeEnergy(formData.energyCost))
        {
            if (isDebugEnergy)
            {
                Debug.Log($"[Energy] Not enough energy to switch form. Required={formData.energyCost}, Current={stats.CurrentEnergy}");
            }

            return false;
        }

        SwitchForm(formData, true);
        return true;
    }

    public void SwitchForm(CombatFormData formData, bool resetTimer)
    {
        if (formData == null || player == null)
        {
            return;
        }

        currentForm?.ExitForm();
        UnequipCurrentWeapon();

        currentFormData = formData;
        currentCombatData = formData.combatData;
        currentCombatIndex = 0;
        formTimer = resetTimer ? formData.duration : 0f;

        ApplyAnimator(formData);
        EquipWeapon(formData);

        context = new CombatContext(player, stats);
        currentForm = CreateForm(formData.formType);
        currentForm.EnterForm(context, formData);
        RefreshCurrentBodyState();
    }

    private void RefreshCurrentBodyState()
    {
        if (player == null || currentForm == null)
        {
            return;
        }

        switch (player.currentState)
        {
            case PlayerStateType.Idle:
                currentForm.OnIdleEnter();
                break;
            case PlayerStateType.Move:
                currentForm.OnMoveEnter();
                break;
        }
    }

    private void TickFormDuration()
    {
        if (currentFormData == null || currentFormData.duration <= 0f || !currentFormData.returnToNormalWhenEnd)
        {
            return;
        }

        if (currentFormData.formType == CombatFormType.Normal)
        {
            return;
        }

        if (currentFormData.formType == CombatFormType.Transform)
        {
            return;
        }

        formTimer -= Time.deltaTime;
        if (formTimer <= 0f)
        {
            ReturnToNormal();
        }
    }

    private void TickTransformEnergyDrain()
    {
        if (currentForm == null ||
            currentForm.FormType != CombatFormType.Transform ||
            stats == null)
        {
            return;
        }

        float drain = Mathf.Max(0f, transformEnergyDrainPerSecond) * Mathf.Max(0f, TransformEnergyDrainMultiplier) * Time.deltaTime;
        if (drain > 0f)
        {
            stats.AddEnergy(-drain, false);
        }

        if (stats.CurrentEnergy <= 0.001f)
        {
            ReturnToNormal();
            if (player != null && player.currentState == PlayerStateType.Skill)
            {
                player.ChangeState(PlayerStateType.Idle);
            }
        }
    }

    private ICombatForm CreateForm(CombatFormType formType)
    {
        switch (formType)
        {
            case CombatFormType.WeaponA:
                return new WeaponACombatForm();
            case CombatFormType.WeaponB:
                return new WeaponBCombatForm();
            case CombatFormType.Transform:
                return new TransformCombatForm();
            default:
                return new NormalCombatForm();
        }
    }

    private void ApplyAnimator(CombatFormData formData)
    {
        if (formData.animatorController == null || player.model == null || player.model.animator == null)
        {
            return;
        }

        player.model.animator.runtimeAnimatorController = formData.animatorController;
    }

    private void EquipWeapon(CombatFormData formData)
    {
        if (formData.weaponPrefab == null || weaponSocket == null)
        {
            currentWeaponCombat = null;
            return;
        }

        currentWeaponObject = Instantiate(formData.weaponPrefab, weaponSocket);
        currentWeaponObject.transform.localPosition = Vector3.zero;
        currentWeaponObject.transform.localRotation = Quaternion.identity;

        currentWeaponCombat = FindWeaponCombat(currentWeaponObject);
        currentWeaponCombat?.Equip(player, this);
    }

    private IWeaponCombat FindWeaponCombat(GameObject weaponObject)
    {
        if (weaponObject == null)
        {
            return null;
        }

        MonoBehaviour[] behaviours = weaponObject.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IWeaponCombat weaponCombat)
            {
                return weaponCombat;
            }
        }

        return null;
    }

    private void UnequipCurrentWeapon()
    {
        currentWeaponCombat?.Unequip();
        currentWeaponCombat = null;

        if (currentWeaponObject != null)
        {
            Destroy(currentWeaponObject);
            currentWeaponObject = null;
        }
    }
}
