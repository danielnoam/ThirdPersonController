using UnityEngine;

public class PlayerLandingState : PlayerBaseState
{
    private Vector3 _moveDirection;
    private float _currentSpeed;
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
        
        // Start with minimal movement control
        _currentSpeed = 0f;
        _recoveryProgress = 0f;
        
        // Set state machine's landing intensity for animations
        StateMachine.SetLandingIntensity(_landingIntensity);
    }
    
    public override void ExitState()
    {
        _moveDirection = Vector3.zero;
        StateMachine.SetLandingIntensity(0);
        StateMachine.SetFallTime(0);
        StateMachine.SetAirTime(0);
    }

    public override void UpdateState()
    {
        
        // Update recovery progress
        _recoveryProgress = Mathf.Min(1f, _recoveryProgress + (Time.deltaTime / (StateMachine.recoveryDuration * _landingIntensity)));
        
        HandleMovement();
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        Vector3 movement = _moveDirection * _currentSpeed;
        movement.y = StateMachine.groundedGravity;
        StateMachine.MoveCharacter(movement);
    }

    private void HandleMovement()
    {
        Vector3 inputDirection = StateMachine.CalculateMoveDirection();
        
        if (inputDirection.magnitude > PlayerStateMachine.MovementInputThreshold)
        {
            _moveDirection = inputDirection;
        
            // Calculate movement control based on landing intensity and recovery progress
            float movementControl = Mathf.Lerp(
                StateMachine.minMovementControl,
                1f,
                _recoveryProgress
            );
        
            // Update rotation if moving
            if (_moveDirection.sqrMagnitude > PlayerStateMachine.RotationInputThreshold)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_moveDirection);
                // Use movement control as multiplier to limit rotation during recovery
                StateMachine.RotateCharacter(targetRotation, StateMachine.rotationSpeed, movementControl * (_currentSpeed > 0.1f ? 1.5f : 1f));
            }

            // Calculate target speed with movement restriction
            float targetSpeed = StateMachine.CalculateTargetSpeed(inputDirection.magnitude) * movementControl;
            
            _currentSpeed = Mathf.MoveTowards(
                _currentSpeed,
                targetSpeed,
                StateMachine.acceleration * movementControl * Time.deltaTime
            );
        }
        else
        {
            // No input - decelerate to 0
            _currentSpeed = Mathf.MoveTowards(
                _currentSpeed,
                0f,
                StateMachine.acceleration * Time.deltaTime
            );
        }
        
        StateMachine.SetMoveSpeed(_currentSpeed);
    }

    private void CheckStateTransitions()
    {
        // Transition to grounded state when recovered
        if (_recoveryProgress >= 1f)
        {
            StateMachine.SwitchState(StateMachine.GroundedState);
        }
        
        
        if (StateMachine.JumpPressed)
        {
            StateMachine.SwitchState(StateMachine.JumpingState);
            return;
        }
    }
}