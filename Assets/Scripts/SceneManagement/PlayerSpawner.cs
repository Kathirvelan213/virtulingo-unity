using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : MonoBehaviour
{
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive)
        {
            GameObject spawn = GameObject.Find("SpawnPoint");

            if (spawn != null)
            {
                transform.position = spawn.transform.position;
                transform.rotation = spawn.transform.rotation;
            }
        }
    }
}
