using UnityEngine;

public class PlayerLandingState : PlayerBaseState
{
    private float _recoveryProgress;
    private float _landingIntensity;

    public PlayerLandingState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void EnterState()
    {
        // Calculate landing intensity based on fall time
        _landingIntensity = Mathf.Clamp01(
            (StateMachine.FallTime - StateMachine.fallThreshold) / 
            (StateMachine.maxFallTime - StateMachine.fallThreshold)
        );
        
        // Start with recovery progress at 0
        _recoveryProgress = 0f;
        
        // Set state machine's landing intensity for animations
        StateMachine.SetLandingIntensity(_landingIntensity);
    }
    
    public override void ExitState()
    {
        StateMachine.SetLandingIntensity(0);
        StateMachine.SetFallTime(0);
        StateMachine.SetAirTime(0);
    }

    public override void UpdateState()
    {
        // Update recovery progress
        _recoveryProgress = Mathf.Min(1f, _recoveryProgress + (Time.deltaTime / (StateMachine.recoveryDuration * _landingIntensity)));
        
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        StateMachine.SetVerticalVelocity(StateMachine.groundedGravity);
        HandleMovement();
        
        // New: Handle aim rotation if aiming
        StateMachine.HandleAimRotation();
    }

    private void HandleMovement()
    {
        Vector3 inputDirection = StateMachine.CalculateMoveDirection();
        float inputMagnitude = inputDirection.magnitude;
    
        // Calculate movement control based on landing intensity and recovery progress
        float movementControl = Mathf.Lerp(
            StateMachine.minMovementControl,
            1f,
            _recoveryProgress
        );
    
        // Only update direction if we have meaningful input
        if (inputMagnitude > PlayerInput.MovementInputThreshold)
        {
            StateMachine.SetMoveDirection(inputDirection);
        
            // Update rotation if moving and NOT aiming
            if (inputDirection.sqrMagnitude > PlayerInput.RotationInputThreshold && !StateMachine.IsAiming)
            {
                Quaternion targetRotation = Quaternion.LookRotation(inputDirection);
                // Use movement control as multiplier to limit rotation during recovery
                StateMachine.RotateCharacter(
                    targetRotation, 
                    StateMachine.rotationSpeed, 
                    movementControl * (StateMachine.activeMoveSpeed > 0.1f ? 1.5f : 1f)
                );
            }

            // Calculate target speed with movement restriction
            float targetSpeed = StateMachine.CalculateTargetSpeed(inputMagnitude) * movementControl;
    
            float newSpeed = Mathf.MoveTowards(
                StateMachine.activeMoveSpeed,
                targetSpeed,
                StateMachine.acceleration * movementControl * Time.fixedDeltaTime
            );
            
            StateMachine.SetMoveSpeed(newSpeed);
        }
        else
        {
            // No input - decelerate to 0
            float newSpeed = Mathf.MoveTowards(
                StateMachine.activeMoveSpeed,
                0f,
                StateMachine.acceleration * movementControl * Time.fixedDeltaTime
            );
            
            StateMachine.SetMoveSpeed(newSpeed);
        }

        // Only reset move direction when completely stopped
        if (StateMachine.activeMoveSpeed <= 0.01f)
        {
            StateMachine.SetMoveDirection(Vector3.zero);
        }
    }

    private void CheckStateTransitions()
    {
        // Grounded
        if (_recoveryProgress >= 1f)
        {
            StateMachine.SwitchState(StateMachine.GroundedState);
            return;
        }
        
        // Jump
        if (StateMachine.input.JumpInput)
        {
            StateMachine.SwitchState(StateMachine.JumpingState);
            return;
        }
    }
}