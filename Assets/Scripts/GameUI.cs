using UnityEngine;
using UnityEngine.UI;

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
    
    void Start()
    {
        // Find timer reference
        levelTimer = LevelTimer.Instance;
        if (levelTimer == null)
        {
            levelTimer = FindObjectOfType<LevelTimer>();
        }
        
        if (levelTimer == null)
        {
            Debug.LogError("GameUI: LevelTimer not found! Timer display will not work.");
        }
        else
        {
            Debug.Log("GameUI: LevelTimer found and connected.");
        }
        
        // Initialize UI state
        if (completionPanel != null)
            completionPanel.SetActive(false);
            
        if (timerPanel != null && !showTimerUI)
            timerPanel.SetActive(false);
            
        // Verify UI connections
        if (timerText == null && timerTextTMP == null)
        {
            Debug.LogWarning("GameUI: No timer text components assigned!");
        }
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
        
        // Debug output every few seconds
        if (Time.time % 2f < Time.deltaTime)
        {
            Debug.Log($"UI Timer Update: {timeString} (Running: {levelTimer.IsRunning()})");
        }
    }
    
    private void ShowCompletionUI()
    {
        if (completionPanel != null)
        {
            completionPanel.SetActive(true);
            
            // Update completion time
            string finalTimeString = FormatCompletionTime(levelTimer.GetFinalTime());
            
            if (completionTimeText != null)
                completionTimeText.text = $"Final Time: {finalTimeString}";
                
            if (completionTimeTextTMP != null)
                completionTimeTextTMP.text = $"Final Time: {finalTimeString}";
                
            // Update completion message
            string message = GetCompletionMessage(levelTimer.GetFinalTime());
            
            if (completionMessageText != null)
                completionMessageText.text = message;
                
            if (completionMessageTextTMP != null)
                completionMessageTextTMP.text = message;
                
            // Setup next level button
            SetupNextLevelButton();
        }
    }
    
    private string FormatCompletionTime(float timeInSeconds)
    {
        System.TimeSpan timeSpan = System.TimeSpan.FromSeconds(timeInSeconds);
        return string.Format("{0:00}:{1:00}:{2:00}", 
            timeSpan.Minutes, 
            timeSpan.Seconds, 
            timeSpan.Milliseconds / 10);
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
    
    private void SetupNextLevelButton()
    {
        // Find finish line to get next level info
        FinishLine finishLine = FindObjectOfType<FinishLine>();
        
        if (nextLevelButton != null && finishLine != null)
        {
            if (finishLine.ShouldShowNextLevelButton())
            {
                nextLevelButton.gameObject.SetActive(true);
                
                // Setup button text
                Text buttonText = nextLevelButton.GetComponentInChildren<Text>();
                TMPro.TextMeshProUGUI buttonTextTMP = nextLevelButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                
                string buttonLabel = $"Next Level ({finishLine.GetNextLevelName()})";
                
                if (buttonText != null)
                    buttonText.text = buttonLabel;
                if (buttonTextTMP != null)
                    buttonTextTMP.text = buttonLabel;
                
                // Clear existing listeners and add new one
                nextLevelButton.onClick.RemoveAllListeners();
                nextLevelButton.onClick.AddListener(() => finishLine.LoadNextLevel());
                
                Debug.Log($"✅ Next Level button configured for: {finishLine.GetNextLevelName()}");
            }
            else
            {
                nextLevelButton.gameObject.SetActive(false);
                Debug.Log("ℹ️ Next Level button hidden (not configured)");
            }
        }
        else if (nextLevelButton == null)
        {
            Debug.LogWarning("⚠️ Next Level button not assigned in GameUI!");
        }
        else if (finishLine == null)
        {
            Debug.LogWarning("⚠️ FinishLine not found for next level setup!");
        }
    }
    
    // Public methods for UI controls
    public void RestartLevel()
    {
        if (levelTimer != null)
        {
            levelTimer.RestartLevel();
        }
        
        if (completionPanel != null)
            completionPanel.SetActive(false);
            
        // Restart the scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
    
    public void NextLevel()
    {
        // Find finish line and load next level
        FinishLine finishLine = FindObjectOfType<FinishLine>();
        if (finishLine != null)
        {
            finishLine.LoadNextLevel();
        }
        else
        {
            Debug.LogWarning("FinishLine not found! Cannot load next level.");
        }
    }
    
    public void MainMenu()
    {
        // Return to main menu
        Debug.Log("Main menu requested");
        // UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
    
    public void ToggleTimerUI()
    {
        showTimerUI = !showTimerUI;
        if (timerPanel != null)
            timerPanel.SetActive(showTimerUI);
    }
}
