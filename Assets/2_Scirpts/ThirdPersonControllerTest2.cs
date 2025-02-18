using Unity.Cinemachine;
using UnityEngine;
using CinemachineCamera = Unity.Cinemachine.CinemachineCamera;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class ThirdPersonControllerTest2 : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float sprintSpeed = 11f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 15f;
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
    [SerializeField] private float maxWalkVelocity = 5f;
    [SerializeField] private float maxRunVelocity = 10f;
    [SerializeField] private float animationSmoothing = 2f;
    
    private CharacterController _controller;
    private Animator _animator;
    private bool _isGrounded;
    private float _defaultFov;
    private Vector3 _moveDirection;
    private Vector3 _verticalVelocity;
    private float _currentMoveSpeed;
    private float _smoothedMoveSpeed;
    private float _turnSmoothVelocity;
    
    // Animation    
    private int _moveSpeedHash;
    private int _turnTriggerHash;
    private int _isRunningHash;
    private Vector2 _lastInputDirection;
    
    
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
        _moveSpeedHash = Animator.StringToHash("MoveSpeed");
        _turnTriggerHash = Animator.StringToHash("TurnTrigger");
        _isRunningHash = Animator.StringToHash("IsRunning");
        
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
        Vector2 currentInput = new Vector2(_horizontalInput, _verticalInput);
        
        // Check for sharp turns when we have input
        if (currentInput.magnitude > 0.1f && _lastInputDirection.magnitude > 0.1f)
        {
            float angle = Vector2.Angle(_lastInputDirection, currentInput);
            
            // If sharp turn detected
            if (angle > 150f)
            {
                // Set whether we're running, then trigger the turn
                _animator.SetBool(_isRunningHash, _sprintInput);
                _animator.SetTrigger(_turnTriggerHash);
            }
        }
        
        // Store current input for next frame
        _lastInputDirection = currentInput;

        // Update regular movement animation
        float currentMaxVelocity = _sprintInput ? maxRunVelocity : maxWalkVelocity;
        float normalizedSpeed = _currentMoveSpeed / (_sprintInput ? sprintSpeed : walkSpeed);
        float targetAnimationSpeed = normalizedSpeed * currentMaxVelocity;
        
        _smoothedMoveSpeed = Mathf.MoveTowards(_smoothedMoveSpeed, targetAnimationSpeed, 
            Time.deltaTime * currentMaxVelocity * animationSmoothing);
        
        _animator.SetFloat(_moveSpeedHash, _smoothedMoveSpeed);
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