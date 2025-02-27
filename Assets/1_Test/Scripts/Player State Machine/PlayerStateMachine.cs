using System;
using TMPro;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Serialization;


public enum PlayerAnimationState
{
    Grounded = 0,
    Jump = 1,
    Fall = 2,
    Landing = 3,
    Interact = 4,
}


[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerStateMachine : MonoBehaviour
{
    public PlayerBaseState CurrentState { get; private set; }
    public PlayerGroundedState GroundedState { get; private set; }
    public PlayerJumpingState JumpingState { get; private set; }
    public PlayerFallingState FallingState { get; private set; }
    public PlayerLandingState LandingState { get; private set; }
    public PlayerInteractingState InteractingState { get; private set; }

    [Header("Movement")]
    [Tooltip("Walking speed when holding the walk button")]
    public float walkSpeed = 4f;
    [Tooltip("Default running speed")]
    public float runSpeed = 8f;
    [Tooltip("Maximum speed when sprinting with sufficient input")]
    public float sprintSpeed = 12f;
    [Tooltip("How quickly the character reaches target speed")]
    public float acceleration = 10f;
    [Tooltip("Base rotation speed when turning on the ground")]
    public float rotationSpeed = 2f;
    [Tooltip("Rotation speed when turning while aiming")]
    public float aimRotationSpeed = 4f;

    [Header("Air Movement")]
    [Tooltip("Maximum horizontal speed while in the air")]
    public float airMoveSpeed = 4f;
    [Tooltip("Base rotation speed when turning in the air")]
    public float airRotationSpeed = 1f;
    [Tooltip("How quickly the character reaches target speed in air")]
    public float airAcceleration = 3f;
    [Tooltip("How quickly the character loses momentum in air")]
    public float airFriction = 2.0f;
    
    [Header("Jump")]
    [Tooltip("Initial upward velocity applied when jumping")]
    public float jumpForce = 8f;

    [Header("Gravity")]
    [Tooltip("Downward acceleration applied while in the air")]
    public float gravity = -15f;
    [Tooltip("Small downward force applied while grounded to stick to slopes")]
    public float groundedGravity = -5f;
    [Tooltip("Maximum downward velocity the character can reach")]
    public float maxVerticalVelocity = -50f;

    [Header("Land")]
    [Tooltip("Minimum time falling before impact animations trigger")]
    public float fallThreshold = 0.1f;
    [Tooltip("Fall time that results in maximum impact effect")]
    public float maxFallTime = 2.0f;
    [Tooltip("Time needed to recover from maximum impact landing")]
    public float recoveryDuration = 1f;
    [Tooltip("Percentage of movement control retained during landing recovery")]
    public float minMovementControl = 0.1f;

    [Header("Ground Check")]
    [Tooltip("Radius of the sphere used to detect ground")]
    [SerializeField] private float groundCheckRadius = 0.23f;
    [Tooltip("Offset from character position for ground detection")]
    [SerializeField] private Vector3 groundCheckOffset = new Vector3(0, -0.1f, 0);
    [Tooltip("Layer mask defining what objects count as ground")]
    [SerializeField] private LayerMask groundLayer = 1;

    
    [Header("References")]
    [Tooltip("Priority of the freelook camera (normal camera)")]
    public int freelookCameraPriority = 10;
    [Tooltip("Priority of the aim camera")]
    public int aimCameraPriority = 15;
    [SerializeField] private CinemachineCamera freeLookCamera;
    [SerializeField] private CinemachineCamera aimCamera;
    [SerializeField] private CharacterController controller;
    [SerializeField] private Animator animator;
    [SerializeField] private TextMeshProUGUI debugText;
    
    

    public PlayerInput input { get; private set; }
    public MultiStateInteractable currentInteractable { get;  set; }
    public RobotCompanion robot { get;  set; }
    public float AirTime { get; private set; }

    public float FallTime { get; private set; }

    public float LandingIntensity { get; private set; }
    public float activeMoveSpeed { get; private set; }
    public float activeVerticalVelocity { get; private set; }
    public Vector3 activeMoveDirection { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool CanInteract { get; private set; }
    public bool LockSprinting { get; private set; }
    public bool IsAiming => _isAiming;
    private bool _isAiming = false;
    private float _timeEnteredAimMode = 0f;
    private Vector3 _lastAimDirection = Vector3.forward;
    

    
    // Animation hashes
    private readonly int _stateHash = Animator.StringToHash("StateIndex");
    private readonly int _verticalHash = Animator.StringToHash("Vertical");
    private readonly int _horizontalHash = Animator.StringToHash("Horizontal");
    private readonly int _fallTimeHash = Animator.StringToHash("FallTime");
    private readonly int _isAimingHash = Animator.StringToHash("IsAiming");
    


    private void Awake()
    {
        if (!controller) controller = GetComponent<CharacterController>();
        if (!animator) animator = GetComponent<Animator>();
        input = GetComponent<PlayerInput>();

        // Initialize states
        GroundedState = new PlayerGroundedState(this);
        JumpingState = new PlayerJumpingState(this);
        FallingState = new PlayerFallingState(this);
        LandingState = new PlayerLandingState(this);
        InteractingState = new PlayerInteractingState(this);
        
        if (freeLookCamera == null) Debug.LogError("Cinemachine cameras not assigned!");

        // Set camera priorities
        freeLookCamera.Priority = 15;

        // Initialize movement properties
        activeMoveSpeed = 0f;
        activeVerticalVelocity = 0f;
        activeMoveDirection = Vector3.zero;

        // Set initial state
        SwitchState(GroundedState);
    }

    private void Start()
    {
        robot = FindFirstObjectByType<RobotCompanion>();
    }

    private void Update()
    {
        CheckGrounded();
        UpdateFallTime();
        UpdateAimState();
        CurrentState.UpdateState();
        SyncAnimations();
        UpdateDebugText();
    }

    private void FixedUpdate()
    {
        CurrentState.FixedUpdateState();
        MoveCharacter(); // Apply movement every fixed update
    }

    private void OnTriggerEnter(Collider other)
    {
        other.TryGetComponent(out MultiStateInteractable interactable);
        
        if (interactable)
        {
            currentInteractable = interactable;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        other.TryGetComponent(out MultiStateInteractable interactable);
        
        if (interactable && interactable == currentInteractable)
        {
            // Allow player interaction
            CanInteract = currentInteractable.PlayerCanInteract() && CurrentState == GroundedState;
            
            // Allow robot interaction
            if (robot && currentInteractable.RobotCanInteract() && input.RobotInteractInput)
            {
                input.ConsumeRobotInteractBuffer();
                robot.InteractWith(currentInteractable);
            }

            if (CanInteract && !currentInteractable.Highlighted())
            {
                currentInteractable.SetHighlight(true);
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        other.TryGetComponent(out MultiStateInteractable interactable);
        if (interactable && interactable == currentInteractable)
        {
            currentInteractable.SetHighlight(false);
            currentInteractable = null;
            CanInteract = false;
        }
    }
    


    #region State Control ---------------------------------------------------------------

    public void SwitchState(PlayerBaseState newState)
    {
        CurrentState?.ExitState();
        CurrentState = newState;
        CurrentState.EnterState();
        
        Debug.Log($"Switched to {newState.GetType().Name}");
    }
    
    public void OnInteractionComplete(MultiStateInteractable interactable)
    {
        InteractingState.OnInteractionComplete(interactable);
    }

    #endregion State Control ---------------------------------------------------------------




    #region Aiming ---------------------------------------------------------------

    
    private void UpdateAimState()
    {
        // Toggle aim mode based on input
        if (input.AimInput)
        {
            if (!_isAiming)
            {
                _isAiming = true;
                _timeEnteredAimMode = Time.time;
            
                // Set camera priorities to switch to aim camera
                if (aimCamera != null && freeLookCamera != null)
                {
                    aimCamera.Priority = aimCameraPriority;
                    freeLookCamera.Priority = freelookCameraPriority;
                }
            }
        }
        else if (_isAiming)
        {
            _isAiming = false;
        
            // Reset camera priorities
            if (aimCamera != null && freeLookCamera != null)
            {
                freeLookCamera.Priority = aimCameraPriority;
                aimCamera.Priority = freelookCameraPriority;
            }
        }
    }
    
    public Vector3 GetCameraAimDirection()
    {
        if (freeLookCamera == null)
            return transform.forward;
        
        Vector3 cameraForward = freeLookCamera.transform.forward;
        cameraForward.y = 0;
    
        if (cameraForward.sqrMagnitude < 0.001f)
            return _lastAimDirection;
        
        cameraForward.Normalize();
        _lastAimDirection = cameraForward;
        return cameraForward;
    }
    public void HandleAimRotation()
    {
        if (!_isAiming)
            return;
        
        Vector3 aimDirection = GetCameraAimDirection();
        Quaternion targetRotation = Quaternion.LookRotation(aimDirection);
    
        // Rotate player to face aim direction
        RotateCharacter(
            targetRotation,
            aimRotationSpeed,
            1.0f
        );
    }
    

    #endregion Aiming ---------------------------------------------------------------
    
    
    
    #region Movement ---------------------------------------------------------------
    


    private void CheckGrounded()
    {
        Vector3 spherePosition = transform.position + groundCheckOffset;
        IsGrounded = Physics.CheckSphere(spherePosition, groundCheckRadius, groundLayer);
    }
    
    private void UpdateFallTime()
    {
        // Reset fall time when grounded
        if (IsGrounded && FallTime > 0)
        {
            SetFallTime(0);
            return;
        }

        // Only increment fall time when moving downward
        if (!IsGrounded && CurrentState != JumpingState)
        {
            SetFallTime(FallTime + Time.deltaTime);
        }
    }


    private void MoveCharacter()
    {
        // Create movement vector using the active properties
        Vector3 movement = Vector3.zero;
    
        // Only apply horizontal movement if we have both direction and speed
        if (activeMoveDirection.sqrMagnitude > 0.001f && activeMoveSpeed > 0.01f)
        {
            movement = activeMoveDirection * activeMoveSpeed;
        }
    
        // Always apply vertical movement
        movement.y = activeVerticalVelocity;
    
        // Apply movement
        controller.Move(movement * Time.fixedDeltaTime);
    }

    public void RotateCharacter(Quaternion targetRotation, float baseSpeed, float multiplier = 1f)
    {
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            baseSpeed * multiplier * Time.deltaTime * 100f
        );
    }
    

    public void SetMoveSpeed(float speed)
    {
        activeMoveSpeed = speed;
    }
    
    public void SetMoveDirection(Vector3 direction)
    {
        activeMoveDirection = direction;
    }
    
    public void SetVerticalVelocity(float velocity)
    {
        activeVerticalVelocity = velocity;
    }
    
    public void SetAirTime(float time)
    {
        AirTime = time;
    }
    
    public void SetFallTime(float time)
    {
        FallTime = time;
    }
    
    public void SetLandingIntensity(float intensity)
    {
        LandingIntensity = intensity;
    }
    
    public float CalculateGravityVelocity(float currentVelocity, float deltaTime)
    {
        // Calculate new vertical velocity with gravity applied
        float newVerticalVelocity = currentVelocity + (gravity * deltaTime);
    
        // Limit to terminal velocity
        newVerticalVelocity = Mathf.Max(newVerticalVelocity, maxVerticalVelocity);
    
        return newVerticalVelocity;
    }

    public float CalculateTargetSpeed(float movementIntensity)
    {
        
        LockSprinting = input.MoveSpeedInput || _isAiming;
    
        if (movementIntensity < PlayerInput.MovementInputThreshold)
            return 0f;

        if (!LockSprinting)
        {
            if (input.WalkInput)
                return walkSpeed;
            else if (input.SprintInput && movementIntensity > PlayerInput.SprintInputThreshold)
                return sprintSpeed;
            else
                return runSpeed;
        }
        else
        {
            if (input.WalkInput)
                return walkSpeed;
            else if (input.SprintInput && movementIntensity > PlayerInput.SprintInputThreshold)
                return runSpeed;
            else
                return walkSpeed;
        }
    }
    
    public Vector3 CalculateMoveDirection()
    {
        // Get camera forward and right
        var forward = freeLookCamera.transform.forward;
        var right = freeLookCamera.transform.right;
    
        // Project onto horizontal plane
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        // Calculate movement direction relative to camera
        return (forward * input.MovementInput.y + 
                right * input.MovementInput.x).normalized;
    }
    
    
    #endregion Movement ---------------------------------------------------------------

    
    #region Animations ---------------------------------------------------------------

    private void SyncAnimations()
    {
    
        // Set current state
        PlayerAnimationState currentAnimState = CurrentState switch
        {
            PlayerGroundedState => PlayerAnimationState.Grounded,
            PlayerJumpingState => PlayerAnimationState.Jump,
            PlayerFallingState => PlayerAnimationState.Fall,
            PlayerLandingState => PlayerAnimationState.Landing,
            PlayerInteractingState => PlayerAnimationState.Interact,
            _ => PlayerAnimationState.Grounded
        };
        animator.SetInteger(_stateHash, (int)currentAnimState);

        // Handle fall/landing blend
        float fallBlend = CurrentState is PlayerLandingState ? LandingIntensity : Mathf.Clamp01(FallTime / maxFallTime);
        animator.SetFloat(_fallTimeHash, fallBlend);

        // Set aiming parameter
        animator.SetBool(_isAimingHash, _isAiming);
        
        UpdateMovementAnimation();
    }

    private void UpdateMovementAnimation()
    {
        float verticalValue = CalculateSpeedBlend();
        animator.SetFloat(_verticalHash, verticalValue, 0.1f, Time.deltaTime);
        animator.SetFloat(_horizontalHash, 0, 0.1f, Time.deltaTime);
    }
    
    private float CalculateSpeedBlend()
    {
        if (activeMoveSpeed <= walkSpeed)
        {
            return (activeMoveSpeed / walkSpeed) * 0.5f;
        }
        
        if (activeMoveSpeed <= runSpeed)
        {
            return 0.5f + ((activeMoveSpeed - walkSpeed) / (runSpeed - walkSpeed)) * 0.5f;
        }
        
        if (activeMoveSpeed <= sprintSpeed) 
        {
            return 1f + (activeMoveSpeed - runSpeed) / (sprintSpeed - runSpeed);
        }

        return 0f;
    }

    #endregion Animations ---------------------------------------------------------------
    
    
    #region Utility ---------------------------------------------------------------

    private void UpdateDebugText()
    {
        if (!debugText) return;

        debugText.text = $"State: {CurrentState.GetType().Name}\n" +
                         $"IsGrounded: {IsGrounded}\n" +
                         $"IsAiming: {_isAiming}\n" +
                         $"AirTime: {AirTime}\n" +
                         $"FallTime: {FallTime}\n" +
                         $"LandingIntensity: {LandingIntensity}\n" +
                         $"MoveDirection: {activeMoveDirection}\n" +
                         $"ActiveMoveSpeed: {activeMoveSpeed}\n" +
                         $"ActiveVerticalVelocity: {activeVerticalVelocity}\n";
    }
    
    private void OnDrawGizmos()
    {
        if (IsGrounded)
        {
            Gizmos.color = Color.green;
        } 
        else if (FallTime < fallThreshold)
        {
            Gizmos.color = Color.yellow;
        }
        else
        {
            Gizmos.color = Color.red;
        }

        
        Vector3 spherePosition = transform.position + groundCheckOffset;
        Gizmos.DrawWireSphere(spherePosition, groundCheckRadius);
    }

    #endregion Utility ---------------------------------------------------------------
    
    
}