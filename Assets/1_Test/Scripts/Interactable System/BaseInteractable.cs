using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Base class for all interactable objects in the game
/// </summary>
public abstract class BaseInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] protected bool playerCanInteract = true;
    [SerializeField] protected bool robotCanInteract = true;
    [SerializeField] protected Transform interactPosition;
    [SerializeField, Min(0.1f)] protected float interactionTime = 0.1f;

    [Header("Feedback")]
    [SerializeField] protected GameObject gfxObject;
    [SerializeField] protected Color highlightColor = Color.yellow;
    
    [Header("Base Events")]
    [SerializeField] protected UnityEvent onInteractStartEvents;
    [SerializeField] protected UnityEvent onInteractEndEvents;

    protected MeshRenderer _meshRenderer;
    protected Color _originalColor;
    protected Coroutine _interactionCoroutine;
    protected GameObject _currentInteractor;
    protected bool _isAimedAt = false;
    
    public bool IsInteracting { get; protected set; }
    public virtual bool CanInteract => !IsInteracting;
    public bool PlayerCanInteract => CanInteract && playerCanInteract;
    public bool RobotCanInteract => CanInteract && robotCanInteract;
    public Transform InteractPosition => interactPosition;

    protected virtual void Awake()
    {
        // Find mesh renderer if not specified
        if (gfxObject == null)
        {
            gfxObject = gameObject;
        }
        
        if (gfxObject.TryGetComponent(out MeshRenderer meshRenderer))
        {
            _meshRenderer = meshRenderer;
            _originalColor = _meshRenderer.material.color;
        }
    }

    protected virtual void OnDestroy()
    {
        // Ensure any in-progress interactions are canceled
        CancelInteraction();
    }

    /// <summary>
    /// Start an interaction with this object
    /// </summary>
    /// <param name="interactor">The GameObject that is interacting with this object</param>
    public virtual void OnInteractionStart(GameObject interactor = null)
    {
        if (!CanInteract) return;

        // Store the interactor and mark as interacting
        _currentInteractor = interactor;
        IsInteracting = true;
        SetHighlight(false);
        
        // Run base interaction events
        onInteractStartEvents?.Invoke();
        
        // Start the interaction timer
        if (interactionTime > 0)
        {
            if (_interactionCoroutine != null)
            {
                StopCoroutine(_interactionCoroutine);
            }
            _interactionCoroutine = StartCoroutine(InteractionTimer());
        }
        else
        {
            // If no timer, complete interaction immediately
            OnInteractionEnd(interactor);
        }
    }

    /// <summary>
    /// Called when the interaction ends
    /// </summary>
    /// <param name="interactor">The GameObject that was interacting with this object</param>
    public virtual void OnInteractionEnd(GameObject interactor = null)
    {
        // Run base end interaction events
        onInteractEndEvents?.Invoke();
        
        // If there's a PlayerController or RobotController component, notify it
        if (interactor)
        {
            // Try to find a player controller
            if (interactor.TryGetComponent(out PlayerStateMachine playerController))
            {
                playerController.OnInteractionComplete(this);
            }
            
            // Try to find a robot controller
            if (interactor.TryGetComponent(out RobotCompanion robotController))
            {
                robotController.OnInteractionComplete(this);
            }
        }
        
        IsInteracting = false;
        _currentInteractor = null;
    }
    
    /// <summary>
    /// Handle the interaction timer
    /// </summary>
    protected virtual IEnumerator InteractionTimer()
    {
        yield return new WaitForSeconds(interactionTime);
        OnInteractionEnd(_currentInteractor);
        _interactionCoroutine = null;
    }
    
    /// <summary>
    /// Cancel the current interaction
    /// </summary>
    public virtual void CancelInteraction()
    {
        if (_interactionCoroutine != null)
        {
            StopCoroutine(_interactionCoroutine);
            _interactionCoroutine = null;
        }
        
        IsInteracting = false;
        _currentInteractor = null;
    }
    
    /// <summary>
    /// Set highlight state for this interactable
    /// </summary>
    public virtual void SetHighlight(bool highlighted)
    {
        if (highlighted && !CanInteract) return;
        
        if (_meshRenderer) // Don't override aim highlight
        {
            _meshRenderer.material.color = highlighted ? highlightColor : _originalColor;
        }
    }
    
    /// <summary>
    /// Check if this interactable is currently highlighted
    /// </summary>
    public virtual bool IsHighlighted()
    {
        return _meshRenderer && (_meshRenderer.material.color == highlightColor);
    }

    /// <summary>
    /// Called when player aims at this interactable
    /// </summary>
    public virtual void OnAimEnter(PlayerStateMachine player)
    {
        _isAimedAt = true;
        SetHighlight(true);
        
    }

    /// <summary>
    /// Called while player is aiming at this interactable
    /// </summary>
    public virtual void OnAimStay(PlayerStateMachine player)
    {

    }

    /// <summary>
    /// Called when player stops aiming at this interactable
    /// </summary>
    public virtual void OnAimExit(PlayerStateMachine player)
    {
        SetHighlight(false);
    }
}
