using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class AnimationNpc : MonoBehaviour
{
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

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


