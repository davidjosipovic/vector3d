using UnityEngine;

public class LevelFinish : MonoBehaviour
{
    [Header("Level Finish Settings")]
    public string playerTag = "Player";
    public bool showDebugInfo = true;
    public bool disablePlayerOnFinish = true;

    [Header("Visual Feedback")]
    public ParticleSystem finishParticles;
    public AudioClip finishSound;

    private bool levelFinished = false;
    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && finishSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!levelFinished && other.CompareTag(playerTag))
        {
            FinishLevel(other.gameObject);
        }
    }

    private void FinishLevel(GameObject player)
    {
        levelFinished = true;

        if (showDebugInfo)
            Debug.Log("🏁 Level Finished! 🏁");

        if (LevelTimer.Instance != null)
        {
            LevelTimer.Instance.StopTimer();
        }
        else
        {
            Debug.LogWarning("LevelTimer not found!");
        }

        if (finishParticles != null)
        {
            finishParticles.Play();
        }

        if (audioSource != null && finishSound != null)
        {
            audioSource.PlayOneShot(finishSound);
        }

        if (disablePlayerOnFinish)
        {
            DisablePlayer(player);
        }

        if (GameUI.Instance != null)
        {
            GameUI.Instance.ShowLevelCompleteMessage();
        }
        else
        {
            Debug.LogWarning("GameUI.Instance not found! Completion UI will not show.");
        }
    }

    private void DisablePlayer(GameObject player)
    {
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        CharacterController characterController = player.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();
        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector3.zero;
            playerRigidbody.isKinematic = true;
        }

        if (showDebugInfo)
            Debug.Log("Player movement disabled");
    }

    [ContextMenu("Finish Level")]
    public void ManualFinishLevel()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            FinishLevel(player);
        }
        else
        {
            Debug.LogWarning("Player not found for manual finish!");
        }
    }

    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = levelFinished ? Color.green : Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is BoxCollider)
            {
                BoxCollider boxCol = col as BoxCollider;
                Gizmos.DrawWireCube(boxCol.center, boxCol.size);
            }
            else if (col is SphereCollider)
            {
                SphereCollider sphereCol = col as SphereCollider;
                Gizmos.DrawWireSphere(sphereCol.center, sphereCol.radius);
            }
        }

        Gizmos.color = Color.white;
        Gizmos.DrawIcon(transform.position, "sv_icon_dot3_pix16_gizmo", true);
    }
}
