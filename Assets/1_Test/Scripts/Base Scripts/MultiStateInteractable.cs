using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class StateData
{
    public string stateName;
    public UnityEvent onState;
}

public class MultiStateInteractable : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private bool playerCanInteract = true;
    [SerializeField] private bool robotCanInteract = true;
    [SerializeField] private Transform interactPosition;
    [SerializeField, Min(0.1f)] private float interactionTime = 0.1f;

    [Header("Feedback")] 
    [SerializeField] private GameObject gfxObject;
    [SerializeField] private Color highlightColor = Color.yellow;
    
    [Header("Base Events")]
    [SerializeField] private UnityEvent onInteractStartEvents;
    [SerializeField] private UnityEvent onInteractEndEvents;
    
    [Header("State Settings")]
    [SerializeField] private bool loopStates = true;
    [SerializeField] private List<StateData> states = new List<StateData>();
    
    private MeshRenderer _meshRenderer;
    private Color _originalColor;
    private Coroutine _interactionCoroutine;
    private GameObject _currentInteractor;
    private bool _isInteracting = false;
    
    private int _currentStateIndex = -1;
    private int _nextStateIndex = -1;
    
    public Transform InteractPosition => interactPosition;

    protected virtual void Start()
    {
        if (gfxObject)
        {
            _meshRenderer = gfxObject.GetComponent<MeshRenderer>();
            if (_meshRenderer != null)
            {
                _originalColor = _meshRenderer.material.color;
            }
        }

        
        // Enter initial state if any exists
        if (states.Count > 0)
        {
            SetState(0, true); // Force set the initial state
        }
    }
    
    protected virtual void OnDestroy()
    {
        // Ensure any in-progress interactions are canceled
        CancelInteraction();
    }
    
    
    public void Interact(GameObject interactor = null)
    {
        if (CanInteract())
        {
            OnInteractionStart(interactor);
        }
    }

    public virtual void OnInteractionStart(GameObject interactor = null)
    {
        if (!CanInteract() || states.Count == 0) return;

        // Store the interactor and mark as interacting
        _currentInteractor = interactor;
        _isInteracting = true;
        SetHighlight(false);
        
        // Run base interaction events
        onInteractStartEvents?.Invoke();
        
        // Calculate the next state index
        if (loopStates)
        {
            _nextStateIndex = (_currentStateIndex + 1) % states.Count;
        }
        else if (_currentStateIndex < states.Count - 1)
        {
            _nextStateIndex = _currentStateIndex + 1;
        }
        else
        {
            // No more states to advance to if not looping
            _nextStateIndex = -1;
        }
        
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

    public virtual void OnInteractionEnd(GameObject interactor = null)
    {
        // Change to the next state if one was determined
        if (_nextStateIndex >= 0)
        {
            SetState(_nextStateIndex);
            _nextStateIndex = -1;
        }
        
        // Run base end interaction events
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
        
        _isInteracting = false;
        _currentInteractor = null;
    }
    
    protected IEnumerator InteractionTimer()
    {
        yield return new WaitForSeconds(interactionTime);
        OnInteractionEnd(_currentInteractor);
        _interactionCoroutine = null;
    }
    
    protected virtual void SetState(int newStateIndex, bool forceSet = false)
    {
        if (newStateIndex < 0 || newStateIndex >= states.Count) return;
        
        // Only set the state if we're not already in it or if force set is true
        if (_currentStateIndex != newStateIndex || forceSet)
        {
            _currentStateIndex = newStateIndex;
            states[_currentStateIndex].onState?.Invoke();
            
        }
    }
    
    public virtual void CancelInteraction()
    {
        if (_interactionCoroutine != null)
        {
            StopCoroutine(_interactionCoroutine);
            _interactionCoroutine = null;
        }
        
        _isInteracting = false;
        _currentInteractor = null;
    }
    
    public virtual void SetHighlight(bool highlighted)
    {
        if (highlighted && !CanInteract()) return;
        
        if (_meshRenderer)
        {
            _meshRenderer.material.color = highlighted ? highlightColor : _originalColor;
        }
    }
    
    public virtual bool Highlighted()
    {
        return  _meshRenderer && _meshRenderer.material.color == highlightColor;
    }

    public virtual bool CanInteract()
    {
        return  !_isInteracting && states.Count > 0;
    }
    
    public virtual bool PlayerCanInteract()
    {
        return  CanInteract() && playerCanInteract;
    }
    
    public virtual bool RobotCanInteract()
    {
        return  CanInteract() && robotCanInteract;
    }
    
}