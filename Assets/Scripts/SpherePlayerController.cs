using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class SpherePlayerController : MonoBehaviour
{
    [Header("Movement Settings")] public float forwardSpeed = 5f; public float strafeSpeed = 3f;
    [Header("Jump Settings")] public float jumpHeight = 2f; public float groundCheckDistance = 0.6f; public LayerMask groundLayers = ~0;
    [Header("Slide Settings")] public float slideSpeed = 8f; public float slideDuration = 1f; public float slideStrafeMultiplier = 0.6f; public float slidingDrag = 0.0f; public float normalDrag = 0.2f;
    [Header("Coyote/Buffer Settings")] public float coyoteTimeDuration = 0.1f; public float jumpBufferDuration = 0.1f;
    [Header("Slow Down Settings")] public float slowDownSpeedMultiplier = 0.3f;
    [Header("Climb Settings")] public float climbSpeed = 5f; public float climbOffsetAboveWall = 0.1f; public float climbApproachRayDistance = 1.0f; public float climbCooldown = 0.5f; public float climbEdgeForwardPush = 0.4f; public float climbEdgeIgnoreTime = 0.2f;

    [Header("Wall Running Settings")] public float wallCheckDistance = 0.6f; public LayerMask wallRunLayer = ~0; public float wallRunGravity = -2f; public float wallRunSpeed = 7f; public float maxWallRunTime = 3f; public float wallJumpForce = 5f; public float wallJumpForwardForce = 0.3f; public float wallJumpSideForce = 0.8f; public float wallJumpUpwardForce = 1.0f; public bool allowWallJumpChaining = true; public float wallJumpChainCooldown = 0.1f; public int maxConsecutiveWallJumps = 5; public float wallJumpGracePeriod = 0.3f; public float wallJumpMovementDuration = 0.4f; public float wallContactTolerance = 0.5f; public float wallRunMaxFallSpeed = -1f; public float wallRunEnterUpwardBoost = 2f;

    [Header("Visuals")] public Color climbColor = Color.yellow; public Color slideColor = Color.magenta; public Color wallRunColor = Color.green; public Color slowColor = Color.red; public float slideScaleMultiplier = 0.7f; public float slideVisualLerpDuration = 0.15f; public AnimationCurve slideVisualCurve = null;

    [Header("Shatter/Respawn Settings")] public float lowSpeedThreshold = 0.25f; public float lowSpeedTime = 0.6f; public float shatterRespawnDelay = 1.2f; public int shatterFragmentCount = 16; public float shatterFragmentScale = 0.15f; public float shatterExplosionForce = 6f; public float shatterExplosionRadius = 1.2f; public float shatterFragmentLifetime = 3f;

    private Rigidbody rb; private Collider myCollider; private bool isGrounded; private bool isSliding; private float slideTimer; private bool isClimbing; private float climbCooldownTimer; private float coyoteTimeCounter; private float jumpBufferCounter; private Renderer rend; private Color originalColor; private Vector3 originalScale; private Coroutine slideVisualCoroutine;
    private float lowSpeedTimer = 0f; private bool isShattered = false; private Vector3 lastPlanarPos; private bool slowColorApplied = false;

    // Wall run state
    private bool isWallRunning = false; private bool isWallRight = false; private bool isWallLeft = false; private float wallRunTimer = 0f; private bool justWallJumped = false; private float wallJumpCooldownTimer = 0f; private int consecutiveWallJumps = 0; private float wallJumpGraceTimer = 0f; private float wallJumpMovementTimer = 0f; private float wallContactLostTimer = 0f; private Vector3 lastWallNormal = Vector3.zero; private int lastWallSide = 0; // 1 right, -1 left

    void Awake()
    {
        rb = GetComponent<Rigidbody>(); myCollider = GetComponent<Collider>(); rb.interpolation = RigidbodyInterpolation.Interpolate; rb.drag = normalDrag; rb.freezeRotation = true;
        rend = GetComponent<Renderer>(); if (rend == null) rend = GetComponentInChildren<Renderer>(); originalScale = transform.localScale; if (rend != null) originalColor = rend.material.color; if (slideVisualCurve == null) slideVisualCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); lastPlanarPos = new Vector3(transform.position.x, 0f, transform.position.z);
    }

    void Start()
    { var levelTimer = FindObjectOfType<LevelTimer>(); if (levelTimer != null) levelTimer.StartTimer(); if (AudioManager.Instance != null) AudioManager.Instance.PlayGameplayMusic(); }

    void Update()
    {
        if (climbCooldownTimer > 0f) climbCooldownTimer -= Time.deltaTime; if (isClimbing) return;
    GroundCheck(); CheckForWalls(); HandleWallRunState(); HandleWallJumpInput(); HandleJumpInput(); HandleSlideInput(); MonitorLowSpeed(); UpdateSlowVisual();
        if (justWallJumped) { wallJumpCooldownTimer += Time.deltaTime; if (wallJumpCooldownTimer >= wallJumpChainCooldown) { justWallJumped = false; wallJumpCooldownTimer = 0f; } }
        if (wallJumpGraceTimer > 0f) wallJumpGraceTimer -= Time.deltaTime; if (wallJumpMovementTimer > 0f) wallJumpMovementTimer -= Time.deltaTime;
    }

    void FixedUpdate()
    { if (isClimbing) return; if (isWallRunning) WallRunningMovement(); else ApplyMovement(); }

    private void GroundCheck()
    {
        Vector3 origin = transform.position + Vector3.up * 0.05f; float radius = 0.49f; RaycastHit hit; bool wasGrounded = isGrounded; isGrounded = Physics.SphereCast(origin, radius, Vector3.down, out hit, groundCheckDistance, groundLayers, QueryTriggerInteraction.Ignore);
        if (isGrounded) { if (!wasGrounded) coyoteTimeCounter = coyoteTimeDuration; consecutiveWallJumps = 0; wallJumpGraceTimer = 0f; if (isWallRunning) StopWallRun(); }
        else { if (coyoteTimeCounter > 0f) coyoteTimeCounter -= Time.deltaTime; }
    }

    private void HandleJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space)) jumpBufferCounter = jumpBufferDuration; if (jumpBufferCounter > 0f) jumpBufferCounter -= Time.deltaTime;
        bool hasJumpInput = jumpBufferCounter > 0f; bool canJump = !isSliding && !isClimbing && hasJumpInput && (isGrounded || coyoteTimeCounter > 0f);
        if (canJump) { float jumpVelocity = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y); Vector3 v = rb.velocity; v.y = jumpVelocity; rb.velocity = v; isGrounded = false; coyoteTimeCounter = 0f; jumpBufferCounter = 0f; }
    }

    private void HandleWallJumpInput()
    {
        bool hasWallContact = isWallRight || isWallLeft; bool canAttemptWallJump = (isWallRunning || hasWallContact || wallJumpGraceTimer > 0f) && !isClimbing && !isSliding;
        if (canAttemptWallJump && Input.GetKeyDown(KeyCode.Space) && !justWallJumped)
        {
            bool canChainWallJump = allowWallJumpChaining && consecutiveWallJumps < maxConsecutiveWallJumps;
            if (canChainWallJump || isGrounded)
            {
                Vector3 outwardNormal;
                if (hasWallContact && lastWallNormal != Vector3.zero) outwardNormal = lastWallNormal;
                else if (!hasWallContact && wallJumpGraceTimer > 0f && lastWallSide != 0) outwardNormal = (lastWallSide == 1 ? -transform.right : transform.right);
                else outwardNormal = -transform.right;
                Vector3 forwardComponent = transform.forward * wallJumpForwardForce;
                Vector3 sideComponent = outwardNormal * wallJumpSideForce;
                Vector3 upComponent = Vector3.up * wallJumpUpwardForce;
                Vector3 jumpDirection = (forwardComponent + sideComponent + upComponent).normalized;
                rb.velocity = jumpDirection * wallJumpForce;
                justWallJumped = true; wallJumpCooldownTimer = 0f; wallJumpMovementTimer = wallJumpMovementDuration; if (!isGrounded) consecutiveWallJumps++; else consecutiveWallJumps = 1; wallJumpGraceTimer = wallJumpGracePeriod; if (isWallRunning) StopWallRun(); coyoteTimeCounter = 0f;
            }
        }
    }

    private void HandleSlideInput()
    {
        if ((isGrounded || coyoteTimeCounter > 0f) && !isSliding && !isClimbing && Input.GetKeyDown(KeyCode.S)) { isSliding = true; slideTimer = slideDuration; rb.drag = slidingDrag; coyoteTimeCounter = 0f; if (slideVisualCoroutine != null) StopCoroutine(slideVisualCoroutine); Vector3 toScale = originalScale * slideScaleMultiplier; Color toColor = rend != null ? slideColor : Color.white; slideVisualCoroutine = StartCoroutine(ScaleColorRoutine(toScale, toColor, slideVisualLerpDuration)); }
        if (isSliding) { slideTimer -= Time.deltaTime; if (slideTimer <= 0f) { isSliding = false; rb.drag = normalDrag; if (slideVisualCoroutine != null) StopCoroutine(slideVisualCoroutine); Color toColor = rend != null ? originalColor : Color.white; slideVisualCoroutine = StartCoroutine(ScaleColorRoutine(originalScale, toColor, slideVisualLerpDuration)); } }
    }

    private void ApplyMovement()
    {
        if (wallJumpMovementTimer > 0f) return; float horizontal = Input.GetAxis("Horizontal"); float currentForwardSpeed = isSliding ? slideSpeed : forwardSpeed; float currentStrafeSpeed = isSliding ? strafeSpeed * slideStrafeMultiplier : strafeSpeed; if (Input.GetKey(KeyCode.C)) { currentForwardSpeed *= slowDownSpeedMultiplier; currentStrafeSpeed *= slowDownSpeedMultiplier; } Vector3 desiredHorizontal = transform.forward * currentForwardSpeed + transform.right * horizontal * currentStrafeSpeed; Vector3 vel = rb.velocity; vel.x = desiredHorizontal.x; vel.z = desiredHorizontal.z; rb.velocity = vel;
    }

    private void UpdateSlowVisual()
    {
        if (rend == null) return;
        bool slowKey = Input.GetKey(KeyCode.C);
        bool canShowSlow = slowKey && !isSliding && !isClimbing && !isWallRunning && !isShattered;
        if (canShowSlow && !slowColorApplied)
        {
            rend.material.color = slowColor;
            slowColorApplied = true;
        }
        else if ((!canShowSlow) && slowColorApplied)
        {
            // revert only if not overridden by other state (wall run start, slide coroutine, climb)
            if (!isSliding && !isClimbing && !isWallRunning && !isShattered)
                rend.material.color = originalColor;
            slowColorApplied = false;
        }
    }

    private void CheckForWalls()
    {
        if (isGrounded || isClimbing) { isWallRight = isWallLeft = false; return; }
        Vector3 pos = transform.position; Vector3 lower = pos + Vector3.down * 0.3f; Vector3 upper = pos + Vector3.up * 0.3f; RaycastHit hitInfo;
        bool r1 = Physics.Raycast(pos, transform.right, out hitInfo, wallCheckDistance, wallRunLayer, QueryTriggerInteraction.Ignore); if (r1) { lastWallNormal = hitInfo.normal; lastWallSide = 1; }
        bool r2 = Physics.Raycast(lower, transform.right, out hitInfo, wallCheckDistance, wallRunLayer, QueryTriggerInteraction.Ignore); if (r2) { lastWallNormal = hitInfo.normal; lastWallSide = 1; }
        bool r3 = Physics.Raycast(upper, transform.right, out hitInfo, wallCheckDistance, wallRunLayer, QueryTriggerInteraction.Ignore); if (r3) { lastWallNormal = hitInfo.normal; lastWallSide = 1; }
        bool l1 = Physics.Raycast(pos, -transform.right, out hitInfo, wallCheckDistance, wallRunLayer, QueryTriggerInteraction.Ignore); if (l1) { lastWallNormal = hitInfo.normal; lastWallSide = -1; }
        bool l2 = Physics.Raycast(lower, -transform.right, out hitInfo, wallCheckDistance, wallRunLayer, QueryTriggerInteraction.Ignore); if (l2) { lastWallNormal = hitInfo.normal; lastWallSide = -1; }
        bool l3 = Physics.Raycast(upper, -transform.right, out hitInfo, wallCheckDistance, wallRunLayer, QueryTriggerInteraction.Ignore); if (l3) { lastWallNormal = hitInfo.normal; lastWallSide = -1; }
        isWallRight = r1 || r2 || r3; isWallLeft = l1 || l2 || l3;
    }

    private void HandleWallRunState()
    {
        if (isWallRunning) { wallRunTimer += Time.deltaTime; if (wallRunTimer >= maxWallRunTime) { StopWallRun(); return; } }
        bool hasWallContact = isWallRight || isWallLeft; bool canWallRun = !isGrounded && hasWallContact && !justWallJumped && !isSliding && !isClimbing;
        if (isWallRunning)
        {
            if (!hasWallContact) { wallContactLostTimer += Time.deltaTime; if (wallContactLostTimer >= wallContactTolerance) { StopWallRun(); return; } }
            else wallContactLostTimer = 0f;
        }
        if (canWallRun && !isWallRunning) StartWallRun(); else if (isGrounded && isWallRunning) StopWallRun();
    }

    private void StartWallRun()
    { isWallRunning = true; wallRunTimer = 0f; Vector3 v = rb.velocity; if (v.y < 0f) v.y = 0f; v.y += wallRunEnterUpwardBoost; rb.velocity = v; if (rend != null) rend.material.color = wallRunColor; }

    private void StopWallRun()
    { isWallRunning = false; wallRunTimer = 0f; wallContactLostTimer = 0f; if (!isGrounded) wallJumpGraceTimer = Mathf.Max(wallJumpGraceTimer, wallJumpGracePeriod); if (rend != null && !isSliding && !isClimbing && !isShattered) rend.material.color = originalColor; }

    private void WallRunningMovement()
    {
        Vector3 outwardNormal;
        if (isWallRight || isWallLeft) outwardNormal = lastWallNormal != Vector3.zero ? lastWallNormal : (isWallRight ? -transform.right : transform.right);
        else outwardNormal = lastWallNormal != Vector3.zero ? lastWallNormal : (lastWallSide == 1 ? -transform.right : transform.right);
        Vector3 inward = -outwardNormal;
        Vector3 wallForward = Vector3.Cross(inward, Vector3.up).normalized; if (Vector3.Dot(wallForward, transform.forward) < 0) wallForward = -wallForward;
        Vector3 vel = rb.velocity; vel.x = wallForward.x * wallRunSpeed; vel.z = wallForward.z * wallRunSpeed; vel.y += wallRunGravity * Time.fixedDeltaTime; if (vel.y < wallRunMaxFallSpeed) vel.y = wallRunMaxFallSpeed; rb.velocity = vel;
    }

    // ---- Climb logic (unchanged) ----
    void TryStartClimb(Collider wallCollider)
    { if (isClimbing || climbCooldownTimer > 0f) return; Bounds b = wallCollider.bounds; Vector3 playerPos = transform.position; float distanceFromTop = b.max.y - playerPos.y; if (distanceFromTop < 1f) return; Vector3 startPos = new Vector3(playerPos.x, b.min.y, playerPos.z); float topY = b.max.y + climbOffsetAboveWall; Vector3 endPos = new Vector3(playerPos.x, topY, playerPos.z); StartCoroutine(ClimbWall(startPos, endPos)); }

    void TryStartClimb(Collider wallCollider, RaycastHit hit)
    { if (isClimbing || climbCooldownTimer > 0f) return; Bounds b = wallCollider.bounds; Vector3 playerPos = transform.position; float distanceFromTop = b.max.y - playerPos.y; if (distanceFromTop < 1f) return; Vector3 hitXZ = new Vector3(hit.point.x, 0f, hit.point.z); Vector3 startPos = new Vector3(hitXZ.x, b.min.y, hitXZ.z); float topY = b.max.y + climbOffsetAboveWall; Vector3 endPos = new Vector3(hitXZ.x, topY, hitXZ.z); if (isSliding) { isSliding = false; rb.drag = normalDrag; } StartCoroutine(ClimbWall(startPos, endPos)); }

    void OnTriggerEnter(Collider other)
    { if (isClimbing) return; if (other.CompareTag("Climbable") || (other.transform.parent != null && other.transform.parent.CompareTag("Climbable"))) { Vector3 origin = transform.position + Vector3.up * 0.5f; if (Physics.Raycast(origin, transform.forward, out RaycastHit hit, climbApproachRayDistance)) { Collider col = hit.collider; if (col == other || col.transform == other.transform || (other.transform.parent != null && col.transform == other.transform.parent)) { TryStartClimb(other, hit); } } } }

    System.Collections.IEnumerator ClimbWall(Vector3 startPos, Vector3 endPos)
    { isClimbing = true; climbCooldownTimer = climbCooldown; rb.isKinematic = true; rb.velocity = Vector3.zero; if (rend != null) rend.material.color = climbColor; transform.position = startPos; float distance = Vector3.Distance(startPos, endPos); float duration = Mathf.Max(0.05f, distance / Mathf.Max(0.01f, climbSpeed)); float elapsed = 0f; while (elapsed < duration) { float t = elapsed / duration; transform.position = Vector3.Lerp(startPos, endPos, t); elapsed += Time.deltaTime; yield return null; } transform.position = endPos; Vector3 forward = transform.forward; Vector3 nudge = forward * climbEdgeForwardPush; if (Physics.Raycast(transform.position + Vector3.up * 0.5f, -forward, out RaycastHit backHit, 1.0f)) { Collider wallCol = backHit.collider; if (myCollider != null && wallCol != null) { Physics.IgnoreCollision(myCollider, wallCol, true); yield return new WaitForSeconds(climbEdgeIgnoreTime); Physics.IgnoreCollision(myCollider, wallCol, false); } } transform.position += nudge; isClimbing = false; rb.isKinematic = false; if (rend != null) rend.material.color = originalColor; transform.localScale = originalScale; }

    private System.Collections.IEnumerator ScaleColorRoutine(Vector3 targetScale, Color targetColor, float duration)
    { Vector3 startScale = transform.localScale; Color startColor = rend != null ? rend.material.color : Color.white; float t = 0f; duration = Mathf.Max(0.01f, duration); while (t < duration) { float u = t / duration; float eased = slideVisualCurve != null ? slideVisualCurve.Evaluate(u) : u; transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, eased); if (rend != null) rend.material.color = Color.LerpUnclamped(startColor, targetColor, eased); t += Time.deltaTime; yield return null; } transform.localScale = targetScale; if (rend != null) rend.material.color = targetColor; }

    private void MonitorLowSpeed()
    { if (isShattered || isClimbing) { lowSpeedTimer = 0f; lastPlanarPos = new Vector3(transform.position.x, 0f, transform.position.z); return; } Vector3 currPlanar = new Vector3(transform.position.x, 0f, transform.position.z); float dt = Mathf.Max(Time.deltaTime, 0.0001f); float planarSpeed = (currPlanar - lastPlanarPos).magnitude / dt; if (planarSpeed < lowSpeedThreshold) { lowSpeedTimer += Time.deltaTime; if (lowSpeedTimer >= lowSpeedTime) { StartCoroutine(ShatterAndRespawn()); } } else lowSpeedTimer = 0f; lastPlanarPos = currPlanar; }

    private System.Collections.IEnumerator ShatterAndRespawn()
    {
        if (isShattered) yield break; isShattered = true; if (slideVisualCoroutine != null) StopCoroutine(slideVisualCoroutine); SpawnFragments(); if (rend != null) rend.enabled = false; if (myCollider != null) myCollider.enabled = false; rb.velocity = Vector3.zero; rb.isKinematic = true; yield return new WaitForSeconds(shatterRespawnDelay);
        bool didRespawn = false; if (CheckpointManager.Instance != null) { CheckpointManager.Instance.RespawnPlayer(); didRespawn = true; }
        if (!didRespawn) { var scene = SceneManager.GetActiveScene(); SceneManager.LoadScene(scene.buildIndex); yield break; }
        transform.localScale = originalScale; if (rend != null) { rend.material.color = originalColor; rend.enabled = true; } if (myCollider != null) myCollider.enabled = true; rb.isKinematic = false; rb.drag = normalDrag; rb.velocity = Vector3.zero; isSliding = false; isClimbing = false; climbCooldownTimer = 0f; coyoteTimeCounter = 0f; jumpBufferCounter = 0f; lowSpeedTimer = 0f; isShattered = false; lastPlanarPos = new Vector3(transform.position.x, 0f, transform.position.z); isWallRunning = false; isWallRight = false; isWallLeft = false; wallRunTimer = 0f; justWallJumped = false; wallJumpCooldownTimer = 0f; consecutiveWallJumps = 0; wallJumpGraceTimer = 0f; wallJumpMovementTimer = 0f; wallContactLostTimer = 0f; lastWallNormal = Vector3.zero; lastWallSide = 0;
    }

    private void SpawnFragments()
    { Vector3 center = transform.position; float worldRadius = 0.5f * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z); for (int i = 0; i < shatterFragmentCount; i++) { var piece = GameObject.CreatePrimitive(PrimitiveType.Sphere); piece.transform.localScale = Vector3.one * shatterFragmentScale; piece.transform.position = center + Random.insideUnitSphere * (worldRadius * 0.5f); var pr = piece.GetComponent<Renderer>(); if (pr != null && rend != null) pr.material.color = originalColor; var pc = piece.GetComponent<Collider>(); if (pc != null && myCollider != null) Physics.IgnoreCollision(pc, myCollider, true); var rbPiece = piece.AddComponent<Rigidbody>(); rbPiece.mass = 0.05f; rbPiece.interpolation = RigidbodyInterpolation.Interpolate; rbPiece.AddExplosionForce(shatterExplosionForce, center, shatterExplosionRadius, 0.1f, ForceMode.Impulse); Destroy(piece, shatterFragmentLifetime); } }
}
