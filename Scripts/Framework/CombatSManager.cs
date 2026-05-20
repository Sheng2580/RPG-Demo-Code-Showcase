using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatSManager : SceneResourceManager<CombatSManager>
{
    [HideInInspector]
    public GameObject currentCamera;

    [SerializeField] private HuallStartTimeLine huallStartTimeLine;

    protected override void OnSceneResourcesLoaded()
    {
        LoadCanvas.Instance.Hied();
    }
}
