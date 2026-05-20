using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class PlayerModel : CharacterModelBase
{
    private static readonly int OutlineGlowColorId = Shader.PropertyToID("_OutlineGlowColor");
    private static readonly int OutlineGlowIntensityId = Shader.PropertyToID("_OutlineGlowIntensity");
    private static readonly int OutlineGlowWidthId = Shader.PropertyToID("_OutlineGlowWidth");
    private static readonly int OutlineGlowOpacityId = Shader.PropertyToID("_OutlineGlowOpacity");

    public List<GameObject> npcs = new List<GameObject>();
    public GameObject lookAtNpc;
    public GameObject currentNpc;
    public float detectionRadius;
    public LayerMask Layer;
    private Vector3 _lookAtTargetPos;
    private float _lookAtWeight;
    private GameObject _lastLookAtNpc;
    public float turnSpeed = 7f;
    public bool isCreate = false;
    [HideInInspector]
    public FreeLookLeftShoulderFinal freeLookCamera;
    [Header("材质控制")]
    // 需要统一控制外描边发光的材质名称，名称会自动忽略 Unity 生成的“(Instance)”后缀。
    [SerializeField] private List<string> outlineGlowMaterialNames = new List<string>
    {
        "日光",
        "日影",
        "躯衣2",
        "镜片+",
        "髮"
    };

    // 运行时缓存到的外描边发光材质实例。
    private readonly List<Material> outlineGlowMaterials = new List<Material>();

    // 是否输出外描边发光调试日志。
    [SerializeField] private bool isDebugOutlineGlow = true;

    // 开启外描边发光时写入的 Glow Width，宽度为 0 时即使颜色和强度正确也看不到外发光。
    [SerializeField] private float outlineGlowWidthWhenEnabled = 2.4f;

    // 开启外描边发光时写入的 Glow Opacity。
    [SerializeField] private float outlineGlowOpacityWhenEnabled = 0.82f;


    [Header("配件")] public List<GameObject> transfigurationObjects;
    public GameObject wing;
    
    

    [Header("召唤物")]
    [SerializeField] private string motorcyclePoolName = "Summon/moT";

    //大范围摄像头用于变身状态大招 
    [SerializeField]
    private CinemachineVirtualCamera cinemachineVirtualCamera;

    
        
        

    #region 动画事件
    public void transfiguration()
    {
        SetTransfigurationObjectsActive(true);
        SpawnTransfigurationMotorcycle();
    }

    public void CloseTransfiguration()
    {
        SetTransfigurationObjectsActive(false);
    }

    private void SetTransfigurationObjectsActive(bool isActive)
    {
        if (transfigurationObjects == null)
        {
            return;
        }

        for (int i = 0; i < transfigurationObjects.Count; i++)
        {
            if (transfigurationObjects[i] != null)
            {
                transfigurationObjects[i].SetActive(isActive);
            }
        }
    }

    private void SpawnTransfigurationMotorcycle()
    {
        PlayerContorller player = GetComponentInParent<PlayerContorller>();
        if (player == null)
        {
            return;
        }

        Motorcycle.SpawnOrbit(player, motorcyclePoolName);
    }
    
    #endregion
    
    
    
    protected override void Awake()
    {
        base.Awake();
        CacheOutlineGlowMaterials();
    }

    private void OnEnable()
    {
        EventCenter.Instance.AddEventListener<bool>(GameEvent.设置玩家摄像机, SetFreeLookCamera);
        EventCenter.Instance.AddEventListener<bool>(GameEvent.设置玩家输入状态, SetPlayerInputState);
        EventCenter.Instance.AddEventListener(GameEvent.玩家检测Npc, ForceRefreshClosestNpc);
        EventCenter.Instance.AddEventListener<OutlineGlowEventData>(GameEvent.外描边发光, SetOutlineGlow);
    }

    private void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener<OutlineGlowEventData>(GameEvent.外描边发光, SetOutlineGlow);
        EventCenter.Instance.RemoveEventListener(GameEvent.玩家检测Npc, ForceRefreshClosestNpc);
        EventCenter.Instance.RemoveEventListener<bool>(GameEvent.设置玩家输入状态, SetPlayerInputState);
        EventCenter.Instance.RemoveEventListener<bool>(GameEvent.设置玩家摄像机, SetFreeLookCamera);
    }

    private void Start()
    {
        freeLookCamera = transform.GetComponent<FreeLookLeftShoulderFinal>();
        SetPlayerInputState(GameInputManger.Instance.IsPlayerInputEnabled);
    }

    private void Update()
    {
        RefreshNpcList();
        FindClosestNpc(false);
    }

    private void ForceRefreshClosestNpc()
    {
        RefreshNpcList();
        FindClosestNpc(true);
    }

    private void SetFreeLookCamera(bool isFreeLookCamera)
    {
        if (isFreeLookCamera)
        {
            freeLookCamera.freeLookCam.gameObject.SetActive(true);
        }
        else
        {
            freeLookCamera.freeLookCam.gameObject.SetActive(false);
        }
    }

    private void SetPlayerInputState(bool isInputEnabled)
    {
        if (freeLookCamera == null)
        {
            return;
        }

        freeLookCamera.SetLookInputEnabled(isInputEnabled);
    }

    private void CacheOutlineGlowMaterials()
    {
        outlineGlowMaterials.Clear();

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        List<Material> fallbackGlowMaterials = new List<Material>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] rendererMaterials = renderers[i].materials;
            for (int j = 0; j < rendererMaterials.Length; j++)
            {
                Material rendererMaterial = rendererMaterials[j];
                if (rendererMaterial != null && rendererMaterial.HasProperty(OutlineGlowIntensityId))
                {
                    fallbackGlowMaterials.Add(rendererMaterial);
                }

                if (!IsOutlineGlowMaterial(rendererMaterial))
                {
                    continue;
                }

                outlineGlowMaterials.Add(rendererMaterial);
            }
        }

        if (outlineGlowMaterials.Count == 0)
        {
            outlineGlowMaterials.AddRange(fallbackGlowMaterials);
        }

        if (isDebugOutlineGlow)
        {
            Debug.Log($"[OutlineGlow] 缓存材质数量={outlineGlowMaterials.Count}");
        }

        SetOutlineGlow(new OutlineGlowEventData(false, Color.white));
    }

    private bool IsOutlineGlowMaterial(Material rendererMaterial)
    {
        if (rendererMaterial == null || !rendererMaterial.HasProperty(OutlineGlowIntensityId))
        {
            return false;
        }

        if (outlineGlowMaterialNames == null || outlineGlowMaterialNames.Count == 0)
        {
            return true;
        }

        string materialName = rendererMaterial.name.Replace(" (Instance)", string.Empty);
        for (int i = 0; i < outlineGlowMaterialNames.Count; i++)
        {
            if (materialName == outlineGlowMaterialNames[i])
            {
                return true;
            }
        }

        return false;
    }

    private void SetOutlineGlow(OutlineGlowEventData eventData)
    {
        if (outlineGlowMaterials.Count == 0)
        {
            CacheOutlineGlowMaterials();
        }

        float glowIntensity = eventData.isEnable ? 6f : 0f;
        float glowWidth = eventData.isEnable ? outlineGlowWidthWhenEnabled : 0f;
        float glowOpacity = eventData.isEnable ? outlineGlowOpacityWhenEnabled : 0f;
        Color hdrGlowColor = eventData.color;

        if (isDebugOutlineGlow)
        {
            Debug.Log($"[OutlineGlow] 设置颜色={hdrGlowColor}, 强度={glowIntensity}, 材质数量={outlineGlowMaterials.Count}");
        }

        for (int i = 0; i < outlineGlowMaterials.Count; i++)
        {
            Material glowMaterial = outlineGlowMaterials[i];
            if (glowMaterial == null)
            {
                continue;
            }

            if (glowMaterial.HasProperty(OutlineGlowColorId))
            {
                glowMaterial.SetColor(OutlineGlowColorId, hdrGlowColor);
                if (isDebugOutlineGlow)
                {
                    Debug.Log($"[OutlineGlow] {glowMaterial.name} 写入后颜色={glowMaterial.GetColor(OutlineGlowColorId)}");
                }
            }
            else if (isDebugOutlineGlow)
            {
                Debug.LogWarning($"[OutlineGlow] 材质 {glowMaterial.name} 没有 _OutlineGlowColor 属性。");
            }

            if (glowMaterial.HasProperty(OutlineGlowIntensityId))
            {
                glowMaterial.SetFloat(OutlineGlowIntensityId, glowIntensity);
            }

            if (glowMaterial.HasProperty(OutlineGlowWidthId))
            {
                glowMaterial.SetFloat(OutlineGlowWidthId, glowWidth);
            }

            if (glowMaterial.HasProperty(OutlineGlowOpacityId))
            {
                glowMaterial.SetFloat(OutlineGlowOpacityId, glowOpacity);
            }

            if (isDebugOutlineGlow)
            {
                float currentIntensity = glowMaterial.HasProperty(OutlineGlowIntensityId) ? glowMaterial.GetFloat(OutlineGlowIntensityId) : -1f;
                float currentWidth = glowMaterial.HasProperty(OutlineGlowWidthId) ? glowMaterial.GetFloat(OutlineGlowWidthId) : -1f;
                float currentOpacity = glowMaterial.HasProperty(OutlineGlowOpacityId) ? glowMaterial.GetFloat(OutlineGlowOpacityId) : -1f;
                Debug.Log($"[OutlineGlow] {glowMaterial.name} 参数确认 intensity={currentIntensity}, width={currentWidth}, opacity={currentOpacity}");
            }
        }
    }

   
    private void RefreshNpcList()
    {
        npcs.Clear();
        HashSet<GameObject> uniqueTargets = new HashSet<GameObject>();
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, Layer);
        foreach (Collider hit in hits)
        {
            if (hit == null)
            {
                continue;
            }
            if (TryGetInteractableTarget(hit, out GameObject interactableTarget) && uniqueTargets.Add(interactableTarget))
            {
                npcs.Add(interactableTarget);
            }
        }
    }

    private void FindClosestNpc(bool forceRefresh)
    {
        lookAtNpc = null;
        float minDis = Mathf.Infinity;

        foreach (var npc in npcs)
        {
            Vector3 dir = npc.transform.position - transform.position;
            float angle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);

            if (Mathf.Abs(angle) < 60f)
            {
                float dis = Vector3.Distance(transform.position, npc.transform.position);
                if (dis < minDis)
                {
                    minDis = dis;
                    lookAtNpc = npc;
                }
            }
        }

        if (lookAtNpc != null)
        {
            if (currentNpc != lookAtNpc || forceRefresh)
            {
                currentNpc = lookAtNpc;
                RefreshInteractionPanel();
            }
        }
        else if (currentNpc != null)
        {
            currentNpc = null;
            if (isCreate)
            {
                isCreate = false;
                UIManager.Instance.ClosePanel<InteractionPanel>();
            }
        }
    }

    private void RefreshInteractionPanel()
    {
        if (currentNpc == null)
        {
            return;
        }

        IInteractable interactable = GetInteractable(currentNpc);
        if (interactable == null || interactable.InteractionActions == null || interactable.InteractionActions.Count == 0)
        {
            if (isCreate)
            {
                isCreate = false;
                UIManager.Instance.ClosePanel<InteractionPanel>();
            }

            return;
        }

        isCreate = true;
        UIManager.Instance.OpenPanelAsync<InteractionPanel>(UILayer.Dynamic, panel =>
        {
            if (panel != null)
            {
                panel.RefreshInteractionItems(interactable.InteractionActions);
            }
        });
    }
    
    
    /// <summary>
    /// 攻击检测
    /// <param name="hit"></param>
    /// <param name="interactableTarget"></param>
    /// <returns></returns>
    private bool TryGetInteractableTarget(Collider hit, out GameObject interactableTarget)
    {
        interactableTarget = null;
        if (hit == null)
        {
            return false;
        }

        MonoBehaviour interactableBehaviour = GetInteractableBehaviour(hit.gameObject);
        if (interactableBehaviour == null)
        {
            return false;
        }

        interactableTarget = interactableBehaviour.gameObject;
        return true;
    }

    private IInteractable GetInteractable(GameObject target)
    {
        return GetInteractableBehaviour(target) as IInteractable;
    }
    
    private MonoBehaviour GetInteractableBehaviour(GameObject target)
    {
        if (target == null)
        {
            return null;
        }

        MonoBehaviour[] behaviours = target.GetComponentsInParent<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IInteractable)
            {
                return behaviours[i];
            }
        }
        
        return null;
    }

    
    /// <summary>
    /// 转头
    /// </summary>
    /// <param name="layerIndex"></param>
    private void OnAnimatorIK(int layerIndex)
    {
        if (lookAtNpc == null && _lookAtWeight <= 0.001f)
        {
            _lastLookAtNpc = null;
            return;
        }

        if (animator == null || layerIndex != 0) return;

        if (lookAtNpc != null && _lastLookAtNpc == null)
        {
            _lookAtTargetPos = lookAtNpc.transform.position + Vector3.up * 1f;
        }

        _lastLookAtNpc = lookAtNpc;
        if (lookAtNpc != null)
        {
            Vector3 target = lookAtNpc.transform.position + Vector3.up * 1f;
            _lookAtTargetPos = Vector3.Lerp(_lookAtTargetPos, target, Time.deltaTime * turnSpeed);
            _lookAtWeight = Mathf.Lerp(_lookAtWeight, 1f, Time.deltaTime * turnSpeed);
        }
        else
        {
            _lookAtWeight = Mathf.Lerp(_lookAtWeight, 0f, Time.deltaTime * turnSpeed * 2f);
        }

        if (_lookAtWeight > 0.01f)
        {
            animator.SetLookAtPosition(_lookAtTargetPos);
            animator.SetLookAtWeight(_lookAtWeight, 0.1f, 1f, 0.4f, 0.4f);
        }
        else
        {
            animator.SetLookAtWeight(0);
        }
    }

    public void SitUp()
    {
        animator.SetBool("isUp", true);
    }

    /// <summary>
    /// npc检测范围
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
