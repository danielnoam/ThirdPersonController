using UnityEngine;


public enum PlayerAnimationState
{
    Grounded = 0,
    Jump = 1,
    Fall = 2,
    Landing = 3,
    Interact = 4,
    Crouch = 5,
}

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerStateMachine))]
public class PlayerAnimationHandler : MonoBehaviour
{
    private Animator _animator;
    private PlayerStateMachine _stateMachine;

    [Header("Animation Smoothing")]
    [SerializeField, Range(0.01f, 1f)] private float verticalSmoothTime = 0.1f;
    [SerializeField, Range(0.01f, 1f)] private float horizontalSmoothTime = 0.15f;
    
    [Header("Animation Blend Ranges")]
    [Tooltip("Value in the blend tree for walk animations")]
    [SerializeField] private float walkBlendMax = 0.5f;
    [Tooltip("Value in the blend tree for run animations")]
    [SerializeField] private float runBlendMax = 1.0f;
    [Tooltip("Value in the blend tree for sprint animations")]
    [SerializeField] private float sprintBlendMax = 2f;
    
    [Header("Fall Animation")]
    [Tooltip("Maximum fall time used for animation blending")]
    [SerializeField] private float maxFallTime = 2.0f;

    // Animation parameter hashes (for performance)
    private readonly int _stateHash = Animator.StringToHash("StateIndex");
    private readonly int _verticalHash = Animator.StringToHash("Vertical");
    private readonly int _horizontalHash = Animator.StringToHash("Horizontal");
    private readonly int _fallTimeHash = Animator.StringToHash("FallTime");
    private readonly int _isAimingHash = Animator.StringToHash("IsAiming");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _stateMachine = GetComponent<PlayerStateMachine>();
        
        // Validate animation blend ranges
        ValidateBlendRanges();
    }
    
    private void ValidateBlendRanges()
    {
        // Ensure blend ranges are properly ordered
        if (walkBlendMax > runBlendMax)
        {
            Debug.LogWarning("Walk blend max should not exceed run blend max. Adjusting to maintain proper order.");
            walkBlendMax = runBlendMax;
        }
        
        if (runBlendMax > sprintBlendMax)
        {
            Debug.LogWarning("Run blend max should not exceed sprint blend max. Adjusting to maintain proper order.");
            runBlendMax = sprintBlendMax;
        }
    }

    private void Update()
    {
        UpdateAnimationState();
        UpdateMovementAnimation();
        UpdateAimingAnimation();
        UpdateFallAnimation();
    }

    private void UpdateAnimationState()
    {
        // Convert current state to animation state enum
        PlayerAnimationState currentAnimState = _stateMachine.CurrentState switch
        {
            PlayerGroundedState => PlayerAnimationState.Grounded,
            PlayerJumpingState => PlayerAnimationState.Jump,
            PlayerFallingState => PlayerAnimationState.Fall,
            PlayerLandingState => PlayerAnimationState.Landing,
            PlayerInteractingState => PlayerAnimationState.Interact,
            PlayerCrouchingState => PlayerAnimationState.Crouch,
            _ => PlayerAnimationState.Grounded
        };

        // Set the animation state parameter
        _animator.SetInteger(_stateHash, (int)currentAnimState);
    }

private void UpdateMovementAnimation()
{
    // Default animation values
    float verticalValue = 0f;
    float horizontalValue = 0f;
    
    // Get current movement speed and direction from state machine
    float activeSpeed = _stateMachine.activeHorizontalVelocity;
    Vector3 moveDirection = _stateMachine.activeMoveDirection;

    // Calculate the speed blend value for animation
    float speedBlendValue = CalculateSpeedBlend(activeSpeed);

    if (_stateMachine.IsAiming && moveDirection.sqrMagnitude > 0.01f && activeSpeed > 0.01f)
    {
        // When aiming, map movement direction to the animation blend tree
        Vector3 localMoveDir = transform.InverseTransformDirection(moveDirection);
        
        if (localMoveDir.sqrMagnitude > 0.01f)
        {
            // Normalize to get pure direction
            localMoveDir.Normalize();
            
            // Use direction components for strafe animations
            horizontalValue = localMoveDir.x;
            verticalValue = localMoveDir.z;
            
            // For diagonal movement, make sure we reach the same magnitude as non-aiming movement
            // by adjusting the scale factor calculation
            float directionMagnitude = Mathf.Sqrt(horizontalValue * horizontalValue + verticalValue * verticalValue);
            if (directionMagnitude > 0.01f)
            {
                // When moving diagonally, we need to apply a correction factor to reach the same blend values
                // as non-aiming movement. This ensures diagonal movement has the proper animation intensity.
                float scaleFactor = speedBlendValue / directionMagnitude;
                
                // For diagonal movement, we need to boost the scale factor to match non-aiming intensity
                if (Mathf.Abs(horizontalValue) > 0.1f && Mathf.Abs(verticalValue) > 0.1f)
                {
                    // This correction ensures diagonal movement reaches the same intensity as cardinal directions
                    scaleFactor *= 1.414f; // Approximately sqrt(2) to compensate for diagonal normalization
                }
                
                horizontalValue *= scaleFactor;
                verticalValue *= scaleFactor;
            }
        }
    }
    else if (activeSpeed > 0.01f)
    {
        // Non-aiming movement just uses forward speed
        verticalValue = speedBlendValue;
        horizontalValue = 0f;
    }

    // Apply with smoothing
    _animator.SetFloat(_verticalHash, verticalValue, verticalSmoothTime, Time.deltaTime);
    _animator.SetFloat(_horizontalHash, horizontalValue, horizontalSmoothTime, Time.deltaTime);
}

    private void UpdateAimingAnimation()
    {
        _animator.SetBool(_isAimingHash, _stateMachine.IsAiming);
    }

    private void UpdateFallAnimation()
    {
        // Calculate fall intensity for animation blending
        float fallBlend;
        
        if (_stateMachine.CurrentState is PlayerLandingState)
        {
            // Use landing intensity when in landing state
            fallBlend = _stateMachine.LandingIntensity;
        }
        else
        {
            // Otherwise use normalized fall time
            fallBlend = Mathf.Clamp01(_stateMachine.FallTime / maxFallTime);
        }
        
        _animator.SetFloat(_fallTimeHash, fallBlend);
    }

    private float CalculateSpeedBlend(float currentSpeed)
    {
        // Map the current speed to animation blend values using the customizable ranges
        if (currentSpeed <= _stateMachine.walkSpeed)
        {
            // Walk range: 0 to walkBlendMax
            return (currentSpeed / _stateMachine.walkSpeed) * walkBlendMax;
        }
        
        if (currentSpeed <= _stateMachine.runSpeed)
        {
            // Run range: walkBlendMax to runBlendMax
            return walkBlendMax + ((currentSpeed - _stateMachine.walkSpeed) / 
                (_stateMachine.runSpeed - _stateMachine.walkSpeed)) * (runBlendMax - walkBlendMax);
        }
        
        if (currentSpeed <= _stateMachine.sprintSpeed)
        {
            // Sprint range: runBlendMax to sprintBlendMax
            return runBlendMax + ((currentSpeed - _stateMachine.runSpeed) / 
                (_stateMachine.sprintSpeed - _stateMachine.runSpeed)) * (sprintBlendMax - runBlendMax);
        }

        return 0f;
    }
}