using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    // Inputs
    public Vector2 MovementInput { get; private set; }
    public Vector2 MouseDelta { get; private set; }
    public bool JumpInput { get; private set; }
    public bool SprintInput { get; private set; }
    public bool WalkInput { get; private set; }
    public bool MoveSpeedToggleInput { get; private set; }
    public bool PlayerInteractInput { get; private set; }
    public bool RobotInteractInput { get; private set; }
    
    // Buffer settings
    [Header("Input Buffer Settings")]
    [SerializeField] private float jumpBufferTime = 0.2f;
    [SerializeField] private float interactBufferTime = 0.15f;
    
    // Buffer timers
    private float _jumpBufferCounter;
    private float _playerInteractBufferCounter;
    private float _robotInteractBufferCounter;
    
    // Constants
    public const float RotationInputThreshold = 0.01f;
    public const float SprintInputThreshold = 0.5f;
    public const float MovementInputThreshold = 0.1f;
    public const float MouseSensitivity = 1f;
    public const float FreeCameraSensitivity = 1f;
    public const float OverShoulderCameraSensitivity = 1f;
    
    private void Update()
    {
        GetInput();
        UpdateBuffers();
    }
    
    private void GetInput()
    {
        MovementInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );
        
        MouseDelta = new Vector2(
            Input.GetAxis("Mouse X"),
            Input.GetAxis("Mouse Y")
        );
        
        // Check for new input presses and update buffers
        if (Input.GetButtonDown("Jump"))
        {
            _jumpBufferCounter = jumpBufferTime;
        }
        
        if (Input.GetKeyDown(KeyCode.E))
        {
            _playerInteractBufferCounter = interactBufferTime;
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            _robotInteractBufferCounter = interactBufferTime;
        }
        
        // Update other inputs
        SprintInput = Input.GetButton("Sprint");
        WalkInput = Input.GetButton("Walk");
        MoveSpeedToggleInput = Input.GetKeyDown(KeyCode.CapsLock);
        
        // Set buffered inputs
        JumpInput = _jumpBufferCounter > 0;
        PlayerInteractInput = _playerInteractBufferCounter > 0;
        RobotInteractInput = _robotInteractBufferCounter > 0;
    }
    
    private void UpdateBuffers()
    {
        // Decrease buffer timers
        if (_jumpBufferCounter > 0)
        {
            _jumpBufferCounter -= Time.deltaTime;
        }
        
        if (_playerInteractBufferCounter > 0)
        {
            _playerInteractBufferCounter -= Time.deltaTime;
        }
        
        if (_robotInteractBufferCounter > 0)
        {
            _robotInteractBufferCounter -= Time.deltaTime;
        }
    }
    
    // Public methods to consume the buffered inputs
    public void ConsumeJumpBuffer()
    {
        _jumpBufferCounter = 0;
    }
    
    public void ConsumePlayerInteractBuffer()
    {
        _playerInteractBufferCounter = 0;
    }
    
    public void ConsumeRobotInteractBuffer()
    {
        _robotInteractBufferCounter = 0;
    }
}