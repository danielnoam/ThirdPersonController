using UnityEngine;
using UnityEngine.Events;



public interface IInteractable
{
    void OnInteractionStart(GameObject interactor = null);
    void OnInteractionEnd(GameObject interactor = null);
    bool CanInteract { get; }
    bool PlayerCanInteract { get; }
    bool RobotCanInteract { get; }
    bool IsInteracting { get; }
    Transform InteractPosition { get; }
}