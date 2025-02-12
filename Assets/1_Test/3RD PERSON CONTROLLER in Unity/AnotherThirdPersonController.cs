using UnityEngine;
using Unity.Cinemachine;

public class AnotherThirdPersonController : MonoBehaviour
{
    public enum PlayerState
    {
        Idle,
        Walking,
        Running,
        Sprinting,
        Jumping,
        Falling,
        Landing
    }
    
    public PlayerState currentState = PlayerState.Idle;

    [Header("Movement")]
    public Transform cameraObject;
    public float walkSpeed = 4f;
    public float runSpeed = 8f;
    public float sprintSpeed = 15f;
    public float acceleration = 3f;
    public float rotationSpeed = 15f;
    public float jumpForce = 8f;
    
    [Header("Gravity")]
    public float gravity = -20f;
    public float groundedGravity = -2f;
    public float minFallTime = 0.5f;    // Minimum time in air before considering it a fall
    
    [Header("Ground Check")]
    [SerializeField] private float groundCheckRadius = 0.23f;
    [SerializeField] private Vector3 groundCheckOffset = new Vector3(0, 0.2f, 0);
    [SerializeField] private LayerMask groundLayer = 1;
    
    private CharacterController _controller;
    private Animator _animator;
    private bool _isGrounded;
    private Vector3 _moveDirection;
    private Vector3 _verticalVelocity;
    private float _currentMoveSpeed;
    private float _targetMoveSpeed;
    private float _moveAmount;
    private float _inAirTimer;
    
    private float _horizontalInput;
    private float _verticalInput;
    private bool _jumpInput;
    private bool _sprintInput;
    
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
    }

    private void UpdateState()
    {
        previousState = currentState;

        // Handle jumping
        if (_isGrounded && _jumpInput && currentState != PlayerState.Jumping)
        {
            if (currentState == PlayerState.Landing)
            {
                stateTimer = 0;
            }
            currentState = PlayerState.Jumping;
            _verticalVelocity.y = jumpForce;
            stateTimer = 0;
            _inAirTimer = 0;
        }
        else
        {
            // Handle falling/landing
            if (!_isGrounded)
            {
                if (currentState == PlayerState.Jumping)
                {
                    if (stateTimer > 0.5f || _verticalVelocity.y < 0)
                    {
                        currentState = PlayerState.Falling;
                    }
                }
                else if (currentState != PlayerState.Falling && _inAirTimer >= minFallTime)
                {
                    currentState = PlayerState.Falling;
                }
            }
            else if (currentState == PlayerState.Falling)
            {
                currentState = PlayerState.Landing;
                stateTimer = 0;
            }

            // Handle movement states when grounded
            if (_isGrounded && currentState != PlayerState.Landing && 
                currentState != PlayerState.Jumping)
            {
                if (_moveAmount < 0.1f)
                {
                    currentState = PlayerState.Idle;
                }
                else if (_sprintInput && _moveAmount > 0.5f)
                {
                    currentState = PlayerState.Sprinting;
                }
                else if (_moveAmount >= 0.5f)
                {
                    currentState = PlayerState.Running;
                }
                else
                {
                    currentState = PlayerState.Walking;
                }
            }

            // Handle landing duration based on fall time
            if (currentState == PlayerState.Landing)
            {
                float landingDuration = Mathf.Lerp(0.3f, 0.8f, Mathf.Clamp01(_inAirTimer / 2f));
                if (stateTimer > landingDuration)
                {
                    currentState = PlayerState.Idle;
                    _inAirTimer = 0;
                }
            }
        }

        stateTimer += Time.deltaTime;
    }

    private void HandleMovement()
    {
        // If landing, don't process movement
        if (currentState == PlayerState.Landing)
        {
            return;
        }
        
        _moveDirection = cameraObject.forward * _verticalInput;
        _moveDirection += cameraObject.right * _horizontalInput;
        _moveDirection.Normalize();
        _moveDirection.y = 0;
        _moveAmount = Mathf.Clamp01(Mathf.Abs(_horizontalInput) + Mathf.Abs(_verticalInput));

        _targetMoveSpeed = currentState switch
        {
            PlayerState.Sprinting => sprintSpeed,
            PlayerState.Running => runSpeed,
            PlayerState.Walking => walkSpeed,
            _ => 0f
        };

        _currentMoveSpeed = Mathf.MoveTowards(_currentMoveSpeed, _targetMoveSpeed, acceleration * Time.deltaTime);
        
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
            _inAirTimer += Time.deltaTime;
            _verticalVelocity.y += gravity * Time.deltaTime;
        }
        else if (_verticalVelocity.y < 0)
        {
            _verticalVelocity.y = groundedGravity;
            if (currentState != PlayerState.Jumping && currentState != PlayerState.Landing)
            {
                _inAirTimer = 0;
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
        // Set the base state
        _animator.SetInteger(_stateHash, (int)currentState);

        // Update fall time parameter for blend trees
        float normalizedFallTime = Mathf.Clamp01(_inAirTimer / 2f); // Normalize to 0-1 range
        _animator.SetFloat(_fallTimeHash, normalizedFallTime);

        // Update movement values
        float verticalValue = currentState switch
        {
            PlayerState.Sprinting => 2f,
            PlayerState.Running => 1f,
            PlayerState.Walking => 0.5f,
            _ => 0f
        };

        _animator.SetFloat(_verticalHash, verticalValue, 0.1f, Time.deltaTime);
        _animator.SetFloat(_horizontalHash, 0, 0.1f, Time.deltaTime);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 spherePosition = transform.position + groundCheckOffset;
        Gizmos.DrawWireSphere(spherePosition, groundCheckRadius);
    }
}