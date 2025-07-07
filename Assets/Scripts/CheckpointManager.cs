using UnityEngine;
using UnityEngine.SceneManagement;
using Cinemachine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }
    
    [Header("Checkpoint Settings")]
    [Tooltip("Default spawn position if no checkpoint is set")]
    public Transform defaultSpawnPoint;
    [Tooltip("Offset from checkpoint position for player respawn")]
    public Vector3 respawnOffset = Vector3.up * 0.5f;
    
    private Vector3 currentCheckpointPosition;
    private Quaternion currentCheckpointRotation;
    private bool hasCheckpoint = false;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    private void Awake()
    {
        // Scene-specific singleton pattern (don't persist between levels)
        if (Instance == null)
        {
            Instance = this;
            // Removed DontDestroyOnLoad to fix level transition glitches
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Set initial checkpoint to default spawn if available
        if (defaultSpawnPoint != null)
        {
            SetCurrentCheckpoint(defaultSpawnPoint.position, defaultSpawnPoint.rotation);
        }
    }
    
    public void SetCurrentCheckpoint(Vector3 position, Quaternion rotation)
    {
        currentCheckpointPosition = position;
        currentCheckpointRotation = rotation;
        hasCheckpoint = true;
        
        if (showDebugInfo)
            Debug.Log($"Checkpoint set at: {position}");
    }
    
    public Vector3 GetRespawnPosition()
    {
        if (hasCheckpoint)
        {
            return currentCheckpointPosition + respawnOffset;
        }
        else if (defaultSpawnPoint != null)
        {
            return defaultSpawnPoint.position + respawnOffset;
        }
        else
        {
            Debug.LogWarning("No checkpoint or default spawn point set!");
            return Vector3.zero + respawnOffset;
        }
    }
    
    public Quaternion GetRespawnRotation()
    {
        if (hasCheckpoint)
        {
            return currentCheckpointRotation;
        }
        else if (defaultSpawnPoint != null)
        {
            return defaultSpawnPoint.rotation;
        }
        else
        {
            return Quaternion.identity;
        }
    }
    
    public void RespawnPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Notify timer system about respawn (timer continues)
            if (LevelTimer.Instance != null)
            {
                LevelTimer.Instance.OnPlayerRespawn();
            }
            
            // Disable CharacterController temporarily for teleportation
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }
            
            // Move player to checkpoint
            player.transform.position = GetRespawnPosition();
            player.transform.rotation = GetRespawnRotation();
            
            // Reset player velocity first
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                // Reset any ongoing states
                playerController.ResetPlayerState();
            }
            
            // Reset camera with a small delay to ensure player is positioned
            StartCoroutine(ResetCameraAfterRespawn());
            
            // Re-enable CharacterController
            if (controller != null)
            {
                controller.enabled = true;
            }
            
            if (showDebugInfo)
                Debug.Log($"Player respawned at: {GetRespawnPosition()}");
        }
        else
        {
            Debug.LogError("Player not found for respawn!");
        }
    }
    
    // Reset all checkpoints in the level
    public void ResetAllCheckpoints()
    {
        Checkpoint[] checkpoints = FindObjectsOfType<Checkpoint>();
        foreach (Checkpoint checkpoint in checkpoints)
        {
            checkpoint.ResetCheckpoint();
        }
        
        hasCheckpoint = false;
        
        // Reset to default spawn if available
        if (defaultSpawnPoint != null)
        {
            SetCurrentCheckpoint(defaultSpawnPoint.position, defaultSpawnPoint.rotation);
        }
    }
    
    private void OnGUI()
    {
        if (showDebugInfo)
        {
            GUI.Label(new Rect(10, 100, 300, 20), $"Has Checkpoint: {hasCheckpoint}");
            if (hasCheckpoint)
            {
                GUI.Label(new Rect(10, 120, 300, 20), $"Checkpoint: {currentCheckpointPosition}");
            }
        }
    }
    
    private System.Collections.IEnumerator ResetCameraAfterRespawn()
    {
        // Wait one frame for player to be positioned
        yield return null;
        
        // Find Cinemachine FreeLook camera
        var freeLookCamera = FindObjectOfType<Cinemachine.CinemachineFreeLook>();
        if (freeLookCamera != null)
        {
            // Reset the camera axes to default position (behind player)
            freeLookCamera.m_XAxis.Value = 0f; // Behind player (0 degrees)
            freeLookCamera.m_YAxis.Value = 0.5f; // Middle height
            
            // Force camera to update immediately
            freeLookCamera.PreviousStateIsValid = false;
            freeLookCamera.InternalUpdateCameraState(Vector3.up, Time.deltaTime);
            
            if (showDebugInfo)
                Debug.Log($"Cinemachine FreeLook reset - X: {freeLookCamera.m_XAxis.Value}, Y: {freeLookCamera.m_YAxis.Value}");
        }
        
        // Wait another frame and do a second reset to ensure it sticks
        yield return null;
        
        if (freeLookCamera != null)
        {
            freeLookCamera.m_XAxis.Value = 0f;
            freeLookCamera.m_YAxis.Value = 0.5f;
            freeLookCamera.PreviousStateIsValid = false;
            
            if (showDebugInfo)
                Debug.Log("Second camera reset completed");
        }
    }
    
    private void ResetCinemachineCamera()
    {
        // Find Cinemachine FreeLook camera
        var freeLookCamera = FindObjectOfType<Cinemachine.CinemachineFreeLook>();
        if (freeLookCamera != null)
        {
            // Force immediate camera update without smooth transition
            freeLookCamera.PreviousStateIsValid = false;
            
            // Reset camera state to prevent following player into the abyss
            freeLookCamera.ForceCameraPosition(freeLookCamera.transform.position, freeLookCamera.transform.rotation);
            
            if (showDebugInfo)
                Debug.Log("Cinemachine FreeLook camera reset for respawn");
        }
        
        // Fallback: try to find any Cinemachine virtual camera
        var virtualCamera = FindObjectOfType<Cinemachine.CinemachineVirtualCamera>();
        if (virtualCamera != null)
        {
            virtualCamera.PreviousStateIsValid = false;
            if (showDebugInfo)
                Debug.Log("Cinemachine Virtual camera reset for respawn");
        }
        
        // Fallback: try custom camera follow
        CameraFollow cameraFollow = FindObjectOfType<CameraFollow>();
        if (cameraFollow != null)
        {
            cameraFollow.ResetCameraPosition();
            if (showDebugInfo)
                Debug.Log("Camera follow reset for respawn");
        }
    }
    
    // Manual camera reset for testing - call this with a key press
    public void ManualCameraReset()
    {
        var freeLookCamera = FindObjectOfType<Cinemachine.CinemachineFreeLook>();
        if (freeLookCamera != null)
        {
            freeLookCamera.m_XAxis.Value = 0f; // Behind player
            freeLookCamera.m_YAxis.Value = 0.5f; // Middle height
            freeLookCamera.PreviousStateIsValid = false;
            
            if (showDebugInfo)
                Debug.Log("Manual camera reset - camera positioned behind player");
        }
    }
    
    // Add this for testing
    private void Update()
    {
        // Press R to manually reset camera
        if (Input.GetKeyDown(KeyCode.R))
        {
            ManualCameraReset();
        }
        
        // Press T to test respawn
        if (Input.GetKeyDown(KeyCode.T))
        {
            RespawnPlayer();
        }
    }
    
    // Called when a new level starts to ensure clean state
    public void OnLevelStart()
    {
        // Reset checkpoint state for new level
        hasCheckpoint = false;
        
        // Set initial checkpoint to default spawn if available
        if (defaultSpawnPoint != null)
        {
            SetCurrentCheckpoint(defaultSpawnPoint.position, defaultSpawnPoint.rotation);
            if (showDebugInfo)
                Debug.Log($"Level started - default checkpoint set at: {defaultSpawnPoint.position}");
        }
        else
        {
            if (showDebugInfo)
                Debug.LogWarning("No default spawn point set for new level!");
        }
    }
    
    // Ensure clean instance on scene load
    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Reset instance reference to prevent conflicts
        if (Instance == this)
        {
            OnLevelStart();
        }
    }

}
