using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class SceneLightingFix : MonoBehaviour
{
    [Header("Lighting Fix Settings")]
    [Tooltip("Ambient light color when no skybox is available")]
    public Color fallbackAmbientLight = new Color(0.3f, 0.3f, 0.3f, 1f);
    [Tooltip("Automatically fix camera clear flags")]
    public bool fixCameraClearFlags = true;
    [Tooltip("Force lighting update on scene load")]
    public bool forceUpdateLighting = true;
    [Tooltip("Show debug information")]
    public bool showDebugInfo = true;
    
    private void Awake()
    {
        // Fix lighting immediately when object is created
        FixLighting();
    }
    
    private void Start()
    {
        // Fix lighting again after everything is initialized
        FixLighting();
        
        // Subscribe to scene loading to fix lighting on every scene change
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Small delay to ensure everything is loaded
        Invoke(nameof(FixLighting), 0.1f);
    }
    
    public void FixLighting()
    {
        if (showDebugInfo)
            Debug.Log("SceneLightingFix: Attempting to fix lighting...");
        
        // 1. Fix Ambient Lighting
        FixAmbientLighting();
        
        // 2. Fix Camera Settings
        if (fixCameraClearFlags)
            FixCameraSettings();
        
        // 3. Force Update Dynamic GI
        if (forceUpdateLighting)
            ForceUpdateLighting();
        
        // 4. Fix Post-Processing (if present)
        FixPostProcessing();
        
        if (showDebugInfo)
            Debug.Log("SceneLightingFix: Lighting fix completed!");
    }
    
    private void FixAmbientLighting()
    {
        // Ensure ambient lighting is properly set
        if (RenderSettings.ambientMode == AmbientMode.Flat)
        {
            if (RenderSettings.ambientLight == Color.black)
            {
                RenderSettings.ambientLight = fallbackAmbientLight;
                if (showDebugInfo)
                    Debug.Log($"SceneLightingFix: Set ambient light to {fallbackAmbientLight}");
            }
        }
        
        // If using skybox but no skybox is assigned, set fallback
        if (RenderSettings.ambientMode == AmbientMode.Skybox && RenderSettings.skybox == null)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = fallbackAmbientLight;
            if (showDebugInfo)
                Debug.Log("SceneLightingFix: No skybox found, switched to flat ambient lighting");
        }
        
        // Ensure ambient intensity is not zero
        if (RenderSettings.ambientIntensity <= 0f)
        {
            RenderSettings.ambientIntensity = 1f;
            if (showDebugInfo)
                Debug.Log("SceneLightingFix: Fixed ambient intensity");
        }
    }
    
    private void FixCameraSettings()
    {
        Camera[] cameras = FindObjectsOfType<Camera>();
        
        foreach (Camera cam in cameras)
        {
            // Fix clear flags
            if (cam.clearFlags == CameraClearFlags.Nothing || cam.clearFlags == CameraClearFlags.Depth)
            {
                if (RenderSettings.skybox != null)
                {
                    cam.clearFlags = CameraClearFlags.Skybox;
                }
                else
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    // Set a reasonable background color if it's black
                    if (cam.backgroundColor == Color.black)
                    {
                        cam.backgroundColor = new Color(0.2f, 0.2f, 0.3f, 1f); // Dark blue-grey
                    }
                }
                
                if (showDebugInfo)
                    Debug.Log($"SceneLightingFix: Fixed camera '{cam.name}' clear flags to {cam.clearFlags}");
            }
        }
    }
    
    private void ForceUpdateLighting()
    {
        try
        {
            // Force update dynamic GI
            DynamicGI.UpdateEnvironment();
            
            if (showDebugInfo)
                Debug.Log("SceneLightingFix: Dynamic GI updated");
        }
        catch (System.Exception e)
        {
            if (showDebugInfo)
                Debug.LogWarning($"SceneLightingFix: Could not update Dynamic GI: {e.Message}");
        }
    }
    
    private void FixPostProcessing()
    {
        // Try to find and refresh post-processing volumes
        var postProcessVolumes = FindObjectsOfType<MonoBehaviour>();
        
        foreach (var component in postProcessVolumes)
        {
            // Check if it's a post-processing component (works with different post-processing systems)
            if (component.GetType().Name.Contains("PostProcess") || 
                component.GetType().Name.Contains("Volume"))
            {
                // Refresh by disabling and re-enabling
                component.enabled = false;
                component.enabled = true;
                
                if (showDebugInfo)
                    Debug.Log($"SceneLightingFix: Refreshed post-processing component: {component.GetType().Name}");
            }
        }
    }
    
    // Public method for manual testing
    [ContextMenu("Fix Lighting Now")]
    public void ManualFixLighting()
    {
        FixLighting();
    }
    
    // Debug method to show current lighting settings
    [ContextMenu("Show Lighting Info")]
    public void ShowLightingInfo()
    {
        Debug.Log("=== LIGHTING INFO ===");
        Debug.Log($"Ambient Mode: {RenderSettings.ambientMode}");
        Debug.Log($"Ambient Light: {RenderSettings.ambientLight}");
        Debug.Log($"Ambient Intensity: {RenderSettings.ambientIntensity}");
        Debug.Log($"Skybox: {(RenderSettings.skybox != null ? RenderSettings.skybox.name : "None")}");
        
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Debug.Log($"Main Camera Clear Flags: {mainCam.clearFlags}");
            Debug.Log($"Main Camera Background: {mainCam.backgroundColor}");
        }
        
        Debug.Log("===================");
    }
}
