using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
public class GameSceneManager:UnitySingleTonMono<GameSceneManager>
{
    public void LoadScene(string sceneName,UnityAction fun=null)
    {
        SceneManager.LoadScene(sceneName);
        fun?.Invoke();  
    }
    public void LoadSceneAsync(string sceneName,UnityAction fun=null)
    {
        StartCoroutine(LoadSceneEnumerator(sceneName,fun));
    }

    private IEnumerator LoadSceneEnumerator(string sceneName,UnityAction fun=null)
    {
        AsyncOperation ao = SceneManager.LoadSceneAsync(sceneName);
        yield return ao;
        fun?.Invoke();  
    }

    public string GetCurrSceneName()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        return currentScene.name;
    }


}


