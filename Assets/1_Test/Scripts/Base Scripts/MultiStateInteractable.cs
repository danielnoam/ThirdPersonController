using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

[System.Serializable]
public class StateData
{
    public string stateName;
    public UnityEvent onState;
}

public class MultiStateInteractable : BaseInteractable
{
    [Header("State Settings")]
    [SerializeField] private bool loopStates = true;
    [SerializeField] private List<StateData> states = new List<StateData>();
    
    private int _currentStateIndex = -1;
    private int _nextStateIndex = -1;

    protected override void Awake()
    {
        base.Awake();
        
        // Enter initial state if any exists
        if (states.Count > 0)
        {
            SetState(0, true); // Force set the initial state
        }
    }

    public override void OnInteractionStart(GameObject interactor = null)
    {
        if (!CanInteract || states.Count == 0) return;
        
        // Calculate the next state index before starting the interaction
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
        
        // Call the base implementation to handle common interaction behavior
        base.OnInteractionStart(interactor);
    }

    public override void OnInteractionEnd(GameObject interactor = null)
    {
        // Change to the next state if one was determined
        if (_nextStateIndex >= 0)
        {
            SetState(_nextStateIndex);
            _nextStateIndex = -1;
        }
        
        // Call the base implementation to handle common end behavior
        base.OnInteractionEnd(interactor);
    }
    
    private void SetState(int newStateIndex, bool forceSet = false)
    {
        if (newStateIndex < 0 || newStateIndex >= states.Count) return;
        
        // Only set the state if we're not already in it or if force set is true
        if (_currentStateIndex != newStateIndex || forceSet)
        {
            _currentStateIndex = newStateIndex;
            states[_currentStateIndex].onState?.Invoke();
        }
    }
    
    public override bool CanInteract => base.CanInteract && states.Count > 0;
    
    // Get the current state name for debugging
    public string GetCurrentStateName()
    {
        if (_currentStateIndex >= 0 && _currentStateIndex < states.Count)
        {
            return states[_currentStateIndex].stateName;
        }
        return "None";
    }
}