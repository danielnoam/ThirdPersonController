using Unity.Cinemachine;
using UnityEngine;

public class AnotherThirdPersonController : MonoBehaviour
{
    

    [Header("Movement")]
    public Transform cameraObject;
    public float walkSpeed = 4f;
    public  float runSpeed = 7f;
    public float sprintSpeed = 10f;
    public float acceleration = 3f;
    public float rotationSpeed = 15f;
    public float jumpForce = 8f;
    
    [Header("Gravity")]
    public float gravity = -20f;
    public float groundedGravity = -2f;
    
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
    
    
    private float _horizontalInput;
    private float _verticalInput;
    private bool _jumpInput;
    private bool _sprintInput;
    
    private int _horizontalHash;
    private int _verticalHash;




    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        _horizontalHash = Animator.StringToHash("Horizontal");
        _verticalHash = Animator.StringToHash("Vertical");
    }


    private void Update()
    {
        GetPlayerInput();
        CheckGrounded();
        
        
        HandleMovement();
        HandleRotation();
        HandleJump();
        HandleGravity();
        
        
        UpdateAnimationValues(0, _moveAmount);
    }
    
    private void GetPlayerInput()
    {
        _horizontalInput = Input.GetAxisRaw("Horizontal");
        _verticalInput = Input.GetAxisRaw("Vertical");
        _jumpInput = Input.GetButtonDown("Jump");
        _sprintInput = Input.GetKey(KeyCode.LeftShift);
    }
    private void HandleMovement()
    {
        _moveDirection = cameraObject.forward * _verticalInput;
        _moveDirection += cameraObject.right * _horizontalInput;
        _moveDirection.Normalize();
        _moveDirection.y = 0;
        _moveAmount = Mathf.Clamp01(Mathf.Abs(_moveDirection.x) + Mathf.Abs(_moveDirection.y));

        if (_sprintInput)
        {
            _targetMoveSpeed = sprintSpeed;
        }
        else
        {
            if (_moveAmount >= 0.5f)
            {
                _targetMoveSpeed = runSpeed;
            }
            else
            {
                _targetMoveSpeed = walkSpeed;
            }
        }

        
        _currentMoveSpeed = Mathf.MoveTowards(_currentMoveSpeed, _targetMoveSpeed, acceleration * Time.deltaTime);
        
        _controller.Move(_moveDirection * (_currentMoveSpeed * Time.deltaTime));
    }

    private void HandleRotation()
    {
        Vector3 targetDirection = Vector3.zero;
        targetDirection = cameraObject.forward * _verticalInput;
        targetDirection = targetDirection + cameraObject.right * _horizontalInput;
        targetDirection.Normalize();
        targetDirection.y = 0;
        
        if (targetDirection == Vector3.zero)
        {
            targetDirection = transform.forward;
        }
        
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        Quaternion playerRotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        transform.rotation = playerRotation;
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
    
    private void CheckGrounded()
    {
        Vector3 spherePosition = transform.position + groundCheckOffset;
        _isGrounded = Physics.CheckSphere(spherePosition, groundCheckRadius, groundLayer);
    }

    private void UpdateAnimationValues(float horizontal, float vertical)
    {
        var snappedHorizontal = horizontal switch
        {
            > 0 and < 0.55f => 0.5f,
            > 0.55f => 1,
            < 0 and > -0.55f => -0.5f,
            < -0.55f => -1,
            _ => 0
        };
        
        var snappedVertical = vertical switch
        {
            > 0 and < 0.55f => 0.5f,
            > 0.55f => 1,
            < 0 and > -0.55f => -0.5f,
            < -0.55f => -1,
            _ => 0
        };

        _animator.SetFloat(_horizontalHash, snappedHorizontal, 0.1f ,Time.deltaTime);
        _animator.SetFloat(_verticalHash, snappedVertical, 0.1f ,Time.deltaTime);
    }

    
    private void OnDrawGizmos()
    {
        // Draw ground check sphere
        Gizmos.color = Color.red;
        Vector3 spherePosition = transform.position + groundCheckOffset;
        Gizmos.DrawWireSphere(spherePosition, groundCheckRadius);
    }

}
