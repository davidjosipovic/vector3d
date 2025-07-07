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
        FixLighting();
    }
    
    private void Start()
    {
        FixLighting();
        
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Invoke(nameof(FixLighting), 0.1f);
    }
    
    public void FixLighting()
    {
        if (showDebugInfo)
            Debug.Log("SceneLightingFix: Attempting to fix lighting...");
        
        FixAmbientLighting();
        
        if (fixCameraClearFlags)
            FixCameraSettings();
        
        if (forceUpdateLighting)
            ForceUpdateLighting();
        
        FixPostProcessing();
        
        if (showDebugInfo)
            Debug.Log("SceneLightingFix: Lighting fix completed!");
    }
    
    private void FixAmbientLighting()
    {
        if (RenderSettings.ambientMode == AmbientMode.Flat)
        {
            if (RenderSettings.ambientLight == Color.black)
            {
                RenderSettings.ambientLight = fallbackAmbientLight;
                if (showDebugInfo)
                    Debug.Log($"SceneLightingFix: Set ambient light to {fallbackAmbientLight}");
            }
        }
        
        if (RenderSettings.ambientMode == AmbientMode.Skybox && RenderSettings.skybox == null)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = fallbackAmbientLight;
            if (showDebugInfo)
                Debug.Log("SceneLightingFix: No skybox found, switched to flat ambient lighting");
        }
        
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
            if (cam.clearFlags == CameraClearFlags.Nothing || cam.clearFlags == CameraClearFlags.Depth)
            {
                if (RenderSettings.skybox != null)
                {
                    cam.clearFlags = CameraClearFlags.Skybox;
                }
                else
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    if (cam.backgroundColor == Color.black)
                    {
                        cam.backgroundColor = new Color(0.2f, 0.2f, 0.3f, 1f); 
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
       
        var postProcessVolumes = FindObjectsOfType<MonoBehaviour>();
        
        foreach (var component in postProcessVolumes)
        {
            if (component.GetType().Name.Contains("PostProcess") || 
                component.GetType().Name.Contains("Volume"))
            {
                component.enabled = false;
                component.enabled = true;
                
                if (showDebugInfo)
                    Debug.Log($"SceneLightingFix: Refreshed post-processing component: {component.GetType().Name}");
            }
        }
    }
    
}
