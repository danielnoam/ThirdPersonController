    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using VInspector;


    public enum RobotState
    {
        Idle = 0,
        GoToTarget = 1,
        FollowPlayer = 2,
        Sit = 3,
        Off = 4,
    }

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class RobotCompanion : MonoBehaviour
    {
        
        [Foldout("Movement")]
        [Tooltip("Base movement speed of the robot")]
        [SerializeField] private float moveSpeed = 25f;
        
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
        
        
        
        [Foldout("Rotation")]
        [Tooltip("How quickly the robot returns to its desired rotation")]
        [SerializeField] private float rotationStability = 4f;
        
        [Tooltip("Maximum angular velocity for rotation")]
        [SerializeField] private float maxAngularVelocity = 3f;
        
        [Tooltip("Animation curve controlling how rotation speed changes based on angle difference")]
        [SerializeField] private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [EndFoldout]
        
        
        [Foldout("Hover")]
        [Tooltip("Maximum distance the robot will hover up and down")]
        [SerializeField] private float hoverAmplitude = 0.3f;

        [Tooltip("Speed of the hover movement cycle")]
        [SerializeField] private float hoverFrequency = 1f;

        [Tooltip("Minimum vertical distance change required before the robot adjusts its height")]
        [SerializeField] private float verticalThreshold = 2f;

        [Tooltip("Base height maintained above ground")]
        [SerializeField] private float baseHeight = 1.5f;

        [Tooltip("Additional height gained at maximum speed")]
        [SerializeField] private float maxSpeedHeight = 1.5f;

        [Tooltip("Speed at which maximum height boost is reached")]
        [SerializeField] private float maxSpeedForHeight = 5f;

        [Tooltip("How smoothly height changes with speed")]
        [SerializeField] private float heightSpeedSmoothness = 0.1f;

        [Tooltip("Layers that the robot considers as ground for hover calculations")]
        [SerializeField] private LayerMask groundLayer;
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
        private Vector3 _leftEarRotation;
        private Vector3 _rightEarRotation;
        private Vector3 _currentVelocity;
        private float _currentDesiredHeight;
        private float _hoverTime;
        private float _lastStableHeight;
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
           ApplyFriction();
           
           if (IsOn())
           {
               Hover();
               HandleRotation();
               UpdateEarRotation();
               
               switch (currentState)
               {
                   case RobotState.GoToTarget:
                       break;
                   case RobotState.FollowPlayer:
                       Follow();
                       break;
                   case RobotState.Sit:
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
           rigidBody.useGravity = false;
           rigidBody.isKinematic = false;
       }
       
       [Button]
       private void SitDown()
       {
           if (!IsOn()) return;

           currentState = RobotState.Sit;
           rigidBody.useGravity = true;
           rigidBody.isKinematic = true;
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
           rigidBody.isKinematic = false;
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

    // Calculate ground height using multiple raycasts
    float groundHeight = GetGroundHeight();

    // Calculate minimum allowed height (base layer)
    float minAllowedHeight = groundHeight + baseHeight;

    // Calculate speed-based height boost (dynamic height layer)
    float currentSpeed = rigidBody.linearVelocity.magnitude;
    float speedRatio = Mathf.Clamp01(currentSpeed / maxSpeedForHeight);
    float speedHeightBoost = maxSpeedHeight * speedRatio;

    // Calculate target height based on state
    if (currentState == RobotState.FollowPlayer && _playerFollowPosition != null)
    {
        float playerHeight = _playerFollowPosition.position.y;
        float heightDifference = Mathf.Abs(playerHeight - _lastStableHeight);
        
        // Check if player height is above minimum and if change exceeds threshold
        if (playerHeight > minAllowedHeight + speedHeightBoost)
        {
            if (heightDifference > verticalThreshold)
            {
                // Player has moved significantly vertically - update stable height
                _lastStableHeight = playerHeight;
                targetHeight = playerHeight;
            }
            else
            {
                // Within threshold - maintain current stable height
                targetHeight = _lastStableHeight;
            }
        }
        else
        {
            // Below minimum height - use minimum height as stable height
            _lastStableHeight = minAllowedHeight + speedHeightBoost;
            targetHeight = _lastStableHeight;
        }
    }
    else
    {
        // When not following player, hover at base height plus speed boost
        targetHeight = minAllowedHeight + speedHeightBoost;
    }

    // Smoothly move the desired height
    _currentDesiredHeight = Mathf.Lerp(_currentDesiredHeight, targetHeight, 
        moveSpeed * heightSpeedSmoothness * Time.fixedDeltaTime);

    // Add hover effect (final layer)
    _hoverTime += Time.fixedDeltaTime;
    float hoverOffset = Mathf.Sin(_hoverTime * hoverFrequency) * hoverAmplitude;
    float finalDesiredHeight = _currentDesiredHeight + hoverOffset;

    // Calculate velocity needed to reach position
    float distanceToDesired = finalDesiredHeight - currentY;
    float finalVelocity = distanceToDesired * moveSpeed;

    // Apply the new velocity while preserving X and Z
    Vector3 currentVelocity = rigidBody.linearVelocity;
    rigidBody.linearVelocity = new Vector3(currentVelocity.x, finalVelocity, currentVelocity.z);
}

private float GetGroundHeight()
{
    Vector3 position = rigidBody.position;
    float lowestHeight = position.y;

    // Cast rays in a small grid pattern for better ground detection
    Vector3[] offsets = new Vector3[]
    {
        Vector3.zero,                          // Center
        new Vector3(0.5f, 0, 0.5f),           // Front-Right
        new Vector3(-0.5f, 0, 0.5f),          // Front-Left
        new Vector3(0.5f, 0, -0.5f),          // Back-Right
        new Vector3(-0.5f, 0, -0.5f),         // Back-Left
    };

    foreach (Vector3 offset in offsets)
    {
        RaycastHit hit;
        Vector3 rayStart = position + offset;
        if (Physics.Raycast(rayStart, Vector3.down, out hit, Mathf.Infinity, groundLayer))
        {
            // Update lowest point found
            lowestHeight = Mathf.Min(lowestHeight, hit.point.y);
        }
    }

    return lowestHeight;
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

       

       #region Debug Visualization ----------------------------------------------------------

    private void OnDrawGizmos()
    {
        // Draw ground detection and hover visualization
        DrawGroundAndHoverGizmos();

        // Draw movement and following visualization if active
        if (currentState == RobotState.FollowPlayer && _playerFollowPosition != null)
        {
            DrawMovementGizmos();
            DrawVerticalThresholdGizmos();
            DrawRotationGizmos();
        }
    }

    private void DrawGroundAndHoverGizmos()
    {
        // Draw multi-point ground detection
        DrawGroundDetectionPoints();

        // Draw hover layers
        DrawHoverLayers();

        // Draw speed-based height adjustment
        DrawSpeedHeightGizmos();
    }

    private void DrawMovementGizmos()
    {
        // Calculate horizontal distance to player (ignoring Y)
        Vector3 robotFlat = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 playerFlat = new Vector3(_playerFollowPosition.position.x, 0, _playerFollowPosition.position.z);
        float distanceToPlayer = Vector3.Distance(robotFlat, playerFlat);

        // Draw follow range circles
        Gizmos.color = new Color(1f, 1f, 1f, 0.2f); // Transparent white
        DrawGizmosCircle(transform.position, minFollowDistance);
        DrawGizmosCircle(transform.position, maxFollowDistance);

        // Draw distance zones
        DrawDistanceZones();

        // Draw line to player with color based on distance
        if (distanceToPlayer < minFollowDistance)
        {
            Gizmos.color = Color.red; // Too close
        }
        else if (distanceToPlayer > maxFollowDistance)
        {
            Gizmos.color = Color.yellow; // Too far
        }
        else
        {
            Gizmos.color = Color.green; // Ideal range
        }
        Gizmos.DrawLine(transform.position, _playerFollowPosition.position);
        
    }

    private void DrawDistanceZones()
    {
        // Draw sectors showing distance zones
        int segments = 36;
        float angleStep = 360f / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle1 = angleStep * i * Mathf.Deg2Rad;
            float angle2 = angleStep * (i + 1) * Mathf.Deg2Rad;
            
            // Draw inner zone (too close)
            Gizmos.color = new Color(1f, 0f, 0f, 0.1f); // Very transparent red
            DrawGizmosArcSegment(transform.position, 0, minFollowDistance, angle1, angle2);
            
            // Draw ideal zone
            Gizmos.color = new Color(0f, 1f, 0f, 0.1f); // Very transparent green
            DrawGizmosArcSegment(transform.position, minFollowDistance, maxFollowDistance, angle1, angle2);
            
            // Draw outer zone (too far)
            Gizmos.color = new Color(1f, 1f, 0f, 0.1f); // Very transparent yellow
            DrawGizmosArcSegment(transform.position, maxFollowDistance, maxFollowDistance + 1f, angle1, angle2);
        }
    }



    private void DrawVerticalThresholdGizmos()
    {
        // Draw vertical movement threshold
        Gizmos.color = new Color(1f, 0f, 1f, 0.3f); // Transparent magenta
        Vector3 thresholdMin = new Vector3(_playerFollowPosition.position.x, 
            _playerFollowPosition.position.y - verticalThreshold, 
            _playerFollowPosition.position.z);
        Vector3 thresholdMax = new Vector3(_playerFollowPosition.position.x, 
            _playerFollowPosition.position.y + verticalThreshold, 
            _playerFollowPosition.position.z);
        
        // Draw threshold range
        Gizmos.DrawWireSphere(thresholdMin, 0.1f);
        Gizmos.DrawWireSphere(thresholdMax, 0.1f);
        Gizmos.DrawLine(thresholdMin, thresholdMax);
        
        // Draw current vertical position indicator
        float currentY = transform.position.y;
        if (Mathf.Abs(currentY - _playerFollowPosition.position.y) > verticalThreshold)
        {
            Gizmos.color = Color.red;
        }
        else
        {
            Gizmos.color = Color.green;
        }
        Vector3 currentYPos = new Vector3(_playerFollowPosition.position.x, currentY, _playerFollowPosition.position.z);
        Gizmos.DrawWireSphere(currentYPos, 0.15f);
    }

    private void DrawRotationGizmos()
    {
        // Draw current forward direction
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * 1.5f);
        
        // Draw target rotation (direction to player)
        Vector3 directionToPlayer = (_playerFollowPosition.position - transform.position).normalized;
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // Transparent green
        Gizmos.DrawRay(transform.position, directionToPlayer * 1.5f);
        
        // Draw angle indicator
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        if (angle > 45f)
        {
            Gizmos.color = Color.yellow;
            DrawGizmosArc(transform.position, 1f, Vector3.up, transform.forward, angle);
        }
    }
    
    
    private void DrawGroundDetectionPoints()
{
    Vector3 position = transform.position;
    Vector3[] offsets = new Vector3[]
    {
        Vector3.zero,                          // Center
        new Vector3(0.5f, 0, 0.5f),           // Front-Right
        new Vector3(-0.5f, 0, 0.5f),          // Front-Left
        new Vector3(0.5f, 0, -0.5f),          // Back-Right
        new Vector3(-0.5f, 0, -0.5f),         // Back-Left
    };

    float lowestPoint = float.MaxValue;
    Dictionary<Vector3, float> groundPoints = new Dictionary<Vector3, float>();

    // Draw each ground detection ray
    foreach (Vector3 offset in offsets)
    {
        Vector3 rayStart = position + offset;
        RaycastHit hit;
        
        if (Physics.Raycast(rayStart, Vector3.down, out hit, Mathf.Infinity, groundLayer))
        {
            // Store ground point for later use
            groundPoints[offset] = hit.point.y;
            lowestPoint = Mathf.Min(lowestPoint, hit.point.y);

            // Draw ray and hit point
            Gizmos.color = Color.green;
            Gizmos.DrawLine(rayStart, hit.point);
            Gizmos.DrawWireSphere(hit.point, 0.1f);
        }
        else
        {
            // Draw failed raycast
            Gizmos.color = Color.red;
            Gizmos.DrawLine(rayStart, rayStart + Vector3.down * 100f);
        }
    }

    // Draw ground plane at lowest point
    if (lowestPoint != float.MaxValue)
    {
        Gizmos.color = new Color(0.5f, 1f, 0.5f, 0.2f); // Transparent green
        Vector3 center = new Vector3(position.x, lowestPoint, position.z);
        Gizmos.DrawWireCube(center, new Vector3(2f, 0.01f, 2f));
    }
}

private void DrawHoverLayers()
{
    Vector3 position = transform.position;
    float groundHeight = GetGroundHeight();

    // Base height layer
    float baseHeightY = groundHeight + baseHeight;
    Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // Transparent yellow
    DrawHoverLayer(baseHeightY, "Base Height");

    // Current speed-based height
    float currentSpeed = rigidBody.linearVelocity.magnitude;
    float speedRatio = Mathf.Clamp01(currentSpeed / maxSpeedForHeight);
    float speedHeightBoost = maxSpeedHeight * speedRatio;
    float speedAdjustedHeight = baseHeightY + speedHeightBoost;
    Gizmos.color = new Color(0f, 1f, 1f, 0.3f); // Transparent cyan
    DrawHoverLayer(speedAdjustedHeight, "Speed Adjusted");

    // Current desired height (before hover effect)
    Gizmos.color = new Color(1f, 1f, 1f, 0.3f); // Transparent white
    DrawHoverLayer(_currentDesiredHeight, "Target Height");

    // Hover effect range
    Gizmos.color = new Color(1f, 0.5f, 1f, 0.2f); // Transparent pink
    float hoverMin = _currentDesiredHeight - hoverAmplitude;
    float hoverMax = _currentDesiredHeight + hoverAmplitude;
    DrawHoverLayer(hoverMin, "Hover Min");
    DrawHoverLayer(hoverMax, "Hover Max");
    
    // Draw vertical lines connecting layers
    Gizmos.color = new Color(1f, 1f, 1f, 0.1f); // Very transparent white
    Vector3 center = new Vector3(position.x, 0, position.z);
    Gizmos.DrawLine(
        center + new Vector3(0, groundHeight, 0),
        center + new Vector3(0, hoverMax, 0)
    );
}

private void DrawHoverLayer(float height, string label)
{
    Vector3 position = transform.position;
    Vector3 center = new Vector3(position.x, height, position.z);
    
    // Draw plane
    Gizmos.DrawWireCube(center, new Vector3(1f, 0.01f, 1f));
    
    // Draw small sphere at robot's position projected onto this layer
    Vector3 projection = new Vector3(position.x, height, position.z);
    Gizmos.DrawWireSphere(projection, 0.1f);
    
    }

    private void DrawSpeedHeightGizmos()
    {
        // Draw speed-based height adjustment gauge
        Vector3 position = transform.position;
        float currentSpeed = rigidBody.linearVelocity.magnitude;
        float speedRatio = Mathf.Clamp01(currentSpeed / maxSpeedForHeight);
        
        Vector3 gaugeStart = position + Vector3.right * 1.5f;
        Vector3 gaugeEnd = gaugeStart + Vector3.up * maxSpeedHeight;
        
        // Draw full range
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        Gizmos.DrawLine(gaugeStart, gaugeEnd);
        
        // Draw current value
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(gaugeStart, gaugeStart + Vector3.up * (maxSpeedHeight * speedRatio));
        Gizmos.DrawWireSphere(gaugeStart + Vector3.up * (maxSpeedHeight * speedRatio), 0.05f);
    }

    private void DrawGizmosCircle(Vector3 center, float radius)
    {
        int segments = 32;
        float angleStep = 360f / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle1 = angleStep * i * Mathf.Deg2Rad;
            float angle2 = angleStep * (i + 1) * Mathf.Deg2Rad;
            
            Vector3 point1 = center + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
            Vector3 point2 = center + new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);
            
            Gizmos.DrawLine(point1, point2);
        }
    }

    private void DrawGizmosArcSegment(Vector3 center, float innerRadius, float outerRadius, float startAngle, float endAngle)
    {
        Vector3 innerStart = center + new Vector3(Mathf.Cos(startAngle) * innerRadius, 0, Mathf.Sin(startAngle) * innerRadius);
        Vector3 innerEnd = center + new Vector3(Mathf.Cos(endAngle) * innerRadius, 0, Mathf.Sin(endAngle) * innerRadius);
        Vector3 outerStart = center + new Vector3(Mathf.Cos(startAngle) * outerRadius, 0, Mathf.Sin(startAngle) * outerRadius);
        Vector3 outerEnd = center + new Vector3(Mathf.Cos(endAngle) * outerRadius, 0, Mathf.Sin(endAngle) * outerRadius);
        
        Gizmos.DrawLine(innerStart, outerStart);
        Gizmos.DrawLine(innerEnd, outerEnd);
        Gizmos.DrawLine(innerStart, innerEnd);
        Gizmos.DrawLine(outerStart, outerEnd);
    }

    private void DrawGizmosArc(Vector3 center, float radius, Vector3 normal, Vector3 from, float angle)
    {
        int segments = Mathf.Max(1, Mathf.FloorToInt(angle / 10f));
        float angleStep = angle / segments;
        
        for (int i = 0; i < segments; i++)
        {
            Quaternion rotation1 = Quaternion.AngleAxis(angleStep * i, normal);
            Quaternion rotation2 = Quaternion.AngleAxis(angleStep * (i + 1), normal);
            
            Vector3 point1 = center + rotation1 * from * radius;
            Vector3 point2 = center + rotation2 * from * radius;
            
            Gizmos.DrawLine(point1, point2);
        }
    }

    #endregion Debug Visualization ----------------------------------------------------------
       

    }