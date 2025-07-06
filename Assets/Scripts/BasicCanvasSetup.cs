using UnityEngine;
using UnityEngine.UI;

public class BasicCanvasSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    public bool autoCreateUI = true;
    public bool useTextMeshPro = true; // Set to false to use legacy UI Text
    
    [Header("Timer UI Settings")]
    public Vector2 timerPosition = new Vector2(20, -20); // Top-left corner
    public Vector2 timerSize = new Vector2(200, 50);
    public int timerFontSize = 24;
    public Color timerColor = Color.white;
    
    [Header("Completion UI Settings")]
    public Vector2 completionPanelSize = new Vector2(400, 300);
    public Color completionBackgroundColor = new Color(0, 0, 0, 0.8f);
    
    void Start()
    {
        if (autoCreateUI)
        {
            CreateBasicUI();
        }
    }
    
    [ContextMenu("Create Basic UI")]
    public void CreateBasicUI()
    {
        // Find or create Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Game Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        
        // Create timer UI
        CreateTimerUI(canvas);
        
        // Create completion UI
        CreateCompletionUI(canvas);
        
        // Setup GameUI component
        SetupGameUI(canvas);
        
        Debug.Log("Basic game UI created successfully!");
    }
    
    private void CreateTimerUI(Canvas canvas)
    {
        // Timer Panel
        GameObject timerPanel = new GameObject("Timer Panel");
        timerPanel.transform.SetParent(canvas.transform, false);
        
        RectTransform timerPanelRT = timerPanel.AddComponent<RectTransform>();
        timerPanelRT.anchorMin = new Vector2(0, 1); // Top-left
        timerPanelRT.anchorMax = new Vector2(0, 1);
        timerPanelRT.pivot = new Vector2(0, 1);
        timerPanelRT.anchoredPosition = timerPosition;
        timerPanelRT.sizeDelta = timerSize;
        
        // Timer Text
        GameObject timerTextGO = new GameObject("Timer Text");
        timerTextGO.transform.SetParent(timerPanel.transform, false);
        
        RectTransform timerTextRT = timerTextGO.AddComponent<RectTransform>();
        timerTextRT.anchorMin = Vector2.zero;
        timerTextRT.anchorMax = Vector2.one;
        timerTextRT.offsetMin = Vector2.zero;
        timerTextRT.offsetMax = Vector2.zero;
        
        if (useTextMeshPro)
        {
            TMPro.TextMeshProUGUI timerText = timerTextGO.AddComponent<TMPro.TextMeshProUGUI>();
            timerText.text = "00:00:00";
            timerText.fontSize = timerFontSize;
            timerText.color = timerColor;
            timerText.alignment = TMPro.TextAlignmentOptions.Center;
        }
        else
        {
            Text timerText = timerTextGO.AddComponent<Text>();
            timerText.text = "00:00:00";
            timerText.fontSize = timerFontSize;
            timerText.color = timerColor;
            timerText.alignment = TextAnchor.MiddleCenter;
            timerText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
    
    private void CreateCompletionUI(Canvas canvas)
    {
        // Completion Panel (initially hidden)
        GameObject completionPanel = new GameObject("Completion Panel");
        completionPanel.transform.SetParent(canvas.transform, false);
        completionPanel.SetActive(false);
        
        RectTransform completionPanelRT = completionPanel.AddComponent<RectTransform>();
        completionPanelRT.anchorMin = new Vector2(0.5f, 0.5f); // Center
        completionPanelRT.anchorMax = new Vector2(0.5f, 0.5f);
        completionPanelRT.pivot = new Vector2(0.5f, 0.5f);
        completionPanelRT.anchoredPosition = Vector2.zero;
        completionPanelRT.sizeDelta = completionPanelSize;
        
        // Background
        Image completionBG = completionPanel.AddComponent<Image>();
        completionBG.color = completionBackgroundColor;
        
        // Title Text
        GameObject titleGO = new GameObject("Title Text");
        titleGO.transform.SetParent(completionPanel.transform, false);
        
        RectTransform titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0.7f);
        titleRT.anchorMax = new Vector2(1, 0.9f);
        titleRT.offsetMin = new Vector2(20, 0);
        titleRT.offsetMax = new Vector2(-20, 0);
        
        if (useTextMeshPro)
        {
            TMPro.TextMeshProUGUI titleText = titleGO.AddComponent<TMPro.TextMeshProUGUI>();
            titleText.text = "Level Completed!";
            titleText.fontSize = 28;
            titleText.color = Color.white;
            titleText.alignment = TMPro.TextAlignmentOptions.Center;
        }
        else
        {
            Text titleText = titleGO.AddComponent<Text>();
            titleText.text = "Level Completed!";
            titleText.fontSize = 28;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        
        // Time Text
        GameObject timeGO = new GameObject("Time Text");
        timeGO.transform.SetParent(completionPanel.transform, false);
        
        RectTransform timeRT = timeGO.AddComponent<RectTransform>();
        timeRT.anchorMin = new Vector2(0, 0.4f);
        timeRT.anchorMax = new Vector2(1, 0.6f);
        timeRT.offsetMin = new Vector2(20, 0);
        timeRT.offsetMax = new Vector2(-20, 0);
        
        if (useTextMeshPro)
        {
            TMPro.TextMeshProUGUI timeText = timeGO.AddComponent<TMPro.TextMeshProUGUI>();
            timeText.text = "Final Time: 00:00:00";
            timeText.fontSize = 20;
            timeText.color = Color.green;
            timeText.alignment = TMPro.TextAlignmentOptions.Center;
        }
        else
        {
            Text timeText = timeGO.AddComponent<Text>();
            timeText.text = "Final Time: 00:00:00";
            timeText.fontSize = 20;
            timeText.color = Color.green;
            timeText.alignment = TextAnchor.MiddleCenter;
            timeText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        
        // Buttons
        CreateButton(completionPanel, "Next Level Button", new Vector2(0, 0.25f), new Vector2(0.45f, 0.45f), "Next Level");
        CreateButton(completionPanel, "Restart Button", new Vector2(0.55f, 0.25f), new Vector2(1, 0.45f), "Restart Level");
    }
    
    private void CreateButton(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, string buttonText)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent.transform, false);
        
        RectTransform buttonRT = buttonGO.AddComponent<RectTransform>();
        buttonRT.anchorMin = anchorMin;
        buttonRT.anchorMax = anchorMax;
        buttonRT.offsetMin = new Vector2(20, 0);
        buttonRT.offsetMax = new Vector2(-20, 0);
        
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = Color.gray;
        
        Button button = buttonGO.AddComponent<Button>();
        
        // Button Text
        GameObject buttonTextGO = new GameObject("Text");
        buttonTextGO.transform.SetParent(buttonGO.transform, false);
        
        RectTransform buttonTextRT = buttonTextGO.AddComponent<RectTransform>();
        buttonTextRT.anchorMin = Vector2.zero;
        buttonTextRT.anchorMax = Vector2.one;
        buttonTextRT.offsetMin = Vector2.zero;
        buttonTextRT.offsetMax = Vector2.zero;
        
        if (useTextMeshPro)
        {
            TMPro.TextMeshProUGUI buttonTextComp = buttonTextGO.AddComponent<TMPro.TextMeshProUGUI>();
            buttonTextComp.text = buttonText;
            buttonTextComp.fontSize = 18;
            buttonTextComp.color = Color.white;
            buttonTextComp.alignment = TMPro.TextAlignmentOptions.Center;
        }
        else
        {
            Text buttonTextComp = buttonTextGO.AddComponent<Text>();
            buttonTextComp.text = buttonText;
            buttonTextComp.fontSize = 18;
            buttonTextComp.color = Color.white;
            buttonTextComp.alignment = TextAnchor.MiddleCenter;
            buttonTextComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
    
    private void SetupGameUI(Canvas canvas)
    {
        // Add GameUI component to canvas
        GameUI gameUI = canvas.GetComponent<GameUI>();
        if (gameUI == null)
        {
            gameUI = canvas.gameObject.AddComponent<GameUI>();
        }
        
        // Find and assign UI references
        Transform timerPanel = canvas.transform.Find("Timer Panel");
        if (timerPanel != null)
        {
            gameUI.timerPanel = timerPanel.gameObject;
            
            if (useTextMeshPro)
                gameUI.timerTextTMP = timerPanel.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            else
                gameUI.timerText = timerPanel.GetComponentInChildren<Text>();
        }
        
        Transform completionPanel = canvas.transform.Find("Completion Panel");
        if (completionPanel != null)
        {
            gameUI.completionPanel = completionPanel.gameObject;
            
            if (useTextMeshPro)
            {
                TMPro.TextMeshProUGUI[] texts = completionPanel.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
                if (texts.Length > 1)
                    gameUI.completionTimeTextTMP = texts[1]; // Second text is the time
            }
            else
            {
                Text[] texts = completionPanel.GetComponentsInChildren<Text>();
                if (texts.Length > 1)
                    gameUI.completionTimeText = texts[1]; // Second text is the time
            }
            
            // Connect buttons
            Button[] buttons = completionPanel.GetComponentsInChildren<Button>();
            foreach (Button button in buttons)
            {
                if (button.name == "Next Level Button")
                {
                    gameUI.nextLevelButton = button;
                    button.onClick.AddListener(gameUI.NextLevel);
                    Debug.Log("✅ Next Level button connected");
                }
                else if (button.name == "Restart Button")
                {
                    gameUI.restartButton = button;
                    button.onClick.AddListener(gameUI.RestartLevel);
                    Debug.Log("✅ Restart button connected");
                }
            }
        }
        
        Debug.Log("GameUI component configured successfully!");
        
        // Verify the setup
        if (gameUI.timerText != null || gameUI.timerTextTMP != null)
        {
            Debug.Log("✅ Timer UI components connected successfully!");
        }
        else
        {
            Debug.LogWarning("⚠️ Timer UI components not connected! Check setup.");
        }
    }
}
