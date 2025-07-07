using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("Platform Settings")]
    public Transform pointA;
    public Transform pointB;
    public float moveSpeed = 2f;
    public float waitTime = 1f;
    public bool autoStart = true;
    public bool startFromCurrentPosition = true;
    public bool useObjectSurface = true;
    public float surfaceOffset = 0f;
    
    [Header("Movement Options")]
    public bool useSmoothMovement = false;
    public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Player Detection")]
    public bool carryPlayer = true;
    public LayerMask carryableLayers = -1;
    

    public bool showGizmos = true;
    
    private Vector3 targetPosition;
    private Vector3 startPosition;
    private bool movingToB = true;
    private bool isMoving = false;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private float journeyLength;
    private float journeyTime = 0f;
    
    private Transform carriedObject = null;
    private Vector3 carriedObjectOffset;
    
    void Start()
    {
        if (pointA == null || pointB == null)
        {
            enabled = false;
            return;
        }
        
        journeyLength = Vector3.Distance(pointA.position, pointB.position);
        
        if (startFromCurrentPosition)
        {
            if (useObjectSurface)
            {
                Vector3 adjustedPosition = AdjustPositionToSurface(transform.position);
                transform.position = adjustedPosition;
            }
            
            DetermineStartingDirection();
        }
        else
        {
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
        }
    }
    
    private void HandleMovement()
    {
        journeyTime += Time.deltaTime;
        
        float currentJourneyLength = Vector3.Distance(startPosition, targetPosition);
        
        float journeyFraction;
        
        if (useSmoothMovement)
        {
            float normalizedTime = (journeyTime * moveSpeed) / currentJourneyLength;
            journeyFraction = movementCurve.Evaluate(Mathf.Clamp01(normalizedTime));
        }
        else
        {
            journeyFraction = (journeyTime * moveSpeed) / currentJourneyLength;
        }
        
        journeyFraction = Mathf.Clamp01(journeyFraction);
        
        Vector3 previousPosition = transform.position;
        
        transform.position = Vector3.Lerp(startPosition, targetPosition, journeyFraction);
        
        if (carryPlayer && carriedObject != null)
        {
            Vector3 platformMovement = transform.position - previousPosition;
            carriedObject.position += platformMovement;
        }
        
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
                HandleWaiting();
            }
        }
    }
    
    public void StartMoving()
    {
        if (pointA == null || pointB == null)
        {
            return;
        }
        
        isMoving = true;
        isWaiting = false;
        journeyTime = 0f;
    }
    
    public void StopMoving()
    {
        isMoving = false;
        isWaiting = false;
    }
    
    public void PauseMoving()
    {
        bool wasPaused = !isMoving && !isWaiting;
        isMoving = !wasPaused && isMoving;
        isWaiting = !wasPaused && isWaiting;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!carryPlayer)
            return;
            
        if (((1 << other.gameObject.layer) & carryableLayers) != 0)
        {
            carriedObject = other.transform;
            carriedObjectOffset = carriedObject.position - transform.position;
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (!carryPlayer)
            return;
            
        if (other.transform == carriedObject)
        {
            carriedObject = null;
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!showGizmos || pointA == null || pointB == null)
            return;
            
        Vector3 pointAPos = useObjectSurface ? AdjustPositionToSurface(pointA.position) : pointA.position;
        Vector3 pointBPos = useObjectSurface ? AdjustPositionToSurface(pointB.position) : pointB.position;
        
        Gizmos.color = useObjectSurface ? Color.yellow : Color.white;
        Gizmos.DrawLine(pointAPos, pointBPos);
        
        if (useObjectSurface)
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(pointA.position, 0.15f);
            Gizmos.DrawWireSphere(pointB.position, 0.15f);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(pointAPos, 0.3f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pointBPos, 0.3f);
            
            float adjustmentDistanceA = Vector3.Distance(pointA.position, pointAPos);
            float adjustmentDistanceB = Vector3.Distance(pointB.position, pointBPos);
            
            if (adjustmentDistanceA > 0.1f)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.7f);
                Gizmos.DrawLine(pointA.position, pointAPos);
                
                for (int i = 1; i < 4; i++)
                {
                    Vector3 markerPos = Vector3.Lerp(pointA.position, pointAPos, i / 4f);
                    Gizmos.DrawWireSphere(markerPos, 0.05f);
                }
            }
            
            if (adjustmentDistanceB > 0.1f)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.7f);
                Gizmos.DrawLine(pointB.position, pointBPos);
                
                for (int i = 1; i < 4; i++)
                {
                    Vector3 markerPos = Vector3.Lerp(pointB.position, pointBPos, i / 4f);
                    Gizmos.DrawWireSphere(markerPos, 0.05f);
                }
            }
            
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
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(pointAPos, 0.3f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pointBPos, 0.3f);
        }
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
        
        float progress = GetProgressOnLine();
        Gizmos.color = Color.cyan;
        Vector3 progressPos = Vector3.Lerp(pointAPos, pointBPos, progress);
        Gizmos.DrawWireSphere(progressPos, 0.2f);
        
        if (Application.isPlaying && isMoving)
        {
            Gizmos.color = movingToB ? Color.red : Color.green;
            Vector3 direction = (targetPosition - transform.position).normalized;
            Gizmos.DrawRay(transform.position, direction * 1f);
            
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(targetPosition, 0.25f);
        }
        else if (!Application.isPlaying && startFromCurrentPosition)
        {
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
            
        Gizmos.color = Color.cyan;
        
        Collider platformCollider = GetComponent<Collider>();
        if (platformCollider != null)
        {
            Gizmos.DrawWireCube(pointA.position, platformCollider.bounds.size);
            Gizmos.DrawWireCube(pointB.position, platformCollider.bounds.size);
        }
        
        Vector3 pathDirection = pointB.position - pointA.position;
        int segments = 10;
        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / segments;
            Vector3 pos = Vector3.Lerp(pointA.position, pointB.position, t);
            Gizmos.DrawWireSphere(pos, 0.1f);
        }
    }
    
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
    
    public void SetPositionAndRecalculate(Vector3 newPosition)
    {
        transform.position = newPosition;
        
        if (pointA != null && pointB != null)
        {
            DetermineStartingDirection();
        }
    }
    
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
    
    public void SetDirection(bool moveTowardsB)
    {
        if (pointA == null || pointB == null)
            return;
            
        movingToB = moveTowardsB;
        startPosition = transform.position;
        Vector3 targetPoint = moveTowardsB ? pointB.position : pointA.position;
        targetPosition = useObjectSurface ? AdjustPositionToSurface(targetPoint) : targetPoint;
        
        journeyTime = 0f;
    }
    
    public void AdjustToSurface()
    {
        if (useObjectSurface)
        {
            Vector3 adjustedPosition = AdjustPositionToSurface(transform.position);
            transform.position = adjustedPosition;
        }
    }
    
    public void ResetToPointA()
    {
        if (pointA != null)
        {
            Vector3 newPos = useObjectSurface ? AdjustPositionToSurface(pointA.position) : pointA.position;
            transform.position = newPos;
        }
    }
    
    public void ResetToPointB()
    {
        if (pointB != null)
        {
            Vector3 newPos = useObjectSurface ? AdjustPositionToSurface(pointB.position) : pointB.position;
            transform.position = newPos;
        }
    }
    
    [ContextMenu("Test Surface Positioning")]
    void TestSurfacePositioning()
    {
        if (pointA != null && pointB != null)
        {
            Vector3 testPosA = AdjustPositionToSurface(pointA.position);
            Vector3 testPosB = AdjustPositionToSurface(pointB.position);
            transform.position = AdjustPositionToSurface(transform.position);
        }
    }
    
    [ContextMenu("Force Surface Snap")]
    void ForceSurfaceSnap()
    {
        if (useObjectSurface)
        {
            transform.position = AdjustPositionToSurface(transform.position);
        }
    }
    
    [ContextMenu("Validate Surface Positioning")]
    void ValidateSurfacePositioning()
    {
        if (!useObjectSurface)
        {
            return;
        }
        
        if (pointA == null || pointB == null)
        {
            return;
        }
        
        Vector3 originalA = pointA.position;
        Vector3 originalB = pointB.position;
        Vector3 surfaceA = AdjustPositionToSurface(originalA);
        Vector3 surfaceB = AdjustPositionToSurface(originalB);
        
        float adjustmentA = surfaceA.y - originalA.y;
        float adjustmentB = surfaceB.y - originalB.y;
        
        Vector3 currentSurface = AdjustPositionToSurface(transform.position);
        float currentAdjustment = currentSurface.y - transform.position.y;
    }


    
    private void DetermineStartingDirection()
    {
        Vector3 currentPos = transform.position;
        Vector3 pointAPos = useObjectSurface ? AdjustPositionToSurface(pointA.position) : pointA.position;
        Vector3 pointBPos = useObjectSurface ? AdjustPositionToSurface(pointB.position) : pointB.position;
        
        float distanceToA = Vector3.Distance(currentPos, pointAPos);
        float distanceToB = Vector3.Distance(currentPos, pointBPos);
        
        Vector3 lineDirection = pointBPos - pointAPos;
        Vector3 pointToA = currentPos - pointAPos;
        float lineLength = lineDirection.magnitude;
        
        if (lineLength > 0.001f)
        {
            float t = Vector3.Dot(pointToA, lineDirection.normalized) / lineLength;
            t = Mathf.Clamp01(t);
            
            Vector3 closestPointOnLine = pointAPos + t * lineDirection;
            
            float distanceToLine;
            if (useObjectSurface)
            {
                Vector3 horizontalCurrent = new Vector3(currentPos.x, 0, currentPos.z);
                Vector3 horizontalClosest = new Vector3(closestPointOnLine.x, 0, closestPointOnLine.z);
                distanceToLine = Vector3.Distance(horizontalCurrent, horizontalClosest);
            }
            else
            {
                distanceToLine = Vector3.Distance(currentPos, closestPointOnLine);
            }
            
            if (distanceToLine < 2f)
            {
                if (useObjectSurface)
                {
                    Vector3 adjustedPos = new Vector3(closestPointOnLine.x, currentPos.y, closestPointOnLine.z);
                    transform.position = adjustedPos;
                    currentPos = adjustedPos;
                }
                else
                {
                    transform.position = closestPointOnLine;
                    currentPos = closestPointOnLine;
                }
            }
            
            if (t < 0.5f)
            {
                movingToB = true;
                startPosition = currentPos;
                targetPosition = pointBPos;
            }
            else
            {
                movingToB = false;
                startPosition = currentPos;
                targetPosition = pointAPos;
            }
            
            journeyTime = 0f;
        }
        else
        {
            movingToB = true;
            startPosition = currentPos;
            targetPosition = pointBPos;
        }
    }
    
    private Vector3 GetSurfacePosition(Vector3 basePosition)
    {
        if (!useObjectSurface)
            return basePosition;
            
        return AdjustPositionToSurface(basePosition);
    }
    
    private Vector3 AdjustPositionToSurface(Vector3 originalPosition)
    {
        if (!useObjectSurface)
            return originalPosition;
            
        Vector3 adjustedPosition = originalPosition;
        
        Vector3 rayStart = new Vector3(originalPosition.x, originalPosition.y + 1000f, originalPosition.z);
        float maxRayDistance = 2000f;
        
        RaycastHit hit;
        
        int layerMask = ~(1 << gameObject.layer);
        
        if (Physics.Raycast(rayStart, Vector3.down, out hit, maxRayDistance, layerMask))
        {
            adjustedPosition = hit.point + Vector3.up * surfaceOffset;
        }
        else
        {
            if (Physics.Raycast(rayStart, Vector3.down, out hit, maxRayDistance))
            {
                if (hit.collider.gameObject != gameObject)
                {
                    adjustedPosition = hit.point + Vector3.up * surfaceOffset;
                }
                else
                {
                    adjustedPosition = FindSurfaceUsingOverlap(originalPosition);
                }
            }
            else
            {
                adjustedPosition = FindSurfaceUsingOverlap(originalPosition);
            }
        }
        
        return adjustedPosition;
    }
    
    private Vector3 FindSurfaceUsingOverlap(Vector3 position)
    {
        Vector3 checkCenter = new Vector3(position.x, position.y - 50f, position.z);
        Collider[] nearbyColliders = Physics.OverlapSphere(checkCenter, 100f);
        
        float highestSurfaceY = float.MinValue;
        bool foundValidSurface = false;
        
        foreach (Collider col in nearbyColliders)
        {
            if (col.gameObject == gameObject) continue;
            
            Bounds bounds = col.bounds;
            
            if (position.x >= bounds.min.x && position.x <= bounds.max.x &&
                position.z >= bounds.min.z && position.z <= bounds.max.z)
            {
                float surfaceY = bounds.max.y;
                
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
            return result;
        }
        
        Vector3 fallback = position + Vector3.up * surfaceOffset;
        return fallback;
    }
    
    public void ResetToStartingPosition()
    {
        isMoving = false;
        isWaiting = false;
        waitTimer = 0f;
        journeyTime = 0f;
        
        if (startFromCurrentPosition)
        {
            Vector3 originalStartPos = useObjectSurface ? AdjustPositionToSurface(pointA.position) : pointA.position;
            transform.position = originalStartPos;
            
            DetermineStartingDirection();
        }
        else
        {
            Vector3 startPos = useObjectSurface ? AdjustPositionToSurface(pointA.position) : pointA.position;
            transform.position = startPos;
            startPosition = startPos;
            targetPosition = useObjectSurface ? AdjustPositionToSurface(pointB.position) : pointB.position;
            movingToB = true;
        }
        
        if (carriedObject != null)
        {
            carriedObject = null;
        }
        
        if (autoStart)
        {
            StartMoving();
        }
    }
}
