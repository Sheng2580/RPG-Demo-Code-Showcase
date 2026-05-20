using UnityEngine;
using System.Collections.Generic;

public enum WeaponDetectionType
{
    Sphere,
    Box
}

public abstract class WeaponBase : MonoBehaviour
{
    [Header("鍒ゅ畾鍩虹閰嶇疆")]
    public LayerMask targetLayerMask;
    public WeaponDetectionType detectionType = WeaponDetectionType.Box;
    public Transform detectionOrigin;
    public Vector3 localOffset = Vector3.forward * 0.5f;
    public float sphereRadius = 0.5f;
    public Vector3 boxSize = new Vector3(0.6f, 0.6f, 1f);
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

    [Header("璋冭瘯缁樺埗")]
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


