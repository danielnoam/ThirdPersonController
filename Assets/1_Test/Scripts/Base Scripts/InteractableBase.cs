using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] protected bool isInteractable = true;
    [SerializeField] protected bool playerCanInteract = true;
    [SerializeField] protected bool robotCanInteract = true;
    [SerializeField] protected Transform interactPosition;
    
    
    [Header("Visual Feedback")]
    [SerializeField] protected Color highlightColor = Color.yellow;
    
    [Header("Events")]
    [SerializeField] protected UnityEvent onInteractStartEvents;
    [SerializeField] protected UnityEvent onInteractEndEvents;
    
    protected MeshRenderer MeshRenderer;
    protected Color OriginalColor;
    
    public bool CanInteract => isInteractable;
    public bool PlayerCanInteract => playerCanInteract;
    public bool RobotCanInteract => robotCanInteract;
    public Transform InteractPosition => interactPosition;

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
    
    public void Interact()
    {
        if (CanInteract)
        {
            OnInteractionStart();
        }
    }

    public virtual void OnInteractionStart()
    {
        onInteractStartEvents?.Invoke();
    }

    public virtual void OnInteractionEnd()
    {
        onInteractEndEvents?.Invoke();
    }
    
}