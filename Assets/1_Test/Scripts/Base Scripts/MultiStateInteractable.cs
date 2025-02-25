using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

[System.Serializable]
public class StateData
{
    public string stateName;
    public UnityEvent onState; // Single event for the state
}

public class MultiStateInteractable : InteractableBase
{
    [Header("State Settings")]
    [SerializeField] protected bool loopStates = true;
    [SerializeField] protected List<StateData> states = new List<StateData>();
    
    
    protected int CurrentStateIndex = -1;
    protected int NextStateIndex = -1;

    protected override void Start()
    {
        base.Start();
        
        // Enter initial state if any exists
        if (states.Count > 0)
        {
            SetState(0, true); // Force set the initial state
        }
    }

    public override void OnInteractionStart(GameObject interactor = null)
    {
        if (!CanInteract || states.Count == 0) return;

        // Store the interactor and mark as interacting
        currentInteractor = interactor;
        isInteracting = true;
        
        // Run base interaction events without timer
        onInteractStartEvents?.Invoke();
        
        // Calculate the next state index
        if (loopStates)
        {
            NextStateIndex = (CurrentStateIndex + 1) % states.Count;
        }
        else if (CurrentStateIndex < states.Count - 1)
        {
            NextStateIndex = CurrentStateIndex + 1;
        }
        else
        {
            // No more states to advance to if not looping
            NextStateIndex = -1;
        }
        
        // Start the interaction timer
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

    public override void OnInteractionEnd(GameObject interactor = null)
    {
        // Change to the next state if one was determined
        if (NextStateIndex >= 0)
        {
            SetState(NextStateIndex);
            NextStateIndex = -1;
        }
        
        // Run base end interaction events
        base.OnInteractionEnd(interactor);
    }

    protected virtual void SetState(int newStateIndex, bool forceSet = false)
    {
        if (newStateIndex < 0 || newStateIndex >= states.Count) return;
        
        // Only set the state if we're not already in it or if force set is true
        if (CurrentStateIndex != newStateIndex || forceSet)
        {
            CurrentStateIndex = newStateIndex;
            states[CurrentStateIndex].onState?.Invoke();
            
            Debug.Log($"Set state: {states[CurrentStateIndex].stateName}");
        }
    }
}