using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(CharacterController))]
public class PlayerStateMachine : MonoBehaviour
{
    public PlayerBaseState CurrentState { get; private set; }
    public PlayerGroundedState GroundedState { get; private set; }
    public PlayerCrouchingState CrouchingState { get; private set; }
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

    [Header("Collision Check")]
    [Tooltip("Radius of the sphere used to detect ground")]
    [SerializeField] private float groundCheckRadius = 0.23f;
    [Tooltip("Offset from character position for ground detection")]
    [SerializeField] private Vector3 groundCheckOffset = new Vector3(0, -0.1f, 0);
    [Tooltip("Layer mask defining what objects count as environment")]
    [SerializeField] private LayerMask environmentLayer = 1;
    
    [Header("Aim Ray")]
    [Tooltip("Maximum distance the aim ray will travel")]
    public float aimRayMaxDistance = 20f;
    [Tooltip("Layer mask for objects that can be hit by the aim ray")]
    public LayerMask aimRayHitMask;

    [Header("References")] 
    [SerializeField] private TextMeshProUGUI debugText;
    
    
    public float AirTime { get;  set; }
    public float FallTime { get;  set; }
    public float LandingIntensity { get; set; }
    public float activeHorizontalVelocity { get; set; }
    public float activeVerticalVelocity { get; set; }
    public Vector3 activeMoveDirection { get; set; } = Vector3.zero;
    public bool IsGrounded { get; private set; }
    public bool CanStand { get; private set; }
    public bool CanInteract { get; private set; }
    public bool IsAiming { get; private set; }
    public PlayerInputHandler InputHandler { get; private set; }
    public IInteractable currentInteractable { get; private set; }
    public IInteractable currentAimedInteractable { get; private set; }
    private CharacterController _controller;
    private RobotCompanion _robot;
    private CameraManager _cameraManager;
    private LineRenderer _lineRenderer;
    private bool _lockSprinting;
    private Vector3 _lastRotationDirection = Vector3.forward;
    private float _defaultCharacterHeight;
    private Vector3 _defaultCharacterCenter;
    private float _crouchCharacterHeight = 1.2333f;
    private Vector3 _crouchCharacterCenter = new Vector3(0, -0.3f, 0.2f);
    
    

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        SetupLineRenderer();
        _controller = GetComponent<CharacterController>();
        InputHandler = GetComponent<PlayerInputHandler>();
        GroundedState = new PlayerGroundedState(this);
        JumpingState = new PlayerJumpingState(this);
        FallingState = new PlayerFallingState(this);
        LandingState = new PlayerLandingState(this);
        InteractingState = new PlayerInteractingState(this);
        CrouchingState = new PlayerCrouchingState(this);
        _defaultCharacterHeight = _controller.height;
        _defaultCharacterCenter = _controller.center;
        
        SwitchState(GroundedState);
    }

    private void Start()
    {
        if (!_robot) _robot = FindFirstObjectByType<RobotCompanion>();
        if (!_cameraManager) _cameraManager = FindFirstObjectByType<CameraManager>();
        _cameraManager.Initialize(InputHandler);
    }

    private void Update()
    {
        if (_cameraManager)
        {
            _cameraManager.UpdateCameraPosition(transform.position);
            _cameraManager.HandleCameraRotation();
        }
        
        CheckCollisions();
        UpdateFallTime();
        UpdateAimRay();
        HandleCameraInputs();
        CurrentState.UpdateState();
        UpdateDebugText();
    }

    private void FixedUpdate()
    {
        CurrentState.FixedUpdateState();
        MoveCharacter(); 
    }
    
    
    #region Collisions ---------------------------------------------------------------

    private void CheckCollisions()
    {
        Vector3 groundSpherePosition = transform.position + groundCheckOffset;
        IsGrounded = Physics.CheckSphere(groundSpherePosition, groundCheckRadius, environmentLayer);
        
        Vector3 ceilingSpherePosition = transform.position - groundCheckOffset;
        CanStand = !Physics.CheckSphere(ceilingSpherePosition, groundCheckRadius, environmentLayer);
    }
    
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out IInteractable interactable))
        {
            currentInteractable = interactable;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent(out IInteractable interactable) && interactable == currentInteractable)
        {
            // Allow player interaction
            CanInteract = currentInteractable.PlayerCanInteract && (CurrentState == GroundedState || CurrentState == CrouchingState) && (currentInteractable != currentAimedInteractable);
        
            // Allow robot interaction
            if (_robot && currentInteractable.RobotCanInteract && InputHandler.RobotInteractInput)
            {
                InputHandler.ConsumeRobotInteractBuffer();
                _robot.InteractWith(currentInteractable);
            }

            if (CanInteract && !currentInteractable.IsHighlighted())
            {
                currentInteractable.SetHighlight(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out IInteractable interactable) && interactable == currentInteractable)
        {
            currentInteractable.SetHighlight(false);
            currentInteractable = null;
            CanInteract = false;
        }
    }

    #endregion Collisions ---------------------------------------------------------------
    
    
    #region State Control ---------------------------------------------------------------

    public void SwitchState(PlayerBaseState newState)
    {
        CurrentState?.ExitState();
        CurrentState = newState;
        CurrentState.EnterState();
    }
    
    public void OnInteractionComplete(IInteractable interactable)
    {
        InteractingState.OnInteractionComplete(interactable);
    }


    #endregion State Control ---------------------------------------------------------------

    
    #region Aiming ---------------------------------------------------------------
    
    public void OnAimEnter(IInteractable interactable)
    {
        interactable.OnAimEnter(this);
        
        if (_robot && currentAimedInteractable != null && currentAimedInteractable.RobotCanInteract)
        {
            interactable.SetHighlight(true);
            Debug.Log("Highlighting");
            
        }
    }

    public void OnAimStay(IInteractable interactable)
    {
        interactable.OnAimStay(this);
        
        if (_robot && currentAimedInteractable != null && currentAimedInteractable.RobotCanInteract && InputHandler.RobotInteractInput)
        {
            InputHandler.ConsumeRobotInteractBuffer();
            _robot.InteractWith(currentAimedInteractable);
        }
    }

    public void OnAimExit(IInteractable interactable)
    {
        interactable.OnAimExit(this);
        if (_robot && currentAimedInteractable != null && currentAimedInteractable.RobotCanInteract)
        {
            interactable.SetHighlight(false);
        }
    }
    
    private void SetupLineRenderer()
    {
        // Initialize line renderer properties
        _lineRenderer.positionCount = 2;
        _lineRenderer.enabled = false;
    }
    private void UpdateAimRay()
{
    if (!IsAiming)
    {
        // Hide line renderer when not aiming
        if (_lineRenderer.enabled)
        {
            _lineRenderer.enabled = false;
            if (currentAimedInteractable != null)
            {
                OnAimExit(currentAimedInteractable);
                currentAimedInteractable = null;
            }
        }
        return;
    }

    // Show line renderer when aiming
    if (!_lineRenderer.enabled)
    {
        _lineRenderer.enabled = true;
    }

    // Set ray origin (player position, slightly adjusted to match camera view)
    Vector3 rayOrigin = transform.position + new Vector3(0, 0.5f, 0);
    
    // Get ray direction from camera
    Vector3 rayDirection = _cameraManager.GetCameraAimDirection();
    
    // Set first point of line renderer
    _lineRenderer.SetPosition(0, rayOrigin);
    
    // Create the actual ray for Physics raycasting
    Ray aimRay = new Ray(rayOrigin, rayDirection);
    
    // Perform raycast to see if we hit anything
    if (Physics.Raycast(aimRay, out RaycastHit hitInfo, aimRayMaxDistance, aimRayHitMask))
    {
        // Set second point of line renderer to hit position
        _lineRenderer.SetPosition(1, hitInfo.point);
        
        // Check if the hit object implements IInteractable
        if (hitInfo.collider.TryGetComponent(out IInteractable hitInteractable))
        {
            if (currentAimedInteractable != hitInteractable)
            {
                // Exit previous target if there was one
                if (currentAimedInteractable != null)
                {
                    OnAimExit(currentAimedInteractable);
                }
                
                // Set new target and enter it
                currentAimedInteractable = hitInteractable;
                OnAimEnter(currentAimedInteractable);
            }
            
            // Update aim on current target
            OnAimStay(currentAimedInteractable);
        }
        else if (currentAimedInteractable != null)
        {
            // We're no longer aiming at an interactable
            OnAimExit(currentAimedInteractable);
            currentAimedInteractable = null;
        }
    }
    else
    {
        // No hit, set line end point to max distance
        _lineRenderer.SetPosition(1, rayOrigin + (rayDirection * aimRayMaxDistance));
        
        // Clear current target if we had one
        if (currentAimedInteractable != null)
        {
            OnAimExit(currentAimedInteractable);
            currentAimedInteractable = null;
        }
    }
}
    
    private void HandleCameraInputs()
    {
        // Toggle aim mode based on input
        if (InputHandler.AimInput)
        {
            if (!IsAiming)
            {
                IsAiming = true;
                
                // When first entering aim mode, immediately align character with camera
                Vector3 initialAimDirection = GetCameraAimDirection();
                Quaternion targetRotation = Quaternion.LookRotation(initialAimDirection);
                transform.rotation = targetRotation;
                _lastRotationDirection = initialAimDirection;
            
                // Switch to aim camera
                _cameraManager.SwitchToAimCamera();
            }
        }
        else if (IsAiming)
        {
            IsAiming = false;
            
            // Switch to free look camera
            _cameraManager.SwitchToFreeLookCamera();
        }
    }
    
    public Vector3 GetCameraAimDirection()
    {
        if (!_cameraManager) return transform.forward;
        return _cameraManager.GetCameraAimDirectionNoY();
    }
    
    public void HandleAimRotation()
    {
        if (!IsAiming)
            return;
        
        // Get the current aim direction
        Vector3 aimDirection = GetCameraAimDirection();
        
        // Calculate the angle difference between current aim direction and last rotation direction
        float angleChange = Vector3.Angle(_lastRotationDirection, aimDirection);
        
        // Check if mouse movement exceeds our threshold
        if (InputHandler.MouseDelta.magnitude > _cameraManager.AimRotationThreshold || angleChange > 1.0f)
        {
            // There's significant mouse movement, so update the character rotation
            Quaternion targetRotation = Quaternion.LookRotation(aimDirection);
            
            // Rotate player to face aim direction
            RotateCharacter(
                targetRotation,
                aimRotationSpeed,
                1.0f
            );
            
            // Store this as the last direction we rotated to
            _lastRotationDirection = aimDirection;
        }
    }
    
    #endregion Aiming ---------------------------------------------------------------
    
    
    #region Movement ---------------------------------------------------------------
    
    private void MoveCharacter()
    {
        // Create movement vector using the active properties
        Vector3 movement = Vector3.zero;
    
        // Only apply horizontal movement if we have both direction and speed
        if (activeMoveDirection.sqrMagnitude > 0.001f && activeHorizontalVelocity > 0.01f)
        {
            movement = activeMoveDirection * activeHorizontalVelocity;
        }
    
        // Always apply vertical movement
        movement.y = activeVerticalVelocity;
    
        // Apply movement
        _controller.Move(movement * Time.fixedDeltaTime);
    }

    public void RotateCharacter(Quaternion targetRotation, float baseSpeed, float multiplier = 1f)
    {
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            baseSpeed * multiplier * Time.fixedDeltaTime * 100f
        );
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
        _lockSprinting = InputHandler.MoveSpeedInput || IsAiming || CurrentState == CrouchingState;
    
        if (movementIntensity < PlayerInputHandler.MovementInputThreshold)
            return 0f;

        if (!_lockSprinting)
        {
            if (InputHandler.SprintInput && movementIntensity > PlayerInputHandler.SprintInputThreshold)
                return sprintSpeed;
            else
                return runSpeed;
        }
        else
        {

            if (InputHandler.SprintInput && movementIntensity > PlayerInputHandler.SprintInputThreshold)
                return runSpeed;
            else
                return walkSpeed;
        }
    }
    
    public Vector3 CalculateMoveDirection()
    {
        if (!_cameraManager) return transform.forward;
        return _cameraManager.CalculateMoveDirection(InputHandler.MovementInput);
    }
    
    #endregion Movement ---------------------------------------------------------------
    
    
    #region Utility ---------------------------------------------------------------

    private void UpdateFallTime()
    {
        // Only increment fall time when moving downward
        if (!IsGrounded && CurrentState != JumpingState)
        {
            FallTime += Time.deltaTime;
        }
    }
    public void SetCharacterHeight(bool crouching)
    {
        if (crouching)
        {
            _controller.height = _crouchCharacterHeight;
            _controller.center = _crouchCharacterCenter;
        }
        else
        {
            _controller.height = _defaultCharacterHeight;
            _controller.center = _defaultCharacterCenter;
        }
    }
    
    private void UpdateDebugText()
    {
        if (!debugText) return;

        debugText.text = $"State: {CurrentState.GetType().Name}\n" +
                         $"IsGrounded: {IsGrounded}\n" +
                         $"CanStand: {CanStand}\n" +
                         $"IsAiming: {IsAiming}\n" +
                         $"AirTime: {AirTime}\n" +
                         $"FallTime: {FallTime}\n" +
                         $"LandingIntensity: {LandingIntensity}\n" +
                         $"MoveDirection: {activeMoveDirection}\n" +
                         $"Interactable: {currentInteractable}\n" +
                         $"AimedInteractable: {currentAimedInteractable}\n" +
                         $"ActiveHorizontalSpeed: {activeHorizontalVelocity}\n" +
                         $"ActiveVerticalVelocity: {activeVerticalVelocity}\n";
    }
    
    private void OnDrawGizmos()
    {
        // Ground sphere
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
        Vector3 groundSpherePosition = transform.position + groundCheckOffset;
        Gizmos.DrawWireSphere(groundSpherePosition, groundCheckRadius);
        
        // Ceiling sphere
        Gizmos.color = CanStand ? Color.green : Color.red;
        Vector3 ceilingSpherePosition = transform.position - groundCheckOffset;
        Gizmos.DrawWireSphere(ceilingSpherePosition, groundCheckRadius);
    }

    #endregion Utility ---------------------------------------------------------------
}