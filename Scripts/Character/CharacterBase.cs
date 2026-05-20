using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class CharacterBase : MonoBehaviour
{
    #region 重力参数
    [SerializeField, Header("地面检测数值")] private float detectionRang; //检测半径
    [SerializeField] private float detectionPositionOffset; //检测点
    [SerializeField] private LayerMask _whatlsMask;//检测层
    protected float _fallOutDeltaTime;
    protected float _fallOutTime = 0.15f; //防止角色下楼梯播放下落动画
    //玩家垂直方向速度
    public float velocityY;
    //玩家垂直方向位移
    protected Vector3 _playerDirectionY;
    //是否使用重力
    [SerializeField] public bool isEnableGravity;
    //角色是否在地面
    [SerializeField] public bool characterIsGrounded;
    protected float gravity = 11.8f;

    //子类里初始化
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

    #region 重力函数
    //重力激活失活
    protected void SetPlayerIsEnableGravity(bool isEnable)
    {
        isEnableGravity = isEnable;
    }
    
    
    //改变垂直速度
    protected void SetPlayerVelocityY(float velocityY)
    {
        if (isEnableGravity)
        {
            this.velocityY = velocityY;   
        }
    }
    
    
    /// <summary>
    /// 地面检测
    /// </summary>
    protected bool GroundedDetction()
    {
        var detectionPosition = new Vector3(transform.position.x, transform.position.y - detectionPositionOffset,
            transform.position.z);
        return Physics.CheckSphere(detectionPosition, detectionRang, _whatlsMask,QueryTriggerInteraction.Ignore);
    }
    

    /// <summary>
    /// 设置重力
    /// </summary>
    public virtual void SetPlayerGravity()
    {
        characterIsGrounded = GroundedDetction();
        if (characterIsGrounded)
        {
            //在地面
            if (velocityY <= 0f)
            {
                velocityY = -2f;
            }
            _fallOutDeltaTime = _fallOutTime;
        }
        else
        {
            //不在地面
                
            if (_fallOutDeltaTime > 0)
            {
                _fallOutDeltaTime -= Time.deltaTime;
                //等待0.15 f,帮助角色从较低的高度差下落
                //大于0不用播放下落donhau
            }
            else
            {
                //小于0,角色还没有落地，可能不是下楼梯，那么必须播放下落动画
                
            }
            
            if (velocityY < 54f && isEnableGravity)
            {
                velocityY -= gravity * Time.deltaTime;
            }
        }
    }

    /// <summary>
    /// 应用重力
    /// </summary>
    private void UpdatePlayerVelocity()
    {
        if (isEnableGravity && characterController != null && characterController.enabled)
        {
            _playerDirectionY.Set(0,velocityY,0);
            characterController.Move(_playerDirectionY * Time.deltaTime);
        }
    }
    
    #endregion
    
    /// <summary>
    /// 播放动画
    /// </summary>
    /// <param name="animationName"></param>
    /// <param name="layer"></param>
    /// <param name="fixedTransitionTime"></param>
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
        //重力检测球
        var detectionPosition = new Vector3(transform.position.x, transform.position.y - detectionPositionOffset,
            transform.position.z);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(detectionPosition, detectionRang); 
        
    }
    
}
