using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Maps controller buttons to keyboard-style keys.
/// Add this to one GameObject in the scene and use the custom Inspector
/// to bind controller buttons by pressing them.
/// </summary>
[DefaultExecutionOrder(-10000)]
[DisallowMultipleComponent]
public sealed class ControllerKeyboardBinder : MonoBehaviour
{
    [Serializable]
    public sealed class Binding
    {
        [Tooltip("Optional note shown in the Inspector only.")]
        public string label;

        [Tooltip("The keyboard key this controller button should behave like.")]
        public KeyCode keyboardKey = KeyCode.None;

        [Tooltip("Filled by the Inspector's press-to-bind button. You can still edit it manually if needed.")]
        public string controlPath;

        [Tooltip("Optional device name/display/layout filter. Leave empty when using an exact control path.")]
        public string deviceContains;

        [Tooltip("Optional control name/display/path filter, for example trigger, buttonSouth, button1. Leave empty when using an exact control path.")]
        public string controlContains;
    }

    private struct KeyState
    {
        public bool held;
        public bool down;
        public bool up;
    }

    private const float DefaultPressPoint = 0.5f;

    private static ControllerKeyboardBinder instance;
    private static readonly Dictionary<KeyCode, KeyState> keyStates = new Dictionary<KeyCode, KeyState>();
    private static bool legacyInputUnavailable;

    [SerializeField] private Binding[] bindings = Array.Empty<Binding>();

    [Header("Button Discovery")]
    [SerializeField] private bool logPressedControls;
    [SerializeField] private bool logOnlyOncePerControl = true;
    [SerializeField] private float pressPoint = DefaultPressPoint;
    [SerializeField, Min(0f)] private float startupSettleSeconds = 0.75f;
    [SerializeField, Min(0f)] private float releaseArmSeconds = 0.25f;

    private readonly Dictionary<Binding, List<ButtonControl>> resolvedControls = new Dictionary<Binding, List<ButtonControl>>();
    private readonly HashSet<string> loggedControls = new HashSet<string>();
    private readonly Dictionary<string, bool> previousDiscoveryPressed = new Dictionary<string, bool>();
    private readonly Dictionary<KeyCode, bool> frameHeldKeys = new Dictionary<KeyCode, bool>();
    private readonly Dictionary<KeyCode, bool> keyArmedForPress = new Dictionary<KeyCode, bool>();
    private readonly Dictionary<KeyCode, float> keyReleasedSince = new Dictionary<KeyCode, float>();
    private readonly List<KeyCode> staleKeys = new List<KeyCode>();
    private bool hasUpdatedOnce;
    private float inputSettleUntil;

    public static bool GetKey(KeyCode key)
    {
        return GetInputSystemKey(key) || GetLegacyKey(key) || TryGetState(key, out KeyState state) && state.held;
    }

    public static bool GetKeyDown(KeyCode key)
    {
        return GetInputSystemKeyDown(key) || GetLegacyKeyDown(key) || TryGetState(key, out KeyState state) && state.down;
    }

    public static bool GetKeyUp(KeyCode key)
    {
        return GetInputSystemKeyUp(key) || GetLegacyKeyUp(key) || TryGetState(key, out KeyState state) && state.up;
    }

#if ENABLE_INPUT_SYSTEM
    public static bool GetKey(Key key)
    {
        bool mapped = TryMapKey(key, out KeyCode keyCode);
        return GetInputSystemKey(key) ||
               mapped && (GetLegacyKey(keyCode) || TryGetState(keyCode, out KeyState state) && state.held);
    }

    public static bool GetKeyDown(Key key)
    {
        bool mapped = TryMapKey(key, out KeyCode keyCode);
        return GetInputSystemKeyDown(key) ||
               mapped && (GetLegacyKeyDown(keyCode) || TryGetState(keyCode, out KeyState state) && state.down);
    }

    public static bool GetKeyUp(Key key)
    {
        bool mapped = TryMapKey(key, out KeyCode keyCode);
        return GetInputSystemKeyUp(key) ||
               mapped && (GetLegacyKeyUp(keyCode) || TryGetState(keyCode, out KeyState state) && state.up);
    }
#endif

    public void RefreshBindings()
    {
        resolvedControls.Clear();

        foreach (Binding binding in bindings)
        {
            if (binding == null || binding.keyboardKey == KeyCode.None)
            {
                continue;
            }

            var controls = new List<ButtonControl>();
            bool hasControlPath = !string.IsNullOrWhiteSpace(binding.controlPath);
            ResolveExactControlPath(binding, controls);

            if (hasControlPath && controls.Count == 0)
            {
                ResolveControlPathSuffix(binding, controls);
            }

            if (!hasControlPath)
            {
                ResolveLooseControlMatch(binding, controls);
            }

            resolvedControls[binding] = controls;
        }

        if (Application.isPlaying)
        {
            ResetPressArming();
        }
    }

    private static bool TryGetState(KeyCode key, out KeyState state)
    {
        if (keyStates.TryGetValue(key, out state))
        {
            return true;
        }

        state = default;
        return false;
    }

    private static bool GetInputSystemKey(KeyCode key)
    {
        KeyControl control = GetInputSystemKeyControl(key);
        return control != null && control.isPressed;
    }

    private static bool GetInputSystemKeyDown(KeyCode key)
    {
        KeyControl control = GetInputSystemKeyControl(key);
        return control != null && control.wasPressedThisFrame;
    }

    private static bool GetInputSystemKeyUp(KeyCode key)
    {
        KeyControl control = GetInputSystemKeyControl(key);
        return control != null && control.wasReleasedThisFrame;
    }

    private static KeyControl GetInputSystemKeyControl(KeyCode keyCode)
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && TryMapKeyCode(keyCode, out Key key) ? keyboard[key] : null;
    }

    private static bool GetInputSystemKey(Key key)
    {
        KeyControl control = GetInputSystemKeyControl(key);
        return control != null && control.isPressed;
    }

    private static bool GetInputSystemKeyDown(Key key)
    {
        KeyControl control = GetInputSystemKeyControl(key);
        return control != null && control.wasPressedThisFrame;
    }

    private static bool GetInputSystemKeyUp(Key key)
    {
        KeyControl control = GetInputSystemKeyControl(key);
        return control != null && control.wasReleasedThisFrame;
    }

    private static KeyControl GetInputSystemKeyControl(Key key)
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && key != Key.None ? keyboard[key] : null;
    }

    private static bool TryMapKeyCode(KeyCode keyCode, out Key key)
    {
        switch (keyCode)
        {
            case KeyCode.None:
                key = Key.None;
                return false;
            case KeyCode.Alpha0:
                key = Key.Digit0;
                return true;
            case KeyCode.Alpha1:
                key = Key.Digit1;
                return true;
            case KeyCode.Alpha2:
                key = Key.Digit2;
                return true;
            case KeyCode.Alpha3:
                key = Key.Digit3;
                return true;
            case KeyCode.Alpha4:
                key = Key.Digit4;
                return true;
            case KeyCode.Alpha5:
                key = Key.Digit5;
                return true;
            case KeyCode.Alpha6:
                key = Key.Digit6;
                return true;
            case KeyCode.Alpha7:
                key = Key.Digit7;
                return true;
            case KeyCode.Alpha8:
                key = Key.Digit8;
                return true;
            case KeyCode.Alpha9:
                key = Key.Digit9;
                return true;
            case KeyCode.Keypad0:
                key = Key.Numpad0;
                return true;
            case KeyCode.Keypad1:
                key = Key.Numpad1;
                return true;
            case KeyCode.Keypad2:
                key = Key.Numpad2;
                return true;
            case KeyCode.Keypad3:
                key = Key.Numpad3;
                return true;
            case KeyCode.Keypad4:
                key = Key.Numpad4;
                return true;
            case KeyCode.Keypad5:
                key = Key.Numpad5;
                return true;
            case KeyCode.Keypad6:
                key = Key.Numpad6;
                return true;
            case KeyCode.Keypad7:
                key = Key.Numpad7;
                return true;
            case KeyCode.Keypad8:
                key = Key.Numpad8;
                return true;
            case KeyCode.Keypad9:
                key = Key.Numpad9;
                return true;
            case KeyCode.KeypadEnter:
                key = Key.NumpadEnter;
                return true;
            case KeyCode.KeypadDivide:
                key = Key.NumpadDivide;
                return true;
            case KeyCode.KeypadMultiply:
                key = Key.NumpadMultiply;
                return true;
            case KeyCode.KeypadMinus:
                key = Key.NumpadMinus;
                return true;
            case KeyCode.KeypadPlus:
                key = Key.NumpadPlus;
                return true;
            case KeyCode.KeypadPeriod:
                key = Key.NumpadPeriod;
                return true;
            case KeyCode.Return:
                key = Key.Enter;
                return true;
            case KeyCode.LeftControl:
                key = Key.LeftCtrl;
                return true;
            case KeyCode.RightControl:
                key = Key.RightCtrl;
                return true;
            case KeyCode.BackQuote:
                key = Key.Backquote;
                return true;
            default:
                return Enum.TryParse(keyCode.ToString(), true, out key);
        }
    }

    private static bool TryMapKey(Key key, out KeyCode keyCode)
    {
        switch (key)
        {
            case Key.None:
                keyCode = KeyCode.None;
                return false;
            case Key.Digit0:
                keyCode = KeyCode.Alpha0;
                return true;
            case Key.Digit1:
                keyCode = KeyCode.Alpha1;
                return true;
            case Key.Digit2:
                keyCode = KeyCode.Alpha2;
                return true;
            case Key.Digit3:
                keyCode = KeyCode.Alpha3;
                return true;
            case Key.Digit4:
                keyCode = KeyCode.Alpha4;
                return true;
            case Key.Digit5:
                keyCode = KeyCode.Alpha5;
                return true;
            case Key.Digit6:
                keyCode = KeyCode.Alpha6;
                return true;
            case Key.Digit7:
                keyCode = KeyCode.Alpha7;
                return true;
            case Key.Digit8:
                keyCode = KeyCode.Alpha8;
                return true;
            case Key.Digit9:
                keyCode = KeyCode.Alpha9;
                return true;
            case Key.Numpad0:
                keyCode = KeyCode.Keypad0;
                return true;
            case Key.Numpad1:
                keyCode = KeyCode.Keypad1;
                return true;
            case Key.Numpad2:
                keyCode = KeyCode.Keypad2;
                return true;
            case Key.Numpad3:
                keyCode = KeyCode.Keypad3;
                return true;
            case Key.Numpad4:
                keyCode = KeyCode.Keypad4;
                return true;
            case Key.Numpad5:
                keyCode = KeyCode.Keypad5;
                return true;
            case Key.Numpad6:
                keyCode = KeyCode.Keypad6;
                return true;
            case Key.Numpad7:
                keyCode = KeyCode.Keypad7;
                return true;
            case Key.Numpad8:
                keyCode = KeyCode.Keypad8;
                return true;
            case Key.Numpad9:
                keyCode = KeyCode.Keypad9;
                return true;
            case Key.NumpadEnter:
                keyCode = KeyCode.KeypadEnter;
                return true;
            case Key.NumpadDivide:
                keyCode = KeyCode.KeypadDivide;
                return true;
            case Key.NumpadMultiply:
                keyCode = KeyCode.KeypadMultiply;
                return true;
            case Key.NumpadMinus:
                keyCode = KeyCode.KeypadMinus;
                return true;
            case Key.NumpadPlus:
                keyCode = KeyCode.KeypadPlus;
                return true;
            case Key.NumpadPeriod:
                keyCode = KeyCode.KeypadPeriod;
                return true;
            case Key.Enter:
                keyCode = KeyCode.Return;
                return true;
            case Key.LeftCtrl:
                keyCode = KeyCode.LeftControl;
                return true;
            case Key.RightCtrl:
                keyCode = KeyCode.RightControl;
                return true;
            case Key.Backquote:
                keyCode = KeyCode.BackQuote;
                return true;
            default:
                return Enum.TryParse(key.ToString(), true, out keyCode);
        }
    }

    private static bool GetLegacyKey(KeyCode key)
    {
        if (legacyInputUnavailable || key == KeyCode.None)
        {
            return false;
        }

        try
        {
            return Input.GetKey(key);
        }
        catch (InvalidOperationException)
        {
            legacyInputUnavailable = true;
            return false;
        }
    }

    private static bool GetLegacyKeyDown(KeyCode key)
    {
        if (legacyInputUnavailable || key == KeyCode.None)
        {
            return false;
        }

        try
        {
            return Input.GetKeyDown(key);
        }
        catch (InvalidOperationException)
        {
            legacyInputUnavailable = true;
            return false;
        }
    }

    private static bool GetLegacyKeyUp(KeyCode key)
    {
        if (legacyInputUnavailable || key == KeyCode.None)
        {
            return false;
        }

        try
        {
            return Input.GetKeyUp(key);
        }
        catch (InvalidOperationException)
        {
            legacyInputUnavailable = true;
            return false;
        }
    }

    private void OnEnable()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning(
                $"[{nameof(ControllerKeyboardBinder)}] More than one binder is enabled. " +
                "Only the newest instance will provide static key states.",
                this);
        }

        instance = this;
        ResetPressArming();
        InputSystem.onDeviceChange += OnDeviceChange;
        RefreshBindings();
    }

    private void OnDisable()
    {
        if (instance == this)
        {
            instance = null;
        }

        InputSystem.onDeviceChange -= OnDeviceChange;
        keyStates.Clear();
        keyArmedForPress.Clear();
        keyReleasedSince.Clear();
        resolvedControls.Clear();
        previousDiscoveryPressed.Clear();
    }

    private void Update()
    {
        staleKeys.Clear();

        foreach (KeyCode key in keyStates.Keys)
        {
            staleKeys.Add(key);
        }

        frameHeldKeys.Clear();
        bool canArmPresses = hasUpdatedOnce && Time.unscaledTime >= inputSettleUntil;

        foreach (KeyValuePair<Binding, List<ButtonControl>> pair in resolvedControls)
        {
            Binding binding = pair.Key;
            bool held = IsAnyPressed(pair.Value);

            frameHeldKeys.TryGetValue(binding.keyboardKey, out bool existingHeld);
            frameHeldKeys[binding.keyboardKey] = existingHeld || held;
        }

        foreach (KeyValuePair<KeyCode, bool> pair in frameHeldKeys)
        {
            keyStates.TryGetValue(pair.Key, out KeyState previousState);
            keyArmedForPress.TryGetValue(pair.Key, out bool armedForPress);

            if (pair.Value)
            {
                keyReleasedSince.Remove(pair.Key);
            }
            else if (canArmPresses && HasBeenReleasedLongEnough(pair.Key))
            {
                armedForPress = true;
            }

            keyStates[pair.Key] = new KeyState
            {
                held = pair.Value,
                down = canArmPresses && armedForPress && pair.Value && !previousState.held,
                up = canArmPresses && !pair.Value && previousState.held
            };

            keyArmedForPress[pair.Key] = armedForPress;
            staleKeys.Remove(pair.Key);
        }

        foreach (KeyCode key in staleKeys)
        {
            KeyState previousState = keyStates[key];
            keyStates[key] = new KeyState
            {
                held = false,
                down = false,
                up = canArmPresses && previousState.held
            };

            if (canArmPresses)
            {
                if (HasBeenReleasedLongEnough(key))
                {
                    keyArmedForPress[key] = true;
                }
            }
        }

        hasUpdatedOnce = true;

        if (logPressedControls)
        {
            LogPressedButtonControls();
        }
    }

    private void OnValidate()
    {
        pressPoint = Mathf.Clamp01(pressPoint <= 0f ? DefaultPressPoint : pressPoint);
        startupSettleSeconds = Mathf.Max(0f, startupSettleSeconds);
        releaseArmSeconds = Mathf.Max(0f, releaseArmSeconds);

        if (Application.isPlaying && isActiveAndEnabled)
        {
            RefreshBindings();
        }
    }

    private void ResetPressArming()
    {
        hasUpdatedOnce = false;
        keyArmedForPress.Clear();
        keyReleasedSince.Clear();
        inputSettleUntil = Time.unscaledTime + startupSettleSeconds;
    }

    private bool HasBeenReleasedLongEnough(KeyCode key)
    {
        if (!keyReleasedSince.TryGetValue(key, out float releasedSince))
        {
            keyReleasedSince[key] = Time.unscaledTime;
            return releaseArmSeconds <= 0f;
        }

        return Time.unscaledTime - releasedSince >= releaseArmSeconds;
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        switch (change)
        {
            case InputDeviceChange.Added:
            case InputDeviceChange.Reconnected:
            case InputDeviceChange.Removed:
            case InputDeviceChange.Disconnected:
            case InputDeviceChange.ConfigurationChanged:
                RefreshBindings();
                break;
        }
    }

    private void ResolveExactControlPath(Binding binding, List<ButtonControl> controls)
    {
        if (string.IsNullOrWhiteSpace(binding.controlPath))
        {
            return;
        }

        InputControl control = InputSystem.FindControl(binding.controlPath.Trim());
        if (control is ButtonControl button)
        {
            AddUnique(controls, button);
        }
        else if (control != null)
        {
            Debug.LogWarning(
                $"[{nameof(ControllerKeyboardBinder)}] Control path '{binding.controlPath}' resolved to '{control.path}', but it is not a button.",
                this);
        }
    }

    private void ResolveControlPathSuffix(Binding binding, List<ButtonControl> controls)
    {
        string suffix = GetControlPathSuffix(binding.controlPath);
        if (string.IsNullOrEmpty(suffix))
        {
            return;
        }

        foreach (InputDevice device in InputSystem.devices)
        {
            if (!string.IsNullOrWhiteSpace(binding.deviceContains) &&
                !ContainsDeviceText(device, binding.deviceContains))
            {
                continue;
            }

            foreach (InputControl control in device.allControls)
            {
                if (!(control is ButtonControl button))
                {
                    continue;
                }

                string controlSuffix = GetControlPathSuffix(button.path);
                if (string.Equals(controlSuffix, suffix, StringComparison.OrdinalIgnoreCase))
                {
                    AddUnique(controls, button);
                }
            }
        }
    }

    private void ResolveLooseControlMatch(Binding binding, List<ButtonControl> controls)
    {
        bool hasDeviceFilter = !string.IsNullOrWhiteSpace(binding.deviceContains);
        bool hasControlFilter = !string.IsNullOrWhiteSpace(binding.controlContains);

        if (!hasDeviceFilter && !hasControlFilter)
        {
            return;
        }

        string deviceFilter = binding.deviceContains ?? string.Empty;
        string controlFilter = binding.controlContains ?? string.Empty;

        foreach (InputDevice device in InputSystem.devices)
        {
            if (hasDeviceFilter && !ContainsDeviceText(device, deviceFilter))
            {
                continue;
            }

            foreach (InputControl control in device.allControls)
            {
                if (!(control is ButtonControl button))
                {
                    continue;
                }

                if (hasControlFilter && !ContainsControlText(button, controlFilter))
                {
                    continue;
                }

                AddUnique(controls, button);
            }
        }
    }

    private bool IsAnyPressed(List<ButtonControl> controls)
    {
        foreach (ButtonControl control in controls)
        {
            if (control.device.added && control.ReadValue() >= pressPoint)
            {
                return true;
            }
        }

        return false;
    }

    private void LogPressedButtonControls()
    {
        foreach (InputDevice device in InputSystem.devices)
        {
            if (device is Keyboard || device is Mouse)
            {
                continue;
            }

            foreach (InputControl control in device.allControls)
            {
                if (!(control is ButtonControl button))
                {
                    continue;
                }

                string key = button.path;
                bool isPressed = button.ReadValue() >= pressPoint;
                previousDiscoveryPressed.TryGetValue(key, out bool wasPressed);
                previousDiscoveryPressed[key] = isPressed;

                if (!isPressed || wasPressed)
                {
                    continue;
                }

                if (logOnlyOncePerControl && !loggedControls.Add(key))
                {
                    continue;
                }

                Debug.Log(
                    $"[{nameof(ControllerKeyboardBinder)}] Pressed control: path='{button.path}', " +
                    $"device='{device.displayName}', layout='{device.layout}', control='{button.name}', display='{button.displayName}'",
                    this);
            }
        }
    }

    private static void AddUnique(List<ButtonControl> controls, ButtonControl button)
    {
        if (!controls.Contains(button))
        {
            controls.Add(button);
        }
    }

    private static bool ContainsDeviceText(InputDevice device, string text)
    {
        return Contains(device.name, text) ||
               Contains(device.displayName, text) ||
               Contains(device.layout, text) ||
               Contains(device.description.product, text) ||
               Contains(device.description.manufacturer, text);
    }

    private static bool ContainsControlText(InputControl control, string text)
    {
        return Contains(control.name, text) ||
               Contains(control.displayName, text) ||
               Contains(control.shortDisplayName, text) ||
               Contains(control.path, text);
    }

    private static string GetControlPathSuffix(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        string normalized = path.Trim();
        if (normalized.StartsWith("<", StringComparison.Ordinal))
        {
            int layoutEnd = normalized.IndexOf('>');
            return layoutEnd >= 0 && layoutEnd + 1 < normalized.Length
                ? normalized.Substring(layoutEnd + 1)
                : string.Empty;
        }

        if (normalized.StartsWith("/", StringComparison.Ordinal))
        {
            int deviceEnd = normalized.IndexOf('/', 1);
            return deviceEnd >= 0 && deviceEnd < normalized.Length
                ? normalized.Substring(deviceEnd)
                : string.Empty;
        }

        return normalized.StartsWith("/", StringComparison.Ordinal) ? normalized : "/" + normalized;
    }

    private static bool Contains(string value, string text)
    {
        return !string.IsNullOrEmpty(value) &&
               value.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
