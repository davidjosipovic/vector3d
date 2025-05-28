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
    public float coyoteTimeDuration = 0.15f;  // 150 ms coyote time window

    [Header("WallRunning")]
    public float wallCheckDistance = 0.6f;
    public LayerMask wallRunLayer;
    public float wallRunGravity = -2f;
    public float wallRunSpeed = 7f;
    public float maxWallRunTime = 1.5f;
    public float wallJumpForce = 8f;
    private bool justWallJumped = false;
    private float wallJumpCooldown = 0.2f;
    private float wallJumpCooldownTimer = 0f;

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

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
            Debug.LogError("CharacterController component missing from player.");

        originalHeight = controller.height;
        originalCenter = controller.center;

        if (animator == null)
            Debug.LogWarning("Animator not assigned.");

        if (visualModel == null)
            Debug.LogWarning("VisualModel not assigned.");
        else
            visualModelOriginalLocalY = visualModel.localPosition.y;
    }

    void Update()
    {
        if (justWallJumped)
        {
            wallJumpCooldownTimer += Time.deltaTime;
            if (wallJumpCooldownTimer >= wallJumpCooldown)
            {
                justWallJumped = false;
                wallJumpCooldownTimer = 0f;
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

        if (isWallRunning && Input.GetKeyDown(KeyCode.Space) && !justWallJumped)
        {
            Vector3 wallNormal = isWallRight ? transform.right : -transform.right;
            Vector3 jumpDirection = (wallNormal * 1.5f + Vector3.up * 1.5f + transform.forward).normalized;

            // Apply jump direction directly to velocity
            velocity.x = jumpDirection.x * wallJumpForce;
            velocity.y = jumpDirection.y * wallJumpForce;
            velocity.z = jumpDirection.z * wallJumpForce;

            justWallJumped = true;
            isWallRunning = false;
            wallRunTimer = 0f;

            if (animator != null)
                animator.SetBool("IsJumping", true);
        }


        HandleWallRunState();
        GroundCheck();
        HandleJumpInput();

        if (isWallRunning)
            WallRunningMovement();
        else
            ApplyGravity();

        HandleMovement();

        HandleSlideInput();

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
        isGrounded = Physics.Raycast(rayOrigin, Vector3.down, groundCheckDistance);

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTimeDuration;

            if (velocity.y < 0)
                velocity.y = -2f; // Small negative to keep grounded

            if (jumpStarted)
            {
                jumpStarted = false;
                if (animator != null)
                    animator.SetBool("IsJumping", false);
            }
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
    }

    private void HandleJumpInput()
    {
        if ((isGrounded || coyoteTimeCounter > 0f) && Input.GetKeyDown(KeyCode.Space) && !isSliding && !jumpStarted)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpStarted = true;
            coyoteTimeCounter = 0f;

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
            // velocity.x and velocity.z already handled in WallRunningMovement
            Vector3 move = new Vector3(velocity.x, velocity.y, velocity.z);
            controller.Move(move * Time.deltaTime);
        }
        else
        {
            float currentSpeed = isSliding ? slideSpeed : runSpeed;
            Vector3 move = transform.forward * currentSpeed + transform.right * horizontalInput * sideSpeed;

            // Add vertical velocity
            move.y = velocity.y;

            controller.Move(move * Time.deltaTime);
        }
    }

    private void HandleSlideInput()
    {
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

    private void UpdateAnimations()
    {
        if (animator != null)
        {
            float speed = isSliding ? slideSpeed : runSpeed;
            animator.SetFloat("Speed", speed);
            animator.SetBool("IsSliding", isSliding);
            animator.SetBool("IsClimbing", isClimbing);
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
                // Start climb at player's current X,Z and current Y to avoid teleportation
                climbStartPos = transform.position;

                float wallTopY = wallCollider.bounds.max.y + climbOffsetAboveWall;
                climbEndPos = new Vector3(transform.position.x, wallTopY, transform.position.z);

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

        Vector3 startPos = climbStartPos;
        Vector3 endPos = climbEndPos;

        float distance = Vector3.Distance(startPos, endPos);
        float duration = distance / climbSpeed;
        float elapsed = 0f;

        if (animator != null)
        {
            animator.SetBool("IsClimbing", true);
            animator.SetBool("IsSliding", false);
            animator.SetBool("IsJumping", false);
            animator.SetFloat("Speed", 0f);
        }

        transform.position = startPos;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
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
        isWallRight = Physics.Raycast(transform.position, transform.right, wallCheckDistance, wallRunLayer);
        isWallLeft = Physics.Raycast(transform.position, -transform.right, wallCheckDistance, wallRunLayer);
    }

    private void HandleWallRunState()
    {
        if (isWallRight || isWallLeft)
        {
            if (!isWallRunning)
            {
                StartWallRun();
            }
            else
            {
                wallRunTimer += Time.deltaTime;
                if (wallRunTimer > maxWallRunTime)
                    StopWallRun();
            }
        }
        else
        {
            if (isWallRunning)
                StopWallRun();
        }
    }

    private void StartWallRun()
    {
        isWallRunning = true;
        wallRunTimer = 0f;
        if (animator != null)
            animator.SetBool("IsJumping", false);
    }

    private void StopWallRun()
    {
        isWallRunning = false;
        wallRunTimer = 0f;
    }

    private void WallRunningMovement()
    {
        Vector3 wallNormal = isWallRight ? transform.right : -transform.right;

        // Calculate wallForward as cross product of wallNormal and up
        Vector3 wallForward = Vector3.Cross(wallNormal, Vector3.up).normalized;

        // Check if wallForward points roughly in the same direction as player forward
        if (Vector3.Dot(wallForward, transform.forward) < 0)
        {
            // If not, invert it
            wallForward = -wallForward;
        }

        // Move player along the wall
        Vector3 horizontalVelocity = wallForward * wallRunSpeed;

        if (!justWallJumped)
        {
            // Only apply horizontal velocity when not just wall jumping
            velocity.x = horizontalVelocity.x;
            velocity.z = horizontalVelocity.z;

            // Reduced gravity during wall run
            velocity.y += wallRunGravity * Time.deltaTime;
        }



    }

}
