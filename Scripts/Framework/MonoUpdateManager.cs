using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class MonoUpdateManager : UnitySingleTonMono<MonoUpdateManager>
{
    private event UnityAction updateEvent;

    void Start()
    {
    }

    public void addUpdateListener(UnityAction a)
    {
        updateEvent += a;
    }

    public void removeUpdateListener(UnityAction a)
    {
        updateEvent -= a;
    }

    void Update()
    {
        updateEvent?.Invoke();
    }
    public Coroutine startCoroutine(IEnumerator routine)
    {
        return StartCoroutine(routine);
    }
    public void stopCoroutine(Coroutine routine)
    {
        StopCoroutine(routine);
    }
}

