using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class KeyConfigRow : MonoBehaviour
{
    public string key;
    public string subKey;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI keyButtonText;

    public Button assignButton;
    public Button resetButton;

    public string type = "Keyboard";
    public InputActionAsset asset;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        nameText.text = subKey.Length > 0 ? subKey : key;
        InputBinding bind;
        if (subKey.Length > 0)
        {
            bind = asset
                .FindAction(key)
                .bindings.First((k) => k.name.Equals(subKey.ToLower()) && k.groups.Contains(type));
        }
        else
        {
            bind = asset.FindAction(key).bindings.First((k) => k.groups.Contains(type));
        }

        keyButtonText.text = InputControlPath.ToHumanReadableString(
            bind.effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice
        );
    }

    public void AssignKey()
    {
        InputAction action = asset.FindAction(key);

        int bindIndex = -1;
        var bindingsList = action.bindings.ToList();
        if (!string.IsNullOrEmpty(subKey))
        {
            bindIndex = bindingsList.FindIndex(x =>
                x.isPartOfComposite && x.name == subKey.ToLower()
            );
        }
        else
        {
            bindIndex = bindingsList.FindIndex(x =>
                !x.isPartOfComposite && x.groups != null && x.groups.Contains(type)
            );
        }

        action.Disable();
        if (bindIndex == -1)
        {
            var rebindOperation = action
                .PerformInteractiveRebinding()
                .WithControlsExcluding("<Pointer>/position")
                .WithControlsExcluding("<Pointer>/delta")
                .WithCancelingThrough("<Keyboard>/escape")
                .WithTimeout(5f)
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(op =>
                {
                    action.Enable();
                    op.Dispose();
                    Refresh();
                })
                .OnCancel(op =>
                {
                    Debug.Log("CANCELED");
                    action.Enable();
                    op.Dispose();
                })
                .Start();
        }
        else
        {
            var rebindOperation = action
                .PerformInteractiveRebinding(bindIndex)
                .WithControlsExcluding("<Pointer>/position")
                .WithControlsExcluding("<Pointer>/delta")
                .WithCancelingThrough("<Keyboard>/escape")
                .WithTimeout(5f)
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(op =>
                {
                    action.Enable();
                    op.Dispose();
                    Refresh();
                })
                .OnCancel(op =>
                {
                    Debug.Log("CANCELED");
                    action.Enable();

                    op.Dispose();
                })
                .Start();
        }
    }

    public void ResetKey()
    {
        InputAction action = asset.FindAction(key);
        int bindIndex = -1;
        var bindingsList = action.bindings.ToList();
        if (!string.IsNullOrEmpty(subKey))
        {
            bindIndex = bindingsList.FindIndex(x =>
                x.isPartOfComposite && x.name == subKey.ToLower()
            );
        }
        else
        {
            bindIndex = bindingsList.FindIndex(x =>
                !x.isPartOfComposite && x.groups != null && x.groups.Contains(type)
            );
        }

        if (bindIndex != -1)
        {
            action.RemoveBindingOverride(bindIndex);
        }
        else
        {
            action.RemoveAllBindingOverrides();
        }

        Refresh();
    }
}
