using System;
using System.Collections.Generic;
using UnityEngine;

public class NPCModle : CharacterModelBase
{
    public GameObject Player;
    public GameObject head;
    public bool isHeadRota;
    private Vector3 _lookAtTargetPos;
    private float _lookAtWeight;
    private Quaternion _originalHeadLocalRot;

    public GameObject lookAtCamera;
    public float turnSpeed = 7f;
    public float autoDisableAngle = 8f;

    protected override void Awake()
    {
        base.Awake();
        if (head != null) _originalHeadLocalRot = head.transform.localRotation;
    }

    private void Start()
    {
        Player = GameManager.Instance != null ? GameManager.Instance.Player : null;
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || layerIndex != 0) return;
        bool canLook = CheckLookCondition();
        if (canLook)
        {
            Vector3 target = Player.transform.position + Vector3.up * 1f;
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
            animator.SetLookAtWeight(_lookAtWeight, 0.1f, 1f, 0.5f, 0.5f);
        }
        else
        {
            animator.SetLookAtWeight(0);
        }
    }


    private bool CheckLookCondition()
    {
        if (Player == null || head == null || !isHeadRota) return false;

        float distance = Vector3.Distance(Player.transform.position, transform.position);
        if (distance >= 5f) return false;

        Vector3 directionToTarget = Player.transform.position - transform.position;
        float angle = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);
        return Mathf.Abs(angle) < 60f;
    }

    public void FaceForwardImmediate()
    {
        if (head == null) return;
        if (animator != null) animator.SetLookAtWeight(0f);
        head.transform.localRotation = _originalHeadLocalRot;
    }

    public void SetHeadRotationEnabled(bool enabled)
    {
        isHeadRota = enabled;
        if (!enabled)
        {
            _lookAtWeight = 0f;
            if (animator != null) animator.SetLookAtWeight(0f);
        }
    }

    public void RestoreHeadRotationImmediate()
    {
        if (head == null) return;
        head.transform.localRotation = _originalHeadLocalRot;
    }
}


