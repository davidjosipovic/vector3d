using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    public bool startTimerOnStart = true;
    public string timeFormat = "mm\\:ss\\:ff";

    private float startTime;
    private float endTime;
    private bool isTimerRunning = false;
    private bool levelCompleted = false;
    private float finalTime = 0f;

    public static LevelTimer Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
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
    }

    void Update()
    {
        if (isTimerRunning && !levelCompleted)
        {
        }
    }

    public void StartTimer()
    {
        startTime = Time.time;
        isTimerRunning = true;
        levelCompleted = false;
    }

    public void StopTimer()
    {
        if (isTimerRunning && !levelCompleted)
        {
            endTime = Time.time;
            finalTime = endTime - startTime;
            isTimerRunning = false;
            levelCompleted = true;

            if (GameProgressManager.Instance != null)
            {
                GameProgressManager.Instance.OnLevelCompleted(finalTime);
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
        System.TimeSpan timeSpan = System.TimeSpan.FromSeconds(timeInSeconds);

        switch (timeFormat)
        {
            case "mm\\:ss\\:ff":
                return string.Format("{0:00}:{1:00}:{2:00}", 
                    timeSpan.Minutes, 
                    timeSpan.Seconds, 
                    timeSpan.Milliseconds / 10);

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

    public void OnPlayerRespawn()
    {
    }

    public void RestartLevel()
    {
        ResetTimer();
    }

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
            ResetTimer();
            if (startTimerOnStart)
            {
                StartTimer();
            }
        }
    }
}
