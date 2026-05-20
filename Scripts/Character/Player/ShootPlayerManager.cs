using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootPlayerManager : MonoBehaviour
{
    public StartPlayer startPlayer;
    public Camera shootCamera;
    private void Start()
    {
        startPlayer = transform.Find("ShootObj").GetComponent<StartPlayer>();
        shootCamera = transform.Find("ShootCamera").GetComponent<Camera>();

    }

    public void ShootAction(StartPlayerState state)
    {
        startPlayer.ChangeState(state);
    }


    public void Show(StartPlayerState state = StartPlayerState.PlayerIdle)
    {

        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(true);
        }
        startPlayer.ChangeState(state);

    }

}


