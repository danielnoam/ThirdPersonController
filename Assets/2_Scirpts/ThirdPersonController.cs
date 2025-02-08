using Unity.Cinemachine;
using UnityEngine;
using CinemachineCamera = Unity.Cinemachine.CinemachineCamera;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 7f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 15f;
    [SerializeField] private float rotationSpeed = 540f; 
    [SerializeField] private float turnSmoothTime = 0.2f; 
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedGravity = -2f;
    
    [Header("Ground Check")]
    [SerializeField] private float groundCheckRadius = 0.45f;
    [SerializeField] private Vector3 groundCheckOffset = new Vector3(0, 0.2f, 0);
    [SerializeField] private LayerMask groundLayer = 1;
    [SerializeField] private Color groundCheckGizmoColor = Color.red;
    
    [Header("Camera")]
    [SerializeField] private float sprintFovMultiplier = 1.2f;
    [SerializeField] private CinemachineCamera cinemachineCamera;

    [Header("Animation")]
    [SerializeField] private float maxWalkVelocity = 0.5f;
    [SerializeField] private float maxRunVelocity = 2.0f;
    [SerializeField] private float animationAcceleration = 2.0f;
    [SerializeField] private float animationDeceleration = 2.0f;
    
    private CharacterController _controller;
    private Animator _animator;
    private bool _isGrounded;
    private float _defaultFov;
    private Quaternion _targetRotation;
    private Vector3 _moveDirection;
    private Vector3 _verticalVelocity;
    private Vector3 _lastMoveDirection;
    private float _currentMoveSpeed;
    private float _turnSmoothVelocity;
    
    // Animation variables
    private int _velocityZHash;
    private int _velocityXHash;
    private float _velocityZ;
    private float _velocityX;
    
    // Input variables
    private float _horizontalInput;
    private float _verticalInput;
    private bool _jumpInput;
    private bool _sprintInput;
    private bool _forwardPressed;
    private bool _leftPressed;
    private bool _rightPressed;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        
        _velocityZHash = Animator.StringToHash("Velocity Z");
        _velocityXHash = Animator.StringToHash("Velocity X");
        
        if (cinemachineCamera)
        {
            _defaultFov = cinemachineCamera.Lens.FieldOfView;
        }
    }

    private void Update()
    {
        GetPlayerInput();
        CheckGrounded();
        UpdateFov();
        
        HandleJump();
        HandleGravity();
        HandleMovement();
        UpdateAnimations();
    }

    private void GetPlayerInput()
    {
        _horizontalInput = Input.GetAxisRaw("Horizontal");
        _verticalInput = Input.GetAxisRaw("Vertical");
        _jumpInput = Input.GetButtonDown("Jump");
        _sprintInput = Input.GetKey(KeyCode.LeftShift);
        
        _forwardPressed = Input.GetKey(KeyCode.W);
        _leftPressed = Input.GetKey(KeyCode.A);
        _rightPressed = Input.GetKey(KeyCode.D);
    }

    private void CheckGrounded()
    {
        Vector3 spherePosition = transform.position + groundCheckOffset;
        _isGrounded = Physics.CheckSphere(spherePosition, groundCheckRadius, groundLayer);
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
    
    private void HandleJump()
    {
        if (_isGrounded && _jumpInput)
        {
            _verticalVelocity.y = jumpForce;
        }
    }

    private void HandleMovement()
    {
        if (!cinemachineCamera) return;
    
        Vector3 forward = cinemachineCamera.transform.forward;
        Vector3 right = cinemachineCamera.transform.right;

        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 input = (forward * _verticalInput + right * _horizontalInput).normalized;
    
        float targetMaxSpeed = _sprintInput ? sprintSpeed : walkSpeed;
    
        if (input.magnitude > 0)
        {
            _lastMoveDirection = input;
            _moveDirection = input;
            
            // Calculate the target angle based on input direction
            float targetAngle = Mathf.Atan2(input.x, input.z) * Mathf.Rad2Deg;
            
            // Smoothly rotate towards the target angle
            float angle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetAngle,
                ref _turnSmoothVelocity,
                turnSmoothTime
            );
            
            // Apply the smooth rotation
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            
            _currentMoveSpeed = Mathf.MoveTowards(_currentMoveSpeed, targetMaxSpeed, acceleration * Time.deltaTime);
        }
        else
        {
            _moveDirection = _lastMoveDirection;
            _currentMoveSpeed = Mathf.MoveTowards(_currentMoveSpeed, 0, deceleration * Time.deltaTime);
        }

        _controller.Move(_moveDirection * (_currentMoveSpeed * Time.deltaTime));
    }

    // Rest of the methods remain the same...
    private void UpdateAnimations()
    {
        float currentMaxVelocity = _sprintInput ? maxRunVelocity : maxWalkVelocity;
        
        // Forward/Backward animation
        if (_forwardPressed && _velocityZ < currentMaxVelocity)
        {
            _velocityZ += Time.deltaTime * animationAcceleration;
        }
        
        if (!_forwardPressed && _velocityZ > 0.0f)
        {
            _velocityZ -= Time.deltaTime * animationDeceleration;
        }
        if (!_forwardPressed && _velocityZ < 0.0f)
        {
            _velocityZ = 0.0f;
        }
        
        // Left/Right animation
        if (_leftPressed && _velocityX > -currentMaxVelocity)
        {
            _velocityX -= Time.deltaTime * animationAcceleration;
        }
        if (_rightPressed && _velocityX < currentMaxVelocity)
        {
            _velocityX += Time.deltaTime * animationAcceleration;
        }
        
        // Deceleration for left/right
        if (!_leftPressed && _velocityX < 0.0f)
        {
            _velocityX += Time.deltaTime * animationDeceleration;
        }
        if (!_rightPressed && _velocityX > 0.0f)
        {
            _velocityX -= Time.deltaTime * animationDeceleration;
        }
        if (!_rightPressed && !_leftPressed && _velocityX != 0.0f && (_velocityX > -0.05f && _velocityX < 0.05f))
        {
            _velocityX = 0.0f;
        }
        
        // Clamp velocities
        _velocityZ = Mathf.Clamp(_velocityZ, 0, currentMaxVelocity);
        _velocityX = Mathf.Clamp(_velocityX, -currentMaxVelocity, currentMaxVelocity);
        
        // Update animator
        _animator.SetFloat(_velocityZHash, _velocityZ);
        _animator.SetFloat(_velocityXHash, _velocityX);
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
        Gizmos.color = groundCheckGizmoColor;
        Vector3 spherePosition = transform.position + groundCheckOffset;
        Gizmos.DrawWireSphere(spherePosition, groundCheckRadius);
    }
}