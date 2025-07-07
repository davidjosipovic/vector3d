using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    [Tooltip("Visual indicator that shows when checkpoint is activated")]
    public GameObject activatedIndicator;
    [Tooltip("Sound to play when checkpoint is activated")]
    public AudioSource checkpointSound;
    [Tooltip("Particle effect when checkpoint is activated")]
    public ParticleSystem checkpointEffect;
    
    private bool isActivated = false;
    
    [Header("Debug")]
    public bool showGizmos = true;
    
    private void Start()
    {
    
        if (activatedIndicator != null)
            activatedIndicator.SetActive(false);
    }
    
    private void OnTriggerEnter(Collider other)
    {
     
        if (other.CompareTag("Player") && !isActivated)
        {
            ActivateCheckpoint(other.transform.position);
        }
    }
    
    private void ActivateCheckpoint(Vector3 playerPosition)
    {
        isActivated = true;
        
    
        CheckpointManager.Instance.SetCurrentCheckpoint(transform.position, transform.rotation);
        
   
        if (activatedIndicator != null)
            activatedIndicator.SetActive(true);
            
  
        if (checkpointSound != null)
            checkpointSound.Play();
            
 
        if (checkpointEffect != null)
            checkpointEffect.Play();
            
        Debug.Log($"Checkpoint activated at position: {transform.position}");
    }
    
  
    public void ResetCheckpoint()
    {
        isActivated = false;
        if (activatedIndicator != null)
            activatedIndicator.SetActive(false);
    }
    
   
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        Gizmos.color = isActivated ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(2f, 3f, 2f));
        
        Gizmos.color = isActivated ? Color.green : Color.yellow;
        Gizmos.DrawIcon(transform.position + Vector3.up * 2f, "checkpoint_icon.png", true);
    }
}
