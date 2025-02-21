using System;
using UnityEngine;
using VInspector;


public enum RobotState
{
    Idle = 0,
    GoToTarget = 1,
    FollowPlayer = 2,
    Off = 3,
}

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class RobotCompanion : MonoBehaviour
{
    
    [Tab("Movement")]
    [Header("Movement")]
    [Tooltip("Base movement speed of the robot")]
    [SerializeField] private float moveSpeed = 5f;
    
    [Tooltip("Minimum distance to maintain from target")]
    [SerializeField] private float minFollowDistance = 1.5f;
    
    [Tooltip("Maximum distance before reaching max speed")]
    [SerializeField] private float maxFollowDistance = 5f;
    
    [Tooltip("How smoothly the robot accelerates and decelerates")]
    [SerializeField] private float followSmoothness = 0.2f;
    
    [Tooltip("Animation curve controlling how speed changes based on distance")]
    [SerializeField] private AnimationCurve followCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Tooltip("Friction coefficient applied when the robot has no target")]
    [SerializeField] private float friction = 1f;
    
    [Tooltip("How quickly the robot returns to its desired rotation")]
    [SerializeField] private float rotationStability = 2f;
    
    [Tooltip("Maximum angular velocity for rotation")]
    [SerializeField] private float maxAngularVelocity = 3f;
    
    [Tooltip("Animation curve controlling how rotation speed changes based on angle difference")]
    [SerializeField] private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Hover")]
    [Tooltip("Maximum distance the robot will hover up and down")]
    [SerializeField] private float hoverAmplitude = 0.3f;
    
    [Tooltip("Speed of the hover movement cycle")]
    [SerializeField] private float hoverFrequency = 1f;
    
    [Tooltip("Minimum vertical distance change required before the robot adjusts its height")]
    [SerializeField] private float verticalThreshold = 2f;
    
    [Tooltip("Height maintained above ground")]
    [SerializeField] private float baseHeight = 1.5f;
    
    [Tooltip("Layers that the robot considers as ground for hover calculations")]
    [SerializeField] private LayerMask groundLayer;
    [EndTab]



    [Tab("Ears")] 
    [SerializeField] private bool rotateEars = true;
    [Tooltip("Transform reference for the left ear/antenna")]
    [SerializeField] private Transform leftEarPivot;
    
    [Tooltip("Transform reference for the right ear/antenna")]
    [SerializeField] private Transform rightEarPivot;
    
    [Tooltip("Maximum rotation angle for forward/backward tilt (X axis)")]
    [SerializeField] private float maxForwardTilt = 45f;

    [Tooltip("Maximum rotation angle for left/right rotation (Y axis)")]
    [SerializeField] private float maxSidewaysTilt = 30f;

    [Tooltip("Maximum rotation angle for side tilt (Z axis)")]
    [SerializeField] private float maxTwistTilt = 15f;
    
    [Tooltip("How smoothly the ears rotate in response to movement")]
    [SerializeField] private float earRotationSmoothness = 0.2f;
    
    [Tooltip("Minimum speed required to start ear rotation")]
    [SerializeField] private float minSpeedForEarRotation = 0.1f;
    
    [Tooltip("Speed at which ears reach their maximum rotation")]
    [SerializeField] private float maxSpeedForEarRotation = 4f;
    [EndTab]

    [Header("References")]
    [Tooltip("Reference to the robot's Rigidbody component")]
    [SerializeField] private Rigidbody rigidBody;

    [Header("Debug")]
    [Tooltip("Current operational state of the robot")]
    [SerializeField, ReadOnly] private RobotState currentState;
    [Tooltip("Current battery level (0-100)")]
    [SerializeField, ReadOnly] private int battery = 100;
    
    
    
    private PlayerStateMachine _player;
    private Transform _playerTransform;
    private Transform _playerFollowPosition;
    private Transform _target;
    private Vector3 _leftEarRotation;
    private Vector3 _rightEarRotation;
    private Vector3 _currentVelocity;
    private float _currentDesiredHeight;
    private float _hoverTime;
    private Vector3 _lastPosition;
    private Quaternion _leftEarBaseRotation;
    private Quaternion _rightEarBaseRotation;

   private void Awake()
   {
       if (!rigidBody) rigidBody = GetComponent<Rigidbody>();
       if (leftEarPivot) _leftEarBaseRotation = leftEarPivot.localRotation;
       if (rightEarPivot) _rightEarBaseRotation = rightEarPivot.localRotation;
   }

   private void Start()
   {
       _player = GameObject.Find("Player").GetComponent<PlayerStateMachine>();
       _playerTransform = _player.transform;
       _playerFollowPosition = _playerTransform.GetChild(2);
       TurnOn();
       FollowPlayer();
   }


   private void Update()
   {
       if (IsOn())
       {
           CheckBattery();
       }
   }

   private void FixedUpdate()
   {
       if (IsOn())
       {
           Hover();
           HandleRotation();
           ApplyFriction();
           UpdateEarRotation();
           
           switch (currentState)
           {
               case RobotState.GoToTarget:
                   break;
               case RobotState.FollowPlayer:
                   Follow();
                   break;
           }
       }
   }




   #region States ------------------------------------------------------------------------------

   
   [Button]
   public void FollowPlayer()
   {
       if (!_player || !IsOn()) return;
       currentState = RobotState.FollowPlayer;
   }
   
   public void GoToTarget(Transform targetToGoTo)
   {
       if (!targetToGoTo || !IsOn()) return;
       
       _target = targetToGoTo;
       currentState = RobotState.GoToTarget;
   }
   
   [Button]
   private void Idle()
   {
       if (!IsOn()) return;
       
       _target = null;
       currentState = RobotState.Idle;
   }
   
   [Button]
   private void TurnOff()
   {
       currentState = RobotState.Off;
       rigidBody.useGravity = true;
   }

   [Button]
   private void TurnOn()
   {
       currentState = RobotState.Idle;
       battery = 100;
       rigidBody.useGravity = false;
   }

   #endregion States ------------------------------------------------------------------------------


   #region Movement -------------------------------------------------------------------


   private void Follow()
   {
       if (!_playerFollowPosition) return;

       // Calculate the direction to the target in the horizontal plane only
       Vector3 targetPosition = new Vector3(_playerFollowPosition.position.x, transform.position.y, _playerFollowPosition.position.z);
       Vector3 directionToTarget = (targetPosition - transform.position);
       
       // Calculate distance to target
       float distanceToTarget = directionToTarget.magnitude;
       
       // Get current horizontal velocity
       Vector3 currentHorizontalVelocity = new Vector3(
           rigidBody.linearVelocity.x,
           0f,
           rigidBody.linearVelocity.z
       );
       
       // Calculate desired velocity
       Vector3 desiredVelocity = Vector3.zero;
       
       if (distanceToTarget > minFollowDistance)
       {
           // Normalize the distance between min and max follow distance
           float normalizedDistance = Mathf.Clamp01(
               (distanceToTarget - minFollowDistance) / (maxFollowDistance - minFollowDistance)
           );
           
           // Apply the curve to get the speed multiplier
           float speedMultiplier = followCurve.Evaluate(normalizedDistance);
           
           // Calculate base desired velocity
           Vector3 moveDirection = directionToTarget.normalized;
           desiredVelocity = moveDirection * (moveSpeed * speedMultiplier);
       }
       
       // Calculate damping force
       Vector3 dampingForce = -currentHorizontalVelocity * followSmoothness;
       
       // Calculate acceleration needed to reach desired velocity
       Vector3 acceleration = (desiredVelocity - currentHorizontalVelocity) * (1f - followSmoothness);
       
       // Combine forces
       Vector3 totalForce = acceleration + dampingForce;
       
       // Apply forces over time
       Vector3 velocityChange = totalForce * Time.fixedDeltaTime;
       
       // Create new velocity vector, preserving Y component (handled by hover)
       Vector3 newVelocity = new Vector3(
           currentHorizontalVelocity.x + velocityChange.x,
           rigidBody.linearVelocity.y,
           currentHorizontalVelocity.z + velocityChange.z
       );
       
       // Apply final velocity
       rigidBody.linearVelocity = newVelocity;
   }
   
   
   private void Hover()
   {
       float currentY = rigidBody.position.y;
       float targetHeight;

       // Always check ground distance from current Y position
       RaycastHit hit;
       bool groundFound = Physics.Raycast(new Vector3(rigidBody.position.x, currentY, rigidBody.position.z), 
           Vector3.down, out hit, Mathf.Infinity, groundLayer);
       float groundHeight = groundFound ? hit.point.y : currentY;

       if (currentState == RobotState.FollowPlayer)
       {
           // Calculate vertical distance between robot and target
           float verticalDistance = Mathf.Abs(_playerFollowPosition.position.y - currentY);
        
           if (verticalDistance < verticalThreshold)
           {
               // If vertical distance is below threshold, maintain current height
               targetHeight = _currentDesiredHeight;
           }
           else
           {
               // Otherwise follow target height, but never go below base height from ground
               targetHeight = Mathf.Max(_playerFollowPosition.position.y, groundHeight + baseHeight);
           }
       }
       else
       {
           // When no target, hover at baseHeight above ground
           targetHeight = groundHeight + baseHeight;
       }

       // Smoothly move the desired height
       _currentDesiredHeight = Mathf.Lerp(_currentDesiredHeight, targetHeight, moveSpeed * 0.1f * Time.fixedDeltaTime);

       // Update hover time
       _hoverTime += Time.fixedDeltaTime;
    
       // Calculate target position with hover
       float hoverOffset = Mathf.Sin(_hoverTime * hoverFrequency) * hoverAmplitude;
       float finalDesiredHeight = _currentDesiredHeight + hoverOffset;

       // Calculate velocity needed to reach position
       float distanceToDesired = finalDesiredHeight - currentY;
       float finalVelocity = distanceToDesired * moveSpeed* 0.1f;

       // Apply the new velocity while preserving X and Z
       Vector3 currentVelocity = rigidBody.linearVelocity;
       rigidBody.linearVelocity = new Vector3(currentVelocity.x, finalVelocity, currentVelocity.z);
   }

   private void ApplyFriction()
   {
       // Only apply friction when there's no target
       if (_target == null)
       {
           // Get current velocity
           Vector3 currentVelocity = rigidBody.linearVelocity;
        
           // Calculate friction force for X and Z components
           float frictionX = -currentVelocity.x * friction * Time.fixedDeltaTime;
           float frictionZ = -currentVelocity.z * friction * Time.fixedDeltaTime;
        
           // Create new velocity vector, preserving Y component (handled by hover)
           Vector3 newVelocity = new Vector3(
               currentVelocity.x + frictionX,
               currentVelocity.y,
               currentVelocity.z + frictionZ
           );
        
           // Apply the new velocity
           rigidBody.linearVelocity = newVelocity;
       }
   }
   
   private void HandleRotation()
   {
       if (!IsOn()) return;

       Quaternion targetRotation;
    
       if (currentState == RobotState.FollowPlayer)
       {
           // If we have a target, calculate rotation to face it
           Vector3 directionToTarget = ((_player.transform.position + new Vector3(0,0.5f, 0)) - transform.position).normalized;
           targetRotation = Quaternion.LookRotation(directionToTarget);
           
       }
       else
       {
           // Default to facing forward
           targetRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
       }

       // Get current angular velocity
       Vector3 currentAngularVelocity = rigidBody.angularVelocity;

       // Calculate the angle difference
       float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);
    
       // Normalize the difference to 0-1 range for the curve
       float normalizedDifference = Mathf.Clamp01(angleDifference / 180f);
    
       // Apply the curve to get the stabilization strength
       float curveMultiplier = rotationCurve.Evaluate(normalizedDifference);

       // Calculate stabilization torque
       Vector3 stabilizationTorque = Vector3.zero;
    
       // Apply torque to counter current angular velocity
       stabilizationTorque -= currentAngularVelocity * rotationStability;
    
       // Add torque towards target rotation
       Vector3 rotationAxis;
       float rotationAngle;
       (targetRotation * Quaternion.Inverse(transform.rotation)).ToAngleAxis(out rotationAngle, out rotationAxis);
    
       if (!float.IsNaN(rotationAngle))
       {
           stabilizationTorque += rotationAxis.normalized * (rotationAngle * rotationStability * curveMultiplier);
       }

       // Clamp the maximum angular velocity
       rigidBody.maxAngularVelocity = maxAngularVelocity;
    
       // Apply the final torque
       rigidBody.AddTorque(stabilizationTorque, ForceMode.Acceleration);
   }
   

    
   #endregion Movement -------------------------------------------------------------------


   #region Utility ------------------------------------------------------------------------
   
   private void UpdateEarRotation()
{
    if (!leftEarPivot || !rightEarPivot) return;
    if (!rotateEars) return;
    

    // Calculate velocity and rotation
    _currentVelocity = (transform.position - _lastPosition) / Time.fixedDeltaTime;
    _lastPosition = transform.position;
    Vector3 angularVelocity = rigidBody.angularVelocity;

    // Remove vertical movement by projecting velocity onto horizontal plane
    Vector3 horizontalVelocity = Vector3.ProjectOnPlane(_currentVelocity, Vector3.up);
    
    // Get speeds (using only horizontal movement)
    float movementSpeed = horizontalVelocity.magnitude;
    float rotationSpeed = angularVelocity.magnitude;

    // Calculate movement influence (ignoring vertical movement)
    Vector3 moveDirection = (movementSpeed > 0.001f) ? -horizontalVelocity.normalized : Vector3.zero;
    
    // Calculate rotation influence
    Vector3 rotateDirection = (rotationSpeed > 0.001f) ? -angularVelocity.normalized : Vector3.zero;

    // Combine influences and calculate total intensity
    Vector3 totalInfluence = moveDirection + rotateDirection;
    float totalSpeed = Mathf.Max(
        Mathf.InverseLerp(minSpeedForEarRotation, maxSpeedForEarRotation, movementSpeed),
        Mathf.InverseLerp(0, maxAngularVelocity, rotationSpeed)
    );

    // Convert influence to local space to apply different limits per axis
    Vector3 localInfluence = transform.InverseTransformDirection(totalInfluence);
    
    // Apply different max angles per axis
    Vector3 scaledInfluence = new Vector3(
        localInfluence.x * maxForwardTilt,
        localInfluence.y * maxSidewaysTilt,
        localInfluence.z * maxTwistTilt
    ) * totalSpeed;

    // Clamp the values to prevent over-rotation
    scaledInfluence = new Vector3(
        Mathf.Clamp(scaledInfluence.x, -maxForwardTilt, maxForwardTilt),
        Mathf.Clamp(scaledInfluence.y, -maxSidewaysTilt, maxSidewaysTilt),
        Mathf.Clamp(scaledInfluence.z, -maxTwistTilt, maxTwistTilt)
    );

    // Convert back to world space
    Vector3 worldInfluence = transform.TransformDirection(scaledInfluence);

    // Create rotation from clamped influence
    Quaternion influenceRotation = worldInfluence != Vector3.zero 
        ? Quaternion.FromToRotation(Vector3.up, Vector3.up + worldInfluence)
        : Quaternion.identity;

    // Apply to ears with base rotation
    Quaternion targetLeftRotation = influenceRotation * _leftEarBaseRotation;
    Quaternion targetRightRotation = influenceRotation * _rightEarBaseRotation;

    // Smooth the transition
    leftEarPivot.localRotation = Quaternion.Slerp(
        leftEarPivot.localRotation,
        targetLeftRotation,
        1f - Mathf.Pow(earRotationSmoothness, Time.fixedDeltaTime)
    );
    
    rightEarPivot.localRotation = Quaternion.Slerp(
        rightEarPivot.localRotation,
        targetRightRotation,
        1f - Mathf.Pow(earRotationSmoothness, Time.fixedDeltaTime)
    );
}
   
   private void CheckBattery()
   {
       if (battery <= 0)
       {
           TurnOff();
       }
   }

   private bool IsOn()
   {
       return  currentState != RobotState.Off;
   }
   
   
private void OnDrawGizmos()
{
    Vector3 rayStart = transform.position;
 
    // Always draw the ground check ray
    RaycastHit hit;
    if (Physics.Raycast(rayStart, Vector3.down, out hit, Mathf.Infinity, groundLayer))
    {
        // Hit ground - draw green ray
        Gizmos.color = Color.green;
        Gizmos.DrawLine(rayStart, hit.point);
        Gizmos.DrawWireSphere(hit.point, 0.1f);

        // Draw minimum hover height
        Gizmos.color = Color.yellow;
        float minHoverHeight = hit.point.y + baseHeight;
        Vector3 minHoverPoint = new Vector3(hit.point.x, minHoverHeight, hit.point.z);
        Gizmos.DrawWireSphere(minHoverPoint, 0.1f);
        Gizmos.DrawLine(hit.point, minHoverPoint);

        // Draw hover amplitude range around desired height
        Gizmos.color = Color.cyan;
        Vector3 hoverMin = new Vector3(transform.position.x, _currentDesiredHeight - hoverAmplitude, transform.position.z);
        Vector3 hoverMax = new Vector3(transform.position.x, _currentDesiredHeight + hoverAmplitude, transform.position.z);
        Gizmos.DrawWireSphere(hoverMin, 0.1f);
        Gizmos.DrawWireSphere(hoverMax, 0.1f);
        Gizmos.DrawLine(hoverMin, hoverMax);

        // If following player, show relevant distances
        if (currentState == RobotState.FollowPlayer && _playerFollowPosition != null)
        {
            // Draw line to player follow position
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, _playerFollowPosition.position);
            
            // Draw vertical threshold range around player position
            Gizmos.color = Color.magenta;
            Vector3 thresholdMin = new Vector3(_playerFollowPosition.position.x, 
                _playerFollowPosition.position.y - verticalThreshold, 
                _playerFollowPosition.position.z);
            Vector3 thresholdMax = new Vector3(_playerFollowPosition.position.x, 
                _playerFollowPosition.position.y + verticalThreshold, 
                _playerFollowPosition.position.z);
            Gizmos.DrawWireSphere(thresholdMin, 0.1f);
            Gizmos.DrawWireSphere(thresholdMax, 0.1f);
            Gizmos.DrawLine(thresholdMin, thresholdMax);
        }
    }
    else
    {
        // No ground found - draw red ray downward
        Gizmos.color = Color.red;
        Gizmos.DrawLine(rayStart, rayStart + Vector3.down * 100f);
    }
}

   #endregion Utility ------------------------------------------------------------------------

   

   
   

}