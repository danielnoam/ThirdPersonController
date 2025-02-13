using UnityEngine;
public class AnotherThirdPersonController : MonoBehaviour
{
    private enum PlayerState
    {
        Idle = 0,
        Walking = 1,
        Running = 2,
        Sprinting = 3,
        Jumping = 4,
        Falling = 5,
        Landing = 6
    }
    
    [SerializeField] private PlayerState currentState = PlayerState.Idle;
    [SerializeField] private float activeSpeed;
    [SerializeField] private float desiredSpeed;

    [Header("Movement")]
    [SerializeField, Tooltip("Reference to the main camera transform for movement direction")] 
    private Transform cameraObject;
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
    [SerializeField, Tooltip("Initial upward force applied when jumping")] 
    private float jumpForce = 8f;
    [SerializeField, Tooltip("Additional forward speed boost when jumping")] 
    private float jumpSpeedBoost = 2f;
    
    [Header("Fall & Landing Settings")]
    [SerializeField, Tooltip("Minimum fall time before impact is registered")] 
    private float fallThreshold = 0.5f;
    [SerializeField, Tooltip("Fall time that results in maximum impact")] 
    private float maxFallTime = 2.0f;
    [SerializeField, Tooltip("Maximum time to recover from landing")] 
    private float recoveryDuration = 0.5f;
    [SerializeField, Tooltip("Minimum movement control during landing recovery")] 
    private float minMovementControl = 0.2f;
    
    [Header("Gravity")]
    [SerializeField, Tooltip("Gravity force applied when in air")] 
    private float gravity = -20f;
    [SerializeField, Tooltip("Small downward force when grounded")] 
    private float groundedGravity = -2f;
    
    [Header("Ground Check")]
    [SerializeField, Tooltip("Radius of the ground detection sphere")] 
    private float groundCheckRadius = 0.23f;
    [SerializeField, Tooltip("Offset from player center for ground detection")] 
    private Vector3 groundCheckOffset = new Vector3(0, 0.2f, 0);
    [SerializeField, Tooltip("Layer mask for ground detection")] 
    private LayerMask groundLayer = 1;
    
    private CharacterController _controller;
    private Animator _animator;
    private bool _isGrounded;
    private Vector3 _movementVector;
    private Vector3 _gravityForce;
    private float _movementIntensity;
    private float _fallTime;
    private float _landingRecoveryTime;
    private float _landingIntensity;
    
    private float _horizontalInput;
    private float _verticalInput;
    private bool _jumpInput;
    private bool _sprintInput;
    private bool _walkInput;
    
    private int _horizontalHash;
    private int _verticalHash;
    private int _stateHash;
    private int _fallTimeHash;
    
    private PlayerState _previousState;
    private float _stateTimer;

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
    }

    private void UpdateState()
    {
        Debug.Log($"State: {currentState}, Grounded: {_isGrounded}, FallTime: {_fallTime}, Vertical Velocity: {_gravityForce.y}");
        
        _previousState = currentState;
        

        if (!_isGrounded)
        {
            if (currentState == PlayerState.Jumping && (_gravityForce.y < 0 || _stateTimer > 0.5f))
            {
                currentState = PlayerState.Falling;
                _fallTime = 0;
            }
            else if (currentState != PlayerState.Falling && currentState != PlayerState.Jumping)
            {
                currentState = PlayerState.Falling;
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
                currentState = PlayerState.Idle;
                ResetStateTimers();
            }
        }

        _stateTimer += Time.deltaTime;
    }

    private void HandleJump()
    {
        if (_isGrounded && _jumpInput && (_landingRecoveryTime <= 0 || currentState != PlayerState.Landing))
        {
            currentState = PlayerState.Jumping;
            _gravityForce.y = jumpForce;
            if (_movementIntensity > 0.1f) activeSpeed += jumpSpeedBoost;
            ResetStateTimers();
        }
    }

    private void HandleMovement()
    {
        _movementVector = cameraObject.forward * _verticalInput;
        _movementVector += cameraObject.right * _horizontalInput;
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
            desiredSpeed = activeSpeed;
        }

        activeSpeed = Mathf.MoveTowards(activeSpeed, desiredSpeed, acceleration * Time.deltaTime);
        activeSpeed = Mathf.Min(activeSpeed, maxMoveSpeed);
        
        // Apply movement and handle rotation
        _controller.Move(_movementVector * (activeSpeed * Time.deltaTime));
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
                desiredSpeed = walkSpeed;
            else if (_sprintInput && _movementIntensity > 0.5f)
                desiredSpeed = sprintSpeed;
            else
                desiredSpeed = runSpeed;
                
            desiredSpeed *= movementMultiplier;
        }
        else
        {
            desiredSpeed = 0;
        }
    }

    private void HandleNormalMovement()
    {
        if (_movementIntensity < 0.1f)
        {
            currentState = PlayerState.Idle;
            desiredSpeed = 0f;
        }
        else
        {
            if (_walkInput)
            {
                currentState = PlayerState.Walking;
                desiredSpeed = walkSpeed;
            }
            else if (_sprintInput && _movementIntensity > 0.5f)
            {
                currentState = PlayerState.Sprinting;
                desiredSpeed = sprintSpeed;
            }
            else if (_movementIntensity >= 0.5f)
            {
                currentState = PlayerState.Running;
                desiredSpeed = runSpeed;
            }
            else
            {
                currentState = PlayerState.Walking;
                desiredSpeed = walkSpeed;
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
        if (activeSpeed <= walkSpeed)
        {
            verticalValue = (activeSpeed / walkSpeed) * 0.5f;
        }
        else if (activeSpeed <= runSpeed)
        {
            verticalValue = 0.5f + ((activeSpeed - walkSpeed) / (runSpeed - walkSpeed)) * 0.5f;
        }
        else
        {
            verticalValue = 1f + ((activeSpeed - runSpeed) / (sprintSpeed - runSpeed));
        }

        _animator.SetFloat(_verticalHash, verticalValue, 0.1f, Time.deltaTime);
        _animator.SetFloat(_horizontalHash, 0, 0.1f, Time.deltaTime);
    }

    private void ResetStateTimers()
    {
        _fallTime = 0;
        _landingRecoveryTime = 0;
        _landingIntensity = 0;
        _stateTimer = 0;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Vector3 spherePosition = transform.position + groundCheckOffset;
        Gizmos.DrawWireSphere(spherePosition, groundCheckRadius);
    }
}