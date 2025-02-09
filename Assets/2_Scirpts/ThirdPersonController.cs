using Unity.Cinemachine;
using UnityEngine;
using CinemachineCamera = Unity.Cinemachine.CinemachineCamera;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 15f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private float turnSmoothTime = 0.2f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedGravity = -2f;
    
    [Header("Ground Check")]
    [SerializeField] private float groundCheckRadius = 0.23f;
    [SerializeField] private Vector3 groundCheckOffset = new Vector3(0, 0.2f, 0);
    [SerializeField] private LayerMask groundLayer = 1;
    
    [Header("Camera")]
    [SerializeField] private float sprintFovMultiplier = 1.2f;
    [SerializeField] private CinemachineCamera cinemachineCamera;

    [Header("Animation")]
    [SerializeField] private float maxWalkVelocity = 0.5f;
    [SerializeField] private float maxRunVelocity = 2.0f;
    [SerializeField] private float turnThreshold = 150f;
    [SerializeField] private float animationSmoothing = 10f;
    [SerializeField] private float turnAnimationDuration = 0.5f;
    
    private CharacterController _controller;
    private Animator _animator;
    private bool _isGrounded;
    private float _defaultFov;
    private Vector3 _moveDirection;
    private Vector3 _verticalVelocity;
    private Vector3 _previousMoveDirection;
    private float _currentMoveSpeed;
    
    
    // Animation    
    private int _velocityZHash;
    private int _velocityXHash;
    private int _turnAmountHash;
    private float _velocityZ;
    private float _velocityX;
    private float _turnAmount;
    private float _lastInputMagnitude;
    private float _turnResetTimer;
    private float _turnSmoothVelocity;
    
    // Input variables
    private float _horizontalInput;
    private float _verticalInput;
    private bool _jumpInput;
    private bool _sprintInput;

    private void Awake()
    {
        // Get components
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        
        // Cache animation hashes
        _velocityZHash = Animator.StringToHash("Velocity Z");
        _velocityXHash = Animator.StringToHash("Velocity X");
        _turnAmountHash = Animator.StringToHash("TurnAmount");
        
        // Initialize
        _previousMoveDirection = transform.forward;
        
        if (cinemachineCamera)
        {
            _defaultFov = cinemachineCamera.Lens.FieldOfView;
        }
        
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        GetPlayerInput();
        CheckGrounded();
        HandleMovement();
        HandleJump();
        HandleGravity();
        UpdateAnimations();
        UpdateFov();
    }

    private void GetPlayerInput()
    {
        _horizontalInput = Input.GetAxisRaw("Horizontal");
        _verticalInput = Input.GetAxisRaw("Vertical");
        _jumpInput = Input.GetButtonDown("Jump");
        _sprintInput = Input.GetKey(KeyCode.LeftShift);
    }

    private void CheckGrounded()
    {
        Vector3 spherePosition = transform.position + groundCheckOffset;
        _isGrounded = Physics.CheckSphere(spherePosition, groundCheckRadius, groundLayer);
    }

    private void HandleMovement()
    {
        if (!cinemachineCamera) return;
        
        // Get camera relative directions
        Vector3 forward = cinemachineCamera.transform.forward;
        Vector3 right = cinemachineCamera.transform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        // Calculate movement direction
        Vector3 moveDir = (forward * _verticalInput + right * _horizontalInput).normalized;
        float targetSpeed = _sprintInput ? sprintSpeed : walkSpeed;

        if (moveDir.magnitude > 0)
        {
            // Rotate towards movement direction
            float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            
            // Accelerate
            _currentMoveSpeed = Mathf.MoveTowards(_currentMoveSpeed, targetSpeed, acceleration * Time.deltaTime);
            _moveDirection = moveDir;
        }
        else
        {
            // Decelerate
            _currentMoveSpeed = Mathf.MoveTowards(_currentMoveSpeed, 0, deceleration * Time.deltaTime);
        }

        // Apply movement
        _controller.Move(_moveDirection * (_currentMoveSpeed * Time.deltaTime));
    }
    
    private void HandleJump()
    {
        if (_isGrounded && _jumpInput)
        {
            _verticalVelocity.y = jumpForce;
        }
    }

    private void HandleGravity()
    {
        if (_isGrounded && _verticalVelocity.y < 0)
        {
            _verticalVelocity.y = groundedGravity;
        }
        else
        {
            _verticalVelocity.y += gravity * Time.deltaTime;
        }

        _controller.Move(_verticalVelocity * Time.deltaTime);
    }

    private void UpdateAnimations()
    {
        float currentMaxVelocity = _sprintInput ? maxRunVelocity : maxWalkVelocity;
        Vector2 input = new Vector2(_horizontalInput, _verticalInput);
        float inputMagnitude = input.magnitude;
        
        // Check for direction change only when moving
        if (inputMagnitude > 0.1f)
        {
            Vector3 currentMoveDirection = new Vector3(input.x, 0, input.y).normalized;
            
            // Only check for turns if we were already moving
            if (_lastInputMagnitude > 0.1f)
            {
                float turnAngle = Vector3.Angle(_previousMoveDirection, currentMoveDirection);
                
                // Check if input direction reversed
                float dotProduct = Vector3.Dot(_previousMoveDirection, currentMoveDirection);
                if (turnAngle > turnThreshold && dotProduct < 0)
                {
                    // Trigger turn
                    _turnAmount = 1f;
                    _turnResetTimer = turnAnimationDuration;
                }
            }
            
            _previousMoveDirection = currentMoveDirection;
            
            // Calculate target velocities - ensure they reach max values
            Vector2 normalizedInput = input.normalized;
            float targetVelocityX = normalizedInput.x * currentMaxVelocity;
            float targetVelocityZ = normalizedInput.y * currentMaxVelocity;
            
            // Faster acceleration to reach targets
            _velocityX = Mathf.MoveTowards(_velocityX, targetVelocityX, Time.deltaTime * currentMaxVelocity * animationSmoothing);
            _velocityZ = Mathf.MoveTowards(_velocityZ, targetVelocityZ, Time.deltaTime * currentMaxVelocity * animationSmoothing);
        }
        else
        {
            // Quick deceleration to zero
            _velocityX = Mathf.MoveTowards(_velocityX, 0f, Time.deltaTime * currentMaxVelocity * animationSmoothing);
            _velocityZ = Mathf.MoveTowards(_velocityZ, 0f, Time.deltaTime * currentMaxVelocity * animationSmoothing);
        }
        
        // Handle turn animation timing
        if (_turnResetTimer > 0)
        {
            _turnResetTimer -= Time.deltaTime;
            if (_turnResetTimer <= 0)
            {
                _turnAmount = 0f;
            }
        }
        
        // Store current input magnitude for next frame
        _lastInputMagnitude = inputMagnitude;
        
        // Update animator with all values
        _animator.SetFloat(_velocityXHash, _velocityX);
        _animator.SetFloat(_velocityZHash, _velocityZ);
        _animator.SetFloat(_turnAmountHash, _turnAmount);
    }
    
    private void UpdateFov()
    {
        if (cinemachineCamera)
        {
            float speedRatio = Mathf.InverseLerp(walkSpeed, sprintSpeed, _currentMoveSpeed);
            float targetFov = Mathf.Lerp(_defaultFov, _defaultFov * sprintFovMultiplier, speedRatio);
            cinemachineCamera.Lens.FieldOfView = targetFov;
        }
    }

    private void OnDrawGizmos()
    {
        // Draw ground check sphere
        Gizmos.color = Color.red;
        Vector3 spherePosition = transform.position + groundCheckOffset;
        Gizmos.DrawWireSphere(spherePosition, groundCheckRadius);
    }
}