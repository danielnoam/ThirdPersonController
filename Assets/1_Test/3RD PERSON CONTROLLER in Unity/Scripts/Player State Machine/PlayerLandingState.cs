
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
        StateMachine.LandingIntensity = Mathf.Clamp01(
            (StateMachine.FallTime - StateMachine.fallThreshold) / 
            (StateMachine.maxFallTime - StateMachine.fallThreshold)
        );

        // Set recovery time based on landing intensity
        _landingRecoveryTime = StateMachine.LandingIntensity * StateMachine.recoveryDuration;
        _stateTimer = 0;

        // Reset movement values
        _moveDirection = Vector3.zero;
        _gravityForce = Vector3.up * StateMachine.groundedGravity;
    }
    
    public override void ExitState()
    {
        // Reset state values
        StateMachine.SetMoveSpeed(0);
        _moveDirection = Vector3.zero;
        StateMachine.LandingIntensity = 0;
        StateMachine.FallTime = 0;
        StateMachine.AirTime = 0;
    }

    public override void UpdateState()
    {
        if (!StateMachine.IsGrounded)
        {
            StateMachine.SwitchState(StateMachine.FallingState);
            return;
        }
        
        if (StateMachine.JumpPressed)
        {
            StateMachine.SwitchState(StateMachine.JumpingState);
            return;
        }

        // Update timers
        _stateTimer += Time.deltaTime;
        if (_landingRecoveryTime > 0)
        {
            _landingRecoveryTime -= Time.deltaTime;
        }
        else if (_stateTimer > StateMachine.LandingIntensity * StateMachine.recoveryDuration * 0.5f)
        {
            StateMachine.SwitchState(StateMachine.GroundedState);
            return;
        }
        
        HandleMovement();
        ApplyGravity();
    }

    public override void FixedUpdateState()
    {
        StateMachine.MoveCharacter(_moveDirection, _movementMultiplier);
    }
    

    private void HandleMovement()
    {
        float recoveryProgress = 1f - (_landingRecoveryTime / (StateMachine.recoveryDuration * StateMachine.LandingIntensity));
        _movementMultiplier = Mathf.Lerp(StateMachine.minMovementControl, 1f, recoveryProgress);

        float movementIntensity = Mathf.Clamp01(
            Mathf.Abs(StateMachine.MovementInput.x) + 
            Mathf.Abs(StateMachine.MovementInput.y)
        );

        float targetSpeed = StateMachine.CalculateTargetSpeed(movementIntensity);
        StateMachine.UpdateMoveSpeed(targetSpeed);

        _moveDirection = StateMachine.CalculateMoveDirection();
        StateMachine.RotateTowardsMoveDirection(_moveDirection, _movementMultiplier);
    }
    
    
    private void ApplyGravity()
    {
        if (!StateMachine.IsGrounded)
        {
            _gravityForce.y += StateMachine.gravity * Time.deltaTime;
        }
        else
        {
            _gravityForce.y = StateMachine.groundedGravity;
        }
    }
}