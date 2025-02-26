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
        
    }
    
    public override void ExitState()
    {
        _moveDirection = Vector3.zero;
    }

    public override void UpdateState()
    {
        HandleMovement();
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        // Create movement vector using the calculated speed and direction
        Vector3 movement = _moveDirection * _currentSpeed;
        movement.y = StateMachine.groundedGravity;
    
        // This ensures the animation speed matches the movement speed
        StateMachine.SetMoveSpeed(_currentSpeed);
    
        StateMachine.MoveCharacter(movement);
    }
    
    private void HandleMovement()
    {
        // Get camera-relative movement direction
        Vector3 inputDirection = StateMachine.CalculateMoveDirection();
        float inputMagnitude = inputDirection.magnitude;
    
        // Calculate movement intensity (0-1)
        float movementIntensity = Mathf.Clamp01(
            Mathf.Abs(StateMachine.input.MovementInput.x) + 
            Mathf.Abs(StateMachine.input.MovementInput.y)
        );

        // Get target speed
        float targetSpeed = StateMachine.CalculateTargetSpeed(movementIntensity);
    
        // Only update direction if we have meaningful input
        if (inputMagnitude > PlayerInput.MovementInputThreshold)
        {
            _moveDirection = inputDirection;
        
            // Update rotation if moving
            if (_moveDirection.sqrMagnitude > PlayerInput.RotationInputThreshold)
            {
                _targetRotation = Quaternion.LookRotation(_moveDirection);
            }
        }
        else
        {
            // No input - keep last direction but set target speed to 0
            targetSpeed = 0f;
        }
    
        // Update current speed with acceleration
        _currentSpeed = Mathf.MoveTowards(
            _currentSpeed, 
            targetSpeed, 
            StateMachine.acceleration * Time.deltaTime
        );
    
        // Only reset move direction when completely stopped
        if (_currentSpeed <= 0.01f)
        {
            _moveDirection = Vector3.zero;
        }
    
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

        if (StateMachine.input.JumpInput)
        {
            StateMachine.SwitchState(StateMachine.JumpingState);
            return;
        }
        
        if (StateMachine.CanInteract && StateMachine.input.PlayerInteractInput)
        {
            StateMachine.currentInteractable.Interact(StateMachine.gameObject);
            StateMachine.input.ConsumePlayerInteractBuffer();
            StateMachine.SwitchState(StateMachine.InteractingState);
        }
        
    }
}