
using UnityEngine;

public class PlayerLandingState : PlayerBaseState
{
    private float _landingRecoveryTime;
    private Vector3 _moveDirection;
    private Vector3 _gravityForce;
    private bool _wasAiming;
    private float _stateTimer;
    private float _movementMultiplier;

    public PlayerLandingState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void EnterState()
    {
        // Calculate landing intensity based on fall time
        stateMachine.LandingIntensity = Mathf.Clamp01(
            (stateMachine.FallTime - stateMachine.fallThreshold) / 
            (stateMachine.maxFallTime - stateMachine.fallThreshold)
        );

        // Set recovery time based on landing intensity
        _landingRecoveryTime = stateMachine.LandingIntensity * stateMachine.recoveryDuration;
        _stateTimer = 0;

        // Reset movement values
        _moveDirection = Vector3.zero;
        _gravityForce = Vector3.up * stateMachine.groundedGravity;
    }
    
    public override void ExitState()
    {
        // Reset state values
        stateMachine.SetMoveSpeed(0);
        _moveDirection = Vector3.zero;
        stateMachine.LandingIntensity = 0;
        stateMachine.FallTime = 0;
        stateMachine.AirTime = 0;
    }

    public override void UpdateState()
    {
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

        // Update timers
        _stateTimer += Time.deltaTime;
        if (_landingRecoveryTime > 0)
        {
            _landingRecoveryTime -= Time.deltaTime;
        }
        else if (_stateTimer > stateMachine.LandingIntensity * stateMachine.recoveryDuration * 0.5f)
        {
            stateMachine.SwitchState(stateMachine.GroundedState);
            return;
        }
        
        HandleMovement();
        ApplyGravity();
    }

    public override void FixedUpdateState()
    {
        stateMachine.MoveCharacter(_moveDirection, _movementMultiplier);
    }
    

    private void HandleMovement()
    {
        float recoveryProgress = 1f - (_landingRecoveryTime / (stateMachine.recoveryDuration * stateMachine.LandingIntensity));
        _movementMultiplier = Mathf.Lerp(stateMachine.minMovementControl, 1f, recoveryProgress);

        float movementIntensity = Mathf.Clamp01(
            Mathf.Abs(stateMachine.MovementInput.x) + 
            Mathf.Abs(stateMachine.MovementInput.y)
        );

        float targetSpeed = stateMachine.CalculateTargetSpeed(movementIntensity);
        stateMachine.UpdateMoveSpeed(targetSpeed);

        _moveDirection = stateMachine.CalculateMoveDirection();
        stateMachine.RotateTowardsMoveDirection(_moveDirection, _movementMultiplier);
    }
    
    
    private void ApplyGravity()
    {
        if (!stateMachine.IsGrounded)
        {
            _gravityForce.y += stateMachine.gravity * Time.deltaTime;
        }
        else
        {
            _gravityForce.y = stateMachine.groundedGravity;
        }
    }
}