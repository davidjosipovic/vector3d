using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float runSpeed = 5f;
    public float sideSpeed = 3f;

    [Header("Jump Settings")]
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public float groundCheckDistance = 0.2f;  // Distance to check for ground

    [Header("Slide Settings")]
    public float slideSpeed = 8f;
    public float slideDuration = 1f;
    public float slideHeight = 1f;
    public Vector3 slideVisualOffset = new Vector3(0, -0.5f, 0);
    public float slideVisualOffsetDelay = 0.2f;
    public float slideVisualOffsetLerpDuration = 0.3f;

    [Header("Climb Settings")]
    public float climbSpeed = 5f;
    public float climbOffsetAboveWall = 0.1f;

    [Header("Animation")]
    public Animator animator;

    [Header("References")]
    public Transform visualModel;

    [Header("Coyote Time Settings")]
    public float coyoteTimeDuration = 0.1f;  // 100ms coyote time window (reduced from 150ms)
    public float jumpBufferDuration = 0.1f;  // Jump input buffer window

    [Header("WallRunning")]
    public float wallCheckDistance = 0.6f;
    public LayerMask wallRunLayer;
    public float wallRunGravity = -2f;
    public float wallRunSpeed = 7f;
    public float maxWallRunTime = 3f; // 3 seconds stick time
    public float wallJumpForce = 5f;
    [Header("Wall Jump Direction")]
    public float wallJumpForwardForce = 0.3f;   // How much forward momentum (reduced for building hopping)
    public float wallJumpSideForce = 0.8f;      // How much sideways push from wall (increased for distance)
    public float wallJumpUpwardForce = 1.0f;    // How much upward force (balanced for chaining)
    [Header("Wall Jump Chaining")]
    public bool allowWallJumpChaining = true;   // Allow multiple wall jumps without touching ground
    public float wallJumpChainCooldown = 0.1f;  // Cooldown between wall jumps when chaining (reduced)
    public int maxConsecutiveWallJumps = 5;     // Max wall jumps before requiring ground touch
    public float wallJumpGracePeriod = 0.3f;    // Grace period to find next wall after jump (increased)
    public float wallJumpMovementDuration = 0.4f; // How long wall jump momentum lasts
    private bool justWallJumped = false;
    
    private float wallJumpCooldownTimer = 0f;
    private bool wallRunExpired = false;
    private float wallRunExpiredCooldown = 0.5f;
    private float wallRunExpiredTimer = 0f;
    private int consecutiveWallJumps = 0;       // Track consecutive wall jumps
    private float wallJumpGraceTimer = 0f;     // Grace period timer for finding next wall
    private float wallJumpMovementTimer = 0f;  // Timer for wall jump movement duration
    private float wallContactLostTimer = 0f;   // Timer for when wall contact is lost
    private float wallContactTolerance = 0.5f; // How long to wait before stopping wall run when contact is lost

    // --- Internal State ---
    private bool isWallRunning = false;
    private bool isWallRight = false;
    private bool isWallLeft = false;
    private float wallRunTimer = 0f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    private bool isSliding = false;
    private float slideTimer = 0f;

    private float originalHeight;
    private Vector3 originalCenter;

    private Vector3 climbStartPos;
    private Vector3 climbEndPos;
    private bool isClimbing = false;

    private bool jumpStarted = false;

    private float visualModelOriginalLocalY;
    private Coroutine slideVisualOffsetCoroutine;

    private float coyoteTimeCounter = 0f;
    private float jumpBufferCounter = 0f;

    [Header("Slow Down Settings")]
    public float slowDownSpeedMultiplier = 0.3f;  // Speed when slowing down (30% of normal)
    public float slowDownAnimationSpeed = 0.5f;   // Animation speed when slowing down
    public bool canSlowDownWhileSliding = false;  // Allow slow down during slide
    
    private bool isSlowingDown = false;
    private float originalAnimatorSpeed = 1f;     // Store original animator speed

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
            Debug.LogError("CharacterController component missing from player.");

        originalHeight = controller.height;
        originalCenter = controller.center; 

        if (animator == null)
            Debug.LogWarning("Animator not assigned.");
        else
            originalAnimatorSpeed = animator.speed; // Store original animator speed

        if (visualModel == null)
            Debug.LogWarning("VisualModel not assigned.");
        else
            visualModelOriginalLocalY = visualModel.localPosition.y;
            
        // Start the level timer when player is ready
        if (LevelTimer.Instance != null)
        {
            LevelTimer.Instance.StartTimer();
            Debug.Log("PlayerController: Timer started successfully!");
        }
        else
        {
            Debug.LogWarning("PlayerController: LevelTimer instance not found! Searching for LevelTimer component...");
            LevelTimer timer = FindObjectOfType<LevelTimer>();
            if (timer != null)
            {
                timer.StartTimer();
                Debug.Log("PlayerController: Found and started LevelTimer component!");
            }
            else
            {
                Debug.LogError("PlayerController: No LevelTimer found in scene! Timer will not work.");
            }
        }
    }

    void Update()
    {
        // Debug keys for testing
        if (Input.GetKeyDown(KeyCode.T))
        {
            TestCoyoteTime();
        }
        
        // Debug key for forcing wall jump (for testing)
        if (Input.GetKeyDown(KeyCode.Y))
        {
            Debug.Log("🔧 FORCE WALL JUMP TEST");
            justWallJumped = false; // Reset flag
            wallJumpCooldownTimer = wallJumpChainCooldown; // Reset cooldown
            Debug.Log($"Flags reset - justWallJumped: {justWallJumped}, cooldown: {wallJumpCooldownTimer}");
        }
        
        // Debug key for wall run timer test
        if (Input.GetKeyDown(KeyCode.U))
        {
            Debug.Log("=== WALL RUN DEBUG INFO ===");
            Debug.Log($"Wall Run Timer: {wallRunTimer:F2}s/{maxWallRunTime:F2}s");
            Debug.Log($"Contact Lost Timer: {wallContactLostTimer:F2}s/{wallContactTolerance:F2}s");
            Debug.Log($"Is Wall Running: {isWallRunning}");
            Debug.Log($"Wall Contact: R{isWallRight} L{isWallLeft}");
            Debug.Log($"Is Grounded: {isGrounded}");
            Debug.Log($"Wall Check Distance: {wallCheckDistance}");
            Debug.Log($"Wall Run Layer: {wallRunLayer.value}");
            float timeLeft = isWallRunning ? (maxWallRunTime - wallRunTimer) : 0f;
            Debug.Log($"Time Remaining: {timeLeft:F2}s");
            Debug.Log("==============================");
        }
        
        // Debug key to force stop wall run
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (isWallRunning)
            {
                Debug.Log("🔧 FORCE STOPPING WALL RUN (Debug Key I)");
                DebugForceStopWallRun();
            }
            else
            {
                Debug.Log("🔧 Not wall running - nothing to stop");
            }
        }
        
        if (justWallJumped)
        {
            wallJumpCooldownTimer += Time.deltaTime;
            if (wallJumpCooldownTimer >= wallJumpChainCooldown) // Use chain cooldown instead
            {
                justWallJumped = false;
                wallJumpCooldownTimer = 0f;
                Debug.Log("Wall jump cooldown finished - can wall jump again");
            }
        }

        // Handle wall jump grace period
        if (wallJumpGraceTimer > 0f)
        {
            wallJumpGraceTimer -= Time.deltaTime;
        }
        
        // Handle wall jump movement timer
        if (wallJumpMovementTimer > 0f)
        {
            wallJumpMovementTimer -= Time.deltaTime;
            if (wallJumpMovementTimer <= 0f)
            {
                Debug.Log("Wall jump movement timer finished");
            }
        }

        if (wallRunExpired)
        {
            wallRunExpiredTimer += Time.deltaTime;
            if (wallRunExpiredTimer >= wallRunExpiredCooldown)
            {
                wallRunExpired = false;
                wallRunExpiredTimer = 0f;
            }
        }

        if (isClimbing)
        {
            HandleClimbingAnimation();
            return;
        }
        else
        {
            if (animator != null)
                animator.SetBool("IsClimbing", false);
        }

        CheckForWalls();

        // Wall Jump - Enhanced for building hopping
        bool isNearWall = isWallRight || isWallLeft;
        bool canAttemptWallJump = (isWallRunning || isNearWall || wallJumpGraceTimer > 0f);
        
        if (canAttemptWallJump && Input.GetKeyDown(KeyCode.Space) && !justWallJumped)
        {
            // Check if we can still chain wall jumps
            bool canChainWallJump = allowWallJumpChaining && consecutiveWallJumps < maxConsecutiveWallJumps;
            
            if (canChainWallJump || isGrounded)
            {
                // Determine which wall we're jumping from
                bool jumpingFromRightWall = isWallRight;
                if (!isWallRight && !isWallLeft)
                {
                    // If no current wall detected, use the last known wall from grace period
                    jumpingFromRightWall = (wallJumpGraceTimer > 0f) ? isWallRight : true;
                }
                
                Vector3 wallNormal = jumpingFromRightWall ? -transform.right : transform.right;
                
                // Enhanced wall jump for building hopping
                Vector3 forwardComponent = transform.forward * wallJumpForwardForce;
                Vector3 sideComponent = wallNormal * wallJumpSideForce;
                Vector3 upComponent = Vector3.up * wallJumpUpwardForce;
                
                Vector3 jumpDirection = (forwardComponent + sideComponent + upComponent).normalized;

                velocity = jumpDirection * wallJumpForce;

                justWallJumped = true;
                jumpStarted = true;
                wallJumpCooldownTimer = 0f; // Reset cooldown timer
                wallJumpMovementTimer = wallJumpMovementDuration; // Start movement timer
                
                // Increment consecutive wall jumps if not grounded
                if (!isGrounded)
                {
                    consecutiveWallJumps++;
                    Debug.Log($"🚀 Wall jump chain #{consecutiveWallJumps} executed!");
                }
                else
                {
                    consecutiveWallJumps = 1;
                    Debug.Log("🚀 Ground wall jump executed!");
                }
                
                // Start grace period for finding next wall
                wallJumpGraceTimer = wallJumpGracePeriod;
                
                // Stop current wall run to prevent sticking
                isWallRunning = false;
                wallRunTimer = 0f;
                coyoteTimeCounter = 0f;

                if (animator != null)
                    animator.SetBool("IsJumping", true);
                    
                Debug.Log($"Wall jump executed - Chain: {consecutiveWallJumps}/{maxConsecutiveWallJumps}, Direction: {jumpDirection}, Wall: {(jumpingFromRightWall ? "Right" : "Left")}");
            }
            else
            {
                Debug.Log($"❌ Wall jump chain limit reached ({consecutiveWallJumps}/{maxConsecutiveWallJumps}) - need to touch ground!");
            }
        }


        HandleWallRunState();
        GroundCheck();
        HandleJumpInput();

        if (isWallRunning)
            WallRunningMovement();
        else
            ApplyGravity();

        // Safety check for wall run timer
        SafetyCheckWallRunTimer();

        HandleMovement();

        HandleSlideInput();

        // Handle slow down input
        HandleSlowDownInput();

        UpdateAnimations();
    }

    private void HandleClimbingAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("IsClimbing", true);
            animator.SetBool("IsSliding", false);
            animator.SetBool("IsJumping", false);
            animator.SetFloat("Speed", 0);
        }
    }

    private void GroundCheck()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        bool wasGrounded = isGrounded;
        isGrounded = Physics.Raycast(rayOrigin, Vector3.down, groundCheckDistance);

        if (isGrounded)
        {
            // Only reset coyote time if we weren't grounded before (just landed)
            if (!wasGrounded)
            {
                coyoteTimeCounter = coyoteTimeDuration;
                consecutiveWallJumps = 0; // Reset wall jump chain when touching ground
                Debug.Log($"Player landed - coyote time set: {coyoteTimeCounter:F3}s, wall jump chain reset");
            }

            if (velocity.y < 0)
                velocity.y = -2f; // Small negative to keep grounded

            // Reset jumping animation when landing (only if we were actually jumping)
            if ((jumpStarted || animator.GetBool("IsJumping")) && velocity.y <= 0f)
            {
                jumpStarted = false;
                if (animator != null)
                    animator.SetBool("IsJumping", false);
                    
                Debug.Log("Jump animation reset - player landed");
            }
        }
        else
        {
            // Only start counting down coyote time if we were previously grounded
            if (wasGrounded)
            {
                Debug.Log($"Player left ground - coyote time started: {coyoteTimeCounter:F3}s");
            }
            
            // Always count down coyote time when in air
            if (coyoteTimeCounter > 0f)
            {
                coyoteTimeCounter -= Time.deltaTime;
            }
        }
    }

    private void HandleJumpInput()
    {
        // Handle jump input buffering
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferCounter = jumpBufferDuration;
            Debug.Log($"Jump input buffered: {jumpBufferCounter:F3}s");
        }
        
        // Count down jump buffer
        if (jumpBufferCounter > 0f)
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // Check if we can execute a jump (either normal or coyote)
        bool hasJumpInput = jumpBufferCounter > 0f;
        bool canCoyoteJump = !isGrounded && coyoteTimeCounter > 0f;
        bool canRegularJump = isGrounded;
        bool canJump = hasJumpInput && (canRegularJump || canCoyoteJump) && !isSliding && !jumpStarted;
        
        if (canJump)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpStarted = true;
            
            // Log for debugging coyote jumps vs regular jumps
            bool isCoyoteJump = canCoyoteJump && !canRegularJump;
            if (isCoyoteJump)
            {
                Debug.Log($"Coyote jump executed! Coyote time remaining: {coyoteTimeCounter:F3}s, Input buffer: {jumpBufferCounter:F3}s");
            }
            else
            {
                Debug.Log($"Regular jump executed. Input buffer: {jumpBufferCounter:F3}s");
            }
            
            // Reset both timers after any jump
            coyoteTimeCounter = 0f;
            jumpBufferCounter = 0f;

            if (animator != null)
                animator.SetBool("IsJumping", true);

            // Stop slide if jumping
            if (isSliding)
                EndSlide();
        }
    }

    private void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
    }

    private void HandleMovement()
    {
        float horizontalInput = Input.GetAxis("Horizontal");

        if (isWallRunning)
        {
            // During wall run, use automatic wall running movement (ignore player input)
            controller.Move(velocity * Time.deltaTime);
        }
        else if (wallJumpMovementTimer > 0f)
        {
            // During wall jump momentum period, preserve wall jump velocity but allow some input
            Vector3 inputMovement = transform.right * horizontalInput * sideSpeed * 0.3f; // Reduced side control
            Vector3 move = velocity + inputMovement;
            controller.Move(move * Time.deltaTime);
            
            Debug.Log($"Wall jump movement: timer {wallJumpMovementTimer:F2}s, velocity: {velocity}");
        }
        else
        {
            float currentSpeed = isSliding ? slideSpeed : runSpeed;
            
            // Apply slow down multiplier if slowing down
            if (isSlowingDown)
                currentSpeed *= slowDownSpeedMultiplier;
                
            Vector3 move = transform.forward * currentSpeed + transform.right * horizontalInput * sideSpeed;

            // Add vertical velocity
            move.y = velocity.y;

            controller.Move(move * Time.deltaTime);
        }
    }

    private void HandleSlideInput()
    {
        // Handle slide input (S key down)
        if ((isGrounded || coyoteTimeCounter > 0f) && Input.GetKeyDown(KeyCode.S) && !isSliding && !isClimbing)
        {
            StartSlide();
            coyoteTimeCounter = 0f;
        }

        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0f)
                EndSlide();
        }
    }
    
    private void HandleSlowDownInput()
    {
        // Handle slow down input (C key hold, not during climb)
        bool shouldSlowDown = Input.GetKey(KeyCode.C) && !isClimbing;
        
        // Only allow slow down if setting permits it during slide, or if not sliding
        if (!canSlowDownWhileSliding && isSliding)
            shouldSlowDown = false;
            
        if (shouldSlowDown && !isSlowingDown)
        {
            StartSlowDown();
        }
        else if (!shouldSlowDown && isSlowingDown)
        {
            EndSlowDown();
        }
    }

    private void UpdateAnimations()
    {
        if (animator != null)
        {
            float speed = isSliding ? slideSpeed : runSpeed;
            
            // Apply slow down multiplier to animation speed if slowing down
            if (isSlowingDown)
                speed *= slowDownSpeedMultiplier;
                
            animator.SetFloat("Speed", speed);
            animator.SetBool("IsSliding", isSliding);
            animator.SetBool("IsClimbing", isClimbing);
            
            // Only force reset jump animation if grounded AND not just started jumping AND falling
            // This prevents interference with coyote jumps
            if (isGrounded && !jumpStarted && animator.GetBool("IsJumping") && velocity.y <= 0f)
            {
                animator.SetBool("IsJumping", false);
                Debug.Log("Jump animation force reset in UpdateAnimations - player landed");
            }
            
            // Reset jump animation if player is in wall run or climbing (special states)
            if ((isWallRunning || isClimbing) && animator.GetBool("IsJumping"))
            {
                animator.SetBool("IsJumping", false);
                jumpStarted = false; // Also reset jump started flag
                Debug.Log("Jump animation reset - player in special state (wall run/climbing)");
            }
        }
    }

    private void StartSlide()
    {
        if (!isGrounded && coyoteTimeCounter <= 0f) return;

        isSliding = true;
        slideTimer = slideDuration;

        StartCoroutine(SmoothAdjustHeightAndCenter(slideHeight, new Vector3(0, slideHeight / 2f, 0)));

        velocity.y = -2f;

        if (slideVisualOffsetCoroutine != null)
            StopCoroutine(slideVisualOffsetCoroutine);

        slideVisualOffsetCoroutine = StartCoroutine(ApplyVisualOffsetSmoothWithDelay());

        if (animator != null)
            animator.SetBool("IsSliding", true);
    }

    private void EndSlide()
    {
        isSliding = false;

        StartCoroutine(SmoothAdjustHeightAndCenter(originalHeight, originalCenter));

        velocity.y = -2f;

        if (slideVisualOffsetCoroutine != null)
        {
            StopCoroutine(slideVisualOffsetCoroutine);
            slideVisualOffsetCoroutine = null;
        }

        if (visualModel != null)
            StartCoroutine(SmoothResetVisualPosition());

        if (animator != null)
            animator.SetBool("IsSliding", false);
    }

    private IEnumerator SmoothAdjustHeightAndCenter(float targetHeight, Vector3 targetCenter)
    {
        float duration = 0.1f;
        float elapsed = 0f;

        float startHeight = controller.height;
        Vector3 startCenter = controller.center;

        while (elapsed < duration)
        {
            controller.height = Mathf.Lerp(startHeight, targetHeight, elapsed / duration);
            controller.center = Vector3.Lerp(startCenter, targetCenter, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        controller.height = targetHeight;
        controller.center = targetCenter;
    }

    private IEnumerator ApplyVisualOffsetSmoothWithDelay()
    {
        yield return new WaitForSeconds(slideVisualOffsetDelay);

        if (visualModel == null)
            yield break;

        Vector3 startPos = visualModel.localPosition;
        Vector3 targetPos = visualModelOriginalLocalY * Vector3.up + slideVisualOffset;

        float elapsed = 0f;

        while (elapsed < slideVisualOffsetLerpDuration)
        {
            visualModel.localPosition = Vector3.Lerp(startPos, targetPos, elapsed / slideVisualOffsetLerpDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        visualModel.localPosition = targetPos;
    }

    private IEnumerator SmoothResetVisualPosition()
    {
        Vector3 startPos = visualModel.localPosition;
        Vector3 targetPos = new Vector3(0, visualModelOriginalLocalY, 0);

        float elapsed = 0f;
        float duration = slideVisualOffsetLerpDuration;

        while (elapsed < duration)
        {
            visualModel.localPosition = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        visualModel.localPosition = targetPos;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isClimbing)
            return;

        Transform climbableTransform = null;
        if (other.CompareTag("Climbable"))
            climbableTransform = other.transform;
        else if (other.transform.parent != null && other.transform.parent.CompareTag("Climbable"))
            climbableTransform = other.transform.parent;

        if (climbableTransform != null)
        {
            Collider wallCollider = climbableTransform.GetComponent<Collider>();
            if (wallCollider != null)
            {
                // Proverava da li je player dovoljno blizu dna zida da počne penjanje
                Vector3 playerPos = transform.position;
                Vector3 wallBottom = wallCollider.bounds.min;
                Vector3 wallTop = wallCollider.bounds.max;
                
                // Ako je player previše blizu vrha zida, ne počinji penjanje
                float distanceFromTop = wallTop.y - playerPos.y;
                if (distanceFromTop < 1f) // Manje od 1 metar od vrha
                {
                    Debug.Log("Player too close to wall top, skipping climb");
                    return;
                }
                
                // Početna pozicija je na dnu zida, ali sa player-ovim X i Z koordinatama
                climbStartPos = new Vector3(playerPos.x, wallBottom.y, playerPos.z);
                
                // Krajnja pozicija je na vrhu zida sa offsetom
                float wallTopY = wallTop.y + climbOffsetAboveWall;
                climbEndPos = new Vector3(playerPos.x, wallTopY, playerPos.z);

                Debug.Log($"Climbing - Wall bottom: {wallBottom.y}, Wall top: {wallTop.y}");
                Debug.Log($"Starting climb from Y={climbStartPos.y} to Y={climbEndPos.y}");
                StartCoroutine(ClimbWall());
            }
            else
            {
                Debug.LogWarning("Climbable object missing Collider component.");
            }
        }
    }

    private IEnumerator ClimbWall()
    {
        isClimbing = true;
        controller.enabled = false;
        velocity = Vector3.zero;
        
        // Reset jump state when starting climb
        jumpStarted = false;

        Vector3 startPos = climbStartPos;
        Vector3 endPos = climbEndPos;

        // Odmah postavi player-a na početnu poziciju (dno zida)
        transform.position = startPos;

        float distance = Vector3.Distance(startPos, endPos);
        float duration = distance / climbSpeed;
        float elapsed = 0f;

        if (animator != null)
        {
            animator.SetBool("IsClimbing", true);
            animator.SetBool("IsSliding", false);
            animator.SetBool("IsJumping", false);
            animator.SetFloat("Speed", 0f);
            
            Debug.Log("Jump animation reset - climbing started");
        }

        Debug.Log($"Climb duration: {duration:F2}s, Distance: {distance:F2}m");

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(startPos, endPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Osiguraj da je player tačno na krajnjoj poziciji
        transform.position = endPos;

        isClimbing = false;
        controller.enabled = true;

        if (animator != null)
            animator.SetBool("IsClimbing", false);
            
        Debug.Log($"Climb completed at Y={transform.position.y}");
    }

    private void CheckForWalls()
    {
        // Check for walls when airborne
        if (!isGrounded)
        {
            // Multiple raycast points for more reliable wall detection
            Vector3 rayStart = transform.position;
            Vector3 rayStartLower = transform.position + Vector3.down * 0.5f;
            Vector3 rayStartUpper = transform.position + Vector3.up * 0.5f;
            
            // Cast multiple rays for better detection
            bool rightWall1 = Physics.Raycast(rayStart, transform.right, wallCheckDistance, wallRunLayer);
            bool rightWall2 = Physics.Raycast(rayStartLower, transform.right, wallCheckDistance, wallRunLayer);
            bool rightWall3 = Physics.Raycast(rayStartUpper, transform.right, wallCheckDistance, wallRunLayer);
            
            bool leftWall1 = Physics.Raycast(rayStart, -transform.right, wallCheckDistance, wallRunLayer);
            bool leftWall2 = Physics.Raycast(rayStartLower, -transform.right, wallCheckDistance, wallRunLayer);
            bool leftWall3 = Physics.Raycast(rayStartUpper, -transform.right, wallCheckDistance, wallRunLayer);
            
            // Wall detected if any of the rays hit
            isWallRight = rightWall1 || rightWall2 || rightWall3;
            isWallLeft = leftWall1 || leftWall2 || leftWall3;
            
            // Draw debug rays in Scene view (only the main ones to avoid clutter)
            Debug.DrawRay(rayStart, transform.right * wallCheckDistance, isWallRight ? Color.green : Color.red);
            Debug.DrawRay(rayStart, -transform.right * wallCheckDistance, isWallLeft ? Color.green : Color.red);
            
            // Additional rays in different colors
            Debug.DrawRay(rayStartLower, transform.right * wallCheckDistance, rightWall2 ? Color.cyan : Color.gray, 0f, false);
            Debug.DrawRay(rayStartLower, -transform.right * wallCheckDistance, leftWall2 ? Color.cyan : Color.gray, 0f, false);
            
            // Debug wall contact loss with more detail
            if (isWallRunning && !isWallRight && !isWallLeft)
            {
                Debug.Log($"⚠️ WALL CONTACT LOST! Distance: {wallCheckDistance}, Layer: {wallRunLayer.value}");
                Debug.Log($"Right rays: {rightWall1}/{rightWall2}/{rightWall3}, Left rays: {leftWall1}/{leftWall2}/{leftWall3}");
                
                // Try raycast without layer mask to see if wall is there but wrong layer
                bool anyWallRight = Physics.Raycast(rayStart, transform.right, wallCheckDistance);
                bool anyWallLeft = Physics.Raycast(rayStart, -transform.right, wallCheckDistance);
                if (anyWallRight || anyWallLeft)
                {
                    Debug.Log($"🔍 Wall detected without layer mask - check wallRunLayer setting!");
                }
            }
        }
        else
        {
            isWallRight = false;
            isWallLeft = false;
        }
    }

    private void HandleWallRunState()
    {
        // Check if wall run timer has expired first - this is the PRIMARY condition
        if (isWallRunning)
        {
            wallRunTimer += Time.deltaTime;
            
            if (wallRunTimer >= maxWallRunTime)
            {
                Debug.Log($"⏰ WALL RUN TIMER EXPIRED! {wallRunTimer:F2}s >= {maxWallRunTime:F2}s - FORCING STOP!");
                StopWallRun();
                return;
            }
        }

        // Enhanced wall detection with contact tolerance
        bool hasWallContact = isWallRight || isWallLeft;
        bool canWallRun = !isGrounded && hasWallContact && 
                         (!justWallJumped || wallJumpCooldownTimer > wallJumpChainCooldown * 0.5f);

        // Handle wall contact lost timer - ONLY stop if tolerance exceeded AND timer hasn't expired
        if (isWallRunning)
        {
            if (!hasWallContact)
            {
                wallContactLostTimer += Time.deltaTime;
                
                // Only log occasionally to avoid spam
                if (Mathf.FloorToInt(wallContactLostTimer * 10) != Mathf.FloorToInt((wallContactLostTimer - Time.deltaTime) * 10))
                {
                    Debug.Log($"⚠️ Wall contact lost for {wallContactLostTimer:F2}s/{wallContactTolerance:F2}s (Timer: {wallRunTimer:F1}s/{maxWallRunTime:F1}s)");
                }
                
                // Only stop if tolerance exceeded AND we still have wall run time left
                if (wallContactLostTimer >= wallContactTolerance)
                {
                    Debug.Log($"❌ Wall contact lost too long - stopping wall run! Contact lost: {wallContactLostTimer:F2}s, Wall run timer: {wallRunTimer:F2}s/{maxWallRunTime:F2}s");
                    StopWallRun();
                    return;
                }
            }
            else
            {
                // Reset contact lost timer when wall contact is restored
                if (wallContactLostTimer > 0f)
                {
                    Debug.Log($"✅ Wall contact restored after {wallContactLostTimer:F2}s (Timer: {wallRunTimer:F1}s/{maxWallRunTime:F1}s)");
                    wallContactLostTimer = 0f;
                }
            }
        }

        // Start wall run
        if (canWallRun && !isWallRunning)
        {
            Debug.Log($"🏃 Starting wall run - Wall: R{isWallRight} L{isWallLeft} - Duration: {maxWallRunTime}s");
            StartWallRun();
        }
        // Stop wall run due to ground contact (immediate stop - this overrides timer)
        else if (isGrounded && isWallRunning)
        {
            Debug.Log($"❌ Stopping wall run - touched ground! Timer: {wallRunTimer:F2}s/{maxWallRunTime:F2}s");
            StopWallRun();
        }
        
        // Debug log every half second during wall run
        if (isWallRunning && Mathf.FloorToInt(wallRunTimer * 2) != Mathf.FloorToInt((wallRunTimer - Time.deltaTime) * 2))
        {
            float timeLeft = maxWallRunTime - wallRunTimer;
            Debug.Log($"🏃 Wall running: {wallRunTimer:F1}s/{maxWallRunTime:F1}s (⏳{timeLeft:F1}s left) - Contact: R{isWallRight} L{isWallLeft} - Lost: {wallContactLostTimer:F2}s");
        }
    }

    private void StartWallRun()
    {
        isWallRunning = true;
        wallRunTimer = 0f;
        velocity.y = 0f; // Reset vertical velocity for smooth wall run
        
        // Reset jump state when starting wall run
        jumpStarted = false;
        
        Debug.Log("Wall run started!");
        if (animator != null)
        {
            animator.SetBool("IsJumping", false);
            Debug.Log("Jump animation reset - wall run started");
        }
    }

    private void StopWallRun()
    {
        isWallRunning = false;
        wallRunTimer = 0f;
        wallContactLostTimer = 0f; // Reset contact lost timer
        Debug.Log("Wall run stopped!");

        // When wall run stops, apply normal gravity to make player fall
        // Don't reset velocity.y to 0 - let them fall naturally
    }

    // Additional debug methods for testing
    private void DebugForceStopWallRun()
    {
        if (isWallRunning)
        {
            Debug.Log($"🔧 FORCE STOPPING WALL RUN - Timer was: {wallRunTimer:F2}s/{maxWallRunTime:F2}s");
            StopWallRun();
        }
    }

    // Safety check to ensure wall run doesn't exceed max time
    private void SafetyCheckWallRunTimer()
    {
        if (isWallRunning && wallRunTimer > maxWallRunTime + 0.1f) // Small buffer for frame timing
        {
            Debug.LogWarning($"⚠️ SAFETY: Wall run exceeded max time! {wallRunTimer:F2}s > {maxWallRunTime:F2}s - Force stopping!");
            StopWallRun();
        }
    }

    private void WallRunningMovement()
    {
        Vector3 wallNormal = isWallRight ? transform.right : -transform.right;
        Vector3 wallForward = Vector3.Cross(wallNormal, Vector3.up).normalized;

        // Ensure wallForward is in the same direction as player forward
        if (Vector3.Dot(wallForward, transform.forward) < 0)
        {
            wallForward = -wallForward;
        }

        // Move player automatically along the wall at constant speed
        velocity.x = wallForward.x * wallRunSpeed;
        velocity.z = wallForward.z * wallRunSpeed;

        // Apply very light gravity during wall run
        velocity.y += wallRunGravity * Time.deltaTime;
    }

    // Method to reset player state when respawning at checkpoint
    public void ResetPlayerState()
    {
        // Reset movement state
        velocity = Vector3.zero;
        isGrounded = false;

        // Reset jumping state
        jumpStarted = false;
        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;

        // Reset wall running state
        isWallRunning = false;
        isWallRight = false;
        isWallLeft = false;
        wallRunTimer = 0f;
        justWallJumped = false;
        wallJumpCooldownTimer = 0f;
        consecutiveWallJumps = 0;
        wallJumpGraceTimer = 0f;
        wallJumpMovementTimer = 0f;
        wallContactLostTimer = 0f;

        // Reset sliding state
        if (isSliding)
        {
            EndSlide();
        }
        
        // Reset slow down state
        if (isSlowingDown)
        {
            EndSlowDown();
        }

        // Reset climbing state
        if (isClimbing)
        {
            StopAllCoroutines();
            isClimbing = false;
            controller.enabled = true;
        }

        // Reset character controller
        controller.height = originalHeight;
        controller.center = originalCenter;

        // Reset animations
        if (animator != null)
        {
            animator.SetBool("IsJumping", false);
            animator.SetBool("IsSliding", false);
            animator.SetBool("IsClimbing", false);
            animator.SetFloat("Speed", 0f);
        }

        // Reset visual model position
        if (visualModel != null)
        {
            visualModel.localPosition = new Vector3(0, visualModelOriginalLocalY, 0);
        }

        Debug.Log("Player state reset for checkpoint respawn");
    }

    // Debug method to test coyote time behavior (called via T key)
    public void TestCoyoteTime()
    {
        Debug.Log($"=== COYOTE TIME DEBUG ===");
        Debug.Log($"Is Grounded: {isGrounded}");
        Debug.Log($"Coyote Time Counter: {coyoteTimeCounter:F3}s / {coyoteTimeDuration:F3}s");
        Debug.Log($"Jump Buffer Counter: {jumpBufferCounter:F3}s / {jumpBufferDuration:F3}s");
        Debug.Log($"Jump Started: {jumpStarted}");
        Debug.Log($"Is Jumping (Animation): {(animator != null ? animator.GetBool("IsJumping") : "No Animator")}");
        Debug.Log($"Velocity Y: {velocity.y:F2}");
        Debug.Log($"Can Coyote Jump: {!isGrounded && coyoteTimeCounter > 0f && !jumpStarted}");
        Debug.Log($"Can Regular Jump: {isGrounded && !jumpStarted}");
        Debug.Log($"Has Jump Input: {jumpBufferCounter > 0f}");
        Debug.Log($"=== WALL JUMP CHAIN DEBUG ===");
        Debug.Log($"Consecutive Wall Jumps: {consecutiveWallJumps}/{maxConsecutiveWallJumps}");
        Debug.Log($"Wall Jump Grace Timer: {wallJumpGraceTimer:F3}s");
        Debug.Log($"Wall Jump Movement Timer: {wallJumpMovementTimer:F3}s/{wallJumpMovementDuration:F3}s");
        Debug.Log($"Just Wall Jumped: {justWallJumped}");
        Debug.Log($"Wall Right: {isWallRight}, Wall Left: {isWallLeft}");
        Debug.Log($"Can Chain Wall Jump: {allowWallJumpChaining && consecutiveWallJumps < maxConsecutiveWallJumps}");
        Debug.Log($"Wall Run Timer: {wallRunTimer:F2}s/{maxWallRunTime:F2}s");
        Debug.Log($"========================");
    }

    private void StartSlowDown()
    {
        isSlowingDown = true;
        
        // Slow down the animator
        if (animator != null)
        {
            animator.speed = slowDownAnimationSpeed;
        }
        
        Debug.Log($"Slow down started - Speed: {slowDownSpeedMultiplier * 100}%, Animation: {slowDownAnimationSpeed * 100}%");
    }
    
    private void EndSlowDown()
    {
        isSlowingDown = false;
        
        // Restore normal animator speed
        if (animator != null)
        {
            animator.speed = originalAnimatorSpeed;
        }
        
        Debug.Log("Slow down ended - Normal speed restored");
    }
}
