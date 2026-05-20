using UnityEngine;
using System.Collections.Generic;

public enum WeaponDetectionType
{
    Sphere,
    Box
}

public abstract class WeaponBase : MonoBehaviour
{
    [Header("判定基础配置")]
    // 判定目标所在 Layer，敌人武器填 Player，玩家武器填 Enemy。
    public LayerMask targetLayerMask;
    // 判定形状：球形适合拳脚，盒形适合剑、枪、爪等有方向的武器。
    public WeaponDetectionType detectionType = WeaponDetectionType.Box;
    // 判定原点，不填时默认使用当前武器物体。
    public Transform detectionOrigin;
    // 相对判定原点的本地偏移，用于把判定盒移动到刀刃、拳头、枪尖等位置。
    public Vector3 localOffset = Vector3.forward * 0.5f;
    // 球形判定半径。
    public float sphereRadius = 0.5f;
    // 盒形判定尺寸。
    public Vector3 boxSize = new Vector3(0.6f, 0.6f, 1f);
    // 是否检测 Trigger Collider。
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

    [Header("调试绘制")]
    public bool drawWeaponGizmos = true;
    public Color gizmoColor = new Color(1f, 0.2f, 0.05f, 0.85f);

    protected bool isDetectionActive;
    protected Transform attackOwner;
    private readonly HashSet<Transform> damagedTargets = new HashSet<Transform>();

    protected Transform DetectionOrigin => detectionOrigin != null ? detectionOrigin : transform;

    protected Vector3 DetectionCenter => DetectionOrigin.TransformPoint(localOffset);

    protected Quaternion DetectionRotation => DetectionOrigin.rotation;

    public Collider[] DetectColliders()
    {
        if (targetLayerMask.value == 0)
        {
            return System.Array.Empty<Collider>();
        }

        if (detectionType == WeaponDetectionType.Sphere)
        {
            return Physics.OverlapSphere(
                DetectionCenter,
                Mathf.Max(0f, sphereRadius),
                targetLayerMask,
                triggerInteraction);
        }

        return Physics.OverlapBox(
            DetectionCenter,
            Vector3.Max(Vector3.zero, boxSize) * 0.5f,
            DetectionRotation,
            targetLayerMask,
            triggerInteraction);
    }

    public virtual void BeginAttackDetection(Transform owner)
    {
        attackOwner = owner;
        damagedTargets.Clear();
        isDetectionActive = true;
    }

    public virtual void EndAttackDetection()
    {
        isDetectionActive = false;
        attackOwner = null;
        damagedTargets.Clear();
    }

    protected virtual void Update()
    {
        if (!isDetectionActive)
        {
            return;
        }

        Collider[] hits = DetectColliders();
        for (int i = 0; i < hits.Length; i++)
        {
            Transform target = GetDamageTarget(hits[i]);
            if (target == null || damagedTargets.Contains(target))
            {
                continue;
            }

            damagedTargets.Add(target);
            OnHitTarget(target, hits[i]);
        }
    }

    protected virtual Transform GetDamageTarget(Collider hit)
    {
        if (hit == null)
        {
            return null;
        }

        return hit.attachedRigidbody != null ? hit.attachedRigidbody.transform : hit.transform;
    }

    protected abstract void OnHitTarget(Transform target, Collider hit);

    protected virtual void OnDrawGizmosSelected()
    {
        if (!drawWeaponGizmos)
        {
            return;
        }

        Gizmos.color = gizmoColor;
        if (detectionType == WeaponDetectionType.Sphere)
        {
            Gizmos.DrawWireSphere(DetectionCenter, Mathf.Max(0f, sphereRadius));
            return;
        }

        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(DetectionCenter, DetectionRotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.Max(Vector3.zero, boxSize));
        Gizmos.matrix = oldMatrix;
    }
}
