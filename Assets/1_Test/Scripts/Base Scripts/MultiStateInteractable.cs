using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;


[System.Serializable]
public class StateData
{
    public string stateName;
    public UnityEvent onStateEnter;
    public UnityEvent onStateExit;
}
public class MultiStateInteractable : InteractableBase
{
    [Header("State Settings")]
    [SerializeField] protected List<StateData> states = new List<StateData>();
    [SerializeField] protected bool loopStates = true;
    
    protected int CurrentStateIndex = -1;

    protected override void Start()
    {
        base.Start();
        
        // Enter initial state if any exists
        if (states.Count > 0)
        {
            SetState(0);
        }
    }

    public override void OnInteractionStart()
    {
        if (!CanInteract || states.Count == 0) return;

        // First run base interaction events
        base.OnInteractionStart();

        // Exit current state
        if (CurrentStateIndex >= 0)
        {
            states[CurrentStateIndex].onStateExit?.Invoke();
        }

        // Move to next state
        if (loopStates)
        {
            SetState((CurrentStateIndex + 1) % states.Count);
        }
        else if (CurrentStateIndex < states.Count - 1)
        {
            SetState(CurrentStateIndex + 1);
        }
    }

    public override void OnInteractionEnd()
    {
        // Run base end interaction events
        base.OnInteractionEnd();
    }

    protected virtual void SetState(int newStateIndex)
    {
        if (newStateIndex < 0 || newStateIndex >= states.Count) return;
        
        CurrentStateIndex = newStateIndex;
        states[CurrentStateIndex].onStateEnter?.Invoke();
        
        Debug.Log($"Entered state: {states[CurrentStateIndex].stateName}");
    }
}