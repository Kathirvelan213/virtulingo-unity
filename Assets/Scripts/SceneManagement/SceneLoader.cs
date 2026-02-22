using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadEnvironment(string environmentName)
    {
        SceneManager.sceneLoaded += OnGameSceneLoaded;
        SceneManager.LoadScene("GameScene");
        sceneToLoad = environmentName;
    }

    private string sceneToLoad;

    void OnGameSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GameScene")
        {
            SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Additive);
            SceneManager.sceneLoaded -= OnGameSceneLoaded;
        }
    }
}
