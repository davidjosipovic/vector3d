using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    
    [Header("Camera Settings")]
    public Vector3 offset = new Vector3(0, 5, -10);
    public float followSpeed = 2f;
    public float rotationSpeed = 2f;
    
    [Header("Look At Settings")]
    public bool lookAtTarget = true;
    public Vector3 lookOffset = Vector3.zero;
    
    private Vector3 velocity = Vector3.zero;
    
    void Start()
    {
  
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
     
        Vector3 desiredPosition = target.position + offset;
        
   
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, 1f / followSpeed);
        
      
        if (lookAtTarget)
        {
            Vector3 lookAtPosition = target.position + lookOffset;
            Vector3 direction = (lookAtPosition - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        }
    }
    

    public void ResetCameraPosition()
    {
        if (target != null)
        {
           
            transform.position = target.position + offset;
            
            if (lookAtTarget)
            {
                Vector3 lookAtPosition = target.position + lookOffset;
                transform.LookAt(lookAtPosition);
            }
            
           
            velocity = Vector3.zero;
            
            Debug.Log("Camera position reset for checkpoint respawn");
        }
    }
    
    
    void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, target.position + lookOffset);
            Gizmos.DrawWireSphere(target.position + offset, 0.5f);
        }
    }
}
