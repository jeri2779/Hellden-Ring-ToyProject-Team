using UnityEngine;
using UnityEngine.InputSystem;

public static class InputManager
{
    public static void RemapButtonClicked(InputAction actionToRebind)
    {
        var rebindOperation = actionToRebind
            .PerformInteractiveRebinding()
            // To avoid accidental input from mouse motion
            .WithControlsExcluding("<Pointer>/position")
            .WithControlsExcluding("<Pointer>/delta")
            .WithCancelingThrough("<Keyboard>/escape")
            .WithTimeout(5f)
            .OnMatchWaitForAnother(0.1f)
            .Start();
    }

    public static void ButtonReset(InputAction actionToReset)
    {
        actionToReset.ApplyBindingOverride(1, default(InputBinding));
    }

    public static string GetCurrentKey(InputAction inputAction, string group)
    {
        return inputAction.GetBindingDisplayString(group: group);
    }
}
