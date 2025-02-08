using UnityEngine.InputSystem;

namespace InputBindingSystem
{
    public interface IInputActionReferenceProvider
    {
        InputActionReference actionReference { get; }
    }
}