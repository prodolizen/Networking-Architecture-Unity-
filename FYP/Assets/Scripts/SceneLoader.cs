using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Linq.Expressions;

public class SceneLoader : NetworkBehaviour
{
    public static SceneLoader Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject); // Prevent duplicates
        }
    }

    public void LoadScene(string sceneName, LoadSceneMode sceneMode) //use networkmanagers correct sceneloading process
    {
        NetworkManager.SceneManager.LoadScene(sceneName, sceneMode);
    }

    public bool SceneLoaded(string sceneName) //check if scene has finished loading
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);

            if(scene.name == sceneName)
                return true;
        }

        return false;
    }

}
