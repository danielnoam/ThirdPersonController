// using System;
// using UnityEngine;
//
//
// [RequireComponent(typeof(PlayerController))]
// [RequireComponent(typeof(PlayerInput))]
// [RequireComponent(typeof(Animator))]
// public class PlayerAnimation : MonoBehaviour
// {
//     private PlayerController _playerController;
//     private Animator _animator;
//     private PlayerInput _playerInput;
//     private int _horizontalHash;
//     private int _verticalHash;
//     private int _stateHash;
//     private int _fallTimeHash;
//     private int _cameraRelativeHash;
//
//     private void Awake()
//     {
//         _animator = GetComponent<Animator>();
//         _playerController = GetComponent<PlayerController>();
//         _playerInput = GetComponent<PlayerInput>();
//         _horizontalHash = Animator.StringToHash("Horizontal");
//         _verticalHash = Animator.StringToHash("Vertical");
//         _stateHash = Animator.StringToHash("StateIndex");
//         _fallTimeHash = Animator.StringToHash("FallTime");
//         _cameraRelativeHash = Animator.StringToHash("CameraRelative");
//     }
//
//     private void Update()
//     {
//         SyncAnimations();
//     }
//
//     private void SyncAnimations()
//     {
//         // Update camera relative state based on right-click input
//         _animator.SetBool(_cameraRelativeHash, _playerInput.RightClickInput);
//         _animator.SetInteger(_stateHash, (int)_playerController.currentState);
//
//         float fallBlend = _playerController.currentState == PlayerState.Landing
//             ? _playerController.LandingIntensity
//             : Mathf.Clamp01(_playerController.FallTime / _playerController.maxFallTime);
//
//         _animator.SetFloat(_fallTimeHash, fallBlend);
//
//         if (_playerInput.RightClickInput)
//         {
//             // Camera relative movement - match blend tree positions
//             float verticalValue = 0;
//             float horizontalValue = 0;
//
//             // Calculate forward/backward movement (Y axis in blend tree)
//             if (_playerInput.VerticalInput != 0)
//             {
//                 if (_playerController.activeMoveSpeed <= _playerController.currentMovement.walkSpeed)
//                 {
//                     // Walking range: 0 to 0.5
//                     verticalValue = 0.5f * (_playerInput.VerticalInput * _playerController.activeMoveSpeed / _playerController.currentMovement.walkSpeed);
//                 }
//                 else if (_playerController.activeMoveSpeed <= _playerController.currentMovement.runSpeed)
//                 {
//                     // Running range: 0.5 to 1.0
//                     verticalValue = _playerInput.VerticalInput * (0.5f + 0.5f * (_playerController.activeMoveSpeed - _playerController.currentMovement.walkSpeed) 
//                         / (_playerController.currentMovement.runSpeed - _playerController.currentMovement.walkSpeed));
//                 }
//                 else
//                 {
//                     // Sprinting range: 1.0 to 2.0
//                     float sprintProgress = (_playerController.activeMoveSpeed - _playerController.currentMovement.runSpeed) 
//                         / (_playerController.currentMovement.runSpeed * (_playerController.currentMovement.sprintSpeedMultiplier - 1));
//                     verticalValue = _playerInput.VerticalInput * (1f + sprintProgress);
//                 }
//             }
//
//             // Calculate strafe movement (X axis in blend tree)
//             if (_playerInput.HorizontalInput != 0 || _playerInput.MouseX != 0)
//             {
//                 float strafeInfluence = _playerInput.HorizontalInput;
//                 if (_playerInput.MouseX != 0 && _playerController.MovementVector != Vector3.zero)
//                 {
//                     strafeInfluence += Mathf.Sign(_playerInput.MouseX) * 0.5f;
//                 }
//                 
//                 strafeInfluence = Mathf.Clamp(strafeInfluence, -1f, 1f);
//                 
//                 if (_playerController.activeMoveSpeed <= _playerController.currentMovement.walkSpeed)
//                     horizontalValue = 0.5f * strafeInfluence;
//                 else 
//                     horizontalValue = strafeInfluence; // Full strafe value for running/sprinting
//             }
//
//             _animator.SetFloat(_verticalHash, verticalValue, 0.1f, Time.deltaTime);
//             _animator.SetFloat(_horizontalHash, horizontalValue, 0.1f, Time.deltaTime);
//         }
//         else
//         {
//             // Character relative movement - original blend tree values
//             float verticalValue = 0f;
//             if (_playerController.activeMoveSpeed <= _playerController.currentMovement.walkSpeed)
//             {
//                 // Walking range: 0 to 0.5
//                 verticalValue = (_playerController.activeMoveSpeed / _playerController.currentMovement.walkSpeed) * 0.5f;
//             }
//             else if (_playerController.activeMoveSpeed <= _playerController.currentMovement.runSpeed)
//             {
//                 // Running range: 0.5 to 1.0
//                 verticalValue = 0.5f + ((_playerController.activeMoveSpeed - _playerController.currentMovement.walkSpeed) /
//                                       (_playerController.currentMovement.runSpeed - _playerController.currentMovement.walkSpeed)) * 0.5f;
//             }
//             else
//             {
//                 // Sprinting range: 1.0 to 2.0
//                 float sprintProgress = (_playerController.activeMoveSpeed - _playerController.currentMovement.runSpeed) 
//                     / (_playerController.currentMovement.runSpeed * (_playerController.currentMovement.sprintSpeedMultiplier - 1));
//                 verticalValue = 1f + sprintProgress;
//             }
//
//             _animator.SetFloat(_verticalHash, verticalValue, 0.1f, Time.deltaTime);
//             _animator.SetFloat(_horizontalHash, 0, 0.1f, Time.deltaTime);
//         }
//     }
// }