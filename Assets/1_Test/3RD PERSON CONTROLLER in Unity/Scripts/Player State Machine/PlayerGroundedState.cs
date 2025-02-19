using UnityEngine;

public class PlayerGroundedState : PlayerBaseState
{
    private Vector3 _moveDirection;
    private float _currentSpeed;
    private Quaternion _targetRotation;

    public PlayerGroundedState(PlayerStateMachine stateMachine) : base(stateMachine) 
    {
        _targetRotation = stateMachine.transform.rotation;
    }
    
    public override void EnterState()
    {
        _currentSpeed = 0f;
    }
    
    public override void ExitState()
    {
        _moveDirection = Vector3.zero;
    }

    public override void UpdateState()
    {
        HandleMovementAndRotation();
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        Vector3 movement = _moveDirection * _currentSpeed;
        // Apply constant downward force while grounded to stick to slopes
        movement.y = StateMachine.groundedGravity;
        StateMachine.MoveCharacter(movement);
    }
    
    private void HandleMovementAndRotation()
    {
        // Get camera-relative movement direction
        _moveDirection = StateMachine.CalculateMoveDirection();
    
        // Update rotation if moving
        if (_moveDirection.sqrMagnitude > PlayerStateMachine.RotationInputThreshold)
        {
            _targetRotation = Quaternion.LookRotation(_moveDirection);
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
        
        // Apply rotation - faster rotation when moving, slower when stopping
        StateMachine.RotateCharacter(_targetRotation, StateMachine.rotationSpeed, _currentSpeed > 0.1f ? 2f : 1f);
        
        // Update state machine's speed for animations
        StateMachine.SetMoveSpeed(_currentSpeed);
    }
    
    private void CheckStateTransitions()
    {
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
    }
}