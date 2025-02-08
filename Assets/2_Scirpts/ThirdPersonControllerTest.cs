using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;
using CinemachineCamera = Unity.Cinemachine.CinemachineCamera;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonControllerTest : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 7f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 15f;
    [SerializeField] private float rotationSpeed = 720f;
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
    
    
    private CharacterController _controller;
    private bool _isGrounded;
    private float _defaultFov;
    private Quaternion _targetRotation;
    private Vector3 _moveDirection;
    private Vector3 _verticalVelocity;
    private Vector3 _lastMoveDirection;
    private float _currentMoveSpeed;
    private float _targetMoveSpeed;
    
    
    private float _horizontalInput;
    private float _verticalInput;
    private bool _jumpInput;
    private bool _sprintInput;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        if (cinemachineCamera)
        {
            _defaultFov = cinemachineCamera.Lens.FieldOfView;
        }
        _controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        GetPlayerInput();
        CheckGrounded();
        UpdateFov();
        
        HandleJump();
        HandleGravity();
        HandleMovement();
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
    
        // Calculate target speed based on input and sprint state
        float targetMaxSpeed = _sprintInput ? sprintSpeed : walkSpeed;
    
        if (input.magnitude > 0)
        {
            // Store the movement direction when there is input
            _lastMoveDirection = input;
            _moveDirection = input;
        
            // Accelerate
            _currentMoveSpeed = Mathf.MoveTowards(_currentMoveSpeed, targetMaxSpeed, acceleration * Time.deltaTime);
        
            // Handle rotation
            _targetRotation = Quaternion.LookRotation(_moveDirection);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                _targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
        else
        {
            // Use the last valid direction for deceleration
            _moveDirection = _lastMoveDirection;
        
            // Decelerate when no input
            _currentMoveSpeed = Mathf.MoveTowards(_currentMoveSpeed, 0, deceleration * Time.deltaTime);
        }

        // Apply movement with current speed
        _controller.Move(_moveDirection * (_currentMoveSpeed * Time.deltaTime));
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