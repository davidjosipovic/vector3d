using UnityEngine;
using UnityEngine.SceneManagement;

public class FallDetector : MonoBehaviour
{
    [Tooltip("Y position at which the player will be considered fallen and the level restarts.")]
    public float fallLimitY = -10f;

    void Update()
    {
        if (transform.position.y < fallLimitY)
        {
            RestartLevel();
        }
    }

    private void RestartLevel()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }
}
