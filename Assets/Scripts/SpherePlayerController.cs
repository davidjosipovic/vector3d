using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class SpherePlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float forwardSpeed = 5f;
    public float strafeSpeed = 3f;

    [Header("Jump Settings")]
    public float jumpHeight = 2f;
    public float groundCheckDistance = 0.6f;
    public LayerMask groundLayers = ~0; // default: everything

    [Header("Slide Settings")]
    public float slideSpeed = 8f;
    public float slideDuration = 1f;
    public float slideStrafeMultiplier = 0.6f;
    public float slidingDrag = 0.0f;
    public float normalDrag = 0.2f;

    [Header("Coyote/Buffer Settings")]
    public float coyoteTimeDuration = 0.1f;
    public float jumpBufferDuration = 0.1f;

    [Header("Slow Down Settings")]
    public float slowDownSpeedMultiplier = 0.3f;

    [Header("Climb Settings")]
    public float climbSpeed = 5f;
    public float climbOffsetAboveWall = 0.1f;
    public float climbApproachRayDistance = 1.0f;
    public float climbCooldown = 0.5f;
    public float climbEdgeForwardPush = 0.4f;
    public float climbEdgeIgnoreTime = 0.2f;

    [Header("Visuals")]
    public Color climbColor = Color.yellow;
    public Color slideColor = Color.magenta;
    public float slideScaleMultiplier = 0.7f;
    public float slideVisualLerpDuration = 0.15f;
    public AnimationCurve slideVisualCurve = null; // if null, use linear

    // NEW: Shatter/Respawn settings
    [Header("Shatter/Respawn Settings")]
    public float lowSpeedThreshold = 0.25f;          // horizontal speed below this counts as stopped
    public float lowSpeedTime = 0.6f;                // how long to be below threshold to trigger
    public float shatterRespawnDelay = 1.2f;         // wait before respawn
    public int shatterFragmentCount = 16;            // number of spawned pieces
    public float shatterFragmentScale = 0.15f;       // scale of pieces
    public float shatterExplosionForce = 6f;         // outward force
    public float shatterExplosionRadius = 1.2f;      // explosion radius
    public float shatterFragmentLifetime = 3f;       // auto-destroy time for pieces

    private Rigidbody rb;
    private Collider myCollider;
    private bool isGrounded;
    private bool isSliding;
    private float slideTimer;
    private bool isClimbing;
    private float climbCooldownTimer;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;

    private Renderer rend;
    private Color originalColor;
    private Vector3 originalScale;
    private Coroutine slideVisualCoroutine;

    // NEW: shatter state
    private float lowSpeedTimer = 0f;
    private bool isShattered = false;
    private Vector3 lastPlanarPos; // track movement for real planar speed

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        myCollider = GetComponent<Collider>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.drag = normalDrag;
        rb.freezeRotation = true; // prevent rolling

        rend = GetComponent<Renderer>();
        if (rend == null) rend = GetComponentInChildren<Renderer>();
        originalScale = transform.localScale;
        if (rend != null)
            originalColor = rend.material.color;

        if (slideVisualCurve == null)
            slideVisualCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        // init last planar position
        lastPlanarPos = new Vector3(transform.position.x, 0f, transform.position.z);
    }

    void Start()
    {
        // Optional game systems hooks
        var levelTimer = FindObjectOfType<LevelTimer>();
        if (levelTimer != null) levelTimer.StartTimer();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayGameplayMusic();
    }

    void Update()
    {
        if (climbCooldownTimer > 0f) climbCooldownTimer -= Time.deltaTime;
        if (isClimbing) return;

        GroundCheck();
        HandleJumpInput();
        HandleSlideInput();

        // NEW: monitor for low speed -> shatter
        MonitorLowSpeed();
    }

    void FixedUpdate()
    {
        if (!isClimbing)
            ApplyMovement();
    }

    private void GroundCheck()
    {
        // Sphere cast slightly below the sphere's center to detect ground contact
        Vector3 origin = transform.position + Vector3.up * 0.05f;
        float radius = 0.49f; // assume unit sphere; adjust if your sphere scale differs
        RaycastHit hit;
        bool wasGrounded = isGrounded;
        isGrounded = Physics.SphereCast(origin, radius, Vector3.down, out hit, groundCheckDistance, groundLayers, QueryTriggerInteraction.Ignore);

        if (isGrounded)
        {
            if (!wasGrounded) coyoteTimeCounter = coyoteTimeDuration;
        }
        else
        {
            if (coyoteTimeCounter > 0f) coyoteTimeCounter -= Time.deltaTime;
        }
    }

    private void HandleJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            jumpBufferCounter = jumpBufferDuration;

        if (jumpBufferCounter > 0f)
            jumpBufferCounter -= Time.deltaTime;

        bool hasJumpInput = jumpBufferCounter > 0f;
        bool canJump = !isSliding && !isClimbing && hasJumpInput && (isGrounded || coyoteTimeCounter > 0f);
        if (canJump)
        {
            float jumpVelocity = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
            Vector3 v = rb.velocity;
            v.y = jumpVelocity;
            rb.velocity = v;

            isGrounded = false;
            coyoteTimeCounter = 0f;
            jumpBufferCounter = 0f;
        }
    }

    private void HandleSlideInput()
    {
        if ((isGrounded || coyoteTimeCounter > 0f) && !isSliding && !isClimbing && Input.GetKeyDown(KeyCode.S))
        {
            isSliding = true;
            slideTimer = slideDuration;
            rb.drag = slidingDrag;
            coyoteTimeCounter = 0f;

            if (slideVisualCoroutine != null) StopCoroutine(slideVisualCoroutine);
            Vector3 toScale = originalScale * slideScaleMultiplier;
            Color toColor = rend != null ? slideColor : Color.white;
            slideVisualCoroutine = StartCoroutine(ScaleColorRoutine(toScale, toColor, slideVisualLerpDuration));
        }

        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0f)
            {
                isSliding = false;
                rb.drag = normalDrag;

                if (slideVisualCoroutine != null) StopCoroutine(slideVisualCoroutine);
                Color toColor = rend != null ? originalColor : Color.white;
                slideVisualCoroutine = StartCoroutine(ScaleColorRoutine(originalScale, toColor, slideVisualLerpDuration));
            }
        }
    }

    private void ApplyMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float currentForwardSpeed = isSliding ? slideSpeed : forwardSpeed;
        float currentStrafeSpeed = isSliding ? strafeSpeed * slideStrafeMultiplier : strafeSpeed;

        if (Input.GetKey(KeyCode.C))
        {
            currentForwardSpeed *= slowDownSpeedMultiplier;
            currentStrafeSpeed *= slowDownSpeedMultiplier;
        }

        Vector3 desiredHorizontal = transform.forward * currentForwardSpeed + transform.right * horizontal * currentStrafeSpeed;

        Vector3 vel = rb.velocity;
        vel.x = desiredHorizontal.x;
        vel.z = desiredHorizontal.z;
        rb.velocity = vel;
    }



    void TryStartClimb(Collider wallCollider)
    {
        if (isClimbing || climbCooldownTimer > 0f) return;
        Bounds b = wallCollider.bounds;
        Vector3 playerPos = transform.position;
        float distanceFromTop = b.max.y - playerPos.y;
        if (distanceFromTop < 1f) return; // too close to the top

        // Start on the front face projected point (same XZ as player, Y at bottom)
        Vector3 startPos = new Vector3(playerPos.x, b.min.y, playerPos.z);
        float topY = b.max.y + climbOffsetAboveWall;
        Vector3 endPos = new Vector3(playerPos.x, topY, playerPos.z);

        StartCoroutine(ClimbWall(startPos, endPos));
    }

    void TryStartClimb(Collider wallCollider, RaycastHit hit)
    {
        if (isClimbing || climbCooldownTimer > 0f) return;
        Bounds b = wallCollider.bounds;
        Vector3 playerPos = transform.position;
        float distanceFromTop = b.max.y - playerPos.y;
        if (distanceFromTop < 1f) return;

        Vector3 hitXZ = new Vector3(hit.point.x, 0f, hit.point.z);
        Vector3 startPos = new Vector3(hitXZ.x, b.min.y, hitXZ.z);
        float topY = b.max.y + climbOffsetAboveWall;
        Vector3 endPos = new Vector3(hitXZ.x, topY, hitXZ.z);

        if (isSliding)
        {
            isSliding = false;
            rb.drag = normalDrag;
        }

        StartCoroutine(ClimbWall(startPos, endPos));
    }

    void OnTriggerEnter(Collider other)
    {
        if (isClimbing) return;
        if (other.CompareTag("Climbable") || (other.transform.parent != null && other.transform.parent.CompareTag("Climbable")))
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(origin, transform.forward, out RaycastHit hit, climbApproachRayDistance))
            {
                Collider col = hit.collider;
                if (col == other || col.transform == other.transform || (other.transform.parent != null && col.transform == other.transform.parent))
                {
                    TryStartClimb(other, hit);
                }
            }
        }
    }

    System.Collections.IEnumerator ClimbWall(Vector3 startPos, Vector3 endPos)
    {
        isClimbing = true;
        climbCooldownTimer = climbCooldown;
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;

        if (rend != null) rend.material.color = climbColor;

        transform.position = new Vector3(startPos.x, startPos.y, startPos.z);

        float distance = Vector3.Distance(startPos, endPos);
        float duration = Mathf.Max(0.05f, distance / Mathf.Max(0.01f, climbSpeed));
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(startPos, endPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;

        // small forward nudge to get over the lip
        Vector3 forward = transform.forward;
        Vector3 nudge = forward * climbEdgeForwardPush;

        // temporarily disable collision with the wall we climbed if possible
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, -forward, out RaycastHit backHit, 1.0f))
        {
            Collider wallCol = backHit.collider;
            if (myCollider != null && wallCol != null)
            {
                Physics.IgnoreCollision(myCollider, wallCol, true);
                yield return new WaitForSeconds(climbEdgeIgnoreTime);
                Physics.IgnoreCollision(myCollider, wallCol, false);
            }
        }

        transform.position += nudge;

        isClimbing = false;
        rb.isKinematic = false;

        if (rend != null)
            rend.material.color = originalColor;
        transform.localScale = originalScale;
    }

    // Smoothly animate scale and color
    private System.Collections.IEnumerator ScaleColorRoutine(Vector3 targetScale, Color targetColor, float duration)
    {
        Vector3 startScale = transform.localScale;
        Color startColor = rend != null ? rend.material.color : Color.white;
        float t = 0f;
        duration = Mathf.Max(0.01f, duration);
        while (t < duration)
        {
            float u = t / duration;
            float eased = slideVisualCurve != null ? slideVisualCurve.Evaluate(u) : u;
            transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, eased);
            if (rend != null)
                rend.material.color = Color.LerpUnclamped(startColor, targetColor, eased);
            t += Time.deltaTime;
            yield return null;
        }
        transform.localScale = targetScale;
        if (rend != null)
            rend.material.color = targetColor;
    }

    // NEW: monitor speed and trigger shatter
    private void MonitorLowSpeed()
    {
        if (isShattered || isClimbing)
        {
            lowSpeedTimer = 0f;
            lastPlanarPos = new Vector3(transform.position.x, 0f, transform.position.z);
            return;
        }

        Vector3 currPlanar = new Vector3(transform.position.x, 0f, transform.position.z);
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        float planarSpeed = (currPlanar - lastPlanarPos).magnitude / dt; // meters per second actually moved

        if (planarSpeed < lowSpeedThreshold)
        {
            lowSpeedTimer += Time.deltaTime;
            if (lowSpeedTimer >= lowSpeedTime)
            {
                StartCoroutine(ShatterAndRespawn());
            }
        }
        else
        {
            lowSpeedTimer = 0f;
        }

        lastPlanarPos = currPlanar;
    }

    // NEW: shatter effect and respawn via checkpoint
    private System.Collections.IEnumerator ShatterAndRespawn()
    {
        if (isShattered) yield break;
        isShattered = true;

        // Stop any slide visuals
        if (slideVisualCoroutine != null) StopCoroutine(slideVisualCoroutine);

        // Spawn fragments
        SpawnFragments();

        // Disable player visuals and physics movement
        if (rend != null) rend.enabled = false;
        if (myCollider != null) myCollider.enabled = false;
        rb.velocity = Vector3.zero;
        rb.isKinematic = true;

        yield return new WaitForSeconds(shatterRespawnDelay);

        bool didRespawn = false;
        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.RespawnPlayer();
            didRespawn = true;
        }

        if (!didRespawn)
        {
            var scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex);
            yield break; // scene reload will recreate objects
        }

        // Restore player state
        transform.localScale = originalScale;
        if (rend != null)
        {
            rend.material.color = originalColor;
            rend.enabled = true;
        }
        if (myCollider != null) myCollider.enabled = true;
        rb.isKinematic = false;
        rb.drag = normalDrag;
        rb.velocity = Vector3.zero;

        isSliding = false;
        isClimbing = false;
        climbCooldownTimer = 0f;
        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;
        lowSpeedTimer = 0f;
        isShattered = false;

        // reset planar baseline after teleport to checkpoint
        lastPlanarPos = new Vector3(transform.position.x, 0f, transform.position.z);
    }

    // NEW: helper to spawn physics fragments
    private void SpawnFragments()
    {
        Vector3 center = transform.position;
        float worldRadius = 0.5f * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);

        for (int i = 0; i < shatterFragmentCount; i++)
        {
            var piece = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            piece.transform.localScale = Vector3.one * shatterFragmentScale;
            piece.transform.position = center + Random.insideUnitSphere * (worldRadius * 0.5f);
            var pr = piece.GetComponent<Renderer>();
            if (pr != null && rend != null)
            {
                pr.material.color = originalColor;
            }
            var pc = piece.GetComponent<Collider>();
            if (pc != null && myCollider != null)
            {
                Physics.IgnoreCollision(pc, myCollider, true); // don't collide with disabled player
            }
            var rbPiece = piece.AddComponent<Rigidbody>();
            rbPiece.mass = 0.05f;
            rbPiece.interpolation = RigidbodyInterpolation.Interpolate;
            rbPiece.AddExplosionForce(shatterExplosionForce, center, shatterExplosionRadius, 0.1f, ForceMode.Impulse);
            Destroy(piece, shatterFragmentLifetime);
        }
    }
}
