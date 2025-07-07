using UnityEngine;

public class LevelFinish : MonoBehaviour
{
    [Header("Level Finish Settings")]
    [Tooltip("Tag of the player GameObject")]
    public string playerTag = "Player";
    [Tooltip("Show debug information")]
    public bool showDebugInfo = true;
    [Tooltip("Disable player movement after finish")]
    public bool disablePlayerOnFinish = true;
    
    [Header("Visual Feedback")]
    [Tooltip("Particle system to play on finish")]
    public ParticleSystem finishParticles;
    [Tooltip("Audio clip to play on finish")]
    public AudioClip finishSound;
    
    private bool levelFinished = false;
    private AudioSource audioSource;
    
    private void Start()
    {
        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && finishSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if player entered the finish area
        if (!levelFinished && other.CompareTag(playerTag))
        {
            FinishLevel(other.gameObject);
        }
    }
    
    private void FinishLevel(GameObject player)
    {
        levelFinished = true;
        
        if (showDebugInfo)
            Debug.Log("🏁 Level Finished! 🏁");
        
        // Stop the timer and notify GameProgressManager
        if (LevelTimer.Instance != null)
        {
            LevelTimer.Instance.StopTimer();
            
            // Let GameProgressManager handle level completion
            // Don't trigger automatic transitions here
        }
        else
        {
            Debug.LogWarning("LevelTimer not found!");
        }
        
        // Play visual effects
        if (finishParticles != null)
        {
            finishParticles.Play();
        }
        
        // Play finish sound
        if (audioSource != null && finishSound != null)
        {
            audioSource.PlayOneShot(finishSound);
        }
        
        // Disable player movement if requested
        if (disablePlayerOnFinish)
        {
            DisablePlayer(player);
        }
        
        // Show completion UI - let GameUI handle the buttons and transitions
        if (GameUI.Instance != null)
        {
            GameUI.Instance.ShowLevelCompleteMessage();
        }
        else
        {
            Debug.LogWarning("GameUI.Instance not found! Completion UI will not show.");
        }
    }
    
    private void DisablePlayer(GameObject player)
    {
        // Disable player controller
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // Stop player movement
        CharacterController characterController = player.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
        }
        
        // Stop any rigidbody movement
        Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();
        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector3.zero;
            playerRigidbody.isKinematic = true;
        }
        
        if (showDebugInfo)
            Debug.Log("Player movement disabled");
    }
    
    // Method to manually trigger level finish (for testing)
    [ContextMenu("Finish Level")]
    public void ManualFinishLevel()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            FinishLevel(player);
        }
        else
        {
            Debug.LogWarning("Player not found for manual finish!");
        }
    }
    
    // Visual indicator in Scene view
    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = levelFinished ? Color.green : Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            
            if (col is BoxCollider)
            {
                BoxCollider boxCol = col as BoxCollider;
                Gizmos.DrawWireCube(boxCol.center, boxCol.size);
            }
            else if (col is SphereCollider)
            {
                SphereCollider sphereCol = col as SphereCollider;
                Gizmos.DrawWireSphere(sphereCol.center, sphereCol.radius);
            }
        }
        
        // Draw finish line icon
        Gizmos.color = Color.white;
        Gizmos.DrawIcon(transform.position, "sv_icon_dot3_pix16_gizmo", true);
    }
}
