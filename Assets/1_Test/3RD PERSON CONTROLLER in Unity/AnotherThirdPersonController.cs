using System;
using Unity.Cinemachine;
using UnityEngine;

public class AnotherThirdPersonController : MonoBehaviour
{
    


    [Header("Movement")]
    public Transform cameraObject;
    public float walkSpeed = 4f;
    public  float runSpeed = 8f;
    public float sprintSpeed = 15f;
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
    private bool _isSprinting;
    private bool _isJumping;
    private bool _isLocked;
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
    private int _isLockedHash;
    private int _isJumpingHash;




    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        _horizontalHash = Animator.StringToHash("Horizontal");
        _verticalHash = Animator.StringToHash("Vertical");
        _isLockedHash = Animator.StringToHash("isLocked");
        _isJumpingHash = Animator.StringToHash("isJumping");
    }


    private void Update()
    {
        
        CheckGrounded();

        if (!_isLocked)
        {
            GetPlayerInput();
            HandleMovement();
            HandleRotation();
            HandleJump();
        }
        
        HandleGravity();
        UpdateAnimationValues(0, _moveAmount, _isSprinting);
    }

    private void LateUpdate()
    {
        _isLocked = _animator.GetBool(_isLockedHash);
        _isJumping = _animator.GetBool(_isJumpingHash);
        _animator.SetBool("isGrounded", _isGrounded);
    }

    private void GetPlayerInput()
    {
        _horizontalInput = Input.GetAxisRaw("Horizontal");
        _verticalInput = Input.GetAxisRaw("Vertical");
        _jumpInput = Input.GetButtonDown("Jump");
        _sprintInput = Input.GetButton("Sprint");
    }
    private void HandleMovement()
    {
        _moveDirection = cameraObject.forward * _verticalInput;
        _moveDirection += cameraObject.right * _horizontalInput;
        _moveDirection.Normalize();
        _moveDirection.y = 0;
        _moveAmount = Mathf.Clamp01(Mathf.Abs(_horizontalInput) + Mathf.Abs(_verticalInput));

        if (_sprintInput && _moveAmount > 0.5f)
        {
            _targetMoveSpeed = sprintSpeed;
            _isSprinting = true;
            
        } else {
            
            _targetMoveSpeed = _moveAmount >= 0.5f ? runSpeed : walkSpeed;
            _isSprinting = false;
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
            _animator.SetBool(_isJumpingHash, true);
            PlayTargetAnimation("Jump", false);
            _verticalVelocity.y = jumpForce;
        }
    }

    private void HandleGravity()
    {
        if (!_isGrounded)
        {
            if (!_isLocked && !_isJumping)
            {
                PlayTargetAnimation("Falling", false);
            }
            
            _inAirTimer += Time.deltaTime;
            _verticalVelocity.y += gravity * Time.deltaTime;
            
        } else if (_isGrounded && _verticalVelocity.y < 0) {
            
            _verticalVelocity.y = groundedGravity;
        }

        _controller.Move(_verticalVelocity * Time.deltaTime);
    }
    
    private void CheckGrounded()
    {
        if (!_isGrounded && !_isLocked && !_isJumping)
        {
            PlayTargetAnimation("Land", true);
            _inAirTimer = 0;
        }
        
        Vector3 spherePosition = transform.position + groundCheckOffset;
        _isGrounded = Physics.CheckSphere(spherePosition, groundCheckRadius, groundLayer);
    }

    private void UpdateAnimationValues(float horizontal, float vertical, bool sprinting)
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
        
        if (sprinting)
        {
            snappedVertical = 2;
        }

        _animator.SetFloat(_horizontalHash, snappedHorizontal, 0.1f ,Time.deltaTime);
        _animator.SetFloat(_verticalHash, snappedVertical, 0.1f ,Time.deltaTime);
    }

    private void PlayTargetAnimation(string targetAnimation, bool isLocked)
    {
        _animator.SetBool(_isLockedHash, isLocked);
        _animator.CrossFade(targetAnimation, 0.2f);
    }

    
    private void OnDrawGizmos()
    {
        // Draw ground check sphere
        Gizmos.color = Color.red;
        Vector3 spherePosition = transform.position + groundCheckOffset;
        Gizmos.DrawWireSphere(spherePosition, groundCheckRadius);
    }

}
