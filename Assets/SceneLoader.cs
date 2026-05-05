using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public string gameSceneName = "GameScene";

    public void LoadGameScene()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}