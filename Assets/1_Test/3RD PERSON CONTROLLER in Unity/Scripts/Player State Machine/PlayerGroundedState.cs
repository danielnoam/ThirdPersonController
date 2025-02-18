
using UnityEngine;

public class PlayerGroundedState : PlayerBaseState
{
    private Vector3 _moveDirection;
    private float _targetMoveSpeed;
    private Vector3 _gravityForce;
    private float _movementIntensity;

    public PlayerGroundedState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void EnterState()
    {
        _gravityForce = Vector3.zero;
    }
    
    public override void ExitState()
    {
        _moveDirection = Vector3.zero;
    }

    public override void UpdateState()
    {
        // Check for state transitions
        if (!stateMachine.IsGrounded)
        {
            stateMachine.SwitchState(stateMachine.FallingState);
            return;
        }

        if (stateMachine.JumpPressed)
        {
            stateMachine.SwitchState(stateMachine.JumpingState);
            return;
        }
        
        
        HandleMovement();
        ApplyGravity();
    }

    public override void FixedUpdateState()
    {
        stateMachine.MoveCharacter(_moveDirection);
    }
    

    private void HandleMovement()
    {
        float movementIntensity = Mathf.Clamp01(
            Mathf.Abs(stateMachine.MovementInput.x) + 
            Mathf.Abs(stateMachine.MovementInput.y)
        );

        float targetSpeed = stateMachine.CalculateTargetSpeed(movementIntensity);
        stateMachine.UpdateMoveSpeed(targetSpeed);

        _moveDirection = stateMachine.CalculateMoveDirection();
        stateMachine.RotateTowardsMoveDirection(_moveDirection);
    }

    private void ApplyGravity()
    {
        if (!stateMachine.IsGrounded)
        {
            _gravityForce.y += stateMachine.gravity * Time.deltaTime;
        }
        else if (_gravityForce.y < 0)
        {
            _gravityForce.y = stateMachine.groundedGravity;
        }
    }
    
}