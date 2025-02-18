
using UnityEngine;

public class PlayerLandingState : PlayerBaseState
{
    private float landingRecoveryTime;
    private Vector3 moveDirection;
    private float activeMoveSpeed;
    private float targetMoveSpeed;
    private Vector3 gravityForce;
    private bool wasAiming;
    private float stateTimer;

    public PlayerLandingState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void EnterState()
    {
        // Calculate landing intensity based on fall time
        stateMachine.LandingIntensity = Mathf.Clamp01(
            (stateMachine.FallTime - stateMachine.fallThreshold) / 
            (stateMachine.maxFallTime - stateMachine.fallThreshold)
        );

        // Set recovery time based on landing intensity
        landingRecoveryTime = stateMachine.LandingIntensity * stateMachine.recoveryDuration;
        stateTimer = 0;

        // Reset movement values
        activeMoveSpeed = 0;
        moveDirection = Vector3.zero;
        gravityForce = Vector3.up * stateMachine.groundedGravity;
    }

    public override void UpdateState()
    {
        if (!stateMachine.IsGrounded)
        {
            stateMachine.SwitchState(stateMachine.FallingState);
            return;
        }

        // Update timers
        stateTimer += Time.deltaTime;
        if (landingRecoveryTime > 0)
        {
            landingRecoveryTime -= Time.deltaTime;
        }
        else if (stateTimer > stateMachine.LandingIntensity * stateMachine.recoveryDuration * 0.5f)
        {
            stateMachine.SwitchState(stateMachine.GroundedState);
            return;
        }

        HandleCameraTransition();
        HandleMovement();
        ApplyGravity();
    }

    public override void FixedUpdateState()
    {
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
        // Calculate recovery progress
        float recoveryProgress = 1f - (landingRecoveryTime / (stateMachine.recoveryDuration * stateMachine.LandingIntensity));
        float movementMultiplier = Mathf.Lerp(stateMachine.minMovementControl, 1f, recoveryProgress);

        // Calculate move direction relative to camera
        var cameraTransform = stateMachine.IsAiming ? 
            stateMachine.aimCamera.transform : 
            stateMachine.freeLookCamera.transform;

        var forward = cameraTransform.forward;
        var right = cameraTransform.right;

        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        // Calculate movement direction
        moveDirection = (forward * stateMachine.MovementInput.y + 
                        right * stateMachine.MovementInput.x).normalized;

        float movementIntensity = Mathf.Clamp01(
            Mathf.Abs(stateMachine.MovementInput.x) + 
            Mathf.Abs(stateMachine.MovementInput.y)
        );

        // Set target speed with recovery multiplier
        if (movementIntensity > 0)
        {
            if (stateMachine.WalkPressed)
                targetMoveSpeed = stateMachine.walkSpeed;
            else if (stateMachine.SprintPressed && movementIntensity > 0.5f)
                targetMoveSpeed = stateMachine.runSpeed * stateMachine.sprintSpeedMultiplier;
            else
                targetMoveSpeed = stateMachine.runSpeed;

            targetMoveSpeed *= movementMultiplier;
        }
        else
        {
            targetMoveSpeed = 0;
        }

        // Smoothly update active speed
        activeMoveSpeed = Mathf.MoveTowards(
            activeMoveSpeed,
            targetMoveSpeed,
            stateMachine.acceleration * Time.deltaTime
        );

        // Rotate player if there's movement input
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            stateMachine.transform.rotation = Quaternion.RotateTowards(
                stateMachine.transform.rotation,
                targetRotation,
                stateMachine.rotationSpeed * movementMultiplier * Time.deltaTime * 100f
            );
        }
    }

    private void ApplyGravity()
    {
        if (!stateMachine.IsGrounded)
        {
            gravityForce.y += stateMachine.gravity * Time.deltaTime;
        }
        else
        {
            gravityForce.y = stateMachine.groundedGravity;
        }
    }

    public override void ExitState()
    {
        // Reset state values
        activeMoveSpeed = 0;
        moveDirection = Vector3.zero;
        stateMachine.LandingIntensity = 0;
        stateMachine.FallTime = 0;
        stateMachine.AirTime = 0;
    }
}