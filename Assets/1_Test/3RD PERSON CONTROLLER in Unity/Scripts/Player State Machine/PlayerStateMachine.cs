
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
}


[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerStateMachine : MonoBehaviour
{
    public PlayerBaseState CurrentState { get; private set; }
    public PlayerGroundedState GroundedState { get; private set; }
    public PlayerJumpingState JumpingState { get; private set; }
    public PlayerFallingState FallingState { get; private set; }
    public PlayerLandingState LandingState { get; private set; }

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
    public float rotationSpeed = 3f;
    [Tooltip("Initial upward velocity applied when jumping")]
    public float jumpForce = 8f;

    [Header("Air Movement")]
    [Tooltip("Maximum horizontal speed while in the air")]
    public float airMoveSpeed = 4f;
    [Tooltip("Base rotation speed when turning in the air")]
    public float airRotationSpeed = 2f;
    [Tooltip("How quickly the character reaches target speed in air")]
    public float airAcceleration = 3f;
    [Tooltip("How quickly the character loses momentum in air")]
    public float airFriction = 2.0f;

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

    
    [Header("Camera")]
    [SerializeField] private CinemachineCamera freeLookCamera;
    
    [Header("Debug")]
    [SerializeField] private TextMeshProUGUI debugText;
    

    private CharacterController _controller;
    private Animator _animator;
    public float AirTime { get; private set; }

    public float FallTime { get; private set; }

    public float LandingIntensity { get; private set; }

    public bool IsGrounded { get; private set; }

    public float activeMoveSpeed { get; private set; }
    
    
    // Input properties
    public Vector2 MovementInput { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool SprintPressed { get; private set; }
    public bool WalkPressed { get; private set; }
    public Vector2 MouseDelta { get; private set; }
    
    // Animation hashes
    private readonly int _stateHash = Animator.StringToHash("StateIndex");
    private readonly int _verticalHash = Animator.StringToHash("Vertical");
    private readonly int _horizontalHash = Animator.StringToHash("Horizontal");
    private readonly int _fallTimeHash = Animator.StringToHash("FallTime");


    // Constants
    public const float RotationInputThreshold = 0.01f;
    public const float SprintInputThreshold = 0.5f;
    public const float MovementInputThreshold = 0.1f;


    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();

        // Initialize states
        GroundedState = new PlayerGroundedState(this);
        JumpingState = new PlayerJumpingState(this);
        FallingState = new PlayerFallingState(this);
        LandingState = new PlayerLandingState(this);
        
        if (freeLookCamera == null) Debug.LogError("Cinemachine cameras not assigned!");

        // Set camera priorities
        freeLookCamera.Priority = 15;

        // Set initial state
        SwitchState(GroundedState);
    }

    private void Update()
    {
        GetInput();
        CheckGrounded();
        UpdateFallTime();
        CurrentState.UpdateState();
        SyncAnimations();
        UpdateDebugText();
    }

    private void FixedUpdate()
    {
        CurrentState.FixedUpdateState();
    }
    




    #region State Control ---------------------------------------------------------------

    public void SwitchState(PlayerBaseState newState)
    {
        CurrentState?.ExitState();
        CurrentState = newState;
        CurrentState.EnterState();

        // Debug state changes
        Debug.Log($"Switched to {newState.GetType().Name}");
    }

    #endregion State Control ---------------------------------------------------------------
    
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
        if (!IsGrounded)
        {
            SetFallTime(FallTime + Time.deltaTime);
        }
    }

    public void MoveCharacter(Vector3 movement)
    {
        _controller.Move(movement * Time.fixedDeltaTime);
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

    public float CalculateTargetSpeed(float movementIntensity)
    {
        if (movementIntensity < MovementInputThreshold)
            return 0f;
        if (WalkPressed)
            return walkSpeed;
        else if (SprintPressed && movementIntensity > SprintInputThreshold)
            return sprintSpeed;
        else
            return runSpeed;
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
        return (forward * MovementInput.y + 
                right * MovementInput.x).normalized;
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
            _ => PlayerAnimationState.Grounded
        };
        _animator.SetInteger(_stateHash, (int)currentAnimState);

        // Handle fall/landing blend
        float fallBlend = CurrentState is PlayerLandingState ? LandingIntensity : Mathf.Clamp01(FallTime / maxFallTime);
        _animator.SetFloat(_fallTimeHash, fallBlend);

        UpdateMovementAnimation();
    }

    private void UpdateMovementAnimation()
    {
        float verticalValue = CalculateSpeedBlend();
        _animator.SetFloat(_verticalHash, verticalValue, 0.1f, Time.deltaTime);
        _animator.SetFloat(_horizontalHash, 0, 0.1f, Time.deltaTime);
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

    
    #region Input ---------------------------------------------------------------

    private void GetInput()
    {
        MovementInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );
        
        JumpPressed = Input.GetButtonDown("Jump");
        SprintPressed = Input.GetButton("Sprint");
        WalkPressed = Input.GetButton("Walk");
        
        MouseDelta = new Vector2(
            Input.GetAxis("Mouse X"),
            Input.GetAxis("Mouse Y")
        );
        
    }

    #endregion Input ---------------------------------------------------------------
    
    
    #region Utility ---------------------------------------------------------------

    private void UpdateDebugText()
    {
        if (!debugText) return;

        debugText.text = $"State: {CurrentState.GetType().Name}\n" +
                         $"IsGrounded: {IsGrounded}\n" +
                         $"AirTime: {AirTime}\n" +
                         $"FallTime: {FallTime}\n" +
                         $"LandingIntensity: {LandingIntensity}\n" +
                         $"ActiveMoveSpeed: {activeMoveSpeed}\n"
                         ;

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