using System;
using UnityEngine;
using VInspector;


public enum RobotState
{
    Idle = 0,
    GoToTarget = 1,
    FollowTarget = 2,
    Off = 3,
}

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class RobotFollower : MonoBehaviour
{
    
   [Header("Movement")]
   [SerializeField] private float moveSpeed = 5f;    // Movement speed
   [SerializeField] private float friction = 1f;
   [SerializeField] private float rotationStability = 5f;    // How quickly it returns to desired rotation
   [SerializeField] private float maxAngularVelocity = 5f;   // Maximum rotation speed
   [SerializeField] private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // Curve for rotation interpolation

   [Header("Hover")] 
   [SerializeField] private float hoverAmplitude = 0.3f;  // How high it hovers
   [SerializeField] private float hoverFrequency = 1f;    // How fast it hovers
   [SerializeField] private float baseHeight = 1.5f; // Hover effect height when in idle
   [SerializeField] private LayerMask groundLayer; // Which layers to check for ground

   [Header("Ears")]
   [SerializeField] private Transform leftEar;       // Left ear/antenna transform
   [SerializeField] private Transform rightEar;      // Right ear/antenna transform
   [SerializeField] private float maxEarAngle = 45f; // Maximum ear rotation angle
   [SerializeField] private float earRotationSmoothness = 0.2f; // Ear rotation smoothness
   [SerializeField] private float minSpeedForEarRotation = 0.1f; // Minimum speed to start ear rotation
   [SerializeField] private float maxSpeedForEarRotation = 4f;   // Speed at which ears reach max rotation
   
   [Header("References")]
   [SerializeField] private Rigidbody rigidBody;
   
   [Header("Debug")]
   [SerializeField, ReadOnly] private RobotState currentState; // Current robot state
   [SerializeField, ReadOnly] private int battery = 100; // Robot health
   [SerializeField, ReadOnly] private Transform target; // Target to follow

   private void Awake()
   {
       if (!rigidBody) rigidBody = GetComponent<Rigidbody>();
   }

   private void Start()
   {
       TurnOn();
   }


   private void Update()
   {
       if (IsOn())
       {
           CheckBattery();
           
           switch (currentState)
           {
               case RobotState.GoToTarget:
                   break;
               case RobotState.FollowTarget:
                   break;
           }
       }
   }

   private void FixedUpdate()
   {
       if (IsOn())
       {
           Hover();
           HandleRotation();
           ApplyFriction();
       }
   }



   #region States ------------------------------------------------------------------------------

   
   [Button]
   public void FollowTarget(Transform targetToFollow)
   {
       if (!targetToFollow || !IsOn()) return;
       
       target = targetToFollow;
       currentState = RobotState.FollowTarget;
   }
   
   [Button]
   public void GoToTarget(Transform targetToGoTo)
   {
       if (!targetToGoTo || !IsOn()) return;
       
       target = targetToGoTo;
       currentState = RobotState.GoToTarget;
   }
   
   [Button]
   private void Idle()
   {
       if (!IsOn()) return;
       
       target = null;
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

   private void Hover()
   {
       float currentY = rigidBody.position.y;
       float desiredHeight;

       // Always check ground distance from current Y position
       RaycastHit hit;
       bool groundFound = Physics.Raycast(new Vector3(rigidBody.position.x, currentY, rigidBody.position.z), 
           Vector3.down, out hit, Mathf.Infinity, groundLayer);
       float groundHeight = groundFound ? hit.point.y : currentY;

       if (target)
       {
           // When following target, use target's Y position but ensure minimum ground distance
           float heightFromGround = target.position.y - groundHeight;
           if (heightFromGround < baseHeight)
           {
               // If target is too close to ground, maintain baseHeight from ground
               desiredHeight = groundHeight + baseHeight;
           }
           else
           {
               // Otherwise follow target height
               desiredHeight = target.position.y;
           }
       }
       else
       {
           // When no target, hover at baseHeight above ground
           desiredHeight = groundHeight + baseHeight;
       }

       // Apply hover effect using sine wave with fixed time
       float hoverOffset = Mathf.Sin(Time.fixedTime * hoverFrequency) * hoverAmplitude;
       float targetHeight = desiredHeight + hoverOffset;

       // Calculate velocity needed to reach target height
       float distanceToTarget = targetHeight - currentY;
       float targetVelocity = distanceToTarget * moveSpeed; // Base velocity needed

       // Add some damping based on current velocity to prevent oscillation
       float currentYVelocity = rigidBody.linearVelocity.y;
       float dampedVelocity = targetVelocity - (currentYVelocity * 0.5f); // Damping factor of 0.5

       // Apply the new velocity while preserving X and Z
       Vector3 currentVelocity = rigidBody.linearVelocity;
       rigidBody.linearVelocity = new Vector3(currentVelocity.x, dampedVelocity, currentVelocity.z);
   }


   private void ApplyFriction()
   {
       // Only apply friction when there's no target
       if (target == null)
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
    
       if (target && currentState != RobotState.Idle)
       {
           // If we have a target, calculate rotation to face it
           Vector3 directionToTarget = (target.position - transform.position).normalized;
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

           // If there's a target, show target position and actual hover height
           if (target)
           {
               // Draw line to target
               Gizmos.color = Color.blue;
               Gizmos.DrawLine(transform.position, target.position);
         
               // Draw line showing actual hover height (respecting minimum height)
               float actualHoverHeight = Mathf.Max(minHoverHeight, target.position.y);
               Vector3 actualHoverPoint = new Vector3(target.position.x, actualHoverHeight, target.position.z);
               Gizmos.DrawLine(target.position, actualHoverPoint);
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