using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGhost : MonoBehaviour
{
    private enum GhostType
    {
        Fresnel,
        Dodge
    }

    private class GhostMaterialInfo
    {
        public Material material;
        public Color baseColor;
        public float startRimAlpha;
    }

    [Header("通用残影参数")]
    [SerializeField] private bool enableGhost = true;
    [SerializeField] private bool includeMeshRendererGhost = true;
    [SerializeField] private string[] ignoredMaterialNames = { "眉毛上 1" };

    [Header("菲涅尔残影")]
    [SerializeField] private Color fresnelBaseColor = new Color(0.45f, 0.85f, 1f, 0.45f);
    [SerializeField] private float fresnelDuration = 0.45f;
    [SerializeField] private bool enableFresnelEmission = true;
    [SerializeField] private Color fresnelRimColor = new Color(0.45f, 0.85f, 1f, 1f);
    [SerializeField, Range(0f, 10f)] private float fresnelEmissionIntensity = 2.5f;
    [SerializeField, Range(0.2f, 8f)] private float fresnelPower = 2f;
    [SerializeField, Range(0f, 10f)] private float fresnelIntensity = 3f;
    [SerializeField, Range(0f, 1f)] private float fresnelAlpha = 0.75f;

    [Header("闪避残影")]
    [SerializeField] private float dodgeDuration = 0.35f;
    [SerializeField, Range(0f, 1f)] private float dodgeAlpha = 0.35f;

    private void OnEnable()
    {
        // Branch attacks pass a color so the same Fresnel ghost can be reused by different combo data.
        EventCenter.Instance.AddEventListener<Color>(GameEvent.生成残影, CreateFresnelGhost);
        EventCenter.Instance.AddEventListener(GameEvent.生成闪避残影, CreateDodgeGhost);
    }

    private void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener(GameEvent.生成闪避残影, CreateDodgeGhost);
        EventCenter.Instance.RemoveEventListener<Color>(GameEvent.生成残影, CreateFresnelGhost);
    }

    private void CreateFresnelGhost(Color ghostColor)
    {
        CreateGhost(GhostType.Fresnel, ghostColor);
    }

    private void CreateDodgeGhost()
    {
        CreateGhost(GhostType.Dodge, fresnelBaseColor);
    }

    /// <summary>
    /// 记录当前所有可见 Renderer 的姿势，生成一份静态 Mesh 残影。
    /// “生成残影”使用菲涅尔材质，“生成闪避残影”使用原材质透明副本。
    /// </summary>
    private void CreateGhost(GhostType ghostType, Color ghostColor)
    {
        if (!enableGhost)
        {
            return;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
        {
            Debug.LogWarning("[PlayerGhost] CreateGhost skipped: no renderer found.");
            return;
        }

        GameObject ghostRoot = new GameObject($"{name}_{ghostType}Ghost");
        List<GhostMaterialInfo> ghostMaterials = new List<GhostMaterialInfo>();
        List<Mesh> ghostMeshes = new List<Mesh>();
        List<Material> temporaryMaterials = new List<Material>();
        Material depthOnlyMaterial = CreateDepthOnlyMaterial();

        foreach (Renderer rendererItem in renderers)
        {
            if (rendererItem == null || !rendererItem.enabled)
            {
                continue;
            }

            if (rendererItem is SkinnedMeshRenderer skinnedMeshRenderer)
            {
                CreateSkinnedMeshGhost(skinnedMeshRenderer, ghostRoot.transform, ghostMaterials, ghostMeshes, temporaryMaterials, depthOnlyMaterial, ghostType, ghostColor);
            }
            else if (includeMeshRendererGhost && rendererItem is MeshRenderer meshRenderer)
            {
                CreateMeshGhost(meshRenderer, ghostRoot.transform, ghostMaterials, temporaryMaterials, depthOnlyMaterial, ghostType, ghostColor);
            }
        }

        if (ghostMaterials.Count == 0)
        {
            Destroy(depthOnlyMaterial);
            Destroy(ghostRoot);
            return;
        }

        if (depthOnlyMaterial != null)
        {
            temporaryMaterials.Add(depthOnlyMaterial);
        }

        float duration = ghostType == GhostType.Fresnel ? fresnelDuration : dodgeDuration;
        StartCoroutine(FadeAndDestroyGhost(ghostRoot, ghostMaterials, ghostMeshes, temporaryMaterials, duration));
    }

    /// <summary>
    /// 把骨骼动画当前姿势烘焙成普通 Mesh，残影销毁前不会再受动画影响。
    /// </summary>
    private void CreateSkinnedMeshGhost(SkinnedMeshRenderer sourceRenderer, Transform ghostRoot, List<GhostMaterialInfo> ghostMaterials, List<Mesh> ghostMeshes, List<Material> temporaryMaterials, Material depthOnlyMaterial, GhostType ghostType, Color ghostColor)
    {
        Mesh bakedMesh = new Mesh();
        sourceRenderer.BakeMesh(bakedMesh);
        ghostMeshes.Add(bakedMesh);

        CreateDepthOnlyGhost(sourceRenderer.transform, bakedMesh, ghostRoot, depthOnlyMaterial);
        CreateVisibleGhostPart(sourceRenderer.transform, bakedMesh, sourceRenderer.sharedMaterials, ghostRoot, ghostMaterials, temporaryMaterials, depthOnlyMaterial, ghostType, ghostColor);
    }

    /// <summary>
    /// 复制普通 MeshRenderer，主要用于武器、挂件等非骨骼模型。
    /// </summary>
    private void CreateMeshGhost(MeshRenderer sourceRenderer, Transform ghostRoot, List<GhostMaterialInfo> ghostMaterials, List<Material> temporaryMaterials, Material depthOnlyMaterial, GhostType ghostType, Color ghostColor)
    {
        MeshFilter sourceFilter = sourceRenderer.GetComponent<MeshFilter>();
        if (sourceFilter == null || sourceFilter.sharedMesh == null)
        {
            return;
        }

        CreateDepthOnlyGhost(sourceRenderer.transform, sourceFilter.sharedMesh, ghostRoot, depthOnlyMaterial);
        CreateVisibleGhostPart(sourceRenderer.transform, sourceFilter.sharedMesh, sourceRenderer.sharedMaterials, ghostRoot, ghostMaterials, temporaryMaterials, depthOnlyMaterial, ghostType, ghostColor);
    }

    private void CreateVisibleGhostPart(Transform sourceTransform, Mesh mesh, Material[] sourceMaterials, Transform ghostRoot, List<GhostMaterialInfo> ghostMaterials, List<Material> temporaryMaterials, Material hiddenMaterial, GhostType ghostType, Color ghostColor)
    {
        GameObject ghostPart = new GameObject($"{sourceTransform.name}_{ghostType}Ghost");
        ghostPart.transform.SetParent(ghostRoot);
        ghostPart.transform.SetPositionAndRotation(sourceTransform.position, sourceTransform.rotation);
        ghostPart.transform.localScale = sourceTransform.lossyScale;

        MeshFilter meshFilter = ghostPart.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        MeshRenderer meshRenderer = ghostPart.AddComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.sharedMaterials = CreateGhostMaterials(sourceMaterials, ghostMaterials, temporaryMaterials, hiddenMaterial, ghostType, ghostColor);
    }

    /// <summary>
    /// 先生成不可见深度副本，减少透明残影内部层叠穿透。
    /// </summary>
    private void CreateDepthOnlyGhost(Transform sourceTransform, Mesh mesh, Transform ghostRoot, Material depthOnlyMaterial)
    {
        if (sourceTransform == null || mesh == null || depthOnlyMaterial == null)
        {
            return;
        }

        GameObject depthPart = new GameObject($"{sourceTransform.name}_GhostDepth");
        depthPart.transform.SetParent(ghostRoot);
        depthPart.transform.SetPositionAndRotation(sourceTransform.position, sourceTransform.rotation);
        depthPart.transform.localScale = sourceTransform.lossyScale;

        MeshFilter meshFilter = depthPart.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        MeshRenderer meshRenderer = depthPart.AddComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.sharedMaterial = depthOnlyMaterial;
    }

    private Material[] CreateGhostMaterials(Material[] sourceMaterials, List<GhostMaterialInfo> ghostMaterials, List<Material> temporaryMaterials, Material hiddenMaterial, GhostType ghostType, Color ghostColor)
    {
        int materialCount = Mathf.Max(1, sourceMaterials != null ? sourceMaterials.Length : 0);
        Material[] materials = new Material[materialCount];

        for (int i = 0; i < materialCount; i++)
        {
            Material sourceMaterial = sourceMaterials != null && i < sourceMaterials.Length ? sourceMaterials[i] : null;
            if (IsIgnoredMaterial(sourceMaterial))
            {
                materials[i] = hiddenMaterial != null ? hiddenMaterial : CreateTransparentHiddenMaterial(temporaryMaterials);
                continue;
            }

            GhostMaterialInfo materialInfo = ghostType == GhostType.Fresnel
                ? CreateFresnelMaterial(ghostColor)
                : CreateDodgeMaterial(sourceMaterial);

            materials[i] = materialInfo.material;
            ghostMaterials.Add(materialInfo);
            temporaryMaterials.Add(materialInfo.material);
        }

        return materials;
    }

    private bool IsIgnoredMaterial(Material material)
    {
        if (material == null || ignoredMaterialNames == null)
        {
            return false;
        }

        string materialName = material.name.Replace(" (Instance)", string.Empty);
        for (int i = 0; i < ignoredMaterialNames.Length; i++)
        {
            if (materialName == ignoredMaterialNames[i])
            {
                return true;
            }
        }

        return false;
    }

    private GhostMaterialInfo CreateFresnelMaterial(Color ghostColor)
    {
        Shader shader = Shader.Find("Custom/AfterimageFresnel");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.name = "Ghost_Fresnel_Material";
        // Keep alpha controlled by ghost settings, but take hue from the branch combat data.
        Color baseColor = ghostColor;
        baseColor.a = fresnelAlpha;
        Color rimColor = ghostColor;
        rimColor.a = 1f;

        SetupTransparentMaterial(material, baseColor);
        SetupEmissionMaterial(material, rimColor);
        SetupFresnelMaterial(material, rimColor);

        return new GhostMaterialInfo
        {
            material = material,
            baseColor = baseColor,
            startRimAlpha = fresnelAlpha
        };
    }

    private GhostMaterialInfo CreateDodgeMaterial(Material sourceMaterial)
    {
        // 闪避残影不能直接复制角色 Toon shader：原 shader 主 Pass 是不透明混合，
        // 还包含 OutlineGlow Pass。这里改用透明 Unlit，再复制原贴图和颜色。
        Material material = new Material(GetDefaultTransparentShader());
        material.name = "Ghost_Dodge_Material";

        CopyBaseTexture(sourceMaterial, material);

        Color baseColor = ReadMaterialColor(sourceMaterial);
        baseColor.a *= dodgeAlpha;
        SetupTransparentMaterial(material, baseColor);
        DisableGlowMaterial(material);

        return new GhostMaterialInfo
        {
            material = material,
            baseColor = baseColor,
            startRimAlpha = 0f
        };
    }

    private Material CreateTransparentHiddenMaterial(List<Material> temporaryMaterials)
    {
        Material material = new Material(GetDefaultTransparentShader());
        material.name = "Ghost_Hidden_Material";
        SetupTransparentMaterial(material, new Color(0f, 0f, 0f, 0f));
        temporaryMaterials.Add(material);
        return material;
    }

    private Material CreateDepthOnlyMaterial()
    {
        Shader shader = Shader.Find("Custom/AfterimageDepthOnly");
        if (shader == null)
        {
            Debug.LogWarning("[PlayerGhost] AfterimageDepthOnly shader not found, inner renderer hiding may be incomplete.");
            return null;
        }

        Material material = new Material(shader);
        material.name = "Ghost_DepthOnly_Material";
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent - 10;
        return material;
    }

    private Shader GetDefaultTransparentShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        return shader;
    }

    private Color ReadMaterialColor(Material material)
    {
        if (material != null && material.HasProperty("_BaseColor"))
        {
            return material.GetColor("_BaseColor");
        }

        if (material != null && material.HasProperty("_Color"))
        {
            return material.GetColor("_Color");
        }

        return Color.white;
    }

    private void CopyBaseTexture(Material sourceMaterial, Material targetMaterial)
    {
        if (sourceMaterial == null || targetMaterial == null)
        {
            return;
        }

        Texture texture = null;
        Vector2 textureScale = Vector2.one;
        Vector2 textureOffset = Vector2.zero;

        if (sourceMaterial.HasProperty("_BaseMap"))
        {
            texture = sourceMaterial.GetTexture("_BaseMap");
            textureScale = sourceMaterial.GetTextureScale("_BaseMap");
            textureOffset = sourceMaterial.GetTextureOffset("_BaseMap");
        }
        else if (sourceMaterial.HasProperty("_MainTex"))
        {
            texture = sourceMaterial.GetTexture("_MainTex");
            textureScale = sourceMaterial.GetTextureScale("_MainTex");
            textureOffset = sourceMaterial.GetTextureOffset("_MainTex");
        }

        if (texture == null)
        {
            return;
        }

        if (targetMaterial.HasProperty("_BaseMap"))
        {
            targetMaterial.SetTexture("_BaseMap", texture);
            targetMaterial.SetTextureScale("_BaseMap", textureScale);
            targetMaterial.SetTextureOffset("_BaseMap", textureOffset);
        }

        if (targetMaterial.HasProperty("_MainTex"))
        {
            targetMaterial.SetTexture("_MainTex", texture);
            targetMaterial.SetTextureScale("_MainTex", textureScale);
            targetMaterial.SetTextureOffset("_MainTex", textureOffset);
        }
    }

    /// <summary>
    /// 闪避残影只保留原色透明，不继承角色材质里的发光或边缘光。
    /// </summary>
    private void DisableGlowMaterial(Material material)
    {
        if (material == null)
        {
            return;
        }

        material.DisableKeyword("_EMISSION");

        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", Color.black);
        }

        if (material.HasProperty("_EmissionIntensity"))
        {
            material.SetFloat("_EmissionIntensity", 0f);
        }

        if (material.HasProperty("_RimIntensity"))
        {
            material.SetFloat("_RimIntensity", 0f);
        }

        if (material.HasProperty("_OutlineWidth"))
        {
            material.SetFloat("_OutlineWidth", 0f);
        }

        if (material.HasProperty("_OutlineGlowWidth"))
        {
            material.SetFloat("_OutlineGlowWidth", 0f);
        }

        if (material.HasProperty("_OutlineGlowIntensity"))
        {
            material.SetFloat("_OutlineGlowIntensity", 0f);
        }

        if (material.HasProperty("_OutlineGlowOpacity"))
        {
            material.SetFloat("_OutlineGlowOpacity", 0f);
        }
    }

    /// <summary>
    /// 设置材质透明混合。不同管线属性名不完全一致，所以同时兼容 _BaseColor 和 _Color。
    /// </summary>
    private void SetupTransparentMaterial(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat("_Blend", 0f);
        }

        if (material.HasProperty("_SrcBlend"))
        {
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        }

        if (material.HasProperty("_DstBlend"))
        {
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }

        if (material.HasProperty("_ZWrite"))
        {
            material.SetFloat("_ZWrite", 0f);
        }

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private void SetupEmissionMaterial(Material material, Color rimColor)
    {
        if (!enableFresnelEmission || material == null || !material.HasProperty("_EmissionColor"))
        {
            return;
        }

        Color emissionColor = rimColor * fresnelEmissionIntensity;
        material.SetColor("_EmissionColor", emissionColor);
        material.EnableKeyword("_EMISSION");
    }

    /// <summary>
    /// 设置专用残影 Shader 的 Fresnel 参数。截图里的白蓝边缘主要靠这里实现。
    /// </summary>
    private void SetupFresnelMaterial(Material material, Color rimColor)
    {
        if (material.HasProperty("_RimColor"))
        {
            material.SetColor("_RimColor", rimColor);
        }

        if (material.HasProperty("_RimPower"))
        {
            material.SetFloat("_RimPower", fresnelPower);
        }

        if (material.HasProperty("_RimIntensity"))
        {
            float intensity = enableFresnelEmission ? fresnelIntensity : 0f;
            material.SetFloat("_RimIntensity", intensity);
        }

        if (material.HasProperty("_RimAlpha"))
        {
            material.SetFloat("_RimAlpha", fresnelAlpha);
        }
    }

    private IEnumerator FadeAndDestroyGhost(GameObject ghostRoot, List<GhostMaterialInfo> ghostMaterials, List<Mesh> ghostMeshes, List<Material> temporaryMaterials, float duration)
    {
        float timer = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (timer < safeDuration)
        {
            timer += Time.deltaTime;
            float alphaRate = 1f - Mathf.Clamp01(timer / safeDuration);

            for (int i = 0; i < ghostMaterials.Count; i++)
            {
                SetMaterialAlpha(ghostMaterials[i], alphaRate);
            }

            yield return null;
        }

        for (int i = 0; i < temporaryMaterials.Count; i++)
        {
            Destroy(temporaryMaterials[i]);
        }

        for (int i = 0; i < ghostMeshes.Count; i++)
        {
            Destroy(ghostMeshes[i]);
        }

        Destroy(ghostRoot);
    }

    private void SetMaterialAlpha(GhostMaterialInfo materialInfo, float alphaRate)
    {
        if (materialInfo == null || materialInfo.material == null)
        {
            return;
        }

        Color color = materialInfo.baseColor;
        color.a *= alphaRate;

        if (materialInfo.material.HasProperty("_BaseColor"))
        {
            materialInfo.material.SetColor("_BaseColor", color);
        }

        if (materialInfo.material.HasProperty("_Color"))
        {
            materialInfo.material.SetColor("_Color", color);
        }

        if (materialInfo.material.HasProperty("_RimAlpha"))
        {
            materialInfo.material.SetFloat("_RimAlpha", materialInfo.startRimAlpha * alphaRate);
        }
    }
}
