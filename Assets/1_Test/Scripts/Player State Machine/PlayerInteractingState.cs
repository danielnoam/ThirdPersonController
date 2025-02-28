using UnityEngine;

public class PlayerInteractingState : PlayerBaseState
{
    public PlayerInteractingState(PlayerStateMachine stateMachine) : base(stateMachine) { }
    
    public override void EnterState()
    {
        StateMachine.InputHandler.ConsumePlayerInteractBuffer();
    }
    
    public override void ExitState()
    {
    }

    public override void UpdateState()
    {
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        HandleMovement();
        StateMachine.activeVerticalVelocity = StateMachine.groundedGravity;
    }
    
    private void HandleMovement()
    {
        // Gradually reduce speed to 0
        float newSpeed = Mathf.MoveTowards(
            StateMachine.activeHorizontalVelocity, 
            0f, 
            StateMachine.acceleration * 2f * Time.fixedDeltaTime
        );
        
        StateMachine.activeHorizontalVelocity = newSpeed;

        // Only reset move direction when completely stopped
        if (newSpeed <= 0.01f)
        {
            StateMachine.activeMoveDirection = Vector3.zero;
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
        if (StateMachine.InputHandler.JumpInput)
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