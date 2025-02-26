using UnityEngine;

public class PlayerFallingState : PlayerBaseState
{
    private Vector3 _moveDirection;
    private float _currentSpeed;
    private float _verticalVelocity;

    public PlayerFallingState(PlayerStateMachine stateMachine) : base(stateMachine) { }
    
    public override void EnterState()
    {
        StateMachine.SetFallTime(0);
        _currentSpeed = StateMachine.activeMoveSpeed;
        _verticalVelocity = 0f;
    }
    
    public override void ExitState()
    {
        _moveDirection = Vector3.zero;
    }

    public override void UpdateState()
    {
        StateMachine.SetAirTime(StateMachine.AirTime + Time.deltaTime);
        
        HandleMovement();
        HandleGravity();
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        // Create movement vector using the calculated speed and direction
        Vector3 movement = _moveDirection * _currentSpeed;
        movement.y = _verticalVelocity;
    
        // This ensures the animation speed matches the movement speed
        StateMachine.SetMoveSpeed(_currentSpeed);
    
        StateMachine.MoveCharacter(movement);
    }

    private void HandleGravity()
    {
        // Apply gravity to vertical velocity
        _verticalVelocity += StateMachine.gravity * Time.deltaTime;
        
        // Limit to terminal velocity
        if (_verticalVelocity < StateMachine.maxVerticalVelocity)
        {
            _verticalVelocity = StateMachine.maxVerticalVelocity;
        }
    }

    private void HandleMovement()
    {
        Vector3 inputDirection = StateMachine.CalculateMoveDirection();
        float inputMagnitude = inputDirection.magnitude;

        // Determine target speed and acceleration/deceleration rate
        float targetSpeed;
        float speedChange;

        if (inputMagnitude > PlayerInput.MovementInputThreshold)
        {
            // Update direction when we have meaningful input
            _moveDirection = inputDirection;
        
            // Update rotation if moving
            if (_moveDirection.sqrMagnitude > PlayerInput.RotationInputThreshold)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_moveDirection);
                // Use air rotation speed multiplier based on movement
                StateMachine.RotateCharacter(targetRotation, StateMachine.airRotationSpeed, _currentSpeed > 0.1f ? 1.5f : 1f);
            }

            // Set target speed to air move speed
            targetSpeed = StateMachine.airMoveSpeed;
        }
        else
        {
            // No input - keep last valid direction but target zero speed
            targetSpeed = 0f;
        }
    
        // Determine acceleration/deceleration rate
        speedChange = _currentSpeed > targetSpeed ? 
            StateMachine.airFriction : 
            StateMachine.airAcceleration;

        // Update speed
        _currentSpeed = Mathf.MoveTowards(
            _currentSpeed,
            targetSpeed,
            speedChange * Time.deltaTime
        );
    
        // Only reset move direction when completely stopped
        if (_currentSpeed <= 0.01f)
        {
            _moveDirection = Vector3.zero;
        }
    
        StateMachine.SetMoveSpeed(_currentSpeed);
    }

    private void CheckStateTransitions()
    {
        if (StateMachine.IsGrounded)
        {
            if (StateMachine.FallTime > StateMachine.fallThreshold)
            {
                StateMachine.SwitchState(StateMachine.LandingState);
            }
            else
            {
                StateMachine.SwitchState(StateMachine.GroundedState);
            }
        }
    }
}