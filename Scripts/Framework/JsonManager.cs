using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum JsonType    
{
    JsonUtility,
    LitJson,
    Newtonsoft
}

public class JsonManager:SingleTon<JsonManager>
{
    public JsonManager() { }

    public void SaveData(object data, string fileName, string directPath = "", JsonType type = JsonType.Newtonsoft)
    {
        string directoryPath = Path.Combine(Application.persistentDataPath, directPath);
        string filePath = Path.Combine(directoryPath, fileName + ".json");

        string jsonStr = "";
        switch (type)
        {
            case JsonType.JsonUtility:
                jsonStr = JsonUtility.ToJson(data, prettyPrint: true);
                break;

            case JsonType.Newtonsoft:
                jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
                break;
        }

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(filePath, jsonStr);

        Debug.Log("保存成功：" + filePath);
    }

    public T LoadData<T>(string fileName, JsonType type = JsonType.Newtonsoft) where T : new()
    {
        T data = new T();
        string path = Application.streamingAssetsPath + "/" + fileName + ".json";
        if(!File.Exists(path))
            path = Application.persistentDataPath + "/" + fileName + ".json";
        if (!File.Exists(path))
            return data;
        string jsonStr = File.ReadAllText(path);
        try
        {
            switch (type)
            {
                case JsonType.JsonUtility:
                    data = JsonUtility.FromJson<T>(jsonStr);
                    break;
                case JsonType.Newtonsoft:
                    data = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(jsonStr);
                    break;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[JsonManager] Load json failed: {path}\n{e.Message}");
            return new T();
        }
        return data;
    }

    public List<string> GetAllJsonFileNames(string directPath = "")
    {
        List<string> fileNames = new List<string>();

        string directoryPath = Path.Combine(Application.persistentDataPath, directPath);

        if (!Directory.Exists(directoryPath))
        {
            return fileNames;
        }

        string[] jsonFiles = Directory.GetFiles(directoryPath, "*.json", SearchOption.TopDirectoryOnly);

        foreach (string file in jsonFiles)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            fileNames.Add(name);
        }

        return fileNames;
    }

}


