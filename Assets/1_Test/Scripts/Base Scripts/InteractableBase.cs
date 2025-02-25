using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] protected bool isInteractable = true;
    [SerializeField] protected bool playerCanInteract = true;
    [SerializeField] protected bool robotCanInteract = true;
    [SerializeField] protected Transform interactPosition;
    [SerializeField] protected float interactionTime = 0f;
    
    [Header("Visual Feedback")]
    [SerializeField] protected Color highlightColor = Color.yellow;
    
    [Header("Events")]
    [SerializeField] protected UnityEvent onInteractStartEvents;
    [SerializeField] protected UnityEvent onInteractEndEvents;
    
    protected MeshRenderer MeshRenderer;
    protected Color OriginalColor;
    protected Coroutine interactionCoroutine;
    protected GameObject currentInteractor;
    protected bool isInteracting = false;
    
    public bool CanInteract => isInteractable && !isInteracting;
    public bool PlayerCanInteract => playerCanInteract;
    public bool RobotCanInteract => robotCanInteract;
    public Transform InteractPosition => interactPosition;
    public bool IsInteracting => isInteracting;

    protected virtual void Start()
    {
        MeshRenderer = GetComponent<MeshRenderer>();
        if (MeshRenderer != null)
        {
            OriginalColor = MeshRenderer.material.color;
        }
    }
    
    public virtual void SetHighlight(bool highlighted)
    {
        if (MeshRenderer != null)
        {
            MeshRenderer.material.color = highlighted ? highlightColor : OriginalColor;
        }
    }
    
    public void Interact(GameObject interactor = null)
    {
        if (CanInteract)
        {
            OnInteractionStart(interactor);
        }
    }

    public virtual void OnInteractionStart(GameObject interactor = null)
    {
        isInteracting = true;
        currentInteractor = interactor;
        onInteractStartEvents?.Invoke();
        
        // Start interaction timer if needed
        if (interactionTime > 0)
        {
            if (interactionCoroutine != null)
            {
                StopCoroutine(interactionCoroutine);
            }
            interactionCoroutine = StartCoroutine(InteractionTimer());
        }
        else
        {
            // If no timer, complete interaction immediately
            OnInteractionEnd(interactor);
        }
    }

    public virtual void OnInteractionEnd(GameObject interactor = null)
    {
        onInteractEndEvents?.Invoke();
        
        // If there's a PlayerController or RobotController component, notify it
        if (interactor)
        {
            // Try to find a player controller
            var playerController = interactor.GetComponent<PlayerStateMachine>();
            if (playerController)
            {
                playerController.OnInteractionComplete(this);
            }
            
            // Try to find a robot controller
            var robotController = interactor.GetComponent<RobotCompanion>();
            if (robotController)
            {
                robotController.OnInteractionComplete(this);
            }
        }
        
        isInteracting = false;
        currentInteractor = null;
    }
    
    protected IEnumerator InteractionTimer()
    {
        yield return new WaitForSeconds(interactionTime);
        OnInteractionEnd(currentInteractor);
        interactionCoroutine = null;
    }
    
    // Cancel ongoing interaction
    public virtual void CancelInteraction()
    {
        if (interactionCoroutine != null)
        {
            StopCoroutine(interactionCoroutine);
            interactionCoroutine = null;
        }
        
        isInteracting = false;
        currentInteractor = null;
    }
    
    protected virtual void OnDestroy()
    {
        // Ensure any in-progress interactions are canceled
        CancelInteraction();
    }
}