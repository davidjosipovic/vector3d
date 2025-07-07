using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUI : MonoBehaviour
{
    [Header("Timer UI")]
    public Text timerText;  // Legacy UI Text
    public TMPro.TextMeshProUGUI timerTextTMP;  // TextMeshPro alternative
    public GameObject timerPanel;
    
    [Header("Completion UI")]
    public GameObject completionPanel;
    public Text completionTimeText;
    public TMPro.TextMeshProUGUI completionTimeTextTMP;
    public Text completionMessageText;
    public TMPro.TextMeshProUGUI completionMessageTextTMP;
    public Button nextLevelButton;
    public Button restartButton;
    
    [Header("UI Settings")]
    public bool showTimerUI = true;
    public Color normalTimeColor = Color.white;
    public Color completedTimeColor = Color.green;
    
    private LevelTimer levelTimer;
    
    // Singleton pattern for easy access
    public static GameUI Instance { get; private set; }
    
    void Awake()
    {
        // Scene-specific singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Find timer reference
        levelTimer = LevelTimer.Instance;
        if (levelTimer == null)
        {
            levelTimer = FindObjectOfType<LevelTimer>();
        }
        
        // Initialize UI state
        if (completionPanel != null)
            completionPanel.SetActive(false);
            
        if (timerPanel != null && !showTimerUI)
            timerPanel.SetActive(false);
    }
    
    void Update()
    {
        // Update timer display
        if (levelTimer != null && showTimerUI)
        {
            UpdateTimerDisplay();
            
            // Check if level completed
            if (levelTimer.IsCompleted() && completionPanel != null && !completionPanel.activeSelf)
            {
                ShowCompletionUI();
            }
        }
    }
    
    private void UpdateTimerDisplay()
    {
        if (levelTimer == null) return;
        
        // Get current time and format it
        float currentTime = levelTimer.GetCurrentTime();
        string timeString = levelTimer.FormatTime(currentTime);
        
        // Set color based on completion state
        Color timeColor = levelTimer.IsCompleted() ? completedTimeColor : normalTimeColor;
        
        // Update timer text content AND color
        if (timerText != null)
        {
            timerText.text = timeString;
            timerText.color = timeColor;
        }
            
        if (timerTextTMP != null)
        {
            timerTextTMP.text = timeString;
            timerTextTMP.color = timeColor;
        }
    }
    
    private void ShowCompletionUI()
    {
        if (completionPanel != null && levelTimer != null)
        {
            completionPanel.SetActive(true);
            
            // Show completion time
            float finalTime = levelTimer.GetFinalTime();
            string timeString = levelTimer.FormatTime(finalTime);
            
            // IMPORTANT: Ensure level completion is recorded in GameProgressManager
            if (GameProgressManager.Instance != null)
            {
                // Try normal method first
                GameProgressManager.Instance.OnLevelCompleted(finalTime);
                
                // Also use fallback method to ensure it's recorded
                GameProgressManager.Instance.ForceRecordLevelCompletion(finalTime);
                
                Debug.Log($"🏁 Level completion recorded - Time: {finalTime}");
            }
            
            if (completionTimeText != null)
            {
                completionTimeText.text = $"Level Completed!\nTime: {timeString}";
                completionTimeText.color = completedTimeColor;
            }
            
            if (completionTimeTextTMP != null)
            {
                completionTimeTextTMP.text = $"Level Completed!\nTime: {timeString}";
                completionTimeTextTMP.color = completedTimeColor;
            }
            
            // Set completion message based on whether this is the last level
            string message = GetCompletionMessage(finalTime);
            if (GameProgressManager.Instance != null)
            {
                if (GameProgressManager.Instance.IsLastLevel())
                {
                    message = "🎉 All Levels Complete! 🎉\nCheck the main menu for your results!";
                }
                else
                {
                    message = GetCompletionMessage(finalTime) + "\nReady for the next challenge?";
                }
            }
            
            if (completionMessageText != null)
                completionMessageText.text = message;
            if (completionMessageTextTMP != null)
                completionMessageTextTMP.text = message;
            
            // Setup buttons based on game state
            SetupCompletionButtons();
            
            Debug.Log($"✅ Completion UI shown - Time: {timeString}");
        }
        else
        {
            Debug.LogWarning("⚠️ Cannot show completion UI - missing references!");
        }
    }
    
    private string GetCompletionMessage(float timeInSeconds)
    {
        // Different messages based on completion time
        if (timeInSeconds < 30f)
            return "🏆 AMAZING! Speed runner! 🏆";
        else if (timeInSeconds < 60f)
            return "⭐ Excellent time! ⭐";
        else if (timeInSeconds < 120f)
            return "👍 Good job! 👍";
        else
            return "✅ Level completed! ✅";
    }
    
    private void SetupCompletionButtons()
    {
        // Setup Next Level button
        if (nextLevelButton != null)
        {
            if (GameProgressManager.Instance != null && GameProgressManager.Instance.HasNextLevel())
            {
                nextLevelButton.gameObject.SetActive(true);
                
                // Setup button text
                Text buttonText = nextLevelButton.GetComponentInChildren<Text>();
                TMPro.TextMeshProUGUI buttonTextTMP = nextLevelButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                
                string buttonLabel = "Next Level";
                
                if (buttonText != null)
                    buttonText.text = buttonLabel;
                if (buttonTextTMP != null)
                    buttonTextTMP.text = buttonLabel;
                
                // Clear existing listeners and add new one
                nextLevelButton.onClick.RemoveAllListeners();
                nextLevelButton.onClick.AddListener(() => {
                    if (GameProgressManager.Instance != null)
                    {
                        GameProgressManager.Instance.LoadNextLevel();
                    }
                });
                
                Debug.Log($"✅ Next Level button configured");
            }
            else
            {
                nextLevelButton.gameObject.SetActive(false);
                Debug.Log("ℹ️ Next Level button hidden (last level or no GameProgressManager)");
            }
        }
        
        // Setup Restart button
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
            
            // Setup button text
            Text buttonText = restartButton.GetComponentInChildren<Text>();
            TMPro.TextMeshProUGUI buttonTextTMP = restartButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            
            string buttonLabel = "Restart Level";
            
            if (buttonText != null)
                buttonText.text = buttonLabel;
            if (buttonTextTMP != null)
                buttonTextTMP.text = buttonLabel;
            
            // Clear existing listeners and add new one
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(() => {
                if (GameProgressManager.Instance != null)
                {
                    GameProgressManager.Instance.RestartCurrentLevel();
                }
                else
                {
                    RestartLevel(); // Fallback to old method
                }
            });
            
            Debug.Log($"✅ Restart button configured");
        }
        
        // If this is the last level, show a main menu button instead of next level
        if (GameProgressManager.Instance != null && GameProgressManager.Instance.IsLastLevel())
        {
            // We can repurpose the next level button as a main menu button
            if (nextLevelButton != null)
            {
                nextLevelButton.gameObject.SetActive(true);
                
                // Setup button text
                Text buttonText = nextLevelButton.GetComponentInChildren<Text>();
                TMPro.TextMeshProUGUI buttonTextTMP = nextLevelButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                
                string buttonLabel = "Main Menu";
                
                if (buttonText != null)
                    buttonText.text = buttonLabel;
                if (buttonTextTMP != null)
                    buttonTextTMP.text = buttonLabel;
                
                // Clear existing listeners and add new one
                nextLevelButton.onClick.RemoveAllListeners();
                nextLevelButton.onClick.AddListener(() => {
                    if (GameProgressManager.Instance != null)
                    {
                        // Before going to main menu, ensure level completion is recorded one more time
                        if (levelTimer != null)
                        {
                            float currentFinalTime = levelTimer.GetFinalTime();
                            GameProgressManager.Instance.ForceRecordLevelCompletion(currentFinalTime);
                            Debug.Log($"🏁 Final check - recorded time {currentFinalTime} before going to main menu");
                        }
                        
                        GameProgressManager.Instance.LoadMainMenuWithResults();
                    }
                });
                
                Debug.Log($"✅ Main Menu button configured (last level)");
            }
        }
    }
    
    // Public methods for UI controls
    public void RestartLevel()
    {
        if (completionPanel != null)
            completionPanel.SetActive(false);
            
        // Use GameProgressManager if available, otherwise fallback
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.RestartCurrentLevel();
        }
        else
        {
            // Fallback: restart the scene directly
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }
    
    public void NextLevel()
    {
        // Use GameProgressManager for level transitions
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.LoadNextLevel();
        }
        else
        {
            Debug.LogWarning("GameProgressManager not found! Cannot load next level.");
        }
    }
    
    public void MainMenu()
    {
        // Return to main menu using GameProgressManager
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.LoadMainMenuWithResults();
        }
        else
        {
            // Fallback: load scene 0 (assumed to be main menu)
            Debug.Log("GameProgressManager not found, loading scene 0 as main menu");
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }
    
    public void ToggleTimerUI()
    {
        showTimerUI = !showTimerUI;
        if (timerPanel != null)
            timerPanel.SetActive(showTimerUI);
    }
    
    public void ShowLevelCompleteMessage()
    {
        if (levelTimer != null && levelTimer.IsCompleted())
        {
            ShowCompletionUI();
        }
    }
}
