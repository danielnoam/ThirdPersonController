using UnityEngine;
using UnityEngine.Events;

public interface IInteractable
{
    void OnInteractionStart();
    void OnInteractionEnd();
    bool CanInteract { get; }
    bool PlayerCanInteract { get; }
    bool RobotCanInteract { get; }
    Transform InteractPosition { get; }
}