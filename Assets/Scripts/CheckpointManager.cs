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
            
            // Reset all moving platforms to their starting positions
            ResetAllMovingPlatforms();
            
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
            
            
            // Reset camera with a small delay to ensure player is positioned
            StartCoroutine(ResetCameraAfterRespawn());
            
            // Re-enable CharacterController
            if (controller != null)
            {
                controller.enabled = true;
            }
        }
        else
        {
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
    
    // Reset all moving platforms in the level
    public void ResetAllMovingPlatforms()
    {
        MovingPlatform[] movingPlatforms = FindObjectsOfType<MovingPlatform>();
        foreach (MovingPlatform platform in movingPlatforms)
        {
            platform.ResetToStartingPosition();
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
            freeLookCamera.m_XAxis.Value = 0f;
            freeLookCamera.m_YAxis.Value = 0.5f; 
            
         
            freeLookCamera.PreviousStateIsValid = false;
            freeLookCamera.InternalUpdateCameraState(Vector3.up, Time.deltaTime);
        }
        
        yield return null;
        
        if (freeLookCamera != null)
        {
            freeLookCamera.m_XAxis.Value = 0f;
            freeLookCamera.m_YAxis.Value = 0.5f;
            freeLookCamera.PreviousStateIsValid = false;
        }
    }
    
    private void ResetCinemachineCamera()
    {
        var freeLookCamera = FindObjectOfType<Cinemachine.CinemachineFreeLook>();
        if (freeLookCamera != null)
        {
            freeLookCamera.PreviousStateIsValid = false;
            
            freeLookCamera.ForceCameraPosition(freeLookCamera.transform.position, freeLookCamera.transform.rotation);
        }
        
        var virtualCamera = FindObjectOfType<Cinemachine.CinemachineVirtualCamera>();
        if (virtualCamera != null)
        {
            virtualCamera.PreviousStateIsValid = false;
        }
        
        CameraFollow cameraFollow = FindObjectOfType<CameraFollow>();
        if (cameraFollow != null)
        {
            cameraFollow.ResetCameraPosition();
        }
    }
    
    public void ManualCameraReset()
    {
        var freeLookCamera = FindObjectOfType<Cinemachine.CinemachineFreeLook>();
        if (freeLookCamera != null)
        {
            freeLookCamera.m_XAxis.Value = 0f;
            freeLookCamera.m_YAxis.Value = 0.5f;
            freeLookCamera.PreviousStateIsValid = false;
        }
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ManualCameraReset();
        }
        
        if (Input.GetKeyDown(KeyCode.T))
        {
            RespawnPlayer();
        }
        
        if (Input.GetKeyDown(KeyCode.L))
        {
            FixSceneLighting();
        }
    }
    
    public void OnLevelStart()
    {
        hasCheckpoint = false;
        
        if (defaultSpawnPoint != null)
        {
            SetCurrentCheckpoint(defaultSpawnPoint.position, defaultSpawnPoint.rotation);
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
        if (Instance == this)
        {
            OnLevelStart();
            
            FixSceneLighting();
        }
    }
    
    private void FixSceneLighting()
    {
        SceneLightingFix lightingFix = FindObjectOfType<SceneLightingFix>();
        
        if (lightingFix != null)
        {
            lightingFix.FixLighting();
        }
        else
        {
            GameObject tempFix = new GameObject("TempLightingFix");
            SceneLightingFix tempComponent = tempFix.AddComponent<SceneLightingFix>();
            
            tempComponent.FixLighting();
            
            Destroy(tempFix, 1f);
        }
    }
}
