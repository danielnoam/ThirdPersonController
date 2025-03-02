using UnityEngine;

public interface IInteractable
{

    bool CanInteract { get; }
    bool PlayerCanInteract { get; }
    bool RobotCanInteract { get; }
    bool IsInteracting { get; }
    Transform InteractPosition { get; }
    void OnInteractionStart(GameObject interactor = null);
    void OnInteractionEnd(GameObject interactor = null);
    void CancelInteraction();
    void OnAimEnter(PlayerStateMachine player);
    void OnAimStay(PlayerStateMachine player);
    void OnAimExit(PlayerStateMachine player);
    void SetHighlight(bool highlighted);
    bool IsHighlighted();
}