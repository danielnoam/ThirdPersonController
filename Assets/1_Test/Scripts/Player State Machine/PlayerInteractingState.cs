using UnityEngine;

public class PlayerInteractingState : PlayerBaseState
{
    private Vector3 _moveDirection;
    private float _currentSpeed;
    private Quaternion _targetRotation;

    public PlayerInteractingState(PlayerStateMachine stateMachine) : base(stateMachine) 
    {
        _targetRotation = stateMachine.transform.rotation;
    }
    
    public override void EnterState()
    {
        StateMachine.SetMoveSpeed(0f);
    }
    
    public override void ExitState()
    {
        _moveDirection = Vector3.zero;
    }

    public override void UpdateState()
    {
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        Vector3 movement = _moveDirection * _currentSpeed;
        // Apply constant downward force while grounded to stick to slopes
        movement.y = StateMachine.groundedGravity;
        StateMachine.MoveCharacter(movement);
    }
    
    
    private void CheckStateTransitions()
    {
        if (!StateMachine.IsGrounded && StateMachine.FallTime > StateMachine.fallThreshold)
        {
            StateMachine.SwitchState(StateMachine.FallingState);
            StateMachine.currentInteractable.CancelInteraction();
            return;
        }

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