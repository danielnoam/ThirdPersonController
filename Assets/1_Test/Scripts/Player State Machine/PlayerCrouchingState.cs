using UnityEngine;

public class PlayerCrouchingState : PlayerBaseState
{
    private Quaternion _targetRotation;

    public PlayerCrouchingState(PlayerStateMachine stateMachine) : base(stateMachine) 
    {
        _targetRotation = stateMachine.transform.rotation;
    }
    
    public override void EnterState()
    {
        StateMachine.SetCharacterHeight(true);
    }
    
    public override void ExitState()
    {
        StateMachine.SetCharacterHeight(false);
    }

    public override void UpdateState()
    {
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        StateMachine.activeVerticalVelocity = StateMachine.groundedGravity;
        HandleMovement();
        
        // Handle aim rotation if aiming - this now uses the improved rotation logic
        StateMachine.HandleAimRotation();
    }
    
    private void HandleMovement()
    {
        // Calculate movement intensity (0-1)
        float movementIntensity = Mathf.Clamp01(
            Mathf.Abs(StateMachine.InputHandler.MovementInput.x) + 
            Mathf.Abs(StateMachine.InputHandler.MovementInput.y)
        );

        // Determine movement direction
        Vector3 inputDirection;
        
        if (StateMachine.IsAiming)
        {
            // When aiming, maintain player orientation toward camera
            // and calculate movement direction relative to player orientation
            Vector3 aimDirection = StateMachine.GetCameraAimDirection();
            _targetRotation = Quaternion.LookRotation(aimDirection);
            
            // Calculate movement relative to player orientation when aiming
            // This creates proper strafing movement
            Vector3 forward = aimDirection;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            
            inputDirection = (forward * StateMachine.InputHandler.MovementInput.y + 
                             right * StateMachine.InputHandler.MovementInput.x).normalized;
        }
        else
        {
            // Standard camera-relative movement for non-aiming
            inputDirection = StateMachine.CalculateMoveDirection();
            
            // Update rotation if moving
            if (inputDirection.sqrMagnitude > PlayerInputHandler.RotationInputThreshold)
            {
                _targetRotation = Quaternion.LookRotation(inputDirection);
            }
        }
        
        float inputMagnitude = inputDirection.magnitude;
        
        // Get target speed
        float targetSpeed = StateMachine.CalculateTargetSpeed(movementIntensity);
    
        // Only update direction if we have meaningful input
        if (inputMagnitude > PlayerInputHandler.MovementInputThreshold)
        {
            StateMachine.activeMoveDirection = inputDirection;
        }
        else
        {
            // No input - keep last direction but set target speed to 0
            targetSpeed = 0f;
        }
    
        // Update current speed with acceleration
        float newSpeed = Mathf.MoveTowards(
            StateMachine.activeHorizontalVelocity, 
            targetSpeed, 
            StateMachine.acceleration * Time.fixedDeltaTime
        );
    
        // Update state machine's speed
        StateMachine.activeHorizontalVelocity = newSpeed;
        
        // Only reset move direction when completely stopped
        if (newSpeed <= 0.01f)
        {
            StateMachine.activeMoveDirection = Vector3.zero;
        }
    
        // Apply rotation based on situation
        if (StateMachine.IsAiming)
        {
            // When aiming, rotation is handled by HandleAimRotation in the StateMachine
            // We don't need to apply rotation here anymore
        }
        else
        {
            // Normal rotation behavior when not aiming
            StateMachine.RotateCharacter(_targetRotation, StateMachine.rotationSpeed, newSpeed > 0.1f ? 2f : 1f);
        }
    }
    
    private void CheckStateTransitions()
    {
        // Fall
        if (!StateMachine.IsGrounded && StateMachine.FallTime > StateMachine.fallThreshold)
        {
            StateMachine.SwitchState(StateMachine.FallingState);
            return;
        }

        // Jump
        if (StateMachine.InputHandler.JumpInput)
        {
            StateMachine.SwitchState(StateMachine.JumpingState);
            return;
        }
        
        // Interact
        if (StateMachine.CanInteract && StateMachine.InputHandler.PlayerInteractInput)
        {
            StateMachine.currentInteractable.OnInteractionStart(StateMachine.gameObject);
            StateMachine.SwitchState(StateMachine.InteractingState);
            return;
        }
        
        // Grounded
        if (StateMachine.CanStand)
        {
            if (StateMachine.InputHandler.IsCrouchToggle)
            {
                if (StateMachine.InputHandler.CrouchInput)
                {
                    StateMachine.InputHandler.ConsumeCrouchInput();
                    StateMachine.SwitchState(StateMachine.GroundedState);
                    return;
                }
            } else {
                if (!StateMachine.InputHandler.CrouchInput)
                {
                    StateMachine.SwitchState(StateMachine.GroundedState);
                    return;
                }
            }
        }
    }
}