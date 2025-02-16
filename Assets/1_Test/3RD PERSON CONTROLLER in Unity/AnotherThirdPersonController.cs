using System;
using UnityEngine;
using UnityEngine.Serialization;
using VInspector;


[Serializable]
public class MovementType
{
    public Transform cameraTransform;
    public float  walkSpeed = 4f;
    public float runSpeed = 8f;
    public float sprintSpeed = 15f;
    public float maxMoveSpeed = 20f;
    public float acceleration = 20f;
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
    
    private enum PlayerMovement
    {
        CharacterRelative = 0,
        CameraRelative = 1,
    }
    
    [Foldout("Character Relative Movement")]
    [SerializeField, Tooltip("Reference to the main camera transform for movement direction")] 
    private Transform characterRelativeCamera;
    [SerializeField, Tooltip("Base walking speed")] 
    private float walkSpeed = 4f;
    [SerializeField, Tooltip("Running speed - faster than walk")] 
    private float runSpeed = 8f;
    [SerializeField, Tooltip("Sprint speed - fastest movement")] 
    private float sprintSpeed = 15f;
    [SerializeField, Tooltip("Absolute maximum movement speed cap")] 
    private float maxMoveSpeed = 20f;
    [SerializeField, Tooltip("How quickly speed changes occur")] 
    private float acceleration = 20f;
    [SerializeField, Tooltip("How fast the character rotates to face movement direction")] 
    private float rotationSpeed = 6f;
    [EndFoldout]
    
    
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
    
    private CharacterController _controller;
    private Animator _animator;
    private bool _isGrounded;
    private Vector3 _movementVector;
    private Vector3 _gravityForce;
    private float _movementIntensity;
    private float _airTime;      // Time spent in air for fall detection
    private float _fallTime;     // Time spent specifically in falling state
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
    
    private int _horizontalHash;
    private int _verticalHash;
    private int _stateHash;
    private int _fallTimeHash;
    
    private PlayerState _previousState;
    private float _stateTimer;
    
    [Header("Debug")]
    [SerializeField, ReadOnly] private PlayerState currentState = PlayerState.Grounded;
    [SerializeField, ReadOnly] private PlayerMovement movementType = PlayerMovement.CharacterRelative;
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
        
        
        // Set jump buffer when jump is pressed
        if (_jumpInput)
        {
            _hasBufferedJump = true;
            _jumpBufferTimer = jumpBufferTime;
        }
        
        // Update the jump buffer timer
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
        Debug.Log($"State: {currentState}, Grounded: {_isGrounded}, AirTime: {_airTime}, FallTime: {_fallTime}, Vertical Velocity: {_gravityForce.y}");
        
        _previousState = currentState;

        if (!_isGrounded)
        {
            _airTime += Time.deltaTime;
            
            // Enter falling state if:
            // 1. Was jumping and now descending
            // 2. Has been in air longer than fall threshold
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
            // Check for either immediate jump input or buffered jump
            if (_jumpInput || _hasBufferedJump)
            {
                currentState = PlayerState.Jumping;
                _gravityForce.y = jumpForce;
                if (_movementIntensity > 0.1f) activeMoveSpeed += jumpSpeedBoost;
                ResetStateTimers();
                _hasBufferedJump = false; // Clear the buffer after using it
            }
        }
    }

    private void HandleMovement()
    {
        _movementVector = characterRelativeCamera.forward * _verticalInput;
        _movementVector += characterRelativeCamera.right * _horizontalInput;
        _movementVector.Normalize();
        _movementVector.y = 0;
        _movementIntensity = Mathf.Clamp01(Mathf.Abs(_horizontalInput) + Mathf.Abs(_verticalInput));

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

        activeMoveSpeed = Mathf.MoveTowards(activeMoveSpeed, targetMoveSpeed, acceleration * Time.deltaTime);
        activeMoveSpeed = Mathf.Min(activeMoveSpeed, maxMoveSpeed);
        
        _controller.Move(_movementVector * (activeMoveSpeed * Time.deltaTime));
        if (_movementVector != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_movementVector);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void HandleLandingMovement()
    {
        float recoveryProgress = 1f - (_landingRecoveryTime / (recoveryDuration * _landingIntensity));
        float movementMultiplier = Mathf.Lerp(minMovementControl, 1f, recoveryProgress);
        
        if (_movementIntensity > 0)
        {
            if (_walkInput)
                targetMoveSpeed = walkSpeed;
            else if (_sprintInput && _movementIntensity > 0.5f)
                targetMoveSpeed = sprintSpeed;
            else
                targetMoveSpeed = runSpeed;
                
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
                targetMoveSpeed = walkSpeed;
            }
            else if (_sprintInput && _movementIntensity > 0.5f)
            {
                targetMoveSpeed = sprintSpeed;
            }
            else
            {
                targetMoveSpeed = runSpeed;
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
        _animator.SetInteger(_stateHash, (int)currentState);

        float fallBlend = currentState == PlayerState.Landing ? 
            _landingIntensity : 
            Mathf.Clamp01(_fallTime / maxFallTime);
            
        _animator.SetFloat(_fallTimeHash, fallBlend);

        float verticalValue;
        if (activeMoveSpeed <= walkSpeed)
        {
            verticalValue = (activeMoveSpeed / walkSpeed) * 0.5f;
        }
        else if (activeMoveSpeed <= runSpeed)
        {
            verticalValue = 0.5f + ((activeMoveSpeed - walkSpeed) / (runSpeed - walkSpeed)) * 0.5f;
        }
        else
        {
            verticalValue = 1f + ((activeMoveSpeed - runSpeed) / (sprintSpeed - runSpeed));
        }

        _animator.SetFloat(_verticalHash, verticalValue, 0.1f, Time.deltaTime);
        _animator.SetFloat(_horizontalHash, 0, 0.1f, Time.deltaTime);
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