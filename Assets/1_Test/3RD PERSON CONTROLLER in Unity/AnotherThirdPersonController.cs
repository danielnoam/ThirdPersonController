using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;
using VInspector;

[Serializable]
public class MovementType
{
    [Tooltip("Type of movement (Character or Camera relative)")]
    public bool isCameraRelative;
    
    [Tooltip("Reference to the main camera for movement direction")] 
    public CinemachineCamera camera;
    
    [Tooltip("Base walking speed")] 
    public float walkSpeed = 4f;
    
    [Tooltip("Running speed - faster than walk")]
    public float runSpeed = 8f;
    
    [Tooltip("Crouching speed - slower than walk")]
    public float crouchSpeed = 2f;

    [Tooltip("Enable sprinting")]
    public bool enableSprint = true;
    
    [Tooltip("Sprint speed - fastest movement")]
    public float sprintSpeed = 15f;
    
    [Tooltip("Absolute maximum movement speed cap")]
    public float maxMoveSpeed = 20f;
    
    [Tooltip("How quickly speed changes occur")]
    public float acceleration = 20f;
    
    [Tooltip("How fast the character rotates to face movement direction")]
    public float rotationSpeed = 6f;
}

public class AnotherThirdPersonController : MonoBehaviour
{
    private enum PlayerState
    {
        Grounded = 0,
        Jumping = 1,
        Falling = 2,
        Landing = 3
    }

    [Header("Movement")]
    [SerializeField] private MovementType characterRelativeMovement;
    [SerializeField] private MovementType cameraRelativeMovement;

    [Header("Jumping")] 
    [SerializeField, Tooltip("Initial upward force applied when jumping")]
    private float jumpForce = 8f;
    [SerializeField, Tooltip("Additional forward speed boost when jumping")]
    private float jumpSpeedBoost = 2f;
    [SerializeField, Tooltip("How long to buffer jump input before player lands")]
    private float jumpBufferTime = 0.2f;

    [Header("Fall & Landing")] 
    [SerializeField, Tooltip("Minimum fall time before impact is registered")]
    private float fallThreshold = 0.5f;
    [SerializeField, Tooltip("Fall time that results in maximum impact")]
    private float maxFallTime = 2.0f;
    [SerializeField, Tooltip("Maximum time to recover from landing")]
    private float recoveryDuration = 0.5f;
    [SerializeField, Tooltip("Minimum movement control during landing recovery")]
    private float minMovementControl = 0.1f;

    [Header("Gravity")] 
    [SerializeField, Tooltip("Gravity force applied when in air")]
    private float gravity = -20f;
    [SerializeField, Tooltip("Small downward force when grounded")]
    private float groundedGravity = -2f;

    [Header("Ground Check")] 
    [SerializeField, Tooltip("Radius of the ground detection sphere")]
    private float groundCheckRadius = 0.23f;
    [SerializeField, Tooltip("Offset from player center for ground detection")]
    private Vector3 groundCheckOffset = new Vector3(0, -0.1f, 0);
    [SerializeField, Tooltip("Layer mask for ground detection")]
    private LayerMask groundLayer = 1;
    
    [Header("Aim Control")]
    [SerializeField] private Transform aimCore;
    [SerializeField] private float mouseSensitivity = 1f;
    [SerializeField] private Vector2 verticalLookLimits = new Vector2(-70f, 70f);
    
    private float _verticalLookAngle;
    private float _horizontalLookAngle;
    private CharacterController _controller;
    private Animator _animator;
    private MovementType _currentMovement;
    private bool _isGrounded;
    private Vector3 _movementVector;
    private Vector3 _gravityForce;
    private float _movementIntensity;
    private float _airTime;
    private float _fallTime;
    private float _landingRecoveryTime;
    private float _landingIntensity;
    private float _jumpBufferTimer;
    private bool _hasBufferedJump;

    private float _horizontalInput;
    private float _verticalInput;
    private bool _jumpInput;
    private bool _sprintInput;
    private bool _walkInput;
    private bool _rightClickInput;
    private float _mouseX;
    private float _mouseY;

    private int _horizontalHash;
    private int _verticalHash;
    private int _stateHash;
    private int _fallTimeHash;
    private int _cameraRelativeHash;

    private PlayerState _previousState;
    private float _stateTimer;

    [Header("Debug")] 
    [SerializeField, ReadOnly] private PlayerState currentState = PlayerState.Grounded;
    [SerializeField, ReadOnly] private float activeMoveSpeed;
    [SerializeField, ReadOnly] private float targetMoveSpeed;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        _horizontalHash = Animator.StringToHash("Horizontal");
        _verticalHash = Animator.StringToHash("Vertical");
        _stateHash = Animator.StringToHash("StateIndex");
        _fallTimeHash = Animator.StringToHash("FallTime");
        _cameraRelativeHash = Animator.StringToHash("CameraRelative");
        _currentMovement = characterRelativeMovement;
    }

    private void Update()
    {
        IsGrounded();
        GetPlayerInput();
        UpdateState();
        HandleJump();
        HandleMovement();
        ApplyGravity();
        SyncAnimations();
    }

    private void GetPlayerInput()
    {
        _horizontalInput = Input.GetAxisRaw("Horizontal");
        _verticalInput = Input.GetAxisRaw("Vertical");
        _jumpInput = Input.GetButtonDown("Jump");
        _sprintInput = Input.GetButton("Sprint");
        _walkInput = Input.GetButton("Walk");
        _rightClickInput = Input.GetMouseButton(1);

        _mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        _mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        _verticalLookAngle -= _mouseY;
        _verticalLookAngle = Mathf.Clamp(_verticalLookAngle, verticalLookLimits.x, verticalLookLimits.y);

        _horizontalLookAngle += _mouseX;
    
        aimCore.rotation = Quaternion.Euler(_verticalLookAngle, transform.eulerAngles.y + _horizontalLookAngle, 0f);

        if (_jumpInput)
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
    }

    private void UpdateState()
    {
        _previousState = currentState;

        if (!_isGrounded)
        {
            _airTime += Time.deltaTime;

            if (currentState == PlayerState.Jumping && _gravityForce.y < 0)
            {
                currentState = PlayerState.Falling;
                _fallTime = 0;
            }
            else if (currentState != PlayerState.Falling &&
                     currentState != PlayerState.Jumping &&
                     _airTime > fallThreshold)
            {
                currentState = PlayerState.Falling;
                _fallTime = 0;
            }

            if (currentState == PlayerState.Falling)
            {
                _fallTime += Time.deltaTime;
            }
        }
        else if (currentState == PlayerState.Falling)
        {
            currentState = PlayerState.Landing;

            if (_fallTime > fallThreshold)
            {
                _landingIntensity = Mathf.Clamp01((_fallTime - fallThreshold) / (maxFallTime - fallThreshold));
                _landingRecoveryTime = _landingIntensity * recoveryDuration;
            }
            else
            {
                _landingIntensity = 0;
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
            else if (_stateTimer > _landingIntensity * recoveryDuration * 0.5f)
            {
                currentState = PlayerState.Grounded;
                ResetStateTimers();
            }
        }

        _stateTimer += Time.deltaTime;
    }

    private void HandleJump()
    {
        if (_isGrounded && (_landingRecoveryTime <= 0 || currentState != PlayerState.Landing))
        {
            if (_jumpInput || _hasBufferedJump)
            {
                currentState = PlayerState.Jumping;
                _gravityForce.y = jumpForce;
                if (_movementIntensity > 0.1f) activeMoveSpeed += jumpSpeedBoost;
                ResetStateTimers();
                _hasBufferedJump = false;
            }
        }
    }

    private void HandleMovement()
    {
        _currentMovement = _rightClickInput ? cameraRelativeMovement : characterRelativeMovement;

        characterRelativeMovement.camera.Priority = !_rightClickInput ? 10 : 0;
        cameraRelativeMovement.camera.Priority = _rightClickInput ? 10 : 0;

        _movementIntensity = Mathf.Clamp01(Mathf.Abs(_horizontalInput) + Mathf.Abs(_verticalInput));

        if (_currentMovement.isCameraRelative)
        {
            HandleCameraRelativeMovement();
        }
        else
        {
            HandleCharacterRelativeMovement();
        }

        activeMoveSpeed = Mathf.MoveTowards(activeMoveSpeed, targetMoveSpeed,
            _currentMovement.acceleration * Time.deltaTime);
        activeMoveSpeed = Mathf.Min(activeMoveSpeed, _currentMovement.maxMoveSpeed);
    }

    private void HandleCharacterRelativeMovement()
    {
        _movementVector = _currentMovement.camera.transform.forward * _verticalInput;
        _movementVector += _currentMovement.camera.transform.right * _horizontalInput;
        _movementVector.Normalize();
        _movementVector.y = 0;

        if (_isGrounded && currentState != PlayerState.Jumping)
        {
            if (currentState == PlayerState.Landing)
            {
                HandleLandingMovement();
            }
            else
            {
                HandleNormalMovement();
            }
        }
        else if (currentState == PlayerState.Jumping || currentState == PlayerState.Falling)
        {
            targetMoveSpeed = activeMoveSpeed;
        }

        if (_movementVector != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_movementVector);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                _currentMovement.rotationSpeed * Time.deltaTime);
        }

        _controller.Move(_movementVector * (activeMoveSpeed * Time.deltaTime));
    }

    private void HandleCameraRelativeMovement()
    {
        // Calculate movement vector based on camera orientation
        _movementVector = aimCore.forward * _verticalInput;
        _movementVector += aimCore.right * _horizontalInput;
        _movementVector.Normalize();
        _movementVector.y = 0;

        // Handle movement speed
        if (_isGrounded && currentState != PlayerState.Jumping)
        {
            if (currentState == PlayerState.Landing)
            {
                HandleLandingMovement();
            }
            else
            {
                HandleNormalMovement();
            }
        }
        else if (currentState == PlayerState.Jumping || currentState == PlayerState.Falling)
        {
            targetMoveSpeed = activeMoveSpeed;
        }

        // Handle rotation
        Quaternion targetRotation;
    
        // Get the camera's flat forward direction
        Vector3 cameraForward = aimCore.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();
    
        // Only update player rotation based on movement or camera change
        if (_movementVector != Vector3.zero)
        {
            // Align with movement direction while maintaining camera relative orientation
            targetRotation = Quaternion.LookRotation(cameraForward);
        }
        else if (_mouseX != 0)
        {
            // When camera rotates without movement, update player rotation
            targetRotation = Quaternion.Euler(0, aimCore.eulerAngles.y, 0);
        }
        else
        {
            // When not moving or rotating camera, maintain current rotation
            targetRotation = transform.rotation;
        }

        // Apply rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
            _currentMovement.rotationSpeed * Time.deltaTime);

        // Apply movement
        _controller.Move(_movementVector * (activeMoveSpeed * Time.deltaTime));
    }

    private void HandleLandingMovement()
    {
        float recoveryProgress = 1f - (_landingRecoveryTime / (recoveryDuration * _landingIntensity));
        float movementMultiplier = Mathf.Lerp(minMovementControl, 1f, recoveryProgress);

        if (_movementIntensity > 0)
        {
            if (_walkInput)
                targetMoveSpeed = _currentMovement.walkSpeed;
            else if (_sprintInput && _currentMovement.enableSprint && _movementIntensity > 0.5f)
                targetMoveSpeed = _currentMovement.sprintSpeed;
            else
                targetMoveSpeed = _currentMovement.runSpeed;

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
            if (_walkInput)
            {
                targetMoveSpeed = _currentMovement.walkSpeed;
            }
            else if (_sprintInput && _currentMovement.enableSprint && _movementIntensity > 0.5f)
            {
                targetMoveSpeed = _currentMovement.sprintSpeed;
            }
            else
            {
                targetMoveSpeed = _currentMovement.runSpeed;
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
                _airTime = 0;
                _fallTime = 0;
            }
        }

        _controller.Move(_gravityForce * Time.deltaTime);
    }

    private void IsGrounded()
    {
        Vector3 spherePosition = transform.position + groundCheckOffset;
        _isGrounded = Physics.CheckSphere(spherePosition, groundCheckRadius, groundLayer);
    }

    private void SyncAnimations()
    {
        _animator.SetBool(_cameraRelativeHash, _currentMovement.isCameraRelative);
        _animator.SetInteger(_stateHash, (int)currentState);

        float fallBlend = currentState == PlayerState.Landing
            ? _landingIntensity
            : Mathf.Clamp01(_fallTime / maxFallTime);

        _animator.SetFloat(_fallTimeHash, fallBlend);

        if (_currentMovement.isCameraRelative)
        {
            // Camera relative movement - match blend tree positions
            float verticalValue = 0;
            float horizontalValue = 0;

            // Calculate forward/backward movement (Y axis in blend tree)
            if (_verticalInput != 0)
            {
                if (activeMoveSpeed <= _currentMovement.walkSpeed)
                    verticalValue = 0.5f * (_verticalInput * activeMoveSpeed / _currentMovement.walkSpeed);
                else if (activeMoveSpeed <= _currentMovement.runSpeed)
                    verticalValue = _verticalInput * (0.5f + 0.5f * (activeMoveSpeed - _currentMovement.walkSpeed) / (_currentMovement.runSpeed - _currentMovement.walkSpeed));
                else if (_currentMovement.enableSprint)
                    verticalValue = _verticalInput * (1f + (activeMoveSpeed - _currentMovement.runSpeed) / (_currentMovement.sprintSpeed - _currentMovement.runSpeed));
            }

            // Calculate strafe movement (X axis in blend tree)
            if (_horizontalInput != 0 || _mouseX != 0)
            {
                // Use horizontal input for strafe animation, and add camera rotation influence
                float strafeInfluence = _horizontalInput;
                if (_mouseX != 0 && _movementVector != Vector3.zero)
                {
                    // Add camera rotation influence to strafe when moving
                    strafeInfluence += Mathf.Sign(_mouseX) * 0.5f;
                }
                
                strafeInfluence = Mathf.Clamp(strafeInfluence, -1f, 1f);
                
                if (activeMoveSpeed <= _currentMovement.walkSpeed)
                    horizontalValue = 0.5f * strafeInfluence;
                else 
                    horizontalValue = strafeInfluence; // Full strafe value for running/sprinting
            }

            _animator.SetFloat(_verticalHash, verticalValue, 0.1f, Time.deltaTime);
            _animator.SetFloat(_horizontalHash, horizontalValue, 0.1f, Time.deltaTime);
        }
        else
        {
            // Character relative movement - original blend tree values
            float verticalValue = 0f;
            if (activeMoveSpeed <= _currentMovement.walkSpeed)
            {
                verticalValue = (activeMoveSpeed / _currentMovement.walkSpeed) * 0.5f;
            }
            else if (activeMoveSpeed <= _currentMovement.runSpeed)
            {
                verticalValue = 0.5f + ((activeMoveSpeed - _currentMovement.walkSpeed) /
                                        (_currentMovement.runSpeed - _currentMovement.walkSpeed)) * 0.5f;
            }
            else if (_currentMovement.enableSprint)
            {
                verticalValue = 1f + ((activeMoveSpeed - _currentMovement.runSpeed) /
                                      (_currentMovement.sprintSpeed - _currentMovement.runSpeed));
            }

            _animator.SetFloat(_verticalHash, verticalValue, 0.1f, Time.deltaTime);
            _animator.SetFloat(_horizontalHash, 0, 0.1f, Time.deltaTime);
        }
    }

    private void ResetStateTimers()
    {
        _airTime = 0;
        _fallTime = 0;
        _landingRecoveryTime = 0;
        _landingIntensity = 0;
        _stateTimer = 0;
        _jumpBufferTimer = 0;
        _hasBufferedJump = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Vector3 spherePosition = transform.position + groundCheckOffset;
        Gizmos.DrawWireSphere(spherePosition, groundCheckRadius);
    }
}