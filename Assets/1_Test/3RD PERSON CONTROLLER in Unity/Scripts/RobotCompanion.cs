    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Serialization;
    using VInspector;


    public enum RobotState
    {
        Idle = 0,
        GoToTarget = 1,
        FollowingPlayer = 2,
        Sitting = 3,
        Off = 4,
    }

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class RobotCompanion : MonoBehaviour
    {
        
        [Foldout("Horizontal Movement")]
        [Tooltip("Base movement speed of the robot")]
        [SerializeField] private float horizontalMoveSpeed = 25f;
        
        [Tooltip("Minimum distance to maintain from target")]
        [SerializeField] private float minFollowDistance = 2f;
        
        [Tooltip("Maximum distance before reaching max speed")]
        [SerializeField] private float maxFollowDistance = 4f;
        
        [Tooltip("Friction coefficient applied when the robot has no target")]
        [SerializeField] private float friction = 1f;
        
        [Tooltip("How smoothly the robot accelerates and decelerates")]
        [SerializeField] private float followSmoothness = 0.02f;
        
        [Tooltip("Animation curve controlling how speed changes based on distance")]
        [SerializeField] private AnimationCurve followCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [EndFoldout]
        
        
        
        [Foldout("Vertical movement")]
        
        [Tooltip("Vertical movement speed of the robot")]
        [SerializeField] private float verticalMoveSpeed = 5f;
        
        [SerializeField] private float sitDownSpeed = 3f; // Speed at which the robot sits down
        
        [Tooltip("Minimum safe distance the robot must maintain from ground")]
        [SerializeField] private float minEnvironmentClearance = 0.5f;
        
        [Tooltip("Base height maintained above ground")]
        [SerializeField] private float baseHeight = 1.5f;
        
        [Tooltip("Time to wait before adjusting height for small changes")]
        [SerializeField] private float heightAdjustmentDelay = 1f;
        
        [Tooltip("Minimum vertical distance change required before the robot adjusts its height")]
        [SerializeField] private float verticalThreshold = 1f;
        
        [Tooltip("Layers that the robot considers as ground for hover calculations")]
        [SerializeField] private LayerMask environmentLayer;
        
        [Tooltip("Animation curve controlling how vertical speed changes based on height difference")]
        [SerializeField] private AnimationCurve heightAdjustmentCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [Header("Hover effect")]
        [Tooltip("Maximum distance the robot will hover up and down")]
        [SerializeField] private float hoverHeight = 0.3f;
        
        [Tooltip("Speed of the hover movement cycle")]
        [SerializeField] private float hoverSpeed = 1f;
        [EndFoldout]

        
        [Foldout("Rotation")]
        [Tooltip("How quickly the robot returns to its desired rotation")]
        [SerializeField] private float rotationStability = 4f;
        
        [Tooltip("Maximum angular velocity for rotation")]
        [SerializeField] private float maxAngularVelocity = 3f;
        
        [Tooltip("Animation curve controlling how rotation speed changes based on angle difference")]
        [SerializeField] private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [EndFoldout]


        [Foldout("Ears")] 
        [SerializeField] private bool rotateEars = true;
        [Tooltip("Transform reference for the left ear/antenna")]
        [SerializeField] private Transform leftEarPivot;

        [Tooltip("Transform reference for the right ear/antenna")]
        [SerializeField] private Transform rightEarPivot;

        [Tooltip("Maximum bend angle of the ears")]
        [SerializeField] private float maxEarBend = 45f;

        [Tooltip("How smoothly the ears rotate")]
        [SerializeField] private float earRotationSmoothness = 0.2f;

        [Tooltip("Speed at which ears reach their maximum bend")]
        [SerializeField] private float maxSpeedForEarRotation = 4f;
        [EndFoldout]

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
        
        private float _lastHeightAdjustmentTime;
        private float _lastTargetHeight;
        private float _currentHoverOffset;
        private float _hoverTime;
        
        private float _targetSitHeight; // The ground height where we want to sit
        
        private Vector3 _leftEarRotation;
        private Vector3 _rightEarRotation;
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
           ApplyFriction();
           
           if (IsOn())
           {
               HandleRotation();
               UpdateEarRotation();
               
               switch (currentState)
               {
                   case RobotState.GoToTarget:
                       AdjustHeight();
                       break;
                   case RobotState.FollowingPlayer:
                       AdjustHeight();
                       Follow();
                       break;
                   case  RobotState.Idle:
                       AdjustHeight();
                       break;
                   case RobotState.Sitting:
                       HandleSitting();
                       break;
               }
           }
       }




       #region States ------------------------------------------------------------------------------

       
       [Button]
       public void FollowPlayer()
       {
           if (!_player || !IsOn()) return;
           rigidBody.isKinematic = false;
           rigidBody.useGravity = false;
           currentState = RobotState.FollowingPlayer;
       }
       
       public void GoToTarget(Transform targetToGoTo)
       {
           if (!targetToGoTo || !IsOn()) return;
           
           _target = targetToGoTo;
           currentState = RobotState.GoToTarget;
       }
       
       [Button]
       public void Idle()
       {
           if (!IsOn()) return;
           
           _target = null;
           currentState = RobotState.Idle;
           rigidBody.useGravity = false;
           rigidBody.isKinematic = false;
       }
       
       [Button]
       public void SitDown()
       {
           if (!IsOn()) return;

           currentState = RobotState.Sitting;
           rigidBody.useGravity = false;
           rigidBody.isKinematic = false;

           // Find the ground position
           if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, Mathf.Infinity, environmentLayer))
           {
               _targetSitHeight = hit.point.y + minEnvironmentClearance;
           }
       }
       
       [Button]
       public void TurnOff()
       {
           currentState = RobotState.Off;
           rigidBody.useGravity = true;
       }

       [Button]
       public void TurnOn()
       {
           currentState = RobotState.Idle;
           battery = 100;
           rigidBody.useGravity = false;
           rigidBody.isKinematic = false;
       }

       #endregion States ------------------------------------------------------------------------------


       #region Vertical movement -------------------------------------------------------------------------------
       
       private void HandleSitting()
       {
           // Calculate distance to target height
           float heightDifference = transform.position.y - _targetSitHeight;

           if (heightDifference <= 0.01f)
           {
               // We've reached the sitting position
               rigidBody.isKinematic = true; // Now lock it in place
               transform.position = new Vector3(transform.position.x, _targetSitHeight, transform.position.z);
               rigidBody.linearVelocity = Vector3.zero;
               return;
           }

           // Calculate and apply downward velocity
           Vector3 currentVelocity = rigidBody.linearVelocity;
           float desiredVerticalVelocity = -sitDownSpeed;
    
           Vector3 newVelocity = new Vector3(
               currentVelocity.x,
               desiredVerticalVelocity,
               currentVelocity.z
           );
    
           rigidBody.linearVelocity = newVelocity;
       }
       
private void AdjustHeight()
{
    if (!IsOn()) return;

    // Update hover time
    _hoverTime += Time.fixedDeltaTime * hoverSpeed;
    
    // Get target height and ground/ceiling constraints
    float targetHeight = GetTargetHeight();
    (float minHeight, float maxHeight) = GetHeightConstraints();
    
    // Calculate hover offset (only positive values to hover ABOVE target height)
    float hoverOffset = (Mathf.Sin(_hoverTime) + 1) * hoverHeight * 0.5f;
    
    // Check if height change requires delay
    float heightDifference = Mathf.Abs(targetHeight - _lastTargetHeight);
    bool needsDelay = heightDifference <= verticalThreshold;
    
    // Update target if threshold exceeded or delay passed
    if (!needsDelay || Time.time - _lastHeightAdjustmentTime >= heightAdjustmentDelay)
    {
        _lastTargetHeight = targetHeight;
        _lastHeightAdjustmentTime = Time.time;
    }
    
    // Calculate final target position with hover
    float finalTargetHeight = Mathf.Clamp(_lastTargetHeight + hoverOffset, minHeight, maxHeight);
    
    // Get current vertical velocity
    float currentVerticalVelocity = rigidBody.linearVelocity.y;
    
    // Calculate desired vertical velocity
    float heightError = finalTargetHeight - transform.position.y;
    float normalizedHeightDifference = Mathf.Clamp01(Mathf.Abs(heightError) / verticalThreshold);
    float speedMultiplier = heightAdjustmentCurve.Evaluate(normalizedHeightDifference);
    float desiredVerticalVelocity = heightError * verticalMoveSpeed * speedMultiplier;
    
    // Calculate damping force
    float dampingForce = -currentVerticalVelocity * followSmoothness;
    
    // Calculate acceleration needed to reach desired velocity
    float acceleration = (desiredVerticalVelocity - currentVerticalVelocity) * (1f - followSmoothness);
    
    // Combine forces
    float totalForce = acceleration + dampingForce;
    
    // Apply forces over time
    float velocityChange = totalForce * Time.fixedDeltaTime;
    
    // Create new velocity vector, preserving X and Z components
    Vector3 currentVelocity = rigidBody.linearVelocity;
    Vector3 newVelocity = new Vector3(
        currentVelocity.x,
        currentVerticalVelocity + velocityChange,
        currentVelocity.z
    );
    
    // Apply final velocity
    rigidBody.linearVelocity = newVelocity;
}

private float GetTargetHeight()
{
    if (currentState != RobotState.FollowingPlayer || !_playerFollowPosition)
        return baseHeight;

    // Cast ray between robot and player to check terrain
    Vector3 toPlayer = _playerFollowPosition.position - transform.position;
    Vector3 midPoint = transform.position + toPlayer * 0.5f;
    
    if (Physics.Raycast(midPoint, Vector3.down, out RaycastHit midHit, Mathf.Infinity, environmentLayer))
    {
        return Mathf.Max(_playerFollowPosition.position.y, midHit.point.y + baseHeight);
    }
    
    return _playerFollowPosition.position.y;
}

private (float minHeight, float maxHeight) GetHeightConstraints()
{
    float minHeight = 0;
    float maxHeight = Mathf.Infinity;
    
    // Check ground clearance
    if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit groundHit, Mathf.Infinity, environmentLayer))
    {
        minHeight = groundHit.point.y + minEnvironmentClearance;
    }
    
    // Check ceiling clearance
    if (Physics.Raycast(transform.position, Vector3.up, out RaycastHit ceilingHit, Mathf.Infinity, environmentLayer))
    {
        maxHeight = ceilingHit.point.y - minEnvironmentClearance;
    }
    
    return (minHeight, maxHeight);
}

    private void OnDrawGizmos()
    {
        if (!IsOn()) return;
    
        // Draw ground and ceiling raycasts
        Gizmos.color = Color.blue;
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit groundHit, Mathf.Infinity, environmentLayer))
        {
            Gizmos.DrawLine(transform.position, groundHit.point);
            Gizmos.DrawWireSphere(groundHit.point, 0.1f);
        }
        if (Physics.Raycast(transform.position, Vector3.up, out RaycastHit ceilingHit, Mathf.Infinity, environmentLayer))
        {
            Gizmos.DrawLine(transform.position, ceilingHit.point);
            Gizmos.DrawWireSphere(ceilingHit.point, 0.1f);
        }
    
        // Draw hover range
        Gizmos.color = Color.yellow;
        float desiredHeight = GetTargetHeight();
        Vector3 desiredPosition = new Vector3(transform.position.x, desiredHeight, transform.position.z);
        Gizmos.DrawWireSphere(desiredPosition + Vector3.up * hoverHeight, 0.1f);
        Gizmos.DrawWireSphere(desiredPosition, 0.1f);
    
        // Draw target height when following player
        if (currentState == RobotState.FollowingPlayer && _playerFollowPosition)
        {
            Gizmos.color = Color.blue;
            Vector3 midPoint = Vector3.Lerp(transform.position, _playerFollowPosition.position, 0.5f);
            Gizmos.DrawLine(midPoint, midPoint + Vector3.down * 10f);
            Gizmos.DrawWireSphere(_playerFollowPosition.position, 0.2f);
        }
    }
    
        

       

       #endregion Vertical movement -------------------------------------------------------------------------------
       
       
       #region Actions -------------------------------------------------------------------
       
       
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
               desiredVelocity = moveDirection * (horizontalMoveSpeed * speedMultiplier);
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
        
           if (currentState == RobotState.FollowingPlayer)
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
       

        
       #endregion Actions -------------------------------------------------------------------


       #region Utility ------------------------------------------------------------------------
       
        private void UpdateEarRotation()
        {
            if (!leftEarPivot || !rightEarPivot) return;
            if (!rotateEars) return;

            // Get the movement direction in local space
            Vector3 localVelocity = transform.InverseTransformDirection(rigidBody.linearVelocity);
            Vector3 localAngularVelocity = transform.InverseTransformDirection(rigidBody.angularVelocity);
            
            float movementSpeed = rigidBody.linearVelocity.magnitude;
            float rotationSpeed = rigidBody.angularVelocity.magnitude;

            // Calculate bend strength for both movement and rotation
            float movementBendStrength = Mathf.Clamp01(movementSpeed / maxSpeedForEarRotation);
            float rotationBendStrength = Mathf.Clamp01(rotationSpeed / maxAngularVelocity);

            // Calculate movement-based rotation
            Vector3 movementRotation = new Vector3(
                -localVelocity.z, // Forward/back movement causes up/down rotation
                -localVelocity.x, // Left/right movement causes side rotation
                0
            ).normalized * (maxEarBend * movementBendStrength);

            // Calculate rotation-based ear bend
            // For the left ear
            Vector3 leftRotationBend = new Vector3(
                0,
                localAngularVelocity.y, // Yaw rotation causes side bend
                0
            ) * (maxEarBend * rotationBendStrength);

            // For the right ear (opposite of left ear for rotation)
            Vector3 rightRotationBend = new Vector3(
                0,
                localAngularVelocity.y, // Opposite direction for right ear
                0
            ) * (maxEarBend * rotationBendStrength);

            // Combine movement and rotation effects
            Quaternion leftTargetRotation = Quaternion.Euler(movementRotation + leftRotationBend);
            Quaternion rightTargetRotation = Quaternion.Euler(movementRotation + rightRotationBend);

            // Apply rotation with smoothing
            leftEarPivot.localRotation = Quaternion.Slerp(
                leftEarPivot.localRotation,
                leftTargetRotation * _leftEarBaseRotation,
                1f - Mathf.Pow(earRotationSmoothness, Time.deltaTime)
            );

            rightEarPivot.localRotation = Quaternion.Slerp(
                rightEarPivot.localRotation,
                rightTargetRotation * _rightEarBaseRotation,
                1f - Mathf.Pow(earRotationSmoothness, Time.deltaTime)
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
       
       

       #endregion Utility ------------------------------------------------------------------------


    }