using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FinishLine : MonoBehaviour
{
    [Header("Finish Settings")]
    public bool showCompletionMessage = true;
    public float completionMessageDuration = 3f;
    public string completionMessage = "Level Completed!";
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip finishSound;
    
    [Header("Visual Effects")]
    public ParticleSystem finishParticles;
    public GameObject celebrationEffect;
    
    [Header("Level Transition")]
    public bool autoLoadNextLevel = false; // Disabled by default to prevent auto-transitions
    public string nextLevelName = "Level2";
    public float delayBeforeNextLevel = 2f;
    public bool showNextLevelButton = false; // Disabled - using GameUI buttons instead
    
    private bool levelCompleted = false;
    
    void Start()
    {
        // Ensure this object has the "Finish" tag
        if (!gameObject.CompareTag("Finish"))
        {
            Debug.LogWarning($"FinishLine object '{gameObject.name}' doesn't have 'Finish' tag!");
        }
        else
        {
            Debug.Log($"✅ FinishLine '{gameObject.name}' has correct 'Finish' tag");
        }
        
        // Check for trigger collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError($"❌ FinishLine '{gameObject.name}' has NO COLLIDER! Add a Collider component.");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning($"⚠️ FinishLine '{gameObject.name}' collider is NOT a trigger! Enable 'Is Trigger' checkbox.");
        }
        else
        {
            Debug.Log($"✅ FinishLine '{gameObject.name}' has trigger collider properly set up");
        }
        
        // Setup audio if not assigned
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
            
        Debug.Log($"🏁 FinishLine '{gameObject.name}' initialized and ready!");
    }
    
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"🔔 FinishLine trigger entered by: '{other.gameObject.name}' with tag: '{other.tag}'");
        
        // Check if player reached the finish line
        if (other.CompareTag("Player"))
        {
            if (!levelCompleted)
            {
                Debug.Log($"🏁 PLAYER REACHED FINISH LINE! Completing level...");
                CompleteLevel(other.gameObject);
            }
            else
            {
                Debug.Log($"ℹ️ Level already completed, ignoring trigger");
            }
        }
        else
        {
            Debug.Log($"⚠️ Object '{other.gameObject.name}' doesn't have 'Player' tag, ignoring");
        }
    }
    
    // Fallback collision detection in case trigger doesn't work
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"🔔 FinishLine collision entered by: '{collision.gameObject.name}' with tag: '{collision.gameObject.tag}'");
        
        if (collision.gameObject.CompareTag("Player") && !levelCompleted)
        {
            Debug.Log("🏁 PLAYER COLLISION DETECTED! Using collision fallback...");
            CompleteLevel(collision.gameObject);
        }
    }
    
    // Alternative detection method using distance check
    void Update()
    {
        if (!levelCompleted)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < 2f) // Within 2 units
                {
                    Debug.Log($"🏁 PLAYER WITHIN RANGE ({distance:F2}m)! Completing level...");
                    CompleteLevel(player);
                }
            }
        }
    }
    
    private void CompleteLevel(GameObject player)
    {
        Debug.Log($"🏆 CompleteLevel() called for player: {player.name}");
        levelCompleted = true;
        
        // Stop the timer
        if (LevelTimer.Instance != null)
        {
            LevelTimer.Instance.StopTimer();
            float finalTime = LevelTimer.Instance.GetFinalTime();
            
            Debug.Log($"🏁 LEVEL COMPLETED! 🏁");
            Debug.Log($"Final Time: {LevelTimer.Instance.GetCurrentTime():F2} seconds");
        }
        else
        {
            Debug.LogWarning("LevelTimer instance not found!");
        }
        
        // Play audio
        if (audioSource != null && finishSound != null)
        {
            audioSource.PlayOneShot(finishSound);
            Debug.Log("🔊 Playing finish sound");
        }
        else if (audioSource == null)
        {
            Debug.Log("🔇 No AudioSource for finish sound");
        }
        else if (finishSound == null)
        {
            Debug.Log("🔇 No finish sound clip assigned");
        }
        
        // Trigger visual effects
        if (finishParticles != null)
        {
            finishParticles.Play();
            Debug.Log("✨ Playing finish particles");
        }
        
        if (celebrationEffect != null)
        {
            celebrationEffect.SetActive(true);
            Debug.Log("🎉 Activating celebration effect");
        }
        
        // Get player controller to stop movement (optional)
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            // You can disable player input here if desired
            // playerController.enabled = false;
        }
        
        // Show completion message
        if (showCompletionMessage)
        {
            StartCoroutine(ShowCompletionMessage());
        }
        
        // Debug check for next level setup
        Debug.Log($"🔍 Level completion debug:");
        Debug.Log($"  - showNextLevelButton: {showNextLevelButton}");
        Debug.Log($"  - nextLevelName: '{nextLevelName}'");
        Debug.Log($"  - ShouldShowNextLevelButton(): {ShouldShowNextLevelButton()}");
        
        // Auto load next level if specified
        if (autoLoadNextLevel && !string.IsNullOrEmpty(nextLevelName))
        {
            StartCoroutine(LoadNextLevelAfterDelay());
        }
    }
    
    private IEnumerator ShowCompletionMessage()
    {
        Debug.Log($"⭐ {completionMessage} ⭐");
        
        // Here you could show a UI popup with the completion message
        // For now, we'll just use Debug.Log
        
        yield return new WaitForSeconds(completionMessageDuration);
        
        // Hide message
        Debug.Log("Completion message hidden");
    }
    
    private IEnumerator LoadNextLevelAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeNextLevel);
        
        Debug.Log($"Loading next level: {nextLevelName}");
        
        // Load next level
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextLevelName);
    }
    
    // Public method to load next level (called by UI button)
    public void LoadNextLevel()
    {
        if (!string.IsNullOrEmpty(nextLevelName))
        {
            Debug.Log($"🚀 Loading next level: {nextLevelName}");
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextLevelName);
        }
        else
        {
            Debug.LogWarning("Next level name not specified!");
        }
    }
    
    // Method to get next level name (used by UI)
    public string GetNextLevelName()
    {
        return nextLevelName;
    }
    
    // Method to check if next level button should be shown
    public bool ShouldShowNextLevelButton()
    {
        bool result = showNextLevelButton && !string.IsNullOrEmpty(nextLevelName);
        Debug.Log($"🔍 ShouldShowNextLevelButton() check:");
        Debug.Log($"  - showNextLevelButton: {showNextLevelButton}");
        Debug.Log($"  - nextLevelName: '{nextLevelName}' (empty: {string.IsNullOrEmpty(nextLevelName)})");
        Debug.Log($"  - Final result: {result}");
        return result;
    }
    
    // Method to manually trigger level completion (for testing)
    [ContextMenu("Complete Level")]
    public void ManualCompleteLevel()
    {
        if (!levelCompleted)
        {
            // Find player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                CompleteLevel(player);
            }
            else
            {
                Debug.LogWarning("Player not found for manual level completion!");
            }
        }
    }
    
    // Reset finish line state (useful for level restart)
    public void ResetFinishLine()
    {
        levelCompleted = false;
        
        if (finishParticles != null)
            finishParticles.Stop();
            
        if (celebrationEffect != null)
            celebrationEffect.SetActive(false);
            
        Debug.Log("Finish line reset");
    }
    
    // Debug method to test finish line detection
    [ContextMenu("Test Finish Line Detection")]
    public void TestFinishLineDetection()
    {
        Debug.Log("=== FINISH LINE DEBUG TEST ===");
        Debug.Log($"GameObject Name: {gameObject.name}");
        Debug.Log($"GameObject Tag: {gameObject.tag}");
        Debug.Log($"Has 'Finish' tag: {gameObject.CompareTag("Finish")}");
        
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Debug.Log($"Has Collider: YES");
            Debug.Log($"Is Trigger: {col.isTrigger}");
            Debug.Log($"Collider Enabled: {col.enabled}");
            Debug.Log($"Collider Bounds: {col.bounds}");
        }
        else 
        {
            Debug.Log($"Has Collider: NO");
        }
        
        Debug.Log($"Level Completed: {levelCompleted}");
        Debug.Log($"FinishLine Script Enabled: {enabled}");
        Debug.Log($"GameObject Active: {gameObject.activeInHierarchy}");
        Debug.Log("===============================");
    }
    
    // Method to force complete level (for testing)
    [ContextMenu("Force Complete Level")]
    public void ForceCompleteLevel()
    {
        Debug.Log("🔧 FORCING LEVEL COMPLETION FOR TESTING");
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            CompleteLevel(player);
        }
        else
        {
            Debug.LogError("No player found with 'Player' tag!");
        }
    }
    
    // Test method to check next level button configuration
    [ContextMenu("Test Next Level Button Config")]
    public void TestNextLevelButtonConfig()
    {
        Debug.Log("=== NEXT LEVEL BUTTON CONFIG TEST ===");
        Debug.Log($"Show Next Level Button: {showNextLevelButton}");
        Debug.Log($"Next Level Name: '{nextLevelName}'");
        Debug.Log($"Is Name Empty: {string.IsNullOrEmpty(nextLevelName)}");
        Debug.Log($"Should Show Button: {ShouldShowNextLevelButton()}");
        
        // Find GameUI and test connection
        GameUI gameUI = FindObjectOfType<GameUI>();
        if (gameUI != null)
        {
            Debug.Log($"✅ GameUI found: {gameUI.gameObject.name}");
            if (gameUI.nextLevelButton != null)
            {
                Debug.Log($"✅ Next Level Button assigned in GameUI: {gameUI.nextLevelButton.gameObject.name}");
                Debug.Log($"Button Active: {gameUI.nextLevelButton.gameObject.activeSelf}");
            }
            else
            {
                Debug.LogWarning("❌ Next Level Button NOT assigned in GameUI!");
            }
            
            if (gameUI.completionPanel != null)
            {
                Debug.Log($"Completion Panel: {gameUI.completionPanel.name}");
                Button[] buttons = gameUI.completionPanel.GetComponentsInChildren<Button>(true);
                Debug.Log($"Buttons found in completion panel: {buttons.Length}");
                foreach (Button btn in buttons)
                {
                    Debug.Log($"  - {btn.gameObject.name} (Active: {btn.gameObject.activeSelf})");
                }
            }
        }
        else
        {
            Debug.LogError("❌ GameUI not found!");
        }
        Debug.Log("=====================================");
    }
    
    // Force setup next level button for testing
    [ContextMenu("Force Setup Next Level Button")]
    public void ForceSetupNextLevelButton()
    {
        Debug.Log("🔧 FORCING NEXT LEVEL BUTTON SETUP");
        
        GameUI gameUI = FindObjectOfType<GameUI>();
        if (gameUI != null)
        {
            // Force call the setup method if it exists
            if (gameUI.GetType().GetMethod("SetupNextLevelButton", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) != null)
            {
                gameUI.GetType().GetMethod("SetupNextLevelButton", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .Invoke(gameUI, null);
                Debug.Log("✅ SetupNextLevelButton() called via reflection");
            }
            else
            {
                Debug.LogWarning("⚠️ SetupNextLevelButton method not found in GameUI");
            }
            
            // Also show completion panel if hidden
            if (gameUI.completionPanel != null && !gameUI.completionPanel.activeSelf)
            {
                gameUI.completionPanel.SetActive(true);
                Debug.Log("✅ Completion panel activated for testing");
            }
        }
        else
        {
            Debug.LogError("❌ GameUI not found!");
        }
    }
}
