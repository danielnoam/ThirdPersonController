using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
public class PressurePlate : BaseInteractable
{
    [Header("Pressure Plate Settings")]
    [SerializeField] private Transform plateTransform;               // Reference to the moving part of the plate
    [SerializeField] private float plateHeight = 0.1f;               // How far the plate moves down when pressed
    [SerializeField] private float plateAnimationSpeed = 5f;         // Speed of the plate movement animation
    [SerializeField] private bool robotReturnsToPlayer = true;       // Whether the robot should return to player after interaction

    [Header("Pressure Plate Events")]
    [SerializeField] private UnityEvent onPlateActivated;            // Event triggered when the plate is first activated
    [SerializeField] private UnityEvent onPlateDeactivated;          // Event triggered when the plate is deactivated
    
    private Vector3 _initialPlatePosition;                           // Starting position of the plate
    private Vector3 _pressedPlatePosition;                           // Position when the plate is fully pressed
    private bool _isActivated = false;                               // Current activation state
    private HashSet<GameObject> _objectsOnPlate = new HashSet<GameObject>(); // Tracks objects currently on the plate
    
    protected override void Awake()
    {
        base.Awake();
        
        // Find the moving plate part (first child by default)
        if (!plateTransform)
        {
            plateTransform = transform.GetChild(0);
        }
        
        // Store initial positions
        _initialPlatePosition = plateTransform.localPosition;
        _pressedPlatePosition = _initialPlatePosition - new Vector3(0, plateHeight, 0);
    }
    
    private void Update()
    {
        // Animate plate position based on activation state
        Vector3 targetPosition = _isActivated ? _pressedPlatePosition : _initialPlatePosition;
        plateTransform.localPosition = Vector3.Lerp(
            plateTransform.localPosition,
            targetPosition,
            Time.deltaTime * plateAnimationSpeed
        );
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if this object can activate the plate
        if (CanActivate(other.gameObject))
        {
            // Add to tracking set
            _objectsOnPlate.Add(other.gameObject);
            
            // Activate if not already activated
            if (!_isActivated)
            {
                SetActivated(true);
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        // Remove from tracking set
        _objectsOnPlate.Remove(other.gameObject);
        
        // If no objects left on plate, deactivate
        if (_objectsOnPlate.Count == 0 && _isActivated)
        {
            SetActivated(false);
        }
    }
    
    private bool CanActivate(GameObject obj)
    {
        // Check if it's player or robot
        return obj.GetComponent<PlayerStateMachine>() != null || 
               obj.GetComponent<RobotCompanion>() != null;
    }
    
    private void SetActivated(bool activated)
    {
        // Only process if state is changing
        if (_isActivated == activated) return;
        
        _isActivated = activated;
        
        // Handle activation
        if (_isActivated)
        {
            // Trigger activation events
            onPlateActivated?.Invoke();
        }
        // Handle deactivation
        else
        {
            // Trigger deactivation events
            onPlateDeactivated?.Invoke();
        }
    }
    
    public override void OnInteractionStart(GameObject interactor = null)
    {
        // If robot is interacting, tell it to sit down on the plate
        if (interactor != null && interactor.TryGetComponent(out RobotCompanion robot))
        {
            // The robot's SitDown() method will make it stay on the plate
            robot.SitDown();
            
            // If we want the robot to stay on the plate, we need to cancel the interaction timer
            if (!robotReturnsToPlayer)
            {
                // Call base implementation but then cancel the timer
                base.OnInteractionStart(interactor);
                CancelInteraction();
            }
            else
            {
                // Default behavior - let the base class handle the interaction timer
                base.OnInteractionStart(interactor);
            }
        }
        else
        {
            // Not a robot, use default behavior
            base.OnInteractionStart(interactor);
        }
    }
    
    public override void OnAimEnter(PlayerStateMachine player)
    {
        
        if (!_isActivated)
        {
            base.OnAimEnter(player);
        }
    }
    
    public override void OnAimExit(PlayerStateMachine player)
    {
        base.OnAimExit(player);
        SetHighlight(false);
    }
}