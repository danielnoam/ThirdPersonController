using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Camera playerCamera;

    [Header("Movement Settings")]
    [SerializeField] private float runAcceleration = 50f;
    [SerializeField] private float runSpeed = 4f;
    [SerializeField] private float drag = 20f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Camera Settings")]
    [SerializeField] private float lookSensitivityH = 0.1f;
    [SerializeField] private float lookSensitivityV = 0.1f;
    [SerializeField] private float lookLimitV = 89f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundCheckDistance = 0.4f;
    [SerializeField] private float groundCheckRadius = 0.4f;
    [SerializeField] private Color groundCheckGizmoColor = Color.red;

    // Camera control
    private Vector2 _cameraRotation = Vector2.zero;
    private Vector2 _playerTargetRotation = Vector2.zero;
    
    // Ground check
    private Vector3 _spherePosition;
    private bool _isGrounded;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (playerCamera == null)
            playerCamera = Camera.main;

        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleGroundCheck();
        HandleMovement();
    }

    private void LateUpdate()
    {
        HandleCameraRotation();
    }

    private void HandleGroundCheck()
    {
        _spherePosition = transform.position;

        _isGrounded = Physics.SphereCast(
            _spherePosition,
            groundCheckRadius,
            Vector3.down,
            out RaycastHit hit,
            groundCheckDistance,
            groundMask);
    }

    private void HandleMovement()
    {
        // Get input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector2 movementInput = new Vector2(horizontal, vertical);

        // Get camera-relative directions
        Vector3 cameraForwardXZ = new Vector3(playerCamera.transform.forward.x, 0f, playerCamera.transform.forward.z).normalized;
        Vector3 cameraRightXZ = new Vector3(playerCamera.transform.right.x, 0f, playerCamera.transform.right.z).normalized;

        // Calculate movement direction relative to camera
        Vector3 movementDirection = cameraRightXZ * movementInput.x + cameraForwardXZ * movementInput.y;

        // Apply acceleration
        Vector3 movementDelta = movementDirection * runAcceleration * Time.deltaTime;
        Vector3 newVelocity = controller.velocity + movementDelta;

        // Apply drag
        Vector3 currentDrag = newVelocity.normalized * drag * Time.deltaTime;
        newVelocity = (newVelocity.magnitude > drag * Time.deltaTime) ? newVelocity - currentDrag : Vector3.zero;

        // Clamp to max speed
        newVelocity = Vector3.ClampMagnitude(newVelocity, runSpeed);

        // Handle jumping and gravity
        if (_isGrounded)
        {
            // Reset vertical velocity when grounded
            if (newVelocity.y < 0)
            {
                newVelocity.y = -2f; // Small downward force to maintain ground contact
            }

            // Jump when space is pressed
            if (Input.GetButtonDown("Jump"))
            {
                newVelocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            }
        }
        
        // Apply gravity
        newVelocity.y += gravity * Time.deltaTime;

        // Move character
        controller.Move(newVelocity * Time.deltaTime);
    }

    private void HandleCameraRotation()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // Update camera rotation
        _cameraRotation.x += lookSensitivityH * mouseX;
        _cameraRotation.y = Mathf.Clamp(_cameraRotation.y - lookSensitivityV * mouseY, -lookLimitV, lookLimitV);

        // Update player rotation (only Y axis)
        _playerTargetRotation.x = transform.eulerAngles.y + lookSensitivityH * mouseX;
        transform.rotation = Quaternion.Euler(0f, _playerTargetRotation.x, 0f);

        // Update camera rotation (both X and Y axis)
        playerCamera.transform.rotation = Quaternion.Euler(_cameraRotation.y, _cameraRotation.x, 0f);
    }

    private void OnDrawGizmos()
    {
        Vector3 position = Application.isPlaying ? _spherePosition : transform.position;
        
        // Draw the sphere at the start position
        Gizmos.color = groundCheckGizmoColor;
        Gizmos.DrawWireSphere(position, groundCheckRadius);
        
        // Draw a line showing the sweep path
        Vector3 endPosition = position + Vector3.down * groundCheckDistance;
        Gizmos.DrawLine(position, endPosition);
        
        // Draw the sphere at the end position
        Gizmos.DrawWireSphere(endPosition, groundCheckRadius);
    }
}