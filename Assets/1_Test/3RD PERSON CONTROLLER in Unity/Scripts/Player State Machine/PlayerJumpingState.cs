
using UnityEngine;

public class PlayerJumpingState : PlayerBaseState
{
    private Vector3 momentum;
    private float jumpStartTime;
    private const float MAX_JUMP_TIME = 0.4f; // Maximum time in jumping state

    public PlayerJumpingState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void EnterState()
    {
        // Preserve horizontal momentum from previous state
        momentum = stateMachine.Controller.velocity;
        momentum.y = stateMachine.jumpForce;
        jumpStartTime = Time.time;
    }

    public override void UpdateState()
    {
        // Apply gravity
        momentum.y += stateMachine.gravity * Time.deltaTime;

        // Handle air control
        HandleAirControl();

        // Check state transitions
        if (Time.time - jumpStartTime >= MAX_JUMP_TIME || momentum.y < 0)
        {
            stateMachine.SwitchState(stateMachine.FallingState);
            return;
        }

        if (stateMachine.Controller.isGrounded && momentum.y <= 0)
        {
            stateMachine.SwitchState(stateMachine.GroundedState);
            return;
        }
    }

    public override void FixedUpdateState()
    {
        stateMachine.Controller.Move(momentum * Time.fixedDeltaTime);
    }

    private void HandleAirControl()
    {
        // Calculate move direction relative to camera
        var forward = stateMachine.freeLookCamera.transform.forward;
        var right = stateMachine.freeLookCamera.transform.right;

        forward.y = 0;
        right.y = 0;

        Vector3 airMoveDirection = forward.normalized * stateMachine.MovementInput.y +
                                 right.normalized * stateMachine.MovementInput.x;

        // Apply limited air control
        float airControlMultiplier = 0.3f;
        momentum += airMoveDirection * (stateMachine.walkSpeed * airControlMultiplier * Time.deltaTime);

        // Limit horizontal speed
        Vector3 horizontalVelocity = new Vector3(momentum.x, 0, momentum.z);
        if (horizontalVelocity.magnitude > stateMachine.runSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * stateMachine.runSpeed;
            momentum.x = horizontalVelocity.x;
            momentum.z = horizontalVelocity.z;
        }
    }

    public override void ExitState()
    {
        // Pass the current momentum to the next state through the controller's velocity
        stateMachine.Controller.Move(momentum * Time.fixedDeltaTime);
    }
}