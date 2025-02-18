
using UnityEngine;

public class PlayerGroundedState : PlayerBaseState
{
    private Vector3 moveDirection;
    private float activeMoveSpeed;
    private float targetMoveSpeed;
    private Vector3 gravityForce;
    private bool wasAiming;
    private float movementIntensity;

    public PlayerGroundedState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void EnterState()
    {
        // Reset values
        gravityForce = Vector3.zero;
        activeMoveSpeed = 0f;
        targetMoveSpeed = stateMachine.runSpeed;
        wasAiming = false;
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

        if (stateMachine.CrouchPressed)
        {
            stateMachine.SwitchState(stateMachine.CrouchingState);
            return;
        }

        // HandleCameraTransition();
        HandleMovement();
        ApplyGravity();
    }

    public override void FixedUpdateState()
    {
        // Apply both movement and gravity
        Vector3 totalMovement = (moveDirection * activeMoveSpeed * Time.fixedDeltaTime) + 
                               (gravityForce * Time.fixedDeltaTime);
        stateMachine.Controller.Move(totalMovement);
    }

    private void HandleCameraTransition()
    {
        bool isAiming = stateMachine.IsAiming;

        if (isAiming != wasAiming)
        {
            stateMachine.aimCamera.Priority = isAiming ? 15 : 0;
            stateMachine.freeLookCamera.Priority = isAiming ? 10 : 15;
            wasAiming = isAiming;
        }
    }

    private void HandleMovement()
    {
        // Calculate movement intensity for speed calculations
        movementIntensity = Mathf.Clamp01(
            Mathf.Abs(stateMachine.MovementInput.x) + 
            Mathf.Abs(stateMachine.MovementInput.y)
        );

        // Build raw input vector and clamp magnitude
        Vector3 rawInput = new Vector3(stateMachine.MovementInput.x, 0, stateMachine.MovementInput.y);
        rawInput = Vector3.ClampMagnitude(rawInput, 1f);

        // Calculate input frame based on active camera
        var cameraTransform = stateMachine.IsAiming ? 
            stateMachine.aimCamera.transform : 
            stateMachine.freeLookCamera.transform;

        var forward = cameraTransform.forward;
        var right = cameraTransform.right;
        
        // Project camera directions onto the horizontal plane
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        // Calculate movement direction relative to camera
        moveDirection = (forward * rawInput.z + right * rawInput.x).normalized;

        // Handle speed based on input
        if (movementIntensity < 0.1f)
        {
            targetMoveSpeed = 0f;
        }
        else
        {
            if (stateMachine.WalkPressed)
            {
                targetMoveSpeed = stateMachine.walkSpeed;
            }
            else if (stateMachine.SprintPressed && movementIntensity > 0.5f)
            {
                targetMoveSpeed = stateMachine.runSpeed * stateMachine.sprintSpeedMultiplier;
            }
            else
            {
                targetMoveSpeed = stateMachine.runSpeed;
            }
        }

        // Smoothly update active speed
        activeMoveSpeed = Mathf.MoveTowards(
            activeMoveSpeed, 
            targetMoveSpeed, 
            stateMachine.acceleration * Time.deltaTime
        );

        // Handle rotation
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            stateMachine.transform.rotation = Quaternion.RotateTowards(
                stateMachine.transform.rotation,
                targetRotation,
                stateMachine.rotationSpeed * Time.deltaTime * 100f
            );
        }
    }

    private void ApplyGravity()
    {
        if (!stateMachine.IsGrounded)
        {
            gravityForce.y += stateMachine.gravity * Time.deltaTime;
        }
        else if (gravityForce.y < 0)
        {
            gravityForce.y = stateMachine.groundedGravity;
        }
    }

    public override void ExitState()
    {
        activeMoveSpeed = 0;
        moveDirection = Vector3.zero;
    }
}