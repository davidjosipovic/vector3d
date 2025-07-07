using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("Platform Settings")]
    [Tooltip("The starting position (Point A)")]
    public Transform pointA;
    [Tooltip("The ending position (Point B)")]
    public Transform pointB;
    [Tooltip("Speed of movement between points")]
    public float moveSpeed = 2f;
    [Tooltip("Time to wait at each point before moving")]
    public float waitTime = 1f;
    [Tooltip("Should the platform start moving immediately?")]
    public bool autoStart = true;
    [Tooltip("Start from current position instead of Point A")]
    public bool startFromCurrentPosition = true;
    [Tooltip("Use object's surface/bottom instead of center for positioning")]
    public bool useObjectSurface = true;
    [Tooltip("Additional height offset from the surface")]
    public float surfaceOffset = 0f;
    
    [Header("Movement Options")]
    [Tooltip("Use smooth easing movement instead of linear")]
    public bool useSmoothMovement = false;
    [Tooltip("Animation curve for smooth movement (if enabled)")]
    public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Player Detection")]
    [Tooltip("Should the platform carry the player?")]
    public bool carryPlayer = true;
    [Tooltip("Layer mask for objects that should be carried")]
    public LayerMask carryableLayers = -1;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    public bool showGizmos = true;
    
    // Private variables
    private Vector3 targetPosition;
    private Vector3 startPosition;
    private bool movingToB = true;
    private bool isMoving = false;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private float journeyLength;
    private float journeyTime = 0f;
    
    // Player carrying
    private Transform carriedObject = null;
    private Vector3 carriedObjectOffset;
    
    void Start()
    {
        // Initialize positions
        if (pointA == null || pointB == null)
        {
            Debug.LogError($"MovingPlatform '{gameObject.name}': Point A and Point B must be assigned!");
            enabled = false;
            return;
        }
        
        // Calculate journey length
        journeyLength = Vector3.Distance(pointA.position, pointB.position);
        
        if (startFromCurrentPosition)
        {
            // Adjust current position to surface if enabled
            if (useObjectSurface)
            {
                Vector3 adjustedPosition = AdjustPositionToSurface(transform.position);
                transform.position = adjustedPosition;
            }
            
            // Determine which direction to move based on current position
            DetermineStartingDirection();
        }
        else
        {
            // Original behavior - start from Point A (adjusted to surface if enabled)
            Vector3 startPos = useObjectSurface ? AdjustPositionToSurface(pointA.position) : pointA.position;
            transform.position = startPos;
            startPosition = startPos;
            targetPosition = useObjectSurface ? AdjustPositionToSurface(pointB.position) : pointB.position;
            movingToB = true;
        }
        
        if (autoStart)
        {
            StartMoving();
        }
        
        if (showDebugInfo)
            Debug.Log($"MovingPlatform '{gameObject.name}' initialized. Distance: {journeyLength:F2} units, Starting direction: {(movingToB ? "A→B" : "B→A")}");
    }
    
    void Update()
    {
        if (!isMoving && !isWaiting)
            return;
            
        if (isWaiting)
        {
            HandleWaiting();
        }
        else if (isMoving)
        {
            HandleMovement();
        }
    }
    
    private void HandleWaiting()
    {
        waitTimer += Time.deltaTime;
        
        if (waitTimer >= waitTime)
        {
            waitTimer = 0f;
            isWaiting = false;
            
            // Switch target
            if (movingToB)
            {
                startPosition = useObjectSurface ? AdjustPositionToSurface(pointB.position) : pointB.position;
                targetPosition = useObjectSurface ? AdjustPositionToSurface(pointA.position) : pointA.position;
                movingToB = false;
            }
            else
            {
                startPosition = useObjectSurface ? AdjustPositionToSurface(pointA.position) : pointA.position;
                targetPosition = useObjectSurface ? AdjustPositionToSurface(pointB.position) : pointB.position;
                movingToB = true;
            }
            
            journeyTime = 0f;
            isMoving = true;
            
            if (showDebugInfo)
                Debug.Log($"Platform moving to {(movingToB ? "Point B" : "Point A")}");
        }
    }
    
    private void HandleMovement()
    {
        journeyTime += Time.deltaTime;
        
        // Calculate the actual distance for this movement segment
        float currentJourneyLength = Vector3.Distance(startPosition, targetPosition);
        
        float journeyFraction;
        
        if (useSmoothMovement)
        {
            // Use animation curve for smooth movement
            float normalizedTime = (journeyTime * moveSpeed) / currentJourneyLength;
            journeyFraction = movementCurve.Evaluate(Mathf.Clamp01(normalizedTime));
        }
        else
        {
            // Linear movement
            journeyFraction = (journeyTime * moveSpeed) / currentJourneyLength;
        }
        
        // Clamp to prevent overshooting
        journeyFraction = Mathf.Clamp01(journeyFraction);
        
        // Store previous position for carrying objects
        Vector3 previousPosition = transform.position;
        
        // Move platform
        transform.position = Vector3.Lerp(startPosition, targetPosition, journeyFraction);
        
        // Update carried object position
        if (carryPlayer && carriedObject != null)
        {
            Vector3 platformMovement = transform.position - previousPosition;
            carriedObject.position += platformMovement;
        }
        
        // Check if reached target
        if (journeyFraction >= 1f)
        {
            isMoving = false;
            
            if (waitTime > 0f)
            {
                isWaiting = true;
                waitTimer = 0f;
            }
            else
            {
                // Immediately switch direction
                HandleWaiting();
            }
            
            if (showDebugInfo)
                Debug.Log($"Platform reached {(movingToB ? "Point B" : "Point A")}");
        }
    }
    
    public void StartMoving()
    {
        if (pointA == null || pointB == null)
        {
            Debug.LogWarning($"MovingPlatform '{gameObject.name}': Cannot start - points not assigned!");
            return;
        }
        
        isMoving = true;
        isWaiting = false;
        journeyTime = 0f;
        
        if (showDebugInfo)
            Debug.Log($"Platform started moving");
    }
    
    public void StopMoving()
    {
        isMoving = false;
        isWaiting = false;
        
        if (showDebugInfo)
            Debug.Log($"Platform stopped moving");
    }
    
    public void PauseMoving()
    {
        bool wasPaused = !isMoving && !isWaiting;
        isMoving = !wasPaused && isMoving;
        isWaiting = !wasPaused && isWaiting;
        
        if (showDebugInfo)
            Debug.Log($"Platform {(wasPaused ? "resumed" : "paused")}");
    }
    
    // Player/Object carrying functionality
    private void OnTriggerEnter(Collider other)
    {
        if (!carryPlayer)
            return;
            
        // Check if object is on the carryable layers
        if (((1 << other.gameObject.layer) & carryableLayers) != 0)
        {
            carriedObject = other.transform;
            carriedObjectOffset = carriedObject.position - transform.position;
            
            if (showDebugInfo)
                Debug.Log($"Platform started carrying: {other.gameObject.name}");
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (!carryPlayer)
            return;
            
        if (other.transform == carriedObject)
        {
            if (showDebugInfo)
                Debug.Log($"Platform stopped carrying: {other.gameObject.name}");
                
            carriedObject = null;
        }
    }
    
    // Gizmos for editor visualization
    private void OnDrawGizmos()
    {
        if (!showGizmos || pointA == null || pointB == null)
            return;
            
        // Get adjusted positions if using surface positioning
        Vector3 pointAPos = useObjectSurface ? AdjustPositionToSurface(pointA.position) : pointA.position;
        Vector3 pointBPos = useObjectSurface ? AdjustPositionToSurface(pointB.position) : pointB.position;
        
        // Draw line between adjusted points
        Gizmos.color = useObjectSurface ? Color.yellow : Color.white;
        Gizmos.DrawLine(pointAPos, pointBPos);
        
        if (useObjectSurface)
        {
            // Draw original points (smaller, gray)
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(pointA.position, 0.15f);
            Gizmos.DrawWireSphere(pointB.position, 0.15f);
            
            // Draw adjusted surface points (larger, colored)
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(pointAPos, 0.3f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pointBPos, 0.3f);
            
            // Draw vertical lines showing surface adjustment
            float adjustmentDistanceA = Vector3.Distance(pointA.position, pointAPos);
            float adjustmentDistanceB = Vector3.Distance(pointB.position, pointBPos);
            
            if (adjustmentDistanceA > 0.1f)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.7f); // Semi-transparent green
                Gizmos.DrawLine(pointA.position, pointAPos);
                
                // Draw small markers along the adjustment line
                for (int i = 1; i < 4; i++)
                {
                    Vector3 markerPos = Vector3.Lerp(pointA.position, pointAPos, i / 4f);
                    Gizmos.DrawWireSphere(markerPos, 0.05f);
                }
            }
            
            if (adjustmentDistanceB > 0.1f)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.7f); // Semi-transparent red
                Gizmos.DrawLine(pointB.position, pointBPos);
                
                // Draw small markers along the adjustment line
                for (int i = 1; i < 4; i++)
                {
                    Vector3 markerPos = Vector3.Lerp(pointB.position, pointBPos, i / 4f);
                    Gizmos.DrawWireSphere(markerPos, 0.05f);
                }
            }
            
            // Draw surface offset visualization
            if (surfaceOffset > 0.01f)
            {
                Gizmos.color = Color.cyan;
                Vector3 surfacePointA = new Vector3(pointAPos.x, pointAPos.y - surfaceOffset, pointAPos.z);
                Vector3 surfacePointB = new Vector3(pointBPos.x, pointBPos.y - surfaceOffset, pointBPos.z);
                Gizmos.DrawWireSphere(surfacePointA, 0.1f);
                Gizmos.DrawWireSphere(surfacePointB, 0.1f);
                Gizmos.DrawLine(surfacePointA, pointAPos);
                Gizmos.DrawLine(surfacePointB, pointBPos);
            }
        }
        else
        {
            // Original behavior - no surface adjustment
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(pointAPos, 0.3f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pointBPos, 0.3f);
        }
        
        // Draw platform position
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
        
        // Show current progress on the line
        float progress = GetProgressOnLine();
        Gizmos.color = Color.cyan;
        Vector3 progressPos = Vector3.Lerp(pointAPos, pointBPos, progress);
        Gizmos.DrawWireSphere(progressPos, 0.2f);
        
        // Draw direction arrow
        if (Application.isPlaying && isMoving)
        {
            Gizmos.color = movingToB ? Color.red : Color.green;
            Vector3 direction = (targetPosition - transform.position).normalized;
            Gizmos.DrawRay(transform.position, direction * 1f);
            
            // Draw target position
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(targetPosition, 0.25f);
        }
        else if (!Application.isPlaying && startFromCurrentPosition)
        {
            // Preview direction in editor
            Vector3 currentPos = transform.position;
            
            float distanceToA = Vector3.Distance(currentPos, pointAPos);
            float distanceToB = Vector3.Distance(currentPos, pointBPos);
            
            Gizmos.color = distanceToA < distanceToB ? Color.red : Color.green;
            Vector3 previewDirection = distanceToA < distanceToB ? 
                (pointBPos - currentPos).normalized : 
                (pointAPos - currentPos).normalized;
            Gizmos.DrawRay(transform.position, previewDirection * 1f);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (pointA == null || pointB == null)
            return;
            
        // Draw more detailed info when selected
        Gizmos.color = Color.cyan;
        
        // Draw platform bounds
        Collider platformCollider = GetComponent<Collider>();
        if (platformCollider != null)
        {
            Gizmos.DrawWireCube(pointA.position, platformCollider.bounds.size);
            Gizmos.DrawWireCube(pointB.position, platformCollider.bounds.size);
        }
        
        // Show movement path with segments
        Vector3 pathDirection = pointB.position - pointA.position;
        int segments = 10;
        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / segments;
            Vector3 pos = Vector3.Lerp(pointA.position, pointB.position, t);
            Gizmos.DrawWireSphere(pos, 0.1f);
        }
    }
    
    // Public methods for external control
    public bool IsMoving()
    {
        return isMoving;
    }
    
    public bool IsWaiting()
    {
        return isWaiting;
    }
    
    public Vector3 GetCurrentTarget()
    {
        return targetPosition;
    }
    
    public float GetProgress()
    {
        if (!isMoving || targetPosition == Vector3.zero)
            return 0f;
            
        float currentJourneyLength = Vector3.Distance(startPosition, targetPosition);
        if (currentJourneyLength <= 0f)
            return 0f;
            
        return (journeyTime * moveSpeed) / currentJourneyLength;
    }
    
    /// <summary>
    /// Manually set the platform position and recalculate movement direction
    /// </summary>
    public void SetPositionAndRecalculate(Vector3 newPosition)
    {
        transform.position = newPosition;
        
        if (pointA != null && pointB != null)
        {
            DetermineStartingDirection();
            
            if (showDebugInfo)
                Debug.Log($"Platform position updated. New direction: {(movingToB ? "A→B" : "B→A")}");
        }
    }
    
    /// <summary>
    /// Get the current progress along the entire A-B line (0 = at A, 1 = at B)
    /// </summary>
    public float GetProgressOnLine()
    {
        if (pointA == null || pointB == null || journeyLength <= 0f)
            return 0f;
            
        Vector3 currentPos = transform.position;
        Vector3 pointAPos = pointA.position;
        Vector3 pointBPos = pointB.position;
        
        Vector3 lineDirection = pointBPos - pointAPos;
        Vector3 pointToA = currentPos - pointAPos;
        
        float t = Vector3.Dot(pointToA, lineDirection.normalized) / journeyLength;
        return Mathf.Clamp01(t);
    }
    
    /// <summary>
    /// Force the platform to move towards a specific point (A or B)
    /// </summary>
    public void SetDirection(bool moveTowardsB)
    {
        if (pointA == null || pointB == null)
            return;
            
        movingToB = moveTowardsB;
        startPosition = transform.position;
        Vector3 targetPoint = moveTowardsB ? pointB.position : pointA.position;
        targetPosition = useObjectSurface ? AdjustPositionToSurface(targetPoint) : targetPoint;
        
        // Reset journey time to start fresh from current position
        journeyTime = 0f;
        
        if (showDebugInfo)
            Debug.Log($"Platform direction set to: {(movingToB ? "A→B" : "B→A")}");
    }
    
    [ContextMenu("Adjust Position to Surface")]
    public void AdjustToSurface()
    {
        if (useObjectSurface)
        {
            Vector3 adjustedPosition = AdjustPositionToSurface(transform.position);
            transform.position = adjustedPosition;
            
            if (showDebugInfo)
                Debug.Log($"Platform adjusted to surface. New Y position: {adjustedPosition.y}");
        }
        else
        {
            Debug.Log("useObjectSurface is disabled. Enable it to use surface positioning.");
        }
    }
    
    [ContextMenu("Reset to Point A")]
    public void ResetToPointA()
    {
        if (pointA != null)
        {
            Vector3 newPos = useObjectSurface ? AdjustPositionToSurface(pointA.position) : pointA.position;
            transform.position = newPos;
            
            if (showDebugInfo)
                Debug.Log($"Platform reset to Point A: {newPos}");
        }
    }
    
    [ContextMenu("Reset to Point B")]
    public void ResetToPointB()
    {
        if (pointB != null)
        {
            Vector3 newPos = useObjectSurface ? AdjustPositionToSurface(pointB.position) : pointB.position;
            transform.position = newPos;
            
            if (showDebugInfo)
                Debug.Log($"Platform reset to Point B: {newPos}");
        }
    }
    
    [ContextMenu("Test Surface Positioning")]
    void TestSurfacePositioning()
    {
        if (pointA != null && pointB != null)
        {
            Vector3 testPosA = AdjustPositionToSurface(pointA.position);
            Vector3 testPosB = AdjustPositionToSurface(pointB.position);
            
            Debug.Log($"Point A: Original Y={pointA.position.y:F2}, Surface Y={testPosA.y:F2}");
            Debug.Log($"Point B: Original Y={pointB.position.y:F2}, Surface Y={testPosB.y:F2}");
            
            // Optionally move the platform to test position
            transform.position = AdjustPositionToSurface(transform.position);
        }
    }
    
    [ContextMenu("Force Surface Snap")]
    void ForceSurfaceSnap()
    {
        if (useObjectSurface)
        {
            transform.position = AdjustPositionToSurface(transform.position);
            Debug.Log($"Platform snapped to surface at Y: {transform.position.y:F2}");
        }
    }
    
    /// <summary>
    /// Validate that surface positioning is working correctly
    /// </summary>
    [ContextMenu("Validate Surface Positioning")]
    void ValidateSurfacePositioning()
    {
        if (!useObjectSurface)
        {
            Debug.Log("Surface positioning is disabled.");
            return;
        }
        
        if (pointA == null || pointB == null)
        {
            Debug.LogWarning("Points A and B must be assigned first.");
            return;
        }
        
        Vector3 originalA = pointA.position;
        Vector3 originalB = pointB.position;
        Vector3 surfaceA = AdjustPositionToSurface(originalA);
        Vector3 surfaceB = AdjustPositionToSurface(originalB);
        
        float adjustmentA = surfaceA.y - originalA.y;
        float adjustmentB = surfaceB.y - originalB.y;
        
        Debug.Log($"=== Surface Positioning Validation ===");
        Debug.Log($"Point A: Original Y={originalA.y:F2}, Surface Y={surfaceA.y:F2}, Adjustment={adjustmentA:F2}");
        Debug.Log($"Point B: Original Y={originalB.y:F2}, Surface Y={surfaceB.y:F2}, Adjustment={adjustmentB:F2}");
        Debug.Log($"Surface Offset: {surfaceOffset:F2}");
        Debug.Log($"Movement Distance: {Vector3.Distance(surfaceA, surfaceB):F2}");
        
        // Test current platform position
        Vector3 currentSurface = AdjustPositionToSurface(transform.position);
        float currentAdjustment = currentSurface.y - transform.position.y;
        Debug.Log($"Current Platform: Y={transform.position.y:F2}, Surface Y={currentSurface.y:F2}, Adjustment={currentAdjustment:F2}");
        
        if (Mathf.Abs(adjustmentA) < 0.1f && Mathf.Abs(adjustmentB) < 0.1f && Mathf.Abs(currentAdjustment) < 0.1f)
        {
            Debug.LogWarning("No surface adjustments detected. Check if there are objects below the points.");
        }
        else
        {
            Debug.Log("Surface positioning appears to be working correctly!");
        }
    }

    // ...existing code...
    
    private void DetermineStartingDirection()
    {
        Vector3 currentPos = transform.position;
        Vector3 pointAPos = useObjectSurface ? AdjustPositionToSurface(pointA.position) : pointA.position;
        Vector3 pointBPos = useObjectSurface ? AdjustPositionToSurface(pointB.position) : pointB.position;
        
        // Calculate distances to both points
        float distanceToA = Vector3.Distance(currentPos, pointAPos);
        float distanceToB = Vector3.Distance(currentPos, pointBPos);
        
        // Find the closest point on the line between A and B
        Vector3 lineDirection = pointBPos - pointAPos;
        Vector3 pointToA = currentPos - pointAPos;
        float lineLength = lineDirection.magnitude;
        
        if (lineLength > 0.001f) // Avoid division by zero
        {
            float t = Vector3.Dot(pointToA, lineDirection.normalized) / lineLength;
            t = Mathf.Clamp01(t); // Clamp to line segment
            
            Vector3 closestPointOnLine = pointAPos + t * lineDirection;
            
            // Snap platform to the line if it's close enough (only horizontally if using surface)
            float distanceToLine;
            if (useObjectSurface)
            {
                // Only check horizontal distance when using surface positioning
                Vector3 horizontalCurrent = new Vector3(currentPos.x, 0, currentPos.z);
                Vector3 horizontalClosest = new Vector3(closestPointOnLine.x, 0, closestPointOnLine.z);
                distanceToLine = Vector3.Distance(horizontalCurrent, horizontalClosest);
            }
            else
            {
                distanceToLine = Vector3.Distance(currentPos, closestPointOnLine);
            }
            
            if (distanceToLine < 2f) // Within 2 units of the line
            {
                if (useObjectSurface)
                {
                    // Keep current Y position, only adjust X and Z
                    Vector3 adjustedPos = new Vector3(closestPointOnLine.x, currentPos.y, closestPointOnLine.z);
                    transform.position = adjustedPos;
                    currentPos = adjustedPos;
                }
                else
                {
                    transform.position = closestPointOnLine;
                    currentPos = closestPointOnLine;
                }
                
                if (showDebugInfo)
                    Debug.Log($"Platform snapped to line. Progress: {t:P1}");
            }
            
            // Determine direction based on position on the line
            if (t < 0.5f)
            {
                // Closer to A, move towards B
                movingToB = true;
                startPosition = currentPos;
                targetPosition = pointBPos;
            }
            else
            {
                // Closer to B, move towards A
                movingToB = false;
                startPosition = currentPos;
                targetPosition = pointAPos;
            }
            
            // Set journeyTime to 0 and let the platform move naturally from current position
            journeyTime = 0f;
        }
        else
        {
            // Points A and B are at the same position - fallback
            Debug.LogWarning($"MovingPlatform '{gameObject.name}': Points A and B are at the same position!");
            movingToB = true;
            startPosition = currentPos;
            targetPosition = pointBPos;
        }
    }
    
    /// <summary>
    /// Get the surface position by finding the ground/surface below the given position
    /// </summary>
    private Vector3 GetSurfacePosition(Vector3 basePosition)
    {
        if (!useObjectSurface)
            return basePosition;
            
        return AdjustPositionToSurface(basePosition);
    }
    
    /// <summary>
    /// Adjust position to be on the surface/ground level
    /// </summary>
    private Vector3 AdjustPositionToSurface(Vector3 originalPosition)
    {
        if (!useObjectSurface)
            return originalPosition;
            
        Vector3 adjustedPosition = originalPosition;
        
        // Start raycast from high above to ensure we catch tall buildings
        Vector3 rayStart = new Vector3(originalPosition.x, originalPosition.y + 1000f, originalPosition.z);
        float maxRayDistance = 2000f;
        
        // Cast a ray downward to find the ground/surface
        RaycastHit hit;
        
        // First try: exclude our own collider layer to avoid self-collision
        int layerMask = ~(1 << gameObject.layer);
        
        if (Physics.Raycast(rayStart, Vector3.down, out hit, maxRayDistance, layerMask))
        {
            // Found a surface below the target position
            adjustedPosition = hit.point + Vector3.up * surfaceOffset;
            
            if (showDebugInfo)
                Debug.Log($"Platform positioned on surface at Y: {adjustedPosition.y} (hit: {hit.collider.name})");
        }
        else
        {
            // Try again without layer mask exclusion
            if (Physics.Raycast(rayStart, Vector3.down, out hit, maxRayDistance))
            {
                // Make sure we didn't hit ourselves
                if (hit.collider.gameObject != gameObject)
                {
                    adjustedPosition = hit.point + Vector3.up * surfaceOffset;
                    
                    if (showDebugInfo)
                        Debug.Log($"Platform positioned on surface (2nd try) at Y: {adjustedPosition.y} (hit: {hit.collider.name})");
                }
                else
                {
                    // Hit ourselves, try overlap method instead
                    adjustedPosition = FindSurfaceUsingOverlap(originalPosition);
                }
            }
            else
            {
                // No raycast hit, try overlap method
                adjustedPosition = FindSurfaceUsingOverlap(originalPosition);
            }
        }
        
        return adjustedPosition;
    }
    
    /// <summary>
    /// Find surface using overlap detection for cases where raycast fails
    /// </summary>
    private Vector3 FindSurfaceUsingOverlap(Vector3 position)
    {
        // Look for colliders in the area below this position
        Vector3 checkCenter = new Vector3(position.x, position.y - 50f, position.z);
        Collider[] nearbyColliders = Physics.OverlapSphere(checkCenter, 100f);
        
        float highestSurfaceY = float.MinValue;
        bool foundValidSurface = false;
        
        foreach (Collider col in nearbyColliders)
        {
            if (col.gameObject == gameObject) continue; // Skip self
            
            Bounds bounds = col.bounds;
            
            // Check if this collider is horizontally aligned with our position
            if (position.x >= bounds.min.x && position.x <= bounds.max.x &&
                position.z >= bounds.min.z && position.z <= bounds.max.z)
            {
                // This collider is below our position, check its top surface
                float surfaceY = bounds.max.y;
                
                // Only consider surfaces that are below or at our current position
                if (surfaceY <= position.y && surfaceY > highestSurfaceY)
                {
                    highestSurfaceY = surfaceY;
                    foundValidSurface = true;
                }
            }
        }
        
        if (foundValidSurface)
        {
            Vector3 result = new Vector3(position.x, highestSurfaceY + surfaceOffset, position.z);
            
            if (showDebugInfo)
                Debug.Log($"Platform positioned using overlap method at Y: {result.y}");
                
            return result;
        }
        
        // Final fallback: return original position with surface offset
        Vector3 fallback = position + Vector3.up * surfaceOffset;
        
        if (showDebugInfo)
            Debug.Log($"No surface found, using fallback position at Y: {fallback.y}");
            
        return fallback;
    }
    
    /// <summary>
    /// Reset platform to its starting position and state (called on player respawn)
    /// </summary>
    public void ResetToStartingPosition()
    {
        // Stop any current movement
        isMoving = false;
        isWaiting = false;
        waitTimer = 0f;
        journeyTime = 0f;
        
        // Reset movement direction and position based on starting configuration
        if (startFromCurrentPosition)
        {
            // If the platform was set to start from current position, 
            // we need to restore it to where it was originally placed
            Vector3 originalStartPos = useObjectSurface ? AdjustPositionToSurface(pointA.position) : pointA.position;
            transform.position = originalStartPos;
            
            // Recalculate starting direction from this position
            DetermineStartingDirection();
        }
        else
        {
            // Original behavior - reset to Point A
            Vector3 startPos = useObjectSurface ? AdjustPositionToSurface(pointA.position) : pointA.position;
            transform.position = startPos;
            startPosition = startPos;
            targetPosition = useObjectSurface ? AdjustPositionToSurface(pointB.position) : pointB.position;
            movingToB = true;
        }
        
        // Clear any carried objects
        if (carriedObject != null)
        {
            carriedObject = null;
        }
        
        // Restart movement if auto-start is enabled
        if (autoStart)
        {
            StartMoving();
        }
        
        if (showDebugInfo)
            Debug.Log($"MovingPlatform '{gameObject.name}' reset to starting position: {transform.position}");
    }
}
