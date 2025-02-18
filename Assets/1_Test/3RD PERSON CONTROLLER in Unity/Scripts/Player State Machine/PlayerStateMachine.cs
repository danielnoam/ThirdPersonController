
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
    public float sprintSpeedMultiplier = 1.5f;
    public float acceleration = 10f;
    public float rotationSpeed = 6f;
    public float jumpForce = 8f;
    
    [Header("Air Movement")]
    public float airDrag = 2.0f;  // How quickly horizontal speed decreases when no input
    public float airControl = 0.5f;
    public float airRotation = 0.5f;
    
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
    public CinemachineCamera freeLookCamera;

    
    [Header("Debug Info")]
    [SerializeField] private float _airTime;
    [SerializeField] private float _fallTime;
    [SerializeField] private float _landingIntensity;
    [SerializeField] private bool _isGrounded;
    [SerializeField] private float _activeMoveSpeed;
    
    
    // Common components
    public CharacterController Controller { get; private set; }
    public Animator Animator { get; private set; }

    // Input properties
    public Vector2 MovementInput { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool SprintPressed { get; private set; }
    public bool WalkPressed { get; private set; }
    public Vector2 MouseDelta { get; private set; }

    // State tracking
    public float AirTime 
    { 
        get => _airTime;
        set => _airTime = value;
    }

    public float FallTime
    {
        get => _fallTime;
        set => _fallTime = value;
    }

    public float LandingIntensity
    {
        get => _landingIntensity;
        set => _landingIntensity = value;
    }

    public bool IsGrounded
    {
        get => _isGrounded;
        private set => _isGrounded = value;
    }

    public float activeMoveSpeed
    {
        get => _activeMoveSpeed;
        private set => _activeMoveSpeed = value;
    }
    
    // Animation hashes
    private readonly int _stateHash = Animator.StringToHash("StateIndex");
    private readonly int _verticalHash = Animator.StringToHash("Vertical");
    private readonly int _horizontalHash = Animator.StringToHash("Horizontal");
    private readonly int _fallTimeHash = Animator.StringToHash("FallTime");


    

    private void Awake()
    {
        Controller = GetComponent<CharacterController>();
        Animator = GetComponent<Animator>();

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

    private void CheckGrounded()
    {
        Vector3 spherePosition = transform.position + groundCheckOffset;
        IsGrounded = Physics.CheckSphere(spherePosition, groundCheckRadius, groundLayer);
    }

    public void SwitchState(PlayerBaseState newState)
    {
        CurrentState?.ExitState();
        CurrentState = newState;
        CurrentState.EnterState();

        // Debug state changes
        Debug.Log($"Switched to {newState.GetType().Name}");
    }

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
        Animator.SetInteger(_stateHash, (int)currentAnimState);

        // Handle fall/landing blend
        float fallBlend = CurrentState is PlayerLandingState ? 
            LandingIntensity : 
            Mathf.Clamp01(FallTime / maxFallTime);
        Animator.SetFloat(_fallTimeHash, fallBlend);

        UpdateMovementAnimation();
    }

    private void UpdateMovementAnimation()
    {
        float verticalValue = CalculateSpeedBlend();
        Animator.SetFloat(_verticalHash, verticalValue, 0.1f, Time.deltaTime);
        Animator.SetFloat(_horizontalHash, 0, 0.1f, Time.deltaTime);
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
            float sprintProgress = (activeMoveSpeed - runSpeed) / (runSpeed * (sprintSpeedMultiplier - 1));
            return 1f + sprintProgress;
        }
    }
    
    
    public void SetMoveSpeed(float speed)
    {
        activeMoveSpeed = speed;
    }

    public float CalculateTargetSpeed(float movementIntensity, float speedMultiplier = 1f)
    {
        if (movementIntensity < 0.1f)
            return 0f;
        
        if (WalkPressed)
            return walkSpeed * speedMultiplier;
        else if (SprintPressed && movementIntensity > 0.5f)
            return runSpeed * sprintSpeedMultiplier * speedMultiplier;
        else
            return runSpeed * speedMultiplier;
    }

    public void UpdateMoveSpeed(float targetSpeed)
    {
        activeMoveSpeed = Mathf.MoveTowards(
            activeMoveSpeed, 
            targetSpeed, 
            acceleration * Time.deltaTime
        );
    }
    
    public void MoveCharacter(Vector3 moveDirection, float speedMultiplier = 1f, Vector3? verticalOverride = null)
    {
        // Calculate horizontal movement
        Vector3 movement = moveDirection * (activeMoveSpeed * speedMultiplier);
    
        // Apply vertical movement (gravity or jump)
        if (verticalOverride.HasValue)
        {
            movement.y = verticalOverride.Value.y;
        }
        else
        {
            movement.y = groundedGravity;
        }

        // Apply movement
        Controller.Move(movement * Time.fixedDeltaTime);
    }

    public Vector3 CalculateMoveDirection()
    {
        var forward = freeLookCamera.transform.forward;
        var right = freeLookCamera.transform.right;
    
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        // Calculate movement direction relative to camera
        return (forward * MovementInput.y + right * MovementInput.x).normalized;
    }

    public void RotateTowardsMoveDirection(Vector3 moveDirection, float rotationMultiplier = 1f)
    {
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * rotationMultiplier * Time.deltaTime * 100f
            );
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Vector3 spherePosition = transform.position + groundCheckOffset;
        Gizmos.DrawWireSphere(spherePosition, groundCheckRadius);
    }
}