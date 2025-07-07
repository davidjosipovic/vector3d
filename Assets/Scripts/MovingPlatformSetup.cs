using UnityEngine;

[System.Serializable]
public class PlatformPoint
{
    public Vector3 position;
    public string name;
    
    public PlatformPoint(Vector3 pos, string pointName = "")
    {
        position = pos;
        name = pointName;
    }
}

public class MovingPlatformSetup : MonoBehaviour
{
    [Header("Platform Setup Helper")]
    [Tooltip("The moving platform to configure")]
    public MovingPlatform targetPlatform;
    
    [Header("Quick Setup")]
    [Tooltip("Distance to move from current position")]
    public Vector3 moveDistance = new Vector3(5f, 0f, 0f);
    [Tooltip("Create points relative to platform's current position")]
    public bool useRelativePositions = true;
    
    [Header("Manual Point Setup")]
    public PlatformPoint pointA = new PlatformPoint(Vector3.zero, "Point A");
    public PlatformPoint pointB = new PlatformPoint(Vector3.right * 5f, "Point B");
    
    [Header("Auto-Generated Points")]
    [SerializeField] private Transform generatedPointA;
    [SerializeField] private Transform generatedPointB;
    
    [ContextMenu("Auto Setup Platform")]
    public void AutoSetupPlatform()
    {
        if (targetPlatform == null)
        {
            targetPlatform = GetComponent<MovingPlatform>();
            if (targetPlatform == null)
            {
                Debug.LogError("No MovingPlatform found! Please assign targetPlatform or add MovingPlatform component.");
                return;
            }
        }
        
        CreatePoints();
        AssignPointsToPlatform();
        
        Debug.Log($"Moving platform '{targetPlatform.gameObject.name}' setup completed!");
    }
    
    [ContextMenu("Create Points")]
    public void CreatePoints()
    {
        Vector3 basePosition = useRelativePositions ? transform.position : Vector3.zero;
        
        if (generatedPointA == null)
        {
            GameObject pointAObj = new GameObject("Point A");
            generatedPointA = pointAObj.transform;
            generatedPointA.SetParent(transform);
        }
        
        if (generatedPointB == null)
        {
            GameObject pointBObj = new GameObject("Point B");
            generatedPointB = pointBObj.transform;
            generatedPointB.SetParent(transform);
        }
        
        if (useRelativePositions)
        {
            generatedPointA.position = basePosition;
            generatedPointB.position = basePosition + moveDistance;
        }
        else
        {
            generatedPointA.position = pointA.position;
            generatedPointB.position = pointB.position;
        }
        
        AddPointVisuals(generatedPointA, Color.green);
        AddPointVisuals(generatedPointB, Color.red);
        
        Debug.Log("Platform points created successfully!");
    }
    
    private void AddPointVisuals(Transform point, Color color)
    {
        Transform existing = point.Find("Visual");
        if (existing != null)
        {
            DestroyImmediate(existing.gameObject);
        }
        
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "Visual";
        visual.transform.SetParent(point);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = Vector3.one * 0.5f;
        
        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Glossiness", 0.5f);
            renderer.material = mat;
        }
        
        Collider col = visual.GetComponent<Collider>();
        if (col != null)
        {
            DestroyImmediate(col);
        }
    }
    
    [ContextMenu("Assign Points to Platform")]
    public void AssignPointsToPlatform()
    {
        if (targetPlatform == null)
        {
            Debug.LogError("Target platform not assigned!");
            return;
        }
        
        if (generatedPointA != null && generatedPointB != null)
        {
            targetPlatform.pointA = generatedPointA;
            targetPlatform.pointB = generatedPointB;
            Debug.Log("Points assigned to platform successfully!");
        }
        else
        {
            Debug.LogWarning("Points not generated yet! Use 'Create Points' first.");
        }
    }
    
    [ContextMenu("Test Platform Movement")]
    public void TestPlatformMovement()
    {
        if (targetPlatform == null)
        {
            Debug.LogWarning("No target platform assigned!");
            return;
        }
        
        if (Application.isPlaying)
        {
            if (targetPlatform.IsMoving())
            {
                targetPlatform.StopMoving();
                Debug.Log("Platform stopped for testing");
            }
            else
            {
                targetPlatform.StartMoving();
                Debug.Log("Platform started for testing");
            }
        }
        else
        {
            Debug.Log("Enter Play mode to test platform movement");
        }
    }
    
    [ContextMenu("Reset Platform Position")]
    public void ResetPlatformPosition()
    {
        if (targetPlatform != null && generatedPointA != null)
        {
            targetPlatform.transform.position = generatedPointA.position;
            Debug.Log("Platform position reset to Point A");
        }
    }
    
    private void OnDrawGizmos()
    {
        if (targetPlatform == null)
            return;
            
        Vector3 basePos = useRelativePositions ? transform.position : Vector3.zero;
        Vector3 startPos = useRelativePositions ? basePos : pointA.position;
        Vector3 endPos = useRelativePositions ? basePos + moveDistance : pointB.position;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(startPos, endPos);
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(startPos, 0.3f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(endPos, 0.3f);
        
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(startPos + Vector3.up * 0.5f, "A");
        UnityEditor.Handles.Label(endPos + Vector3.up * 0.5f, "B");
        #endif
    }
    
    private void OnDrawGizmosSelected()
    {
        if (useRelativePositions)
        {
            Gizmos.color = Color.cyan;
            Vector3 basePos = transform.position;
            Gizmos.DrawWireSphere(basePos, 0.2f);
            
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(basePos, moveDistance);
        }
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(MovingPlatformSetup))]
public class MovingPlatformSetupEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        MovingPlatformSetup setup = (MovingPlatformSetup)target;
        
        UnityEditor.EditorGUILayout.Space();
        UnityEditor.EditorGUILayout.LabelField("Quick Actions", UnityEditor.EditorStyles.boldLabel);
        
        if (GUILayout.Button("Auto Setup Platform"))
        {
            setup.AutoSetupPlatform();
        }
        
        UnityEditor.EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Create Points"))
        {
            setup.CreatePoints();
        }
        if (GUILayout.Button("Assign to Platform"))
        {
            setup.AssignPointsToPlatform();
        }
        UnityEditor.EditorGUILayout.EndHorizontal();
        
        UnityEditor.EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset Position"))
        {
            setup.ResetPlatformPosition();
        }
        if (GUILayout.Button("Test Movement"))
        {
            setup.TestPlatformMovement();
        }
        UnityEditor.EditorGUILayout.EndHorizontal();
        
        if (setup.targetPlatform != null)
        {
            UnityEditor.EditorGUILayout.Space();
            UnityEditor.EditorGUILayout.LabelField("Platform Status", UnityEditor.EditorStyles.boldLabel);
            
            if (Application.isPlaying)
            {
                UnityEditor.EditorGUILayout.LabelField("Moving:", setup.targetPlatform.IsMoving().ToString());
                UnityEditor.EditorGUILayout.LabelField("Waiting:", setup.targetPlatform.IsWaiting().ToString());
                UnityEditor.EditorGUILayout.LabelField("Progress:", $"{setup.targetPlatform.GetProgress():P1}");
            }
            else
            {
                UnityEditor.EditorGUILayout.LabelField("Enter Play mode to see status");
            }
        }
    }
}
#endif
