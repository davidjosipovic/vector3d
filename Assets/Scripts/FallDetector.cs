using UnityEngine;
using UnityEngine.SceneManagement;

public class FallDetector : MonoBehaviour
{
    [Header("Fall Detection")]
    [Tooltip("Y position at which the player will be considered fallen.")]
    public float fallLimitY = 20f;
    
    [Header("Respawn Settings")]
    [Tooltip("Use checkpoint system instead of restarting level")]
    public bool useCheckpointSystem = true;
    [Tooltip("Delay before respawning player")]
    public float respawnDelay = 1f;
    [Header("Options")] public bool respectLevelFinish = true;
    [Header("Debug")] public bool logFallDebug = false; public float debugLogInterval = 1f;
    
    private bool hasTriggeredFall = false;

    private float _debugTimer = 0f;
    void Update()
    {
        if (respectLevelFinish && GameState.LevelFinished) return;

        if (logFallDebug)
        {
            _debugTimer += Time.deltaTime;
            if (_debugTimer >= debugLogInterval)
            {
                Debug.Log($"[FallDetector] Y={transform.position.y:F2} limit={fallLimitY:F2} triggered={hasTriggeredFall}");
                _debugTimer = 0f;
            }
        }

        if (transform.position.y < fallLimitY && !hasTriggeredFall)
        {
            hasTriggeredFall = true;
            if (useCheckpointSystem && CheckpointManager.Instance != null)
            {
                StartCoroutine(RespawnAfterDelay());
            }
            else
            {
                RestartLevel();
            }
        }
    }
    
    private System.Collections.IEnumerator RespawnAfterDelay()
    {
        Debug.Log("Player fell! Respawning at checkpoint...");
        
        
        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        yield return new WaitForSeconds(respawnDelay);
        
      
        CheckpointManager.Instance.RespawnPlayer();
        
      
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        hasTriggeredFall = false;
    }

    private void RestartLevel()
    {
        Debug.Log("Player fell! Restarting level...");
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }
    
  
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            hasTriggeredFall = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 c = transform.position; c.y = fallLimitY;
        Gizmos.DrawLine(c + Vector3.left * 5f, c + Vector3.right * 5f);
        Gizmos.DrawLine(c + Vector3.forward * 5f, c + Vector3.back * 5f);
    }
}
