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
        _currentSpeed = StateMachine.activeMoveSpeed;
    }
    
    public override void ExitState()
    {
        _moveDirection = Vector3.zero;
    }

    public override void UpdateState()
    {
        // Gradually reduce speed to 0
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, StateMachine.acceleration * 2f * Time.deltaTime);

        // Only reset move direction when completely stopped
        if (_currentSpeed <= 0.01f)
        {
            _moveDirection = Vector3.zero;
        }

        // Update state machine's speed for animations
        StateMachine.SetMoveSpeed(_currentSpeed);

        CheckStateTransitions();
    }


    public override void FixedUpdateState()
    {
        // Create movement vector using the calculated speed and direction
        Vector3 movement = _moveDirection * _currentSpeed;
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