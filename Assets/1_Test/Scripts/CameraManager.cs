using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Serialization;

public class CameraManager : MonoBehaviour
{


    [Header("Camera Settings")]
    [SerializeField] private int freeLookCameraPriority = 10;
    [SerializeField] private int aimCameraPriority = 15;
    [Tooltip("Minimum mouse movement required to rotate character when aiming")]
    [SerializeField] private float aimRotationThreshold = 0.1f;

    [Header("Cursor")] 
    [SerializeField] private bool hideCursor = true;
    
    [Header("References")]
    [SerializeField] private CinemachineCamera freeLookCamera;
    [SerializeField] private CinemachineCamera aimCamera;
    [SerializeField] private GameObject aimCore;
    
    
    
    // Reference to player input for camera controls
    private PlayerInputHandler _playerInputHandler;
    private Vector3 _lastAimDirection = Vector3.forward;
    private float _pitchAccumulation = 0f;
    private float _yawAccumulation = 0f;
    public float AimRotationThreshold => aimRotationThreshold;

    private void Awake()
    {
        // Validate references
        if (freeLookCamera == null || aimCamera == null || aimCore == null)
        {
            Debug.LogError("Camera references not assigned to CameraManager!");
        }

        if (hideCursor)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        // Set initial camera priorities
        freeLookCamera.Priority = freeLookCameraPriority + 5; // Start with free look as active camera
        aimCamera.Priority = freeLookCameraPriority;
    }

    public void Initialize(PlayerInputHandler playerInputHandler)
    {
        _playerInputHandler = playerInputHandler;
        freeLookCamera.Follow = playerInputHandler.transform;
    }

    public void UpdateCameraPosition(Vector3 playerPosition)
    {
        // Update aim core position to follow the player
        if (!aimCore) return;
        
        aimCore.transform.position = playerPosition;
    }

    public void HandleCameraRotation()
    {
        if (!_playerInputHandler) return;

        // Get the appropriate sensitivity based on current camera state
        float cameraSensitivity = IsAimCameraActive() 
            ? _playerInputHandler.AimCameraSensitivity 
            : _playerInputHandler.FreeCameraSensitivity;
    
        // Accumulate rotation values
        _yawAccumulation += _playerInputHandler.MouseDelta.x * _playerInputHandler.MouseSensitivity * cameraSensitivity;
        _pitchAccumulation -= _playerInputHandler.MouseDelta.y * _playerInputHandler.MouseSensitivity * cameraSensitivity;
    
        // Optional: Clamp pitch to prevent camera flipping
        _pitchAccumulation = Mathf.Clamp(_pitchAccumulation, -89f, 89f);
    
        // Apply the accumulated rotation
        aimCore.transform.rotation = Quaternion.Euler(_pitchAccumulation, _yawAccumulation, 0f);
    }

    public void SwitchToAimCamera()
    {
        // Set camera priorities to switch to aim camera
        aimCamera.Priority = aimCameraPriority;
        freeLookCamera.Priority = freeLookCameraPriority;
        
        // When switching to aim camera, align it with the freelook camera
        if (aimCamera && freeLookCamera)
        {
            aimCamera.transform.rotation = freeLookCamera.transform.rotation;
        }
    }

    public void SwitchToFreeLookCamera()
    {
        // Reset camera priorities
        freeLookCamera.Priority = aimCameraPriority;
        aimCamera.Priority = freeLookCameraPriority;
    }

    public bool IsAimCameraActive()
    {
        return aimCamera.Priority > freeLookCamera.Priority;
    }

    public Vector3 GetCameraAimDirectionNoY()
    {
        // Use the active camera to determine the aim direction
        Transform activeCameraTransform = IsAimCameraActive() 
            ? aimCamera.transform 
            : freeLookCamera.transform;

        if (!activeCameraTransform) return _lastAimDirection; // Fallback if camera missing

        // Get forward direction and flatten it
        Vector3 cameraForward = activeCameraTransform.forward;
        cameraForward.y = 0; // Remove vertical tilt

        // Validate direction
        if (cameraForward.sqrMagnitude < 0.001f)
            return _lastAimDirection;

        // Normalize and store
        cameraForward.Normalize();
        _lastAimDirection = cameraForward;
        return cameraForward;
    }
    
    public Vector3 GetCameraAimDirection()
    {
        // Use the active camera to determine the aim direction
        Transform activeCameraTransform = IsAimCameraActive() 
            ? aimCamera.transform 
            : freeLookCamera.transform;

        if (!activeCameraTransform) return _lastAimDirection; // Fallback if camera missing

        // Get forward direction
        Vector3 cameraForward = activeCameraTransform.forward;

        // Validate direction
        if (cameraForward.sqrMagnitude < 0.001f)
            return _lastAimDirection;

        // Normalize and store
        cameraForward.Normalize();
        _lastAimDirection = cameraForward;
        return cameraForward;
    }

    public Vector3 CalculateMoveDirection(Vector2 movementInput)
    {
        // Get camera forward and right
        var forward = freeLookCamera.transform.forward;
        var right = freeLookCamera.transform.right;
    
        // Project onto horizontal plane
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        // Calculate movement direction relative to camera
        return (forward * movementInput.y + 
                right * movementInput.x).normalized;
    }
}