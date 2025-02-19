
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
    public float walkSpeed = 4f;
    public float runSpeed = 8f;
    public float sprintSpeed = 12f;
    public float acceleration = 10f;
    public float rotationSpeed = 6f;
    public float jumpForce = 8f;
    
    [Header("Air Movement")]
    public float airMoveSpeed = 3f;
    public float airRotationSpeed = 3f;
    public float airAcceleration = 5f;
    public float airFriction = 2.0f;
    
    [Header("Gravity")]
    public float gravity = -20f;
    public float groundedGravity = -2f;
    
    [Header("Land")]
    [Tooltip("Minimum fall time before impact is registered")]
    public float fallThreshold = 0.5f;
    [Tooltip("Fall time that results in maximum impact")]
    public float maxFallTime = 2.0f;
    [Tooltip("Maximum time to recover from landing")]
    public float recoveryDuration = 0.5f;
    [Tooltip("Minimum movement control during landing recovery")]
    public float minMovementControl = 0.1f;
    
    [Header("Ground Check")]
    [SerializeField] private float groundCheckRadius = 0.23f;
    [SerializeField] private Vector3 groundCheckOffset = new Vector3(0, -0.1f, 0);
    [SerializeField] private LayerMask groundLayer = 1;
    
    [Header("Camera")]
    [SerializeField] private CinemachineCamera freeLookCamera;
    
    
    
    // Common components
    private CharacterController _controller;
    private Animator _animator;
    

    // State tracking
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
    public const float MinMovementThreshold = 0.01f;
    public const float SprintIntensityThreshold = 0.5f;
    public const float MinMovementIntensity = 0.1f;


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
        freeLookCamera.Priority = 15; // Default movement camera

        // Set initial state
        SwitchState(GroundedState);
    }

    private void Update()
    {
        CheckGrounded();
        GetInput();
        CurrentState.UpdateState();
        SyncAnimations();
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

    public void MoveCharacter(Vector3 movement)
    {
        _controller.Move(movement * Time.fixedDeltaTime);
    }

    public void RotateCharacter(Quaternion targetRotation, float rotationSpeedMultiplier = 1f)
    {
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * rotationSpeedMultiplier * Time.deltaTime * 100f
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
    

    public float CalculateTargetSpeed(float movementIntensity)
    {
        if (movementIntensity < MinMovementIntensity)
            return 0f;
        if (WalkPressed)
            return walkSpeed;
        else if (SprintPressed && movementIntensity > SprintIntensityThreshold)
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
        float fallBlend = CurrentState is PlayerLandingState ? 
            LandingIntensity : 
            Mathf.Clamp01(FallTime / maxFallTime);
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
        else if (activeMoveSpeed <= runSpeed)
        {
            return 0.5f + ((activeMoveSpeed - walkSpeed) / (runSpeed - walkSpeed)) * 0.5f;
        }
        else
        {
            float sprintProgress = (activeMoveSpeed - runSpeed) / (runSpeed * (sprintSpeed - 1));
            return 1f + sprintProgress;
        }
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

    private void OnDrawGizmos()
    {
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Vector3 spherePosition = transform.position + groundCheckOffset;
        Gizmos.DrawWireSphere(spherePosition, groundCheckRadius);
    }

    #endregion Utility ---------------------------------------------------------------
    
    
}