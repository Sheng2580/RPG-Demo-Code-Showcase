using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//普通npc仅播放动画
public class AnimationNpc : MonoBehaviour
{
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }
    
    //外部事件
    public void Holle()
    {
        _animator.SetBool("isHolle", true);
        Invoke("Huifu",0.5f);
    }

    private void Huifu()
    {
        _animator.SetBool("isHolle", true);
    }
    
  
    
    
    
}
