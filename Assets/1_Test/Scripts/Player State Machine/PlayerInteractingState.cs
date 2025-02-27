using UnityEngine;

public class PlayerInteractingState : PlayerBaseState
{
    public PlayerInteractingState(PlayerStateMachine stateMachine) : base(stateMachine) { }
    
    public override void EnterState()
    {
        // We inherit the current speed and direction
    }
    
    public override void ExitState()
    {
        // Don't reset movement properties here - let the next state handle it
    }

    public override void UpdateState()
    {
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        HandleMovement();
        StateMachine.SetVerticalVelocity(StateMachine.groundedGravity);
    }
    
    private void HandleMovement()
    {
        // Gradually reduce speed to 0
        float newSpeed = Mathf.MoveTowards(
            StateMachine.activeMoveSpeed, 
            0f, 
            StateMachine.acceleration * 2f * Time.fixedDeltaTime
        );
        
        StateMachine.SetMoveSpeed(newSpeed);

        // Only reset move direction when completely stopped
        if (newSpeed <= 0.01f)
        {
            StateMachine.SetMoveDirection(Vector3.zero);
        }
        
        // If aiming, maintain camera-facing direction
        if (StateMachine.IsAiming)
        {
            Vector3 aimDirection = StateMachine.GetCameraAimDirection();
            Quaternion targetRotation = Quaternion.LookRotation(aimDirection);
            
            // Smoothly rotate to face camera direction
            StateMachine.RotateCharacter(
                targetRotation, 
                StateMachine.aimRotationSpeed,
                1.0f
            );
        }
    }
    
    private void CheckStateTransitions()
    {
        // Fall
        if (!StateMachine.IsGrounded && StateMachine.FallTime > StateMachine.fallThreshold)
        {
            StateMachine.SwitchState(StateMachine.FallingState);
            StateMachine.currentInteractable.CancelInteraction();
            return;
        }

        // Jump
        if (StateMachine.input.JumpInput)
        {
            StateMachine.SwitchState(StateMachine.JumpingState);
            StateMachine.currentInteractable.CancelInteraction();
            return;
        }
    }
    
    public void OnInteractionComplete(MultiStateInteractable interactable)
    {
        StateMachine.SwitchState(StateMachine.GroundedState);
    }
}