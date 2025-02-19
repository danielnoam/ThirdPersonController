using UnityEngine;

public class PlayerFallingState : PlayerBaseState
{
    public PlayerFallingState(PlayerStateMachine stateMachine) : base(stateMachine) { }
    
    private Vector3 _moveDirection;
    private float _currentSpeed;

    public override void EnterState()
    {
        StateMachine.SetFallTime(0);
        _currentSpeed = StateMachine.activeMoveSpeed;
    }
    
    public override void ExitState()
    {
        _moveDirection = Vector3.zero;
    }

    public override void UpdateState()
    {
        StateMachine.SetFallTime(StateMachine.FallTime + Time.deltaTime);
        StateMachine.SetAirTime(StateMachine.AirTime + Time.deltaTime);

        HandleMovement();
        CheckStateTransitions();
    }
    
    public override void FixedUpdateState()
    {
        Vector3 movement = _moveDirection * _currentSpeed;
        movement.y += StateMachine.gravity * Time.deltaTime;
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