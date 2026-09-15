using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class KeyConfigPanel : MonoBehaviour
{
    public enum InputDevice
    {
        KeyboardMouse,
        Pad,
    }

    public KeyConfigRow keyRowPrefab;

    public Transform keyParentLeft;
    public Transform keyParentRight;

    public InputDevice inputMode = InputDevice.KeyboardMouse;

    public InputActionAsset inputActions;

    string[] keyBoardOptions = { "Up", "Down", "Left", "Right", "Attack", "StrongAttack", "Roll" };

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() { }

    // Update is called once per frame
    void Update() { }
}
