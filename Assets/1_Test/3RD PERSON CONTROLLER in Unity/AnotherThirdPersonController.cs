using UnityEngine;
using Unity.Cinemachine;

public class AnotherThirdPersonController : MonoBehaviour
{
    public enum PlayerState
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
    [SerializeField] private float _currentMoveSpeed;
    [SerializeField] private float _targetMoveSpeed;

    [Header("Movement")]
    [SerializeField] private Transform cameraObject;
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float sprintSpeed = 15f;
    [SerializeField] private float maxMoveSpeed = 20f;
    [SerializeField] private float acceleration = 3f;
    [SerializeField] private float rotationSpeed = 15f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float jumpSpeedBoost = 5f;
    
    [Header("Fall & Landing Settings")]
    [SerializeField] private float fallThreshold = 0.5f;     // Time before fall impact is registered
    [SerializeField] private float maxFallTime = 2.0f;       // Fall time for maximum impact
    [SerializeField] private float recoveryDuration = 0.5f;  // Maximum recovery/landing time
    [SerializeField] private float minMovementControl = 0.2f; // Minimum movement during recovery
    
    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedGravity = -2f;
    
    [Header("Ground Check")]
    [SerializeField] private float groundCheckRadius = 0.23f;
    [SerializeField] private Vector3 groundCheckOffset = new Vector3(0, 0.2f, 0);
    [SerializeField] private LayerMask groundLayer = 1;
    
    private CharacterController _controller;
    private Animator _animator;
    private bool _isGrounded;
    private Vector3 _moveDirection;
    private Vector3 _verticalVelocity;
    private float _moveAmount;
    private float _fallTime;
    private float _recoveryTimer;
    private float _landingImpact; // 0-1 value representing landing intensity
    
    private float _horizontalInput;
    private float _verticalInput;
    private bool _jumpInput;
    private bool _sprintInput;
    private bool _walkInput;
    
    private int _horizontalHash;
    private int _verticalHash;
    private int _stateHash;
    private int _fallTimeHash;
    
    private PlayerState previousState;
    private float stateTimer;

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
        CheckGrounded();
        GetPlayerInput();
        UpdateState();
        HandleMovement();
        HandleRotation();
        HandleGravity();
        UpdateAnimator();
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
        Debug.Log($"State: {currentState}, Grounded: {_isGrounded}, FallTime: {_fallTime}, Vertical Velocity: {_verticalVelocity.y}");
        
        previousState = currentState;

        // Only allow jumping if not in landing recovery
        if (_isGrounded && _jumpInput && (_recoveryTimer <= 0 || currentState != PlayerState.Landing))
        {
            currentState = PlayerState.Jumping;
            _verticalVelocity.y = jumpForce;
            if (_moveAmount > 0.1f) _currentMoveSpeed += jumpSpeedBoost;
            ResetTimers();
            return; // Exit early since we've handled the state change
        }

        // Handle falling
        if (!_isGrounded)
        {
            // Transition from jumping to falling
            if (currentState == PlayerState.Jumping && (_verticalVelocity.y < 0 || stateTimer > 0.5f))
            {
                currentState = PlayerState.Falling;
                _fallTime = 0; // Start counting fall time
            }
            // Transition to falling from any grounded state
            else if (currentState != PlayerState.Falling && currentState != PlayerState.Jumping)
            {
                currentState = PlayerState.Falling;
            }
            
            // Count fall time while in falling state
            if (currentState == PlayerState.Falling)
            {
                _fallTime += Time.deltaTime;
            }
        }
        // Handle landing when touching ground while falling
        else if (currentState == PlayerState.Falling)
        {
            currentState = PlayerState.Landing;
            
            // Calculate landing impact (0-1)
            if (_fallTime > fallThreshold)
            {
                _landingImpact = Mathf.Clamp01((_fallTime - fallThreshold) / (maxFallTime - fallThreshold));
                _recoveryTimer = _landingImpact * recoveryDuration;
            }
            else
            {
                _landingImpact = 0;
                _recoveryTimer = 0;
            }
            
            stateTimer = 0;
        }
        // Update landing state
        else if (currentState == PlayerState.Landing)
        {
            if (_recoveryTimer > 0)
            {
                _recoveryTimer -= Time.deltaTime;
            }
            else if (stateTimer > _landingImpact * recoveryDuration * 0.5f)
            {
                currentState = PlayerState.Idle;
                ResetTimers();
            }
        }

        stateTimer += Time.deltaTime;
    }

    private void HandleMovement()
    {
        _moveDirection = cameraObject.forward * _verticalInput;
        _moveDirection += cameraObject.right * _horizontalInput;
        _moveDirection.Normalize();
        _moveDirection.y = 0;
        _moveAmount = Mathf.Clamp01(Mathf.Abs(_horizontalInput) + Mathf.Abs(_verticalInput));

        if (_isGrounded && currentState != PlayerState.Jumping)
        {
            if (currentState == PlayerState.Landing)
            {
                float recoveryProgress = 1f - (_recoveryTimer / (recoveryDuration * _landingImpact));
                float movementMultiplier = Mathf.Lerp(minMovementControl, 1f, recoveryProgress);
                
                // Calculate base move speed based on input
                if (_moveAmount > 0)
                {
                    if (_walkInput)
                        _targetMoveSpeed = walkSpeed;
                    else if (_sprintInput && _moveAmount > 0.5f)
                        _targetMoveSpeed = sprintSpeed;
                    else
                        _targetMoveSpeed = runSpeed;
                        
                    _targetMoveSpeed *= movementMultiplier;
                }
                else
                {
                    _targetMoveSpeed = 0;
                }
            }
            else if (currentState != PlayerState.Landing) // Only handle other states if not landing
            {
                if (_moveAmount < 0.1f)
                {
                    currentState = PlayerState.Idle;
                    _targetMoveSpeed = 0f;
                }
                else
                {
                    if (_walkInput)
                    {
                        currentState = PlayerState.Walking;
                        _targetMoveSpeed = walkSpeed;
                    }
                    else if (_sprintInput && _moveAmount > 0.5f)
                    {
                        currentState = PlayerState.Sprinting;
                        _targetMoveSpeed = sprintSpeed;
                    }
                    else if (_moveAmount >= 0.5f)
                    {
                        currentState = PlayerState.Running;
                        _targetMoveSpeed = runSpeed;
                    }
                    else
                    {
                        currentState = PlayerState.Walking;
                        _targetMoveSpeed = walkSpeed;
                    }
                }
            }
        }
        else if (currentState == PlayerState.Jumping || currentState == PlayerState.Falling)
        {
            _targetMoveSpeed = _currentMoveSpeed;
        }

        _currentMoveSpeed = Mathf.MoveTowards(_currentMoveSpeed, _targetMoveSpeed, acceleration * Time.deltaTime);
        _currentMoveSpeed = Mathf.Min(_currentMoveSpeed, maxMoveSpeed);
        _controller.Move(_moveDirection * (_currentMoveSpeed * Time.deltaTime));
    }

    private void HandleRotation()
    {
        if (_moveDirection == Vector3.zero) return;
        
        Quaternion targetRotation = Quaternion.LookRotation(_moveDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void HandleGravity()
    {
        if (!_isGrounded)
        {
            _verticalVelocity.y += gravity * Time.deltaTime;
        }
        else if (_verticalVelocity.y < 0)
        {
            _verticalVelocity.y = groundedGravity;
            
            // Only reset fall time when in a grounded state (not falling or landing)
            if (currentState != PlayerState.Falling && currentState != PlayerState.Landing)
            {
                _fallTime = 0;
            }
        }

        _controller.Move(_verticalVelocity * Time.deltaTime);
    }
    
    private void CheckGrounded()
    {
        Vector3 spherePosition = transform.position + groundCheckOffset;
        _isGrounded = Physics.CheckSphere(spherePosition, groundCheckRadius, groundLayer);
    }

    private void UpdateAnimator()
    {
        _animator.SetInteger(_stateHash, (int)currentState);

        // Use landing impact for blend tree during landing
        float fallBlend = currentState == PlayerState.Landing ? 
            _landingImpact : 
            Mathf.Clamp01(_fallTime / maxFallTime);
            
        _animator.SetFloat(_fallTimeHash, fallBlend);

        float verticalValue;
        if (_currentMoveSpeed <= walkSpeed)
        {
            verticalValue = (_currentMoveSpeed / walkSpeed) * 0.5f;
        }
        else if (_currentMoveSpeed <= runSpeed)
        {
            verticalValue = 0.5f + ((_currentMoveSpeed - walkSpeed) / (runSpeed - walkSpeed)) * 0.5f;
        }
        else
        {
            verticalValue = 1f + ((_currentMoveSpeed - runSpeed) / (sprintSpeed - runSpeed));
        }

        _animator.SetFloat(_verticalHash, verticalValue, 0.1f, Time.deltaTime);
        _animator.SetFloat(_horizontalHash, 0, 0.1f, Time.deltaTime);
    }

    private void ResetTimers()
    {
        _fallTime = 0;
        _recoveryTimer = 0;
        _landingImpact = 0;
        stateTimer = 0;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Vector3 spherePosition = transform.position + groundCheckOffset;
        Gizmos.DrawWireSphere(spherePosition, groundCheckRadius);
    }
}