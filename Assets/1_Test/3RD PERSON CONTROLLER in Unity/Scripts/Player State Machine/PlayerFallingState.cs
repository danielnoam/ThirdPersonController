
using UnityEngine;

public class PlayerFallingState : PlayerBaseState
{
    private Vector3 momentum;
    private float activeMoveSpeed;
    private Vector3 moveDirection;
    private bool wasAiming;
    private const float AIR_CONTROL = 0.3f;

    public PlayerFallingState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void EnterState()
    {
        // Keep current momentum
        momentum = stateMachine.Controller.velocity;
        stateMachine.FallTime = 0;
    }

    public override void UpdateState()
    {
        // Track fall time for landing impact
        stateMachine.AirTime += Time.deltaTime;
        stateMachine.FallTime += Time.deltaTime;

        // Apply gravity
        momentum.y += stateMachine.gravity * Time.deltaTime;

        HandleCameraTransition();
        HandleAirControl();

        // Check for landing
        if (stateMachine.IsGrounded)
        {
            if (stateMachine.FallTime > stateMachine.fallThreshold)
            {
                stateMachine.SwitchState(stateMachine.LandingState);
            }
            else
            {
                stateMachine.SwitchState(stateMachine.GroundedState);
            }
        }
    }

    public override void FixedUpdateState()
    {
        stateMachine.Controller.Move(momentum * Time.fixedDeltaTime);
    }

    private void HandleCameraTransition()
    {
        bool isAiming = stateMachine.IsAiming;

        if (isAiming != wasAiming)
        {
            stateMachine.aimCamera.Priority = isAiming ? 15 : 0;
            stateMachine.freeLookCamera.Priority = isAiming ? 10 : 15;
            wasAiming = isAiming;
        }
    }

    private void HandleAirControl()
    {
        // Calculate move direction relative to camera
        var cameraTransform = stateMachine.IsAiming ? 
            stateMachine.aimCamera.transform : 
            stateMachine.freeLookCamera.transform;

        var forward = cameraTransform.forward;
        var right = cameraTransform.right;

        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        // Calculate desired movement direction
        moveDirection = (forward * stateMachine.MovementInput.y + 
                        right * stateMachine.MovementInput.x).normalized;

        // Apply limited air control
        Vector3 horizontalVelocity = new Vector3(momentum.x, 0, momentum.z);
        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            moveDirection * stateMachine.runSpeed,
            stateMachine.acceleration * AIR_CONTROL * Time.deltaTime
        );

        // Update momentum
        momentum.x = horizontalVelocity.x;
        momentum.z = horizontalVelocity.z;

        // Rotate player if there's movement input
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            stateMachine.transform.rotation = Quaternion.RotateTowards(
                stateMachine.transform.rotation,
                targetRotation,
                stateMachine.rotationSpeed * Time.deltaTime * 50f // Reduced rotation speed in air
            );
        }
    }

    public override void ExitState()
    {
        // Preserve momentum for next state
        momentum.y = 0; // Reset vertical momentum
    }
}