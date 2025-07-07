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
    public float groundCheckDistance = 0.2f;

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
    public float coyoteTimeDuration = 0.1f;
    public float jumpBufferDuration = 0.1f;

    [Header("WallRunning")]
    public float wallCheckDistance = 0.6f;
    public LayerMask wallRunLayer;
    public float wallRunGravity = -2f;
    public float wallRunSpeed = 7f;
    public float maxWallRunTime = 3f;
    public float wallJumpForce = 5f;
    
    [Header("Wall Jump Direction")]
    public float wallJumpForwardForce = 0.3f;
    public float wallJumpSideForce = 0.8f;
    public float wallJumpUpwardForce = 1.0f;
    
    [Header("Wall Jump Chaining")]
    public bool allowWallJumpChaining = true;
    public float wallJumpChainCooldown = 0.1f;
    public int maxConsecutiveWallJumps = 5;
    public float wallJumpGracePeriod = 0.3f;
    public float wallJumpMovementDuration = 0.4f;

    [Header("Slow Down Settings")]
    public float slowDownSpeedMultiplier = 0.3f;
    public float slowDownAnimationSpeed = 0.5f;
    public bool canSlowDownWhileSliding = false;

    [Header("Falling Animation Settings")]
    public float fallingThreshold = -8f;
    public float fallingTimeThreshold = 2.5f;
    public float jumpToFallTransition = 1.0f;

    private bool justWallJumped = false;
    private float wallJumpCooldownTimer = 0f;
    private bool wallRunExpired = false;
    private float wallRunExpiredCooldown = 0.5f;
    private float wallRunExpiredTimer = 0f;
    private int consecutiveWallJumps = 0;
    private float wallJumpGraceTimer = 0f;
    private float wallJumpMovementTimer = 0f;
    private float wallContactLostTimer = 0f;
    private float wallContactTolerance = 0.5f;

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

    private bool isSlowingDown = false;
    private float originalAnimatorSpeed = 1f;

    private float fallingTime = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        originalHeight = controller.height;
        originalCenter = controller.center; 

        if (animator != null)
            originalAnimatorSpeed = animator.speed;

        if (visualModel != null)
            visualModelOriginalLocalY = visualModel.localPosition.y;
            
        if (LevelTimer.Instance != null)
        {
            LevelTimer.Instance.StartTimer();
        }
        else
        {
            LevelTimer timer = FindObjectOfType<LevelTimer>();
            if (timer != null)
                timer.StartTimer();
        }
        
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayGameplayMusic();
    }

    void Update()
    {
        if (justWallJumped)
        {
            wallJumpCooldownTimer += Time.deltaTime;
            if (wallJumpCooldownTimer >= wallJumpChainCooldown)
            {
                justWallJumped = false;
                wallJumpCooldownTimer = 0f;
            }
        }

        if (wallJumpGraceTimer > 0f)
        {
            wallJumpGraceTimer -= Time.deltaTime;
        }
        
        if (wallJumpMovementTimer > 0f)
        {
            wallJumpMovementTimer -= Time.deltaTime;
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

        bool isNearWall = isWallRight || isWallLeft;
        bool canAttemptWallJump = (isWallRunning || isNearWall || wallJumpGraceTimer > 0f);
        
        if (canAttemptWallJump && Input.GetKeyDown(KeyCode.Space) && !justWallJumped)
        {
            bool canChainWallJump = allowWallJumpChaining && consecutiveWallJumps < maxConsecutiveWallJumps;
            
            if (canChainWallJump || isGrounded)
            {
                bool jumpingFromRightWall = isWallRight;
                if (!isWallRight && !isWallLeft)
                {
                    jumpingFromRightWall = (wallJumpGraceTimer > 0f) ? isWallRight : true;
                }
                
                Vector3 wallNormal = jumpingFromRightWall ? -transform.right : transform.right;
                
                Vector3 forwardComponent = transform.forward * wallJumpForwardForce;
                Vector3 sideComponent = wallNormal * wallJumpSideForce;
                Vector3 upComponent = Vector3.up * wallJumpUpwardForce;
                
                Vector3 jumpDirection = (forwardComponent + sideComponent + upComponent).normalized;

                velocity = jumpDirection * wallJumpForce;

                justWallJumped = true;
                jumpStarted = true;
                wallJumpCooldownTimer = 0f;
                wallJumpMovementTimer = wallJumpMovementDuration;
                
                if (!isGrounded)
                {
                    consecutiveWallJumps++;
                }
                else
                {
                    consecutiveWallJumps = 1;
                }
                
                wallJumpGraceTimer = wallJumpGracePeriod;
                
                isWallRunning = false;
                wallRunTimer = 0f;
                coyoteTimeCounter = 0f;

                if (animator != null)
                    animator.SetBool("IsJumping", true);
            }
        }

        HandleWallRunState();
        GroundCheck();
        HandleJumpInput();

        if (isWallRunning)
            WallRunningMovement();
        else
            ApplyGravity();

        SafetyCheckWallRunTimer();

        HandleMovement();

        HandleSlideInput();

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
            if (!wasGrounded)
            {
                coyoteTimeCounter = coyoteTimeDuration;
                consecutiveWallJumps = 0;
            }

            if (velocity.y < 0)
                velocity.y = -2f;

            if ((jumpStarted || animator.GetBool("IsJumping")) && velocity.y <= 0f)
            {
                jumpStarted = false;
                if (animator != null)
                {
                    animator.SetBool("IsJumping", false);
                    animator.SetBool("IsFalling", false);
                }
                
                fallingTime = 0f;
            }
        }
        else
        {
            if (coyoteTimeCounter > 0f)
            {
                coyoteTimeCounter -= Time.deltaTime;
            }
        }
    }

    private void HandleJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferCounter = jumpBufferDuration;
        }
        
        if (jumpBufferCounter > 0f)
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        bool hasJumpInput = jumpBufferCounter > 0f;
        bool canCoyoteJump = !isGrounded && coyoteTimeCounter > 0f;
        bool canRegularJump = isGrounded;
        bool canJump = hasJumpInput && (canRegularJump || canCoyoteJump) && !isSliding && !jumpStarted;
        
        if (canJump)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpStarted = true;
            
            fallingTime = 0f;
            
            coyoteTimeCounter = 0f;
            jumpBufferCounter = 0f;

            if (animator != null)
            {
                animator.SetBool("IsJumping", true);
                animator.SetBool("IsFalling", false);
            }

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
            controller.Move(velocity * Time.deltaTime);
        }
        else if (wallJumpMovementTimer > 0f)
        {
            Vector3 inputMovement = transform.right * horizontalInput * sideSpeed * 0.3f;
            Vector3 move = velocity + inputMovement;
            controller.Move(move * Time.deltaTime);
        }
        else
        {
            float currentSpeed = isSliding ? slideSpeed : runSpeed;
            
            if (isSlowingDown)
                currentSpeed *= slowDownSpeedMultiplier;
                
            Vector3 move = transform.forward * currentSpeed + transform.right * horizontalInput * sideSpeed;

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
        bool shouldSlowDown = Input.GetKey(KeyCode.C) && !isClimbing;
        
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
            
            if (isSlowingDown)
                speed *= slowDownSpeedMultiplier;
                
            animator.SetFloat("Speed", speed);
            animator.SetBool("IsSliding", isSliding);
            animator.SetBool("IsClimbing", isClimbing);
            
            bool isDramaticFalling = false;
            
            if (!isGrounded && velocity.y < 0f && !isClimbing && !isWallRunning && !isSliding)
            {
                fallingTime += Time.deltaTime;
                
                bool isCurrentlyJumping = animator.GetBool("IsJumping") || jumpStarted;
                bool allowFallingDuringJump = fallingTime > jumpToFallTransition;
                
                if (!isCurrentlyJumping || allowFallingDuringJump)
                {
                    isDramaticFalling = (fallingTime >= fallingTimeThreshold) || 
                                       (velocity.y <= fallingThreshold);
                }
            }
            else
            {
                fallingTime = 0f;
            }
            
            animator.SetBool("IsFalling", isDramaticFalling);
            
            if (isGrounded && !jumpStarted && animator.GetBool("IsJumping") && velocity.y <= 0f)
            {
                animator.SetBool("IsJumping", false);
                animator.SetBool("IsFalling", false);
                fallingTime = 0f;
            }
            
            if (!isGrounded && velocity.y < -2f && animator.GetBool("IsJumping") && fallingTime > (jumpToFallTransition * 0.7f))
            {
                animator.SetBool("IsJumping", false);
                jumpStarted = false;
            }
            
            if ((isWallRunning || isClimbing) && animator.GetBool("IsJumping"))
            {
                animator.SetBool("IsJumping", false);
                animator.SetBool("IsFalling", false);
                fallingTime = 0f;
                jumpStarted = false;
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
        {
            animator.SetBool("IsSliding", true);
            animator.SetBool("IsFalling", false);
        }
        
        fallingTime = 0f;
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
                Vector3 playerPos = transform.position;
                Vector3 wallBottom = wallCollider.bounds.min;
                Vector3 wallTop = wallCollider.bounds.max;
                
                float distanceFromTop = wallTop.y - playerPos.y;
                if (distanceFromTop < 1f)
                {
                    return;
                }
                
                climbStartPos = new Vector3(playerPos.x, wallBottom.y, playerPos.z);
                
                float wallTopY = wallTop.y + climbOffsetAboveWall;
                climbEndPos = new Vector3(playerPos.x, wallTopY, playerPos.z);

                StartCoroutine(ClimbWall());
            }
        }
    }

    private IEnumerator ClimbWall()
    {
        isClimbing = true;
        controller.enabled = false;
        velocity = Vector3.zero;
        
        jumpStarted = false;

        Vector3 startPos = climbStartPos;
        Vector3 endPos = climbEndPos;

        transform.position = startPos;

        float distance = Vector3.Distance(startPos, endPos);
        float duration = distance / climbSpeed;
        float elapsed = 0f;

        if (animator != null)
        {
            animator.SetBool("IsClimbing", true);
            animator.SetBool("IsSliding", false);
            animator.SetBool("IsJumping", false);
            animator.SetBool("IsFalling", false);
            animator.SetFloat("Speed", 0f);
        }
        
        fallingTime = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(startPos, endPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;

        isClimbing = false;
        controller.enabled = true;

        if (animator != null)
            animator.SetBool("IsClimbing", false);
    }

    private void CheckForWalls()
    {
        if (!isGrounded)
        {
            Vector3 rayStart = transform.position;
            Vector3 rayStartLower = transform.position + Vector3.down * 0.5f;
            Vector3 rayStartUpper = transform.position + Vector3.up * 0.5f;
            
            bool rightWall1 = Physics.Raycast(rayStart, transform.right, wallCheckDistance, wallRunLayer);
            bool rightWall2 = Physics.Raycast(rayStartLower, transform.right, wallCheckDistance, wallRunLayer);
            bool rightWall3 = Physics.Raycast(rayStartUpper, transform.right, wallCheckDistance, wallRunLayer);
            
            bool leftWall1 = Physics.Raycast(rayStart, -transform.right, wallCheckDistance, wallRunLayer);
            bool leftWall2 = Physics.Raycast(rayStartLower, -transform.right, wallCheckDistance, wallRunLayer);
            bool leftWall3 = Physics.Raycast(rayStartUpper, -transform.right, wallCheckDistance, wallRunLayer);
            
            isWallRight = rightWall1 || rightWall2 || rightWall3;
            isWallLeft = leftWall1 || leftWall2 || leftWall3;
        }
        else
        {
            isWallRight = false;
            isWallLeft = false;
        }
    }

    private void HandleWallRunState()
    {
        if (isWallRunning)
        {
            wallRunTimer += Time.deltaTime;
            
            if (wallRunTimer >= maxWallRunTime)
            {
                StopWallRun();
                return;
            }
        }

        bool hasWallContact = isWallRight || isWallLeft;
        bool canWallRun = !isGrounded && hasWallContact && 
                         (!justWallJumped || wallJumpCooldownTimer > wallJumpChainCooldown * 0.5f);

        if (isWallRunning)
        {
            if (!hasWallContact)
            {
                wallContactLostTimer += Time.deltaTime;
                
                if (wallContactLostTimer >= wallContactTolerance)
                {
                    StopWallRun();
                    return;
                }
            }
            else
            {
                wallContactLostTimer = 0f;
            }
        }

        if (canWallRun && !isWallRunning)
        {
            StartWallRun();
        }
        else if (isGrounded && isWallRunning)
        {
            StopWallRun();
        }
    }

    private void StartWallRun()
    {
        isWallRunning = true;
        wallRunTimer = 0f;
        velocity.y = 0f;
        
        jumpStarted = false;
        
        if (animator != null)
        {
            animator.SetBool("IsJumping", false);
        }
    }

    private void StopWallRun()
    {
        isWallRunning = false;
        wallRunTimer = 0f;
        wallContactLostTimer = 0f;
    }

    private void SafetyCheckWallRunTimer()
    {
        if (isWallRunning && wallRunTimer > maxWallRunTime + 0.1f)
        {
            StopWallRun();
        }
    }

    private void WallRunningMovement()
    {
        Vector3 wallNormal = isWallRight ? transform.right : -transform.right;
        Vector3 wallForward = Vector3.Cross(wallNormal, Vector3.up).normalized;

        if (Vector3.Dot(wallForward, transform.forward) < 0)
        {
            wallForward = -wallForward;
        }

        velocity.x = wallForward.x * wallRunSpeed;
        velocity.z = wallForward.z * wallRunSpeed;

        velocity.y += wallRunGravity * Time.deltaTime;
    }

    public void ResetPlayerState()
    {
        velocity = Vector3.zero;
        isGrounded = false;

        jumpStarted = false;
        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;

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

        if (isSliding)
        {
            EndSlide();
        }
        
        if (isSlowingDown)
        {
            EndSlowDown();
        }

        if (isClimbing)
        {
            StopAllCoroutines();
            isClimbing = false;
            controller.enabled = true;
        }

        controller.height = originalHeight;
        controller.center = originalCenter;

        if (animator != null)
        {
            animator.SetBool("IsJumping", false);
            animator.SetBool("IsSliding", false);
            animator.SetBool("IsClimbing", false);
            animator.SetBool("IsFalling", false);
            animator.SetFloat("Speed", 0f);
        }
        
        fallingTime = 0f;
        
        if (animator != null && animator.GetBool("IsFalling"))
        {
            animator.SetBool("IsFalling", false);
        }

        if (visualModel != null)
        {
            visualModel.localPosition = new Vector3(0, visualModelOriginalLocalY, 0);
        }
    }

    private void StartSlowDown()
    {
        isSlowingDown = true;
        
        if (animator != null)
        {
            animator.speed = slowDownAnimationSpeed;
        }
    }
    
    private void EndSlowDown()
    {
        isSlowingDown = false;
        
        if (animator != null)
        {
            animator.speed = originalAnimatorSpeed;
        }
    }
}
