using UnityEngine;
using UnityEngine.SceneManagement;

public class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance { get; private set; }
    
    [Header("Game Progress Settings")]
    [Tooltip("Total number of levels in the game")]
    public int totalLevels = 3;
    [Tooltip("Scene index of the main menu")]
    public int mainMenuSceneIndex = 0;
    [Tooltip("Scene index of the first level")]
    public int firstLevelSceneIndex = 1;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    // Private variables
    private float[] levelTimes;
    private int currentLevel = 0;
    private bool gameCompleted = false;
    
    private void Awake()
    {
        // Persistent singleton across scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialize level times array
            levelTimes = new float[totalLevels];
            for (int i = 0; i < levelTimes.Length; i++)
            {
                levelTimes[i] = 0f;
            }
            
            if (showDebugInfo)
                Debug.Log("GameProgressManager initialized");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Subscribe to scene loading
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Determine current level based on scene index
        int sceneIndex = scene.buildIndex;
        
        if (sceneIndex >= firstLevelSceneIndex && sceneIndex < firstLevelSceneIndex + totalLevels)
        {
            currentLevel = sceneIndex - firstLevelSceneIndex;
            
            if (showDebugInfo)
                Debug.Log($"Loaded Level {currentLevel + 1} (Scene {sceneIndex})");
        }
        else if (sceneIndex == mainMenuSceneIndex)
        {
            if (showDebugInfo)
                Debug.Log("Loaded Main Menu");
        }
    }
    
    public void OnLevelCompleted(float levelTime)
    {
        if (showDebugInfo)
            Debug.Log($"🏁 OnLevelCompleted called - Level: {currentLevel}, Time: {levelTime}");
            
        if (currentLevel < 0 || currentLevel >= totalLevels)
        {
            Debug.LogWarning($"Invalid level index: {currentLevel}");
            return;
        }
        
        // Store the level time
        levelTimes[currentLevel] = levelTime;
        
        if (showDebugInfo)
        {
            Debug.Log($"Level {currentLevel + 1} completed in {FormatTime(levelTime)}");
            Debug.Log($"Stored in levelTimes[{currentLevel}] = {levelTime}");
            
            // Debug: Show all current level times
            for (int i = 0; i < levelTimes.Length; i++)
            {
                Debug.Log($"  levelTimes[{i}] = {levelTimes[i]} ({FormatTime(levelTimes[i])})");
            }
        }
        
        // Check if this was the last level
        if (currentLevel >= totalLevels - 1)
        {
            // Game completed!
            gameCompleted = true;
            
            if (showDebugInfo)
            {
                Debug.Log("🎉 GAME COMPLETED! 🎉");
                LogAllLevelTimes();
            }
            
            // Don't auto-load main menu - let the completion UI handle it
            // The player should see their completion stats and choose what to do next
        }
        
        // Don't auto-load next level - let the completion UI handle transitions
        // The player should see their level time and choose to continue or restart
    }
    
    public bool IsGameCompleted()
    {
        return gameCompleted;
    }
    
    public float[] GetLevelTimes()
    {
        return levelTimes;
    }
    
    public float GetTotalTime()
    {
        float total = 0f;
        for (int i = 0; i < levelTimes.Length; i++)
        {
            total += levelTimes[i];
        }
        return total;
    }
    
    public int GetCurrentLevel()
    {
        return currentLevel;
    }
    
    public void ResetGameProgress()
    {
        gameCompleted = false;
        currentLevel = 0;
        
        // Reset all level times
        for (int i = 0; i < levelTimes.Length; i++)
        {
            levelTimes[i] = 0f;
        }
        
        // Unfreeze game in case it was frozen
        Time.timeScale = 1f;
        
        if (showDebugInfo)
            Debug.Log("Game progress reset");
    }
    
    private void LogAllLevelTimes()
    {
        Debug.Log("=== FINAL RESULTS ===");
        float total = 0f;
        
        for (int i = 0; i < levelTimes.Length; i++)
        {
            Debug.Log($"Level {i + 1}: {FormatTime(levelTimes[i])}");
            total += levelTimes[i];
        }
        
        Debug.Log($"Total Time: {FormatTime(total)}");
        Debug.Log("====================");
    }
    
    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        int milliseconds = Mathf.FloorToInt((timeInSeconds * 1000f) % 1000f);
        
        return string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
    }
    
    // Methods for manual level transitions (called by UI)
    public void LoadNextLevel()
    {
        if (currentLevel < totalLevels - 1)
        {
            int nextSceneIndex = firstLevelSceneIndex + currentLevel + 1;
            
            if (showDebugInfo)
                Debug.Log($"Loading next level: Level {currentLevel + 2} (Scene {nextSceneIndex})");
            
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            if (showDebugInfo)
                Debug.Log("No more levels! Going to main menu to show completion results.");
            
            LoadMainMenuWithResults();
        }
    }
    
    public void LoadMainMenuWithResults()
    {
        if (showDebugInfo)
            Debug.Log("Loading main menu to show completion results");
        
        SceneManager.LoadScene(mainMenuSceneIndex);
    }
    
    public void RestartCurrentLevel()
    {
        int currentSceneIndex = firstLevelSceneIndex + currentLevel;
        
        if (showDebugInfo)
            Debug.Log($"Restarting current level: Level {currentLevel + 1} (Scene {currentSceneIndex})");
        
        SceneManager.LoadScene(currentSceneIndex);
    }
    
    public bool HasNextLevel()
    {
        return currentLevel < totalLevels - 1;
    }
    
    public bool IsLastLevel()
    {
        return currentLevel >= totalLevels - 1;
    }

    // Fallback method to manually record level completion
    public void ForceRecordLevelCompletion(float levelTime)
    {
        // Get current level from scene index
        int sceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        int levelIndex = sceneIndex - firstLevelSceneIndex;
        
        if (levelIndex >= 0 && levelIndex < totalLevels)
        {
            levelTimes[levelIndex] = levelTime;
            
            if (showDebugInfo)
            {
                Debug.Log($"🚨 FORCE recorded Level {levelIndex + 1} time: {FormatTime(levelTime)}");
                
                // Show all times
                for (int i = 0; i < levelTimes.Length; i++)
                {
                    Debug.Log($"  levelTimes[{i}] = {levelTimes[i]} ({FormatTime(levelTimes[i])})");
                }
            }
            
            // Check if this completes the game
            if (levelIndex >= totalLevels - 1)
            {
                gameCompleted = true;
                if (showDebugInfo)
                    Debug.Log("🎉 GAME FORCE COMPLETED! 🎉");
            }
        }
        else
        {
            Debug.LogWarning($"Invalid level index for force record: {levelIndex}");
        }
    }

    // Debug methods
    [ContextMenu("Complete Current Level (Test)")]
    public void TestCompleteLevel()
    {
        // Simulate level completion with random time for testing
        float testTime = Random.Range(30f, 120f);
        OnLevelCompleted(testTime);
    }
    
    [ContextMenu("Reset Progress")]
    public void TestResetProgress()
    {
        ResetGameProgress();
    }
    
    [ContextMenu("Show Progress Info")]
    public void ShowProgressInfo()
    {
        Debug.Log($"Current Level: {currentLevel + 1}");
        Debug.Log($"Game Completed: {gameCompleted}");
        Debug.Log($"Total Time: {FormatTime(GetTotalTime())}");
    }
}
