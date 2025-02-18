using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;
using VInspector;

[Serializable]
public class MovementType
{
    [Tooltip("Reference to the main camera for movement direction")] 
    public CinemachineCamera camera;
    
    [Tooltip("Base walking speed")] 
    [Min(0f)] public float walkSpeed = 4f;
    
    [Tooltip("Base running speed - faster than walk")]
    [Min(0f)] public float runSpeed = 8f;
    
    [Tooltip("Crouching speed - slower than walk")]
    [Min(0f)] public float crouchSpeed = 2f;
    
    [Tooltip("Sprint speed multiplier - applied to run speed")]
    [Min(1f)] public float sprintSpeedMultiplier = 1.5f;
    
    [Tooltip("Absolute maximum movement speed cap")]
    [Min(0f)] public float maxMoveSpeed = 20f;
    
    [Tooltip("How quickly speed changes occur")]
    [Min(0f)] public float acceleration = 20f;
    
    [Tooltip("How fast the player rotates")]
    [Min(0f)] public float rotationSpeed = 6f;
    
    [Tooltip("How the player's rotation is coupled to the camera")]
    public RotationMode rotationMode = RotationMode.Free;
    
    [Tooltip("If true, default speed is walk and sprint uses run speed")]
    public bool walkByDefault = false;
}

public enum PlayerState
{
    Grounded = 0,
    Jumping = 1,
    Falling = 2,
    Landing = 3
}

public enum RotationMode
{
    Free,           // Regular movement-based rotation
    Coupled,        // Always face camera direction
    CoupledMoving   // Face camera only when moving
}

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")] [SerializeField] private MovementType characterRelativeMovement;
    [SerializeField] private MovementType cameraRelativeMovement;

    [Header("Jumping")] [SerializeField, Tooltip("Initial upward force applied when jumping")] [Min(0f)]
    private float jumpForce = 8f;

    [SerializeField, Tooltip("Additional forward speed boost when jumping")] [Min(0f)]
    private float jumpSpeedBoost = 2f;

    [SerializeField, Tooltip("How long to buffer jump input before player lands")] [Min(0f)]
    private float jumpBufferTime = 0.2f;

    [Header("Fall & Landing")] [SerializeField, Tooltip("Minimum fall time before impact is registered")] [Min(0f)]
    private float fallThreshold = 0.5f;

    [SerializeField, Tooltip("Fall time that results in maximum impact")] [Min(0f)]
    public float maxFallTime = 2.0f;

    [SerializeField, Tooltip("Maximum time to recover from landing")] [Min(0f)]
    private float recoveryDuration = 0.5f;

    [SerializeField, Tooltip("Minimum movement control during landing recovery")] [Min(0f)]
    private float minMovementControl = 0.1f;

    [Header("Gravity")] [SerializeField, Tooltip("Gravity force applied when in air")] [Min(0f)]
    private float gravity = -20f;

    [SerializeField, Tooltip("Small downward force when grounded")] [Min(0f)]
    private float groundedGravity = -2f;

    [Header("Ground Check")] [SerializeField, Tooltip("Radius of the ground detection sphere")] [Min(0f)]
    private float groundCheckRadius = 0.23f;

    [SerializeField, Tooltip("Offset from player center for ground detection")]
    private Vector3 groundCheckOffset = new Vector3(0, -0.1f, 0);

    [SerializeField, Tooltip("Layer mask for ground detection")] [Min(0f)]
    private LayerMask groundLayer = 1;

    [Header("Aim Control")] [SerializeField]
    private Transform aimCore;

    [SerializeField] private Vector2 verticalLookLimits = new Vector2(-70f, 70f);

    [Header("Debug")] [ReadOnly] public PlayerState currentState = PlayerState.Grounded;
    [ReadOnly] public float activeMoveSpeed;
    [SerializeField, ReadOnly] private float targetMoveSpeed;

    private CharacterController _controller;
    private PlayerInput _playerInput;
    public MovementType currentMovement;
    private bool _isGrounded;
    private Vector3 _movementVector;

    public Vector3 MovementVector
    {
        get => _movementVector;
        private set => _movementVector = value;
    }

    private Vector3 _gravityForce;
    private float _movementIntensity;
    public float AirTime { private set; get; }
    public float FallTime { private set; get; }
    private float _landingRecoveryTime;
    public float LandingIntensity { private set; get; }
    private float _jumpBufferTimer;
    private bool _hasBufferedJump;
    private float _verticalLookAngle;
    private float _horizontalLookAngle;
    private float _stateTimer;
    private bool _inTopHemisphere = true;
    private float _timeInHemisphere = 100f;
    private bool _wasAiming;



    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _playerInput = GetComponent<PlayerInput>();
        if (aimCore == null) Debug.LogError("AimCore not found as child of player!");
        if (characterRelativeMovement.camera == null || cameraRelativeMovement.camera == null) Debug.LogError("One or both cameras not assigned!");
        
        _horizontalLookAngle = transform.eulerAngles.y;
        currentMovement = characterRelativeMovement;
        characterRelativeMovement.camera.Priority = 10;
        cameraRelativeMovement.camera.Priority = 0;
    }

    private void Update()
    {
        IsGrounded();
        UpdateState();
        HandleAimCore();
        HandleCameraTransition();
        HandleJump();
        HandleMovement();
        ApplyGravity();
    }



    #region State Management ---------------------------------------------------------------------------------

    private void UpdateState()
    {
        if (!_isGrounded)
        {
            AirTime += Time.deltaTime;

            if (currentState == PlayerState.Jumping && _gravityForce.y < 0)
            {
                currentState = PlayerState.Falling;
                FallTime = 0;
            }
            else if (currentState != PlayerState.Falling &&
                     currentState != PlayerState.Jumping &&
                     AirTime > fallThreshold)
            {
                currentState = PlayerState.Falling;
                FallTime = 0;
            }

            if (currentState == PlayerState.Falling)
            {
                FallTime += Time.deltaTime;
            }
        }
        else if (currentState == PlayerState.Falling)
        {
            currentState = PlayerState.Landing;

            if (FallTime > fallThreshold)
            {
                LandingIntensity = Mathf.Clamp01((FallTime - fallThreshold) / (maxFallTime - fallThreshold));
                _landingRecoveryTime = LandingIntensity * recoveryDuration;
            }
            else
            {
                LandingIntensity = 0;
                _landingRecoveryTime = 0;
            }

            _stateTimer = 0;
        }
        else if (currentState == PlayerState.Landing)
        {
            if (_landingRecoveryTime > 0)
            {
                _landingRecoveryTime -= Time.deltaTime;
            }
            else if (_stateTimer > LandingIntensity * recoveryDuration * 0.5f)
            {
                currentState = PlayerState.Grounded;
                ResetStateTimers();
            }
        }

        _stateTimer += Time.deltaTime;
    }

    private void ResetStateTimers()
    {
        AirTime = 0;
        FallTime = 0;
        _landingRecoveryTime = 0;
        LandingIntensity = 0;
        _stateTimer = 0;
        _jumpBufferTimer = 0;
        _hasBufferedJump = false;
    }

    #endregion State Management ---------------------------------------------------------------------------------

    
    #region Camera and Aim Control ---------------------------------------------------------------------------------

    private void HandleAimCore()
    {
        float lookSpeed = 5f; // Adjust this as needed for responsiveness

        _verticalLookAngle = Mathf.Lerp(_verticalLookAngle, _verticalLookAngle - _playerInput.MouseY, Time.deltaTime * lookSpeed);
        _verticalLookAngle = Mathf.Clamp(_verticalLookAngle, verticalLookLimits.x, verticalLookLimits.y);
    
        _horizontalLookAngle = Mathf.Lerp(_horizontalLookAngle, _horizontalLookAngle + _playerInput.MouseX, Time.deltaTime * lookSpeed);

        aimCore.rotation = Quaternion.Euler(_verticalLookAngle, _horizontalLookAngle, 0f);
    }

    private float NormalizeAngle(float angle)
    {
        while (angle > 180) angle -= 360;
        while (angle < -180) angle += 360;
        return angle;
    }

    private void HandleCameraTransition()
    {
        bool isAiming = _playerInput.RightClickInput;

        if (isAiming != _wasAiming)
        {
            StopAllCoroutines(); // Stop any ongoing transitions
            StartCoroutine(SmoothCameraTransition(isAiming));
            _wasAiming = isAiming;
        }
    }

    private IEnumerator SmoothCameraTransition(bool isAiming)
    {
        float transitionDuration = 1f; // Adjust transition speed
        float elapsedTime = 0f;
    
        int startPriorityChar = characterRelativeMovement.camera.Priority;
        int startPriorityCam = cameraRelativeMovement.camera.Priority;
    
        int targetPriorityChar = isAiming ? 0 : 10;
        int targetPriorityCam = isAiming ? 10 : 0;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float blend = elapsedTime / transitionDuration;

            // Round to nearest integer to avoid float-to-int conversion errors
            characterRelativeMovement.camera.Priority = Mathf.RoundToInt(Mathf.Lerp(startPriorityChar, targetPriorityChar, blend));
            cameraRelativeMovement.camera.Priority = Mathf.RoundToInt(Mathf.Lerp(startPriorityCam, targetPriorityCam, blend));

            yield return null;
        }

        // Ensure exact values at the end of the transition
        characterRelativeMovement.camera.Priority = targetPriorityChar;
        cameraRelativeMovement.camera.Priority = targetPriorityCam;
    }

    
    private Quaternion CalculateInputFrame()
    {
        // Get the camera's rotation from the current movement type.
        Quaternion camRotation = currentMovement.camera.transform.rotation;
        // Compute the camera's up vector.
        Vector3 camUp = camRotation * Vector3.up;
        // Get the player's up vector.
        Vector3 playerUp = transform.up;
        // Determine the axis to rotate camUp to match playerUp.
        Vector3 axis = Vector3.Cross(camUp, playerUp);
        if (axis.sqrMagnitude < 0.001f)
            return camRotation; // They’re already nearly aligned.
        // Compute the angle difference.
        float angle = Vector3.SignedAngle(camUp, playerUp, axis);
        // Return the adjusted rotation.
        return Quaternion.AngleAxis(angle, axis) * camRotation;
    }

    #endregion Camera and Aim Control ---------------------------------------------------------------------------------

    
    #region Jump and Gravity ---------------------------------------------------------------------------------

    private void HandleJump()
    {
        if (_playerInput.JumpInput)
        {
            _hasBufferedJump = true;
            _jumpBufferTimer = jumpBufferTime;
        }

        if (_hasBufferedJump)
        {
            _jumpBufferTimer -= Time.deltaTime;
            if (_jumpBufferTimer <= 0)
            {
                _hasBufferedJump = false;
            }
        }

        if (_isGrounded && (_landingRecoveryTime <= 0 || currentState != PlayerState.Landing))
        {
            if (_playerInput.JumpInput || _hasBufferedJump)
            {
                currentState = PlayerState.Jumping;
                _gravityForce.y = jumpForce;
                if (_movementIntensity > 0.1f) activeMoveSpeed += jumpSpeedBoost;
                ResetStateTimers();
                _hasBufferedJump = false;
            }
        }
    }

    private void ApplyGravity()
    {
        if (!_isGrounded)
        {
            _gravityForce.y += gravity * Time.deltaTime;
        }
        else if (_gravityForce.y < 0)
        {
            _gravityForce.y = groundedGravity;

            if (currentState != PlayerState.Falling && currentState != PlayerState.Landing)
            {
                AirTime = 0;
                FallTime = 0;
            }
        }

        _controller.Move(_gravityForce * Time.deltaTime);
    }

    #endregion Jump and Gravity ---------------------------------------------------------------------------------
    
    
    #region Movement System ---------------------------------------------------------------------------------

    private void HandleMovement()
    {
        // Choose the movement type based on aiming input.
        currentMovement = _playerInput.RightClickInput ? cameraRelativeMovement : characterRelativeMovement;

        // Calculate movement intensity (for speed calculations).
        _movementIntensity = Mathf.Clamp01(Mathf.Abs(_playerInput.HorizontalInput) + Mathf.Abs(_playerInput.VerticalInput));

        // Build a raw input vector.
        Vector3 rawInput = new Vector3(_playerInput.HorizontalInput, 0, _playerInput.VerticalInput);
        rawInput = Vector3.ClampMagnitude(rawInput, 1f);

        // Get the dynamic input frame.
        Quaternion inputFrame = CalculateInputFrame();

        // Instead of using camera forward/right directly,
        // compute the movement vector using the input frame.
        _movementVector = (inputFrame * rawInput).normalized;

        // Handle movement speed based on ground state.
        if (_isGrounded && currentState != PlayerState.Jumping)
        {
            if (currentState == PlayerState.Landing)
                HandleLandingMovement();
            else
                HandleNormalMovement();
        }
        else if (currentState == PlayerState.Jumping || currentState == PlayerState.Falling)
        {
            targetMoveSpeed = activeMoveSpeed;
        }

        // Choose rotation handling based on movement mode.
        if (_playerInput.RightClickInput)
        {
            HandleCameraRelativeRotation();
        }
        else
        {
            HandleCharacterRelativeRotation();
        }
            

        // Update active speed with acceleration.
        activeMoveSpeed = Mathf.MoveTowards(activeMoveSpeed, targetMoveSpeed,
            currentMovement.acceleration * Time.deltaTime);
        activeMoveSpeed = Mathf.Min(activeMoveSpeed, currentMovement.maxMoveSpeed);

        // Finally, apply movement.
        _controller.Move(_movementVector * (activeMoveSpeed * Time.deltaTime));
    }
    
    private void HandleCharacterRelativeRotation()
    {
        if (_movementVector.sqrMagnitude > 0.01f)
        {
            Vector3 forwardDirection = _movementVector.normalized;
        
            // Calculate desired rotation
            Quaternion targetRotation = Quaternion.LookRotation(forwardDirection);

            // Instead of Slerp, use velocity-based smoothing like Cinemachine
            float rotationVelocity = currentMovement.rotationSpeed * Time.deltaTime;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationVelocity);
        }
    }
    
    private void HandleCameraRelativeRotation()
    {
        // Get the camera's horizontal forward direction
        Vector3 cameraForward = currentMovement.camera.transform.forward;
        cameraForward.y = 0; // Keep rotation level
        cameraForward.Normalize();

        Quaternion targetRotation = transform.rotation;

        switch (currentMovement.rotationMode)
        {
            case RotationMode.Coupled:
                targetRotation = Quaternion.LookRotation(cameraForward);
                break;
            case RotationMode.CoupledMoving:
                if (_movementVector.sqrMagnitude > 0.01f)
                    targetRotation = Quaternion.LookRotation(cameraForward);
                break;
            case RotationMode.Free:
                if (_movementVector.sqrMagnitude > 0.01f)
                    targetRotation = Quaternion.LookRotation(_movementVector);
                break;
        }

        // Apply Cinemachine-like smooth rotation velocity
        float rotationVelocity = currentMovement.rotationSpeed * Time.deltaTime;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationVelocity);
    }

    private void HandleCharacterRelativeMovement()
    {
        // Movement vector is already computed in HandleMovement()
    
        if (_isGrounded && currentState != PlayerState.Jumping)
        {
            if (currentState == PlayerState.Landing)
                HandleLandingMovement();
            else
                HandleNormalMovement();
        }
        else if (currentState == PlayerState.Jumping || currentState == PlayerState.Falling)
        {
            targetMoveSpeed = activeMoveSpeed;
        }

        // Apply movement
        _controller.Move(_movementVector * (activeMoveSpeed * Time.deltaTime));
    }

    private void HandleCameraRelativeMovement()
    {
        // Movement vector is already computed in HandleMovement()
    
        if (_isGrounded && currentState != PlayerState.Jumping)
        {
            if (currentState == PlayerState.Landing)
                HandleLandingMovement();
            else
                HandleNormalMovement();
        }
        else if (currentState == PlayerState.Jumping || currentState == PlayerState.Falling)
        {
            targetMoveSpeed = activeMoveSpeed;
        }

        // Apply movement
        _controller.Move(_movementVector * (activeMoveSpeed * Time.deltaTime));
    }

    private void HandleLandingMovement()
    {
        float recoveryProgress = 1f - (_landingRecoveryTime / (recoveryDuration * LandingIntensity));
        float movementMultiplier = Mathf.Lerp(minMovementControl, 1f, recoveryProgress);

        if (_movementIntensity > 0)
        {
            if (_playerInput.WalkInput)
                targetMoveSpeed = currentMovement.walkSpeed;
            else if (_playerInput.SprintInput && _movementIntensity > 0.5f)
                targetMoveSpeed = currentMovement.runSpeed * currentMovement.sprintSpeedMultiplier;
            else
                targetMoveSpeed = currentMovement.runSpeed;

            targetMoveSpeed *= movementMultiplier;
        }
        else
        {
            targetMoveSpeed = 0;
        }
    }

    private void HandleNormalMovement()
    {
        if (_movementIntensity < 0.1f)
        {
            targetMoveSpeed = 0f;
        }
        else
        {
            if (_playerInput.WalkInput)
            {
                targetMoveSpeed = currentMovement.walkSpeed;
            }
            else if (_playerInput.SprintInput && _movementIntensity > 0.5f)
            {
                if (currentMovement.walkByDefault)
                {
                    targetMoveSpeed = currentMovement.runSpeed;
                }
                else
                {
                    targetMoveSpeed = currentMovement.runSpeed * currentMovement.sprintSpeedMultiplier;
                }
            }
            else
            {
                targetMoveSpeed = currentMovement.walkByDefault ? currentMovement.walkSpeed : currentMovement.runSpeed;
            }
        }
    }

    #endregion Movement System ---------------------------------------------------------------------------------

    
    #region Ground Check ---------------------------------------------------------------

    private void IsGrounded()
    {
        Vector3 spherePosition = transform.position + groundCheckOffset;
        _isGrounded = Physics.CheckSphere(spherePosition, groundCheckRadius, groundLayer);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Vector3 spherePosition = transform.position + groundCheckOffset;
        Gizmos.DrawWireSphere(spherePosition, groundCheckRadius);
    }
    #endregion Ground Check ---------------------------------------------------------------
}

