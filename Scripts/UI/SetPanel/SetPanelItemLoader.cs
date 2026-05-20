using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class SetPanelItemLoader
{
   public static GameObject Load(string abName, string itemName, string editorAssetPath)
   {
#if UNITY_EDITOR
      GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(editorAssetPath);
      if (prefab != null)
      {
         GameObject obj = Object.Instantiate(prefab);
         obj.name = itemName;
         return obj;
      }
#endif

      return ABManager.Instance != null ? ABManager.Instance.LoadRes<GameObject>(abName, itemName) : null;
   }
}
