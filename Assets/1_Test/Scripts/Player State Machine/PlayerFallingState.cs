using UnityEngine;

public class PlayerFallingState : PlayerBaseState
{
    public PlayerFallingState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void EnterState()
    {
        StateMachine.FallTime = 0f;
    }

    public override void ExitState()
    {

    }

    public override void UpdateState()
    {
        StateMachine.AirTime += Time.deltaTime;
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        HandleMovement();
        HandleGravity();

        // Handle aim rotation if aiming - this now uses the improved rotation logic
        StateMachine.HandleAimRotation();
    }

    private void HandleGravity()
    {
        // Calculate new vertical velocity
        float newVerticalVelocity = StateMachine.CalculateGravityVelocity(
            StateMachine.activeVerticalVelocity,
            Time.fixedDeltaTime
        );

        // Update the state machine's vertical velocity
        StateMachine.activeVerticalVelocity = newVerticalVelocity;
    }

    private void HandleMovement()
    {
        Vector3 inputDirection;
        Quaternion targetRotation;

        // Determine movement direction based on aiming state
        if (StateMachine.IsAiming)
        {
            // When aiming, get the camera direction
            Vector3 aimDirection = StateMachine.GetCameraAimDirection();
            targetRotation = Quaternion.LookRotation(aimDirection);

            // Calculate strafing movement relative to aim direction
            Vector3 forward = aimDirection;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            inputDirection = (forward * StateMachine.input.MovementInput.y +
                              right * StateMachine.input.MovementInput.x).normalized;
        }
        else
        {
            // Normal camera-relative movement
            inputDirection = StateMachine.CalculateMoveDirection();
            targetRotation = Quaternion.LookRotation(inputDirection);
        }

        float inputMagnitude = inputDirection.magnitude;
        float targetSpeed;

        if (inputMagnitude > PlayerInput.MovementInputThreshold)
        {
            // When we have input, update the direction
            StateMachine.activeMoveDirection = inputDirection;

            // Update rotation based on aim state
            if (StateMachine.IsAiming)
            {
                // Aim rotation is now handled in HandleAimRotation
                // No need to rotate character here
            }
            else if (inputDirection.sqrMagnitude > PlayerInput.RotationInputThreshold)
            {
                // Normal rotation when not aiming
                StateMachine.RotateCharacter(
                    targetRotation,
                    StateMachine.airRotationSpeed,
                    StateMachine.activeHorizontalVelocity > 0.1f ? 1.5f : 1f
                );
            }

            // Set target speed to air move speed
            targetSpeed = StateMachine.airMoveSpeed;
        }
        else
        {
            // No input - keep current direction but target zero speed
            targetSpeed = 0f;
        }

        // Determine acceleration/deceleration rate and update speed
        float speedChange = StateMachine.activeHorizontalVelocity > targetSpeed
            ? StateMachine.airFriction
            : StateMachine.airAcceleration;

        // Update speed
        float newSpeed = Mathf.MoveTowards(
            StateMachine.activeHorizontalVelocity,
            targetSpeed,
            speedChange * Time.fixedDeltaTime
        );

        StateMachine.activeHorizontalVelocity = newSpeed;

        // Only reset move direction when completely stopped
        if (newSpeed <= 0.01f)
        {
            StateMachine.activeMoveDirection = Vector3.zero;
        }
    }

    private void CheckStateTransitions()
    {
        if (StateMachine.IsGrounded)
        {
            if (StateMachine.FallTime > StateMachine.fallThreshold)
            {
                StateMachine.SwitchState(StateMachine.LandingState);
                return;
            }
            else
            {
                StateMachine.SwitchState(StateMachine.GroundedState);
                return;
            }
        }
    }
}