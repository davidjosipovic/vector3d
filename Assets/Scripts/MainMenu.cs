using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [Header("Main Menu")]
    public GameObject mainMenuPanel;
    public GameObject gameCompletePanel;
    
    [Header("Game Complete UI")]
    public TextMeshProUGUI level1TimeText;
    public TextMeshProUGUI level2TimeText;
    public TextMeshProUGUI level3TimeText;
    public TextMeshProUGUI totalTimeText;
    public Button playAgainButton;
    public Button mainMenuButton;
    
    private void Start()
    {
        Debug.Log("🏠 MainMenu Start() called");
        
        // Start main menu music
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMainMenuMusic();
            Debug.Log("🎵 Main menu music started");
        }
        else
        {
            Debug.LogWarning("AudioManager not found! Music will not play.");
        }
        
        // Check if game was just completed
        if (GameProgressManager.Instance != null)
        {
            Debug.Log($"GameProgressManager found - IsGameCompleted: {GameProgressManager.Instance.IsGameCompleted()}");
            
            // Check if we have any level times recorded (even if gameCompleted flag isn't set)
            var levelTimes = GameProgressManager.Instance.GetLevelTimes();
            bool hasAnyLevelTimes = false;
            for (int i = 0; i < levelTimes.Length; i++)
            {
                if (levelTimes[i] > 0f)
                {
                    hasAnyLevelTimes = true;
                    break;
                }
            }
            
            if (GameProgressManager.Instance.IsGameCompleted() || hasAnyLevelTimes)
            {
                Debug.Log("Game completed detected OR has level times - showing completion screen");
                ShowGameCompleteScreen();
            }
            else
            {
                Debug.Log("Game not completed and no times - showing main menu");
                ShowMainMenu();
            }
        }
        else
        {
            Debug.LogWarning("GameProgressManager.Instance is null in MainMenu Start()!");
            ShowMainMenu();
        }
    }
    
    public void StartGame()
    {
        // Reset game progress when starting new game
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.ResetGameProgress();
        }
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
    
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game Quit");
    }
    
    public void PlayAgain()
    {
        // Reset progress and start from level 1
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.ResetGameProgress();
        }
        
        // Load first level (assuming it's scene index 1)
        SceneManager.LoadScene(1);
    }
    
    public void ReturnToMainMenu()
    {
        ShowMainMenu();
    }
    
    private void ShowMainMenu()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
        if (gameCompletePanel != null)
            gameCompletePanel.SetActive(false);
            
        // Restore normal time scale
        Time.timeScale = 1f;
    }
    
    private void ShowGameCompleteScreen()
    {
        // Don't hide main menu - keep it visible
        // if (mainMenuPanel != null)
        //     mainMenuPanel.SetActive(false);
        
        if (gameCompletePanel != null)
            gameCompletePanel.SetActive(true);
            
        // Display level times
        DisplayLevelTimes();
        
        // Freeze the game
        Time.timeScale = 0f;
        
        // Unlock cursor for menu interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    private void DisplayLevelTimes()
    {
        if (GameProgressManager.Instance == null) 
        {
            Debug.LogWarning("GameProgressManager.Instance is null in MainMenu!");
            
            // Show placeholder text
            if (level1TimeText != null) level1TimeText.text = "Level 1: No data";
            if (level2TimeText != null) level2TimeText.text = "Level 2: No data";
            if (level3TimeText != null) level3TimeText.text = "Level 3: No data";
            if (totalTimeText != null) totalTimeText.text = "Total Time: No data";
            return;
        }
        
        var levelTimes = GameProgressManager.Instance.GetLevelTimes();
        float totalTime = 0f;
        
        Debug.Log($"📊 MainMenu DisplayLevelTimes - levelTimes array length: {levelTimes.Length}");
        for (int i = 0; i < levelTimes.Length; i++)
        {
            Debug.Log($"  levelTimes[{i}] = {levelTimes[i]} ({FormatTime(levelTimes[i])})");
        }
        Debug.Log($"GameCompleted: {GameProgressManager.Instance.IsGameCompleted()}");
        
        // Level 1 time
        if (level1TimeText != null)
        {
            if (levelTimes.Length > 0 && levelTimes[0] > 0f)
            {
                level1TimeText.text = "Level 1: " + FormatTime(levelTimes[0]);
                totalTime += levelTimes[0];
            }
            else
            {
                level1TimeText.text = "Level 1: Not completed";
            }
        }
        
        // Level 2 time
        if (level2TimeText != null)
        {
            if (levelTimes.Length > 1 && levelTimes[1] > 0f)
            {
                level2TimeText.text = "Level 2: " + FormatTime(levelTimes[1]);
                totalTime += levelTimes[1];
            }
            else
            {
                level2TimeText.text = "Level 2: Not completed";
            }
        }
        
        // Level 3 time
        if (level3TimeText != null)
        {
            if (levelTimes.Length > 2 && levelTimes[2] > 0f)
            {
                level3TimeText.text = "Level 3: " + FormatTime(levelTimes[2]);
                totalTime += levelTimes[2];
            }
            else
            {
                level3TimeText.text = "Level 3: Not completed";
            }
        }
        
        // Total time
        if (totalTimeText != null)
        {
            if (totalTime > 0f)
            {
                totalTimeText.text = "Total Time: " + FormatTime(totalTime);
            }
            else
            {
                totalTimeText.text = "Total Time: --:--:---";
            }
        }
        
        Debug.Log($"📊 Total time calculated: {FormatTime(totalTime)}");
    }
    
    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        int milliseconds = Mathf.FloorToInt((timeInSeconds * 1000f) % 1000f);
        
        return string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
    }
}
