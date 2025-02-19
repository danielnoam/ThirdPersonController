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
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        // Apply gravity to vertical velocity
        _verticalVelocity += StateMachine.gravity * Time.deltaTime;
        
        Vector3 movement = _moveDirection * _currentSpeed;
        movement.y = _verticalVelocity;
        StateMachine.MoveCharacter(movement);
    }

    private void HandleMovement()
    {
        Vector3 inputDirection = StateMachine.CalculateMoveDirection();
        
        if (inputDirection.magnitude > PlayerStateMachine.MinMovementIntensity)
        {
            // Set move direction and handle rotation
            _moveDirection = inputDirection;
            
            // Rotate towards movement direction with air rotation speed
            if (_moveDirection.sqrMagnitude > PlayerStateMachine.MinMovementThreshold)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_moveDirection);
                StateMachine.RotateCharacter(targetRotation, StateMachine.airRotationSpeed);
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
        
        // Update state machine's speed for animations
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
        
        // If somehow grounded during jump, switch to grounded state
        if (StateMachine.IsGrounded)
        {
            StateMachine.SwitchState(StateMachine.GroundedState);
        }
    }
}