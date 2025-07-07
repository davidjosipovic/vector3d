using UnityEngine;
using UnityEngine.SceneManagement;

public class FallDetector : MonoBehaviour
{
    [Header("Fall Detection")]
    [Tooltip("Y position at which the player will be considered fallen.")]
    public float fallLimitY = -10f;
    
    [Header("Respawn Settings")]
    [Tooltip("Use checkpoint system instead of restarting level")]
    public bool useCheckpointSystem = true;
    [Tooltip("Delay before respawning player")]
    public float respawnDelay = 1f;
    
    private bool hasTriggeredFall = false;

    void Update()
    {
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
}
