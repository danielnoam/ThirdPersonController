using UnityEngine;

public class PlayerFallingState : PlayerBaseState
{
    private Vector3 _momentum;
    private Vector3 _moveDirection;

    public PlayerFallingState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void EnterState()
    {
        // Keep current momentum
        _momentum = stateMachine.Controller.velocity;
        stateMachine.FallTime = 0;
    }
    
    public override void ExitState()
    {
        // Reset vertical momentum
        _momentum.y = 0;
    }

    public override void UpdateState()
    {
        // Track fall time for landing impact
        stateMachine.AirTime += Time.deltaTime;
        stateMachine.FallTime += Time.deltaTime;

        // Update vertical and horizontal movement
        UpdateVerticalMovement();
        UpdateHorizontalMovement();

        // Check state transitions
        CheckStateTransitions();
    }
    
    public override void FixedUpdateState()
    {
        // Apply movement with air control
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
        // Check for landing
        if (stateMachine.IsGrounded)
        {
            if (stateMachine.FallTime > stateMachine.fallThreshold)
            {
                stateMachine.SwitchState(stateMachine.LandingState);
            }
            else
            {
                stateMachine.SwitchState(stateMachine.GroundedState);
            }
        }
    }
}