using UnityEngine;

public class PlayerJumpingState : PlayerBaseState
{
    private Vector3 _moveDirection;
    private float _currentSpeed;
    private float _verticalVelocity;

    public PlayerJumpingState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void EnterState()
    {
        _currentSpeed = StateMachine.activeMoveSpeed;
        _verticalVelocity = StateMachine.jumpForce;
        StateMachine.SetAirTime(0);
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

            // If current speed is higher than air move speed, decelerate to it
            // Otherwise accelerate to air move speed
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
        // Transition to falling when vertical velocity becomes negative
        if (_verticalVelocity <= 0)
        {
            StateMachine.SwitchState(StateMachine.FallingState);
            return;
        }
        
        // // If somehow grounded during jump, switch to grounded state
        // if (StateMachine.IsGrounded)
        // {
        //     StateMachine.SwitchState(StateMachine.GroundedState);
        // }
    }
}