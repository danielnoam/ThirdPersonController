using UnityEngine;

public class RobotFollower : MonoBehaviour
{
   [SerializeField] private Transform target;        // Target to follow
   
   [Header("Movement")]
   [SerializeField] private float moveSpeed = 5f;    // Movement speed
   [SerializeField] private float smoothness = 0.3f; // Lower = more responsive, Higher = smoother
   
   [Header("Rotation")]
   [SerializeField] private bool lookAtDirection = true; // Toggle rotation behavior
   [SerializeField] private float rotationSmoothness = 5f; // Control rotation smoothing
   [SerializeField] private float maxPitchAngle = 45f; // Maximum up/down rotation angle
   
   [Header("Hover")]
   [SerializeField] private float hoverAmplitude = 0.5f;  // How high it hovers
   [SerializeField] private float hoverFrequency = 1f;    // How fast it hovers

   [Header("Ears")]
   [SerializeField] private Transform leftEar;       // Left ear/antenna transform
   [SerializeField] private Transform rightEar;      // Right ear/antenna transform
   [SerializeField] private float maxEarAngle = 45f; // Maximum ear rotation angle
   [SerializeField] private float earRotationSmoothness = 0.2f; // Ear rotation smoothness
   [SerializeField] private float minSpeedForEarRotation = 0.1f; // Minimum speed to start ear rotation
   [SerializeField] private float maxSpeedForEarRotation = 4f;   // Speed at which ears reach max rotation

   private Vector3 _velocity = Vector3.zero;
   private Vector3 _positionWithoutHover;
   private Vector3 _lastPositionWithoutHover;
   private Vector3 _smoothedDirection;
   private Vector3 _currentRotationVelocity;
   private Vector3 _earVelocityLeft;
   private Vector3 _earVelocityRight;
   private const float DirectionSmoothSpeed = 0.2f;
   private float _hoverOffset;

   private void Start()
   {
       _positionWithoutHover = transform.position;
       _lastPositionWithoutHover = _positionWithoutHover;
       _smoothedDirection = transform.forward;
   }

   private void Update()
   {
       if (!target) return;

       UpdatePositions();
       HandleRotation();
       UpdateEars();
   }

   private void UpdatePositions()
   {
       // Update hover offset
       _hoverOffset = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;

       // Calculate and update position without hover
       Vector3 targetPosition = target.position;
       _positionWithoutHover = Vector3.SmoothDamp(_positionWithoutHover, targetPosition, ref _velocity, smoothness);

       // Apply hover to final position
       transform.position = _positionWithoutHover + Vector3.up * _hoverOffset;

       // Calculate movement direction without hover influence
       Vector3 currentDirection = (_positionWithoutHover - _lastPositionWithoutHover).normalized;
       _lastPositionWithoutHover = _positionWithoutHover;

       if (currentDirection.magnitude > 0.01f)
       {
           _smoothedDirection = Vector3.SmoothDamp(
               _smoothedDirection, 
               currentDirection, 
               ref _currentRotationVelocity, 
               DirectionSmoothSpeed
           );
       }
   }

   private void HandleRotation()
   {
       if (!lookAtDirection || _smoothedDirection.magnitude <= 0.01f) return;

       // Use smoothed direction for rotation
       Quaternion targetRotation = Quaternion.LookRotation(_smoothedDirection);
       
       // Get the euler angles to clamp the pitch
       Vector3 targetEulerAngles = targetRotation.eulerAngles;
       
       // Convert angles to -180 to 180 range for easier clamping
       float pitch = targetEulerAngles.x;
       if (pitch > 180) pitch -= 360f;
       
       // Clamp the pitch angle
       pitch = Mathf.Clamp(pitch, -maxPitchAngle, maxPitchAngle);
       
       // Create new rotation with clamped pitch
       targetRotation = Quaternion.Euler(pitch, targetEulerAngles.y, targetEulerAngles.z);

       // Apply smooth rotation
       transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothness * Time.deltaTime);
   }

   private void UpdateEars()
   {
       if (!leftEar || !rightEar) return;

       // Calculate movement speed (using velocity magnitude)
       float currentSpeed = _velocity.magnitude;
    
       // Calculate rotation amount based on speed
       float speedRatio = Mathf.Clamp01((currentSpeed - minSpeedForEarRotation) / (maxSpeedForEarRotation - minSpeedForEarRotation));
       float targetEarAngle = maxEarAngle * speedRatio;

       // Get movement direction in local space
       Vector3 localVelocity = transform.InverseTransformDirection(_velocity);
       float directionMultiplier = Mathf.Sign(-localVelocity.z); // Forward/Backward direction (kept your negative sign)

       // Calculate target rotations
       Quaternion targetLeftRotation;
       Quaternion targetRightRotation;

       if (currentSpeed < minSpeedForEarRotation)
       {
           // Return to original rotation when not moving
           targetLeftRotation = Quaternion.identity;
           targetRightRotation = Quaternion.identity;
       }
       else
       {
           // Tilt based on movement
           targetLeftRotation = Quaternion.Euler(targetEarAngle * directionMultiplier, 0, 0);
           targetRightRotation = Quaternion.Euler(targetEarAngle * directionMultiplier, 0, 0);
       }

       // Smooth ear rotations using Quaternion Slerp
       leftEar.localRotation = Quaternion.Slerp(
           leftEar.localRotation,
           targetLeftRotation,
           earRotationSmoothness
       );

       rightEar.localRotation = Quaternion.Slerp(
           rightEar.localRotation,
           targetRightRotation,
           earRotationSmoothness
       );
   }
}