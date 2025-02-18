
using UnityEngine;
using Unity.Cinemachine;


public enum PlayerAnimationState
{
    Grounded = 0,
    Jump = 1,
    Fall = 2,
    Landing = 3,
    Crouch = 4
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
    public PlayerCrouchingState CrouchingState { get; private set; }

    [Header("Movement")]
    public float walkSpeed = 4f;
    public float runSpeed = 8f;
    public float sprintSpeedMultiplier = 1.5f;
    public float crouchSpeed = 2f;
    public float acceleration = 10f;
    public float rotationSpeed = 6f;
    
    [Header("Jump")]
    public float jumpForce = 8f;
    public float gravity = -20f;
    public float groundedGravity = -2f;

    [Header("Fall & Landing")]
    [SerializeField, Tooltip("Minimum fall time before impact is registered")]
    public float fallThreshold = 0.5f;
    [SerializeField, Tooltip("Fall time that results in maximum impact")]
    public float maxFallTime = 2.0f;
    [SerializeField, Tooltip("Maximum time to recover from landing")]
    public float recoveryDuration = 0.5f;
    [SerializeField, Tooltip("Minimum movement control during landing recovery")]
    public float minMovementControl = 0.1f;
    
    [Header("Ground Check")]
    [SerializeField] private float groundCheckRadius = 0.23f;
    [SerializeField] private Vector3 groundCheckOffset = new Vector3(0, -0.1f, 0);
    [SerializeField] private LayerMask groundLayer = 1;
    
    [Header("Camera")]
    public CinemachineCamera freeLookCamera;
    public CinemachineCamera aimCamera;
    public Transform aimCore;

    // Common components
    public CharacterController Controller { get; private set; }
    public Animator Animator { get; private set; }

    // Input properties
    public Vector2 MovementInput { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool SprintPressed { get; private set; }
    public bool WalkPressed { get; private set; }
    public bool CrouchPressed { get; private set; }
    public bool IsAiming { get; private set; }
    public Vector2 MouseDelta { get; private set; }

    // State tracking
    private bool _isGrounded;
    public bool IsGrounded => _isGrounded;
    public float AirTime { get; set; }
    public float FallTime { get; set; }
    public float LandingIntensity { get; set; }
    public float activeMoveSpeed { get; private set; }
    
    // Animation parameter hashes for efficiency
    private readonly int _stateHash = Animator.StringToHash("StateIndex");
    private readonly int _verticalHash = Animator.StringToHash("Vertical");
    private readonly int _horizontalHash = Animator.StringToHash("Horizontal");
    private readonly int _fallTimeHash = Animator.StringToHash("FallTime");
    private readonly int _cameraRelativeHash = Animator.StringToHash("CameraRelative");


    

    private void Awake()
    {
        Controller = GetComponent<CharacterController>();
        Animator = GetComponent<Animator>();

        // Initialize states
        GroundedState = new PlayerGroundedState(this);
        JumpingState = new PlayerJumpingState(this);
        FallingState = new PlayerFallingState(this);
        LandingState = new PlayerLandingState(this);
        CrouchingState = new PlayerCrouchingState(this);

        if (aimCore == null) Debug.LogError("AimCore not found as child of player!");
        if (freeLookCamera == null || aimCamera == null) Debug.LogError("Cinemachine cameras not assigned!");

        // Set camera priorities
        freeLookCamera.Priority = 15; // Default movement camera
        aimCamera.Priority = 0; // Aim camera inactive by default

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
        _isGrounded = Physics.CheckSphere(spherePosition, groundCheckRadius, groundLayer);
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
        CrouchPressed = Input.GetButton("Crouch");
        
        MouseDelta = new Vector2(
            Input.GetAxis("Mouse X"),
            Input.GetAxis("Mouse Y")
        );
        
        IsAiming = Input.GetMouseButton(1);
    }
    
    private void SyncAnimations()
    {
        // Update camera relative state
        Animator.SetBool(_cameraRelativeHash, IsAiming);
    
        // Set current state
        PlayerAnimationState currentAnimState = CurrentState switch
        {
            PlayerGroundedState => PlayerAnimationState.Grounded,
            PlayerJumpingState => PlayerAnimationState.Jump,
            PlayerFallingState => PlayerAnimationState.Fall,
            PlayerLandingState => PlayerAnimationState.Landing,
            PlayerCrouchingState => PlayerAnimationState.Crouch,
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
        if (IsAiming)
        {
            // Camera relative movement
            float verticalValue = CalculateVerticalBlend(MovementInput.y);
            float horizontalValue = CalculateHorizontalBlend(MovementInput.x, MouseDelta.x);
        
            Animator.SetFloat(_verticalHash, verticalValue, 0.1f, Time.deltaTime);
            Animator.SetFloat(_horizontalHash, horizontalValue, 0.1f, Time.deltaTime);
        }
        else
        {
            // Character relative movement
            float verticalValue = CalculateSpeedBlend();
            Animator.SetFloat(_verticalHash, verticalValue, 0.1f, Time.deltaTime);
            Animator.SetFloat(_horizontalHash, 0, 0.1f, Time.deltaTime);
        }
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

    private float CalculateVerticalBlend(float inputValue)
    {
        if (inputValue == 0) return 0;

        if (activeMoveSpeed <= walkSpeed)
        {
            return 0.5f * (inputValue * activeMoveSpeed / walkSpeed);
        }
        else if (activeMoveSpeed <= runSpeed)
        {
            return inputValue * (0.5f + 0.5f * (activeMoveSpeed - walkSpeed) / (runSpeed - walkSpeed));
        }
        else
        {
            float sprintProgress = (activeMoveSpeed - runSpeed) / (runSpeed * (sprintSpeedMultiplier - 1));
            return inputValue * (1f + sprintProgress);
        }
    }

    private float CalculateHorizontalBlend(float horizontalInput, float mouseX)
    {
        if (horizontalInput == 0 && mouseX == 0) return 0;

        float strafeInfluence = horizontalInput;
        if (mouseX != 0 && MovementInput.sqrMagnitude > 0.01f)
        {
            strafeInfluence += Mathf.Sign(mouseX) * 0.5f;
        }
    
        strafeInfluence = Mathf.Clamp(strafeInfluence, -1f, 1f);
    
        return activeMoveSpeed <= walkSpeed ? 
            0.5f * strafeInfluence : 
            strafeInfluence;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Vector3 spherePosition = transform.position + groundCheckOffset;
        Gizmos.DrawWireSphere(spherePosition, groundCheckRadius);
    }
}