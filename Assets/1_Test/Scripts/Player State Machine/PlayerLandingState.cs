using UnityEngine;

public class PlayerLandingState : PlayerBaseState
{
    private float _recoveryProgress;
    private float _landingIntensity;

    public PlayerLandingState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void EnterState()
    {
        // Calculate landing intensity based on fall time
        _landingIntensity = Mathf.Clamp01(StateMachine.FallTime / StateMachine.maxFallTime);
        
        // Store landing intensity in state machine for animation system to access
        StateMachine.LandingIntensity = _landingIntensity;
        
        // Start recovery at 0
        _recoveryProgress = 0f;
    }
    
    public override void ExitState()
    {
        StateMachine.FallTime = 0f;
        StateMachine.AirTime = 0f;
        
        // Reset landing intensity
        StateMachine.LandingIntensity = 0f;
    }

    public override void UpdateState()
    {
        // Update recovery progress
        _recoveryProgress = Mathf.Min(1f, _recoveryProgress + (Time.deltaTime / (StateMachine.recoveryDuration * _landingIntensity)));
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        StateMachine.activeVerticalVelocity = StateMachine.groundedGravity;
        HandleMovement();
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
            StateMachine.activeMoveDirection = inputDirection;
        
            // Update rotation if moving and NOT aiming
            if (inputDirection.sqrMagnitude > PlayerInput.RotationInputThreshold && !StateMachine.IsAiming)
            {
                Quaternion targetRotation = Quaternion.LookRotation(inputDirection);
                // Use movement control as multiplier to limit rotation during recovery
                StateMachine.RotateCharacter(
                    targetRotation, 
                    StateMachine.rotationSpeed, 
                    movementControl * (StateMachine.activeHorizontalVelocity > 0.1f ? 1.5f : 1f)
                );
            }

            // Calculate target speed with movement restriction
            float targetSpeed = StateMachine.CalculateTargetSpeed(inputMagnitude) * movementControl;
    
            float newSpeed = Mathf.MoveTowards(
                StateMachine.activeHorizontalVelocity,
                targetSpeed,
                StateMachine.acceleration * movementControl * Time.fixedDeltaTime
            );
            
            StateMachine.activeHorizontalVelocity = newSpeed;
        }
        else
        {
            // No input - decelerate to 0
            float newSpeed = Mathf.MoveTowards(
                StateMachine.activeHorizontalVelocity,
                0f,
                StateMachine.acceleration * movementControl * Time.fixedDeltaTime
            );
            
            StateMachine.activeHorizontalVelocity = newSpeed;
        }

        // Only reset move direction when completely stopped
        if (StateMachine.activeHorizontalVelocity <= 0.01f)
        {
            StateMachine.activeMoveDirection = Vector3.zero;
        }
    }

    private void CheckStateTransitions()
    {
        // Grounded
        if (_recoveryProgress >= 0f) // 1f
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