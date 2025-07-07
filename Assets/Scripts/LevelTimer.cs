using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    public bool startTimerOnStart = true;
    public string timeFormat = "mm\\:ss\\:ff";  // Format: minutes:seconds:milliseconds
    
    // Timer state
    private float startTime;
    private float endTime;
    private bool isTimerRunning = false;
    private bool levelCompleted = false;
    private float finalTime = 0f;
    
    // Singleton pattern for easy access
    public static LevelTimer Instance { get; private set; }
    
    void Awake()
    {
        // Scene-specific singleton setup (don't persist between levels to avoid glitches)
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
    }
    
    void Start()
    {
        if (startTimerOnStart)
        {
            StartTimer();
        }
        
        Debug.Log("LevelTimer initialized");
    }
    
    void Update()
    {
        // Timer logic only - UI updates are handled by GameUI
        if (isTimerRunning && !levelCompleted)
        {
            // Optional: Add debug output every few seconds
            if (Time.time - startTime > 0f && (int)(Time.time - startTime) % 5 == 0 && Time.time - lastDebugTime > 1f)
            {
                lastDebugTime = Time.time;
                Debug.Log($"Timer running: {FormatTime(GetCurrentTime())}");
            }
        }
    }
    
    private float lastDebugTime = 0f;
    
    public void StartTimer()
    {
        startTime = Time.time;
        isTimerRunning = true;
        levelCompleted = false;
        Debug.Log("Level timer started!");
    }
    
    public void StopTimer()
    {
        if (isTimerRunning && !levelCompleted)
        {
            endTime = Time.time;
            finalTime = endTime - startTime;
            isTimerRunning = false;
            levelCompleted = true;
            
            Debug.Log($"Level completed! Final time: {FormatTime(finalTime)}");
            
            // Notify GameProgressManager about level completion
            if (GameProgressManager.Instance != null)
            {
                GameProgressManager.Instance.OnLevelCompleted(finalTime);
            }
            else
            {
                Debug.LogWarning("GameProgressManager not found! Level completion not recorded.");
            }
        }
    }
    
    public void ResetTimer()
    {
        startTime = Time.time;
        endTime = 0f;
        finalTime = 0f;
        isTimerRunning = true;
        levelCompleted = false;
        Debug.Log("Level timer reset!");
    }
    
    public float GetCurrentTime()
    {
        if (levelCompleted)
            return finalTime;
        else if (isTimerRunning)
            return Time.time - startTime;
        else
            return 0f;
    }
    
    public float GetFinalTime()
    {
        return finalTime;
    }
    
    public bool IsRunning()
    {
        return isTimerRunning;
    }
    
    public bool IsCompleted()
    {
        return levelCompleted;
    }
    
    public string FormatTime(float timeInSeconds)
    {
        // Convert to TimeSpan for easy formatting
        System.TimeSpan timeSpan = System.TimeSpan.FromSeconds(timeInSeconds);
        
        switch (timeFormat)
        {
            case "mm\\:ss\\:ff":
                return string.Format("{0:00}:{1:00}:{2:00}", 
                    timeSpan.Minutes, 
                    timeSpan.Seconds, 
                    timeSpan.Milliseconds / 10); // Convert to centiseconds
                    
            case "mm\\:ss":
                return string.Format("{0:00}:{1:00}", 
                    timeSpan.Minutes, 
                    timeSpan.Seconds);
                    
            case "ss\\.ff":
                return string.Format("{0:00}.{1:00}", 
                    (int)timeInSeconds, 
                    (int)((timeInSeconds % 1) * 100));
                    
            default:
                return timeInSeconds.ToString("F2") + "s";
        }
    }
    
    // Method to be called when player respawns (timer continues)
    public void OnPlayerRespawn()
    {
        // Timer continues running - no action needed
        Debug.Log($"Player respawned. Current time: {FormatTime(GetCurrentTime())}");
    }
    
    // Method to restart the entire level (reset timer)
    public void RestartLevel()
    {
        ResetTimer();
    }

    // Scene management for proper level transitions
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
        // Reset timer for new level
        if (Instance == this)
        {
            ResetTimer();
            if (startTimerOnStart)
            {
                StartTimer();
            }
            Debug.Log($"LevelTimer reset for new scene: {scene.name}");
        }
    }
}
