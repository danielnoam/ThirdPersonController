
using UnityEngine;




public class RobotStateMachine : MonoBehaviour
{
    public RobotBaseState CurrentState { get; private set; }
    public RobotFollowState RobotFollowState { get; private set; }
    
    
    public Transform target;        // Target to follow
   
    [Header("Movement")]
    public float moveSpeed = 5f;    // Movement speed
    public float smoothness = 0.3f; // Lower = more responsive, Higher = smoother
   
    [Header("Rotation")]
    public float rotationSmoothness = 5f; // Control rotation smoothing
    public float maxPitchAngle = 45f; // Maximum up/down rotation angle
   
    [Header("Hover")]
    public float hoverAmplitude = 0.5f;  // How high it hovers
    public float hoverFrequency = 1f;    // How fast it hovers

    [Header("Ears")]
    public Transform leftEar;       // Left ear/antenna transform
    public Transform rightEar;      // Right ear/antenna transform
    public float maxEarAngle = 45f; // Maximum ear rotation angle
    public float earRotationSmoothness = 0.2f; // Ear rotation smoothness
    public float minSpeedForEarRotation = 0.1f; // Minimum speed to start ear rotation
    public float maxSpeedForEarRotation = 4f;   // Speed at which ears reach max rotation
    
    private void Awake()
    {

        // Initialize states
        RobotFollowState = new RobotFollowState(this);

        // Set initial state
        SwitchState(RobotFollowState);
    }

    private void Update()
    {
        CurrentState.UpdateState();
    }

    private void FixedUpdate()
    {
        CurrentState.FixedUpdateState();
    }
    
    

    private void SwitchState(RobotBaseState newState)
    {
        CurrentState?.ExitState();
        CurrentState = newState;
        CurrentState.EnterState();

        // Debug state changes
        Debug.Log($"Switched to {newState.GetType().Name}");
    }
    
    
    
}