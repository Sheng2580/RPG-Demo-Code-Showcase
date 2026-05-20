using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerData
{
    public int gold = 1000;
    public Dictionary<string, int> propertyLevels = new Dictionary<string, int>();
    public Dictionary<int, int> props = new Dictionary<int, int>();

    public void EnsureCollections()
    {
        if (propertyLevels == null)
        {
            propertyLevels = new Dictionary<string, int>();
        }

        if (props == null)
        {
            props = new Dictionary<int, int>();
        }
    }
}
