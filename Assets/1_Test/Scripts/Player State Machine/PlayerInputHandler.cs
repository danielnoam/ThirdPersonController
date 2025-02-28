using UnityEngine;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("Settings")] 
    [SerializeField] private bool toggleMoveSpeedInput = true;
    [SerializeField] private bool toggleSprintInput = false;
    [SerializeField] private bool toggleCrouchInput = true;
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
    public bool CrouchInput { get; private set; }
    public bool MoveSpeedInput { get; private set; }
    public bool PlayerInteractInput { get; private set; }
    public bool RobotInteractInput { get; private set; }
    public bool AimInput { get; private set; }
    public float MouseSensitivity => mouseSensitivity;
    public float AimCameraSensitivity => aimCameraSensitivity;
    public float FreeCameraSensitivity => freeCameraSensitivity;
    public bool IsCrouchToggle => toggleCrouchInput;
    
    // Buffer timers
    private float _jumpBufferCounter;
    private float _playerInteractBufferCounter;
    private float _robotInteractBufferCounter;

    private void Update()
    {
        GetMovementInput();
        GetMouseInput();
        GetAimInput();
        GetJumpInput();
        GetInteractInput();
        GetCrouchInput();
        GetSprintInput();
        GetMoveSpeedInput();
    }
    

    private void GetMovementInput()
    {
        MovementInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );
    }

    private void GetMouseInput()
    {
        MouseDelta = new Vector2(
            Input.GetAxis("Mouse X"),
            Input.GetAxis("Mouse Y")
        );
    }

    private void GetAimInput()
    {
        if (toggleAimInput)
        {
            if (Input.GetMouseButtonDown(1)) 
            {
                AimInput = !AimInput;
            }
        } 
        else 
        {
            AimInput = Input.GetMouseButton(1);
        }
    }

    private void GetJumpInput()
    {
        if (Input.GetButtonDown("Jump"))
        {
            _jumpBufferCounter = jumpBufferTime;
        }
        
        // Set buffered input
        JumpInput = _jumpBufferCounter > 0;
        
        // Decrease buffer timer
        if (_jumpBufferCounter > 0)
        {
            _jumpBufferCounter -= Time.deltaTime;
        }
    }

    private void GetInteractInput()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            _playerInteractBufferCounter = interactBufferTime;
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            _robotInteractBufferCounter = interactBufferTime;
        }
        
        // Set buffered inputs
        PlayerInteractInput = _playerInteractBufferCounter > 0;
        RobotInteractInput = _robotInteractBufferCounter > 0;
        
        
        // Decrease buffer timers
        if (_playerInteractBufferCounter > 0)
        {
            _playerInteractBufferCounter -= Time.deltaTime;
        }
        
        if (_robotInteractBufferCounter > 0)
        {
            _robotInteractBufferCounter -= Time.deltaTime;
        }
    }

    private void GetCrouchInput()
    {
        if (toggleCrouchInput)
        {
            if (Input.GetButtonDown("Crouch")) 
            {
                CrouchInput = !CrouchInput;
            }
        } 
        else 
        {
            CrouchInput = Input.GetButton("Crouch");
        }
    }

    private void GetSprintInput()
    {
        if (toggleSprintInput)
        {
            if (Input.GetButtonDown("Sprint")) 
            {
                SprintInput = !SprintInput;
            }
        } 
        else 
        {
            SprintInput = Input.GetButton("Sprint");
        }
    }

    private void GetMoveSpeedInput()
    {
        if (toggleMoveSpeedInput)
        {
            if (Input.GetButtonDown("Walk")) 
            {
                MoveSpeedInput = !MoveSpeedInput;
            }
        } 
        else 
        {
            MoveSpeedInput = Input.GetButton("Walk");
        }
    }

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
    
    public void ConsumeCrouchInput()
    {
        CrouchInput = false;
    }
}