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
    public bool autoLoadNextLevel = false; 
    public string nextLevelName = "Level2";
    public float delayBeforeNextLevel = 2f;
    public bool showNextLevelButton = false; 
    
    private bool levelCompleted = false;
    
    void Start()
    {
       
        if (!gameObject.CompareTag("Finish"))
        {
            Debug.LogWarning($"FinishLine object '{gameObject.name}' doesn't have 'Finish' tag!");
        }
        else
        {
            Debug.Log($"FinishLine '{gameObject.name}' has correct 'Finish' tag");
        }
        
        
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError($"FinishLine '{gameObject.name}' has NO COLLIDER! Add a Collider component.");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning($"FinishLine '{gameObject.name}' collider is NOT a trigger! Enable 'Is Trigger' checkbox.");
        }
        else
        {
            Debug.Log($"FinishLine '{gameObject.name}' has trigger collider properly set up");
        }
        
       
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
            
        Debug.Log($"🏁 FinishLine '{gameObject.name}' initialized and ready!");
    }
    
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"FinishLine trigger entered by: '{other.gameObject.name}' with tag: '{other.tag}'");
        
        
        if (other.CompareTag("Player"))
        {
            if (!levelCompleted)
            {
                Debug.Log($"PLAYER REACHED FINISH LINE! Completing level...");
                CompleteLevel(other.gameObject);
            }
            else
            {
                Debug.Log($"ℹLevel already completed, ignoring trigger");
            }
        }
        else
        {
            Debug.Log($"Object '{other.gameObject.name}' doesn't have 'Player' tag, ignoring");
        }
    }
    
    
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"FinishLine collision entered by: '{collision.gameObject.name}' with tag: '{collision.gameObject.tag}'");
        
        if (collision.gameObject.CompareTag("Player") && !levelCompleted)
        {
            Debug.Log("PLAYER COLLISION DETECTED! Using collision fallback...");
            CompleteLevel(collision.gameObject);
        }
    }
    
   
    void Update()
    {
        if (!levelCompleted)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < 2f)
                {
                    Debug.Log($"PLAYER WITHIN RANGE ({distance:F2}m)! Completing level...");
                    CompleteLevel(player);
                }
            }
        }
    }
    
    private void CompleteLevel(GameObject player)
    {
        Debug.Log($"CompleteLevel() called for player: {player.name}");
        levelCompleted = true;
        
        
        if (LevelTimer.Instance != null)
        {
            LevelTimer.Instance.StopTimer();
            float finalTime = LevelTimer.Instance.GetFinalTime();
            
            Debug.Log($"LEVEL COMPLETED! 🏁");
            Debug.Log($"Final Time: {LevelTimer.Instance.GetCurrentTime():F2} seconds");
        }
        else
        {
            Debug.LogWarning("LevelTimer instance not found!");
        }
        
       
        if (audioSource != null && finishSound != null)
        {
            audioSource.PlayOneShot(finishSound);
            Debug.Log("Playing finish sound");
        }
        else if (audioSource == null)
        {
            Debug.Log("No AudioSource for finish sound");
        }
        else if (finishSound == null)
        {
            Debug.Log("No finish sound clip assigned");
        }
        
       
        if (finishParticles != null)
        {
            finishParticles.Play();
            Debug.Log("Playing finish particles");
        }
        
        if (celebrationEffect != null)
        {
            celebrationEffect.SetActive(true);
            Debug.Log("Activating celebration effect");
        }
        
       
        PlayerController playerController = player.GetComponent<PlayerController>();
    
        if (showCompletionMessage)
        {
            StartCoroutine(ShowCompletionMessage());
        }
        
      
        Debug.Log($"Level completion debug:");
        Debug.Log($"  - showNextLevelButton: {showNextLevelButton}");
        Debug.Log($"  - nextLevelName: '{nextLevelName}'");
        Debug.Log($"  - ShouldShowNextLevelButton(): {ShouldShowNextLevelButton()}");
      
        if (autoLoadNextLevel && !string.IsNullOrEmpty(nextLevelName))
        {
            StartCoroutine(LoadNextLevelAfterDelay());
        }
    }
    
    private IEnumerator ShowCompletionMessage()
    {
        Debug.Log($" {completionMessage} ");
        
       
        yield return new WaitForSeconds(completionMessageDuration);
        
   
        Debug.Log("Completion message hidden");
    }
    
    private IEnumerator LoadNextLevelAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeNextLevel);
        
        Debug.Log($"Loading next level: {nextLevelName}");
        
      
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextLevelName);
    }
    
 
    public void LoadNextLevel()
    {
        if (!string.IsNullOrEmpty(nextLevelName))
        {
            Debug.Log($"Loading next level: {nextLevelName}");
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextLevelName);
        }
        else
        {
            Debug.LogWarning("Next level name not specified!");
        }
    }
    

    public string GetNextLevelName()
    {
        return nextLevelName;
    }
    

    public bool ShouldShowNextLevelButton()
    {
        bool result = showNextLevelButton && !string.IsNullOrEmpty(nextLevelName);
        Debug.Log($"ShouldShowNextLevelButton() check:");
        Debug.Log($"  - showNextLevelButton: {showNextLevelButton}");
        Debug.Log($"  - nextLevelName: '{nextLevelName}' (empty: {string.IsNullOrEmpty(nextLevelName)})");
        Debug.Log($"  - Final result: {result}");
        return result;
    }
   
    [ContextMenu("Complete Level")]
    public void ManualCompleteLevel()
    {
        if (!levelCompleted)
        {
    
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

    public void ResetFinishLine()
    {
        levelCompleted = false;
        
        if (finishParticles != null)
            finishParticles.Stop();
            
        if (celebrationEffect != null)
            celebrationEffect.SetActive(false);
            
        Debug.Log("Finish line reset");
    }
    

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
    

    [ContextMenu("Test Next Level Button Config")]
    public void TestNextLevelButtonConfig()
    {
        Debug.Log("=== NEXT LEVEL BUTTON CONFIG TEST ===");
        Debug.Log($"Show Next Level Button: {showNextLevelButton}");
        Debug.Log($"Next Level Name: '{nextLevelName}'");
        Debug.Log($"Is Name Empty: {string.IsNullOrEmpty(nextLevelName)}");
        Debug.Log($"Should Show Button: {ShouldShowNextLevelButton()}");
        
 
        GameUI gameUI = FindObjectOfType<GameUI>();
        if (gameUI != null)
        {
            Debug.Log($"GameUI found: {gameUI.gameObject.name}");
            if (gameUI.nextLevelButton != null)
            {
                Debug.Log($"Next Level Button assigned in GameUI: {gameUI.nextLevelButton.gameObject.name}");
                Debug.Log($"Button Active: {gameUI.nextLevelButton.gameObject.activeSelf}");
            }
            else
            {
                Debug.LogWarning("Next Level Button NOT assigned in GameUI!");
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
            Debug.LogError("GameUI not found!");
        }
        Debug.Log("=====================================");
    }
    

    [ContextMenu("Force Setup Next Level Button")]
    public void ForceSetupNextLevelButton()
    {
        Debug.Log("🔧 FORCING NEXT LEVEL BUTTON SETUP");
        
        GameUI gameUI = FindObjectOfType<GameUI>();
        if (gameUI != null)
        {
        
            if (gameUI.GetType().GetMethod("SetupNextLevelButton", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) != null)
            {
                gameUI.GetType().GetMethod("SetupNextLevelButton", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .Invoke(gameUI, null);
                Debug.Log("SetupNextLevelButton() called via reflection");
            }
            else
            {
                Debug.LogWarning("SetupNextLevelButton method not found in GameUI");
            }
            
  
            if (gameUI.completionPanel != null && !gameUI.completionPanel.activeSelf)
            {
                gameUI.completionPanel.SetActive(true);
                Debug.Log("Completion panel activated for testing");
            }
        }
        else
        {
            Debug.LogError("GameUI not found!");
        }
    }
}
