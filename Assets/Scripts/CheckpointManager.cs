using UnityEngine;
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
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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
            // Disable CharacterController temporarily for teleportation
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }
            
            // Move player to checkpoint
            player.transform.position = GetRespawnPosition();
            player.transform.rotation = GetRespawnRotation();
            
            // Reset camera position to prevent glitching
            ResetCinemachineCamera();
            
            // Reset player velocity
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                // Reset any ongoing states
                playerController.ResetPlayerState();
            }
            
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
}
