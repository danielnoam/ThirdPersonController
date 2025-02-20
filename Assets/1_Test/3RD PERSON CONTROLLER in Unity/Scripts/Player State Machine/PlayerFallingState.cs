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
        Vector3 movement = _moveDirection * _currentSpeed;
        movement.y = _verticalVelocity;
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
        
        if (inputDirection.magnitude > PlayerStateMachine.MovementInputThreshold)
        {
            _moveDirection = inputDirection;
        
            // Update rotation if moving
            if (_moveDirection.sqrMagnitude > PlayerStateMachine.RotationInputThreshold)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_moveDirection);
                // Use air rotation speed multiplier based on movement
                StateMachine.RotateCharacter(targetRotation, StateMachine.airRotationSpeed, _currentSpeed > 0.1f ? 1.5f : 1f);
            }

            float targetSpeed = StateMachine.airMoveSpeed;
            float speedChange = _currentSpeed > targetSpeed ? 
                StateMachine.airFriction : 
                StateMachine.airAcceleration;

            _currentSpeed = Mathf.MoveTowards(
                _currentSpeed,
                targetSpeed,
                speedChange * Time.deltaTime
            );
        }
        else
        {
            // No input - decelerate to 0
            _currentSpeed = Mathf.MoveTowards(
                _currentSpeed,
                0f,
                StateMachine.airFriction * Time.deltaTime
            );
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