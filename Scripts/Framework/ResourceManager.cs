using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class ResourceManager : SingleTon<ResourceManager>
{
    public T load<T>(string name,Transform father=null)where T:Object
    {
        T res = Resources.Load<T>(name);
        if (res is GameObject)
        {
            T obj = GameObject.Instantiate(res);
            if (father != null)
            {
                (obj as GameObject).transform.SetParent(father.transform);
                (obj as GameObject).transform.localPosition = Vector3.zero;
                (obj as GameObject).transform.localRotation = Quaternion.identity;
                obj.name = name;
            }

            return obj;
        }
        else
        {
            return res; 
        }
    }
}


