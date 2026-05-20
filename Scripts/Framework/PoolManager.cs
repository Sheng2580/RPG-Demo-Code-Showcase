using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.PlayerLoop;

public class PoolData
{
    private string poolName;
    public GameObject fatherObj;
    public List<GameObject> poolList ;

    public PoolData(GameObject obj,GameObject grandFatherObj)
    {
        poolName = obj.name;
        CreateFatherObj(grandFatherObj);
        poolList = new List<GameObject>();
        PushObj(obj, grandFatherObj);
    }
    public void PushObj(GameObject obj, GameObject grandFatherObj = null)
    {
        if (obj == null)
        {
            return;
        }

        if (fatherObj == null)
        {
            CreateFatherObj(grandFatherObj);
        }

        obj.SetActive(false);
        poolList.Add(obj);  
        obj.transform.parent = fatherObj.transform;
    }
    public GameObject PopObj()
    {
        RemoveDestroyedObjects();
        if (poolList.Count == 0)
        {
            return null;
        }

        GameObject obj=poolList[0];
        poolList.RemoveAt(0);
        obj.transform.parent =null;
        obj.SetActive(true);
        return obj;
    }

    public bool HasAvailableObject()
    {
        RemoveDestroyedObjects();
        return poolList.Count > 0;
    }

    public void RemoveDestroyedObjects()
    {
        for (int i = poolList.Count - 1; i >= 0; i--)
        {
            if (poolList[i] == null)
            {
                poolList.RemoveAt(i);
            }
        }
    }

    private void CreateFatherObj(GameObject grandFatherObj)
    {
        fatherObj = new GameObject(poolName);
        if (grandFatherObj != null)
        {
            fatherObj.transform.parent = grandFatherObj.transform;
        }
    }
}

public class PoolManager : SingleTon<PoolManager>
{
   public Dictionary<string, PoolData> poolDic = new Dictionary<string, PoolData>();
   private GameObject grandFatherObj;

   public GameObject getObj(string name)
   {
       if (poolDic.ContainsKey(name) && poolDic[name].HasAvailableObject())
       {
           return poolDic[name].PopObj();  
       }
       else
       {
           GameObject obj=ResourceManager.Instance.load<GameObject>(name);
           if (obj == null) obj = new GameObject();
           obj.name = name;
           return obj;

       }
   }

   public void  GetObjForAB(string abName, string resName,UnityAction<GameObject> callback)
   {
       if (poolDic.ContainsKey(resName) && poolDic[resName].HasAvailableObject())
       {
           callback(poolDic[resName].PopObj());
       }
       else
       {
           ABManager.Instance.LoadResAsync(abName, resName,typeof (GameObject),(obj) =>
           {
               GameObject resobj = obj as GameObject;
               if (resobj == null)
               {
                   resobj = new GameObject();
               }
               resobj.name = resName;
               callback(resobj);
           });
       }
   }


   public void pushObj(string name, GameObject obj)
   {
       if (obj == null)
       {
           return;
       }

       EnsurePoolRoot();
       if (poolDic.ContainsKey(name))
       {
           poolDic[name].PushObj(obj, grandFatherObj);  
       }
       else
       {
           poolDic.Add(name,new PoolData(obj,grandFatherObj));
       }
   }

   public void clear()
   {
       poolDic.Clear(); 
       if (grandFatherObj != null)
       {
           UnityEngine.Object.Destroy(grandFatherObj);
       }
       grandFatherObj = null;   
   }

   private void EnsurePoolRoot()
   {
       if (grandFatherObj != null)
       {
           return;
       }

       grandFatherObj = new GameObject("Pool");
       UnityEngine.Object.DontDestroyOnLoad(grandFatherObj);
   }

}


