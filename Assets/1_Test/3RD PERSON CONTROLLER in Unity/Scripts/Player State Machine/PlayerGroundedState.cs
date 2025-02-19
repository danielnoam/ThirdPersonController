using UnityEngine;

public class PlayerGroundedState : PlayerBaseState
{
    private Vector3 _moveDirection;
    private Vector2 _lastMovementInput;
    private bool _shouldUpdateDirection;
    private float _currentSpeed;

    public PlayerGroundedState(PlayerStateMachine stateMachine) : base(stateMachine) 
    {
        _shouldUpdateDirection = true;
    }
    
    public override void EnterState()
    {
        _shouldUpdateDirection = true;
        _currentSpeed = 0f;
    }
    
    public override void ExitState()
    {
        _moveDirection = Vector3.zero;
    }

    public override void UpdateState()
    {
        // Check for state transitions
        if (!StateMachine.IsGrounded && StateMachine.FallTime > StateMachine.fallThreshold)
        {
            StateMachine.SwitchState(StateMachine.FallingState);
            return;
        }

        if (StateMachine.JumpPressed)
        {
            StateMachine.SwitchState(StateMachine.JumpingState);
            return;
        }

        // Check if movement input has changed
        if (_lastMovementInput != StateMachine.MovementInput)
        {
            _lastMovementInput = StateMachine.MovementInput;
            _shouldUpdateDirection = true;
        }
        
        HandleRotation();
        HandleMovement();
    }

    public override void FixedUpdateState()
    {
        Vector3 finalMovement = _moveDirection * _currentSpeed;
        StateMachine.MoveCharacter(finalMovement);
    }
    
    private void HandleRotation()
    {
        if (_moveDirection.sqrMagnitude > PlayerStateMachine.MinMovementThreshold)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_moveDirection);
            StateMachine.RotateCharacter(targetRotation);
        }
    }

    private void HandleMovement()
    {
        // Only recalculate direction if input has changed
        if (_shouldUpdateDirection)
        {
            _moveDirection = StateMachine.CalculateMoveDirection();
            _shouldUpdateDirection = false;
        }
        
        // Calculate movement intensity (0-1)
        float movementIntensity = Mathf.Clamp01(
            Mathf.Abs(StateMachine.MovementInput.x) + 
            Mathf.Abs(StateMachine.MovementInput.y)
        );

        // Get target speed
        float targetSpeed = StateMachine.CalculateTargetSpeed(movementIntensity);
        
        // Update current speed with acceleration
        _currentSpeed = Mathf.MoveTowards(
            _currentSpeed, 
            targetSpeed, 
            StateMachine.acceleration * Time.deltaTime
        );
        
        // Update state machine's speed for animations etc
        StateMachine.SetMoveSpeed(_currentSpeed);
    }
}