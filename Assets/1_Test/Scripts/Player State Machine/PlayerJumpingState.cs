using UnityEngine;

public class PlayerJumpingState : PlayerBaseState
{
    public PlayerJumpingState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void EnterState()
    {
        StateMachine.input.ConsumeJumpBuffer();
        StateMachine.SetVerticalVelocity(StateMachine.jumpForce);
        StateMachine.SetAirTime(0);
    }
    
    public override void ExitState()
    {

    }

    public override void UpdateState()
    {
        StateMachine.SetAirTime(StateMachine.AirTime + Time.deltaTime);
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        HandleMovement();
        HandleGravity();
        
        // New: Handle aim rotation if aiming
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
        StateMachine.SetVerticalVelocity(newVerticalVelocity);
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
            StateMachine.SetMoveDirection(inputDirection);
        
            // Update rotation based on aim state
            if (StateMachine.IsAiming)
            {
                // Always face camera direction when aiming
                StateMachine.RotateCharacter(
                    targetRotation, 
                    StateMachine.aimRotationSpeed, 
                    1.0f
                );
            } 
            else if (inputDirection.sqrMagnitude > PlayerInput.RotationInputThreshold)
            {
                // Normal rotation when not aiming
                StateMachine.RotateCharacter(
                    targetRotation, 
                    StateMachine.airRotationSpeed, 
                    StateMachine.activeMoveSpeed > 0.1f ? 1.5f : 1f
                );
            }

            // Set target speed to air move speed
            targetSpeed = StateMachine.airMoveSpeed;
        }
        else
        {
            // No input - keep current direction but target zero speed
            targetSpeed = 0f;
            
            // Still maintain aim rotation when aiming even without movement
            if (StateMachine.IsAiming)
            {
                targetRotation = Quaternion.LookRotation(StateMachine.GetCameraAimDirection());
                StateMachine.RotateCharacter(
                    targetRotation, 
                    StateMachine.aimRotationSpeed, 
                    1.0f
                );
            }
        }
    
        // Determine acceleration/deceleration rate and update speed
        float speedChange = StateMachine.activeMoveSpeed > targetSpeed ? 
            StateMachine.airFriction : 
            StateMachine.airAcceleration;

        // Update speed
        float newSpeed = Mathf.MoveTowards(
            StateMachine.activeMoveSpeed,
            targetSpeed,
            speedChange * Time.fixedDeltaTime
        );
        
        StateMachine.SetMoveSpeed(newSpeed);
        
        // Only reset move direction when completely stopped
        if (newSpeed <= 0.01f)
        {
            StateMachine.SetMoveDirection(Vector3.zero);
        }
    }

    private void CheckStateTransitions()
    {
        // Fall
        if (StateMachine.activeVerticalVelocity <= 0)
        {
            StateMachine.SwitchState(StateMachine.FallingState);
            return;
        }
    }
}