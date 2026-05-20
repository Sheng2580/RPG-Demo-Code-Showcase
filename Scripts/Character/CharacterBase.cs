using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class CharacterBase : MonoBehaviour
{
    #region 閲嶅姏鍙傛暟
    [SerializeField, Header("鍦伴潰妫€娴嬫暟鍊?)] private float detectionRang;
    [SerializeField] private float detectionPositionOffset;
    [SerializeField] private LayerMask _whatlsMask;
    protected float _fallOutDeltaTime;
    protected float _fallOutTime = 0.15f;
    public float velocityY;
    protected Vector3 _playerDirectionY;
    [SerializeField] public bool isEnableGravity;
    [SerializeField] public bool characterIsGrounded;
    protected float gravity = 11.8f;

    public CharacterModelBase model;
    #endregion
    public CharacterController characterController;

    protected virtual void Start()
    {
        characterController = GetComponent<CharacterController>();
        isEnableGravity = true;
    }

    protected virtual void Update()
    {
        SetPlayerGravity();
        UpdatePlayerVelocity();
    }

    #region 閲嶅姏鍑芥暟
    protected void SetPlayerIsEnableGravity(bool isEnable)
    {
        isEnableGravity = isEnable;
    }


    protected void SetPlayerVelocityY(float velocityY)
    {
        if (isEnableGravity)
        {
            this.velocityY = velocityY;   
        }
    }


    protected bool GroundedDetction()
    {
        var detectionPosition = new Vector3(transform.position.x, transform.position.y - detectionPositionOffset,
            transform.position.z);
        return Physics.CheckSphere(detectionPosition, detectionRang, _whatlsMask,QueryTriggerInteraction.Ignore);
    }


    public virtual void SetPlayerGravity()
    {
        characterIsGrounded = GroundedDetction();
        if (characterIsGrounded)
        {
            if (velocityY <= 0f)
            {
                velocityY = -2f;
            }
            _fallOutDeltaTime = _fallOutTime;
        }
        else
        {

            if (_fallOutDeltaTime > 0)
            {
                _fallOutDeltaTime -= Time.deltaTime;
            }
            else
            {

            }

            if (velocityY < 54f && isEnableGravity)
            {
                velocityY -= gravity * Time.deltaTime;
            }
        }
    }

    private void UpdatePlayerVelocity()
    {
        if (isEnableGravity && characterController != null && characterController.enabled)
        {
            _playerDirectionY.Set(0,velocityY,0);
            characterController.Move(_playerDirectionY * Time.deltaTime);
        }
    }

    #endregion

    public virtual void PlayAnimation(string animationName,  int layer=0,float fixedTransitionTime=0.25f )
    {
        if (model.animator == null)
        {
            return;
        }
        model.animator.CrossFadeInFixedTime(animationName, fixedTransitionTime, layer);
    }


    protected virtual void OnDrawGizmos()
    {
        var detectionPosition = new Vector3(transform.position.x, transform.position.y - detectionPositionOffset,
            transform.position.z);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(detectionPosition, detectionRang); 

    }

}


