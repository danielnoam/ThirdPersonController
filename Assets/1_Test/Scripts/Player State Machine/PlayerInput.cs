using UnityEngine;

public class PlayerInput : MonoBehaviour
{

    
    [Header("Settings")] 
    [SerializeField] private bool toggleMoveSpeedInput = true;
    [SerializeField] private bool toggleSprintInput = false;
    [SerializeField] private bool toggleAimInput = false;
    [SerializeField, Min(0f)] private float jumpBufferTime = 0.2f;
    [SerializeField, Min(0f)] private float interactBufferTime = 0.15f;
    [SerializeField, Range(0.1f, 2f)] private float mouseSensitivity = 1f;
    [SerializeField, Range(0.1f, 2f)] private float freeCameraSensitivity = 1f;
    [SerializeField, Range(0.1f, 2f)] private float aimCameraSensitivity = 1f;
    public const float RotationInputThreshold = 0.01f;
    public const float SprintInputThreshold = 0.5f;
    public const float MovementInputThreshold = 0.1f;
    
    public Vector2 MovementInput { get; private set; }
    public Vector2 MouseDelta { get; private set; }
    public bool JumpInput { get; private set; }
    public bool SprintInput { get; private set; }
    public bool WalkInput { get; private set; }
    public bool MoveSpeedInput { get; private set; }
    public bool PlayerInteractInput { get; private set; }
    public bool RobotInteractInput { get; private set; }
    public bool AimInput { get; private set; }
    public float MouseSensitivity => mouseSensitivity;
    public float AimCameraSensitivity => aimCameraSensitivity;
    public float FreeCameraSensitivity => freeCameraSensitivity;
    
    
    // Buffer timers
    private float _jumpBufferCounter;
    private float _playerInteractBufferCounter;
    private float _robotInteractBufferCounter;

    
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
        
        if (toggleAimInput)
        {
            if (Input.GetMouseButton(1)) {
                AimInput = !AimInput;
            }
        } else {
            AimInput = Input.GetMouseButton(1);
        }
        
        
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
        
        
        if (toggleSprintInput)
        {
            if (Input.GetButton("Sprint")) {
                SprintInput = !SprintInput;
            }
        } else {
            SprintInput = Input.GetButton("Sprint");
        }

        if (toggleMoveSpeedInput)
        {
            if (Input.GetKeyDown(KeyCode.CapsLock)) {
                MoveSpeedInput = !MoveSpeedInput;
            }
        } else {
            MoveSpeedInput = Input.GetKeyDown(KeyCode.CapsLock);
        }
        
        WalkInput = Input.GetButton("Walk");

        
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