
using UnityEngine;

public class PlayerJumpingState : PlayerBaseState
{
    private Vector3 _momentum;
    private Vector3 _moveDirection;

    public PlayerJumpingState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void EnterState()
    {
        // Preserve horizontal momentum from previous state
        _momentum = stateMachine.Controller.velocity;
        _momentum.y = stateMachine.jumpForce;
    }
    
    public override void ExitState()
    {
        // Pass the current momentum to the next state through the controller's velocity
        stateMachine.Controller.Move(_momentum * Time.fixedDeltaTime);
    }

    public override void UpdateState()
    {
        // Vertical movement (gravity and jump)
        UpdateVerticalMovement();
        
        // Horizontal movement (air control)
        UpdateHorizontalMovement();

        // State transitions
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        // Combine horizontal movement (with air control) and vertical momentum
        stateMachine.MoveCharacter(_moveDirection, stateMachine.airControl, _momentum);
    }

    private void UpdateVerticalMovement()
    {
        // Apply gravity to vertical momentum
        _momentum.y += stateMachine.gravity * Time.deltaTime;
    }

    private void UpdateHorizontalMovement()
    {
        // Get input-based movement direction relative to camera
        Vector3 inputDirection = stateMachine.CalculateMoveDirection();
    
        // Current horizontal velocity
        Vector3 horizontalVelocity = new Vector3(_momentum.x, 0, _momentum.z);
    
        if (inputDirection.magnitude > 0.1f)
        {
            // Calculate target velocity based on input
            float targetSpeed = stateMachine.CalculateTargetSpeed(inputDirection.magnitude);
            Vector3 targetVelocity = inputDirection * targetSpeed;

            // Blend between current and target velocity using airControl
            horizontalVelocity = Vector3.Lerp(
                horizontalVelocity,
                targetVelocity,
                stateMachine.airControl * Time.deltaTime
            );
        
            // Update move direction and handle rotation
            _moveDirection = inputDirection; // Use input direction for rotation
            stateMachine.RotateTowardsMoveDirection(_moveDirection, stateMachine.airRotation);
        }
        else
        {
            // When no input, maintain current direction but apply air drag
            _moveDirection = horizontalVelocity.normalized;
            horizontalVelocity = Vector3.Lerp(
                horizontalVelocity,
                Vector3.zero,
                stateMachine.airDrag * Time.deltaTime
            );
        }
    
        // Update momentum
        _momentum.x = horizontalVelocity.x;
        _momentum.z = horizontalVelocity.z;
    }

    private void CheckStateTransitions()
    {
        // We've reached the apex and started falling
        if (_momentum.y < 0)  
        {
            stateMachine.SwitchState(stateMachine.FallingState);
            return;
        }

        // Transition to grounded if we hit the ground moving downward
        if (stateMachine.Controller.isGrounded && _momentum.y <= 0)
        {
            stateMachine.SwitchState(stateMachine.GroundedState);
            return;
        }
    }
    
}