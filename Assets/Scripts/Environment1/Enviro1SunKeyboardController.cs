using Tenkoku.Core;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

/// <summary>
/// Maps keyboard keys 1-9 to Tenkoku hours from 09:00 through 18:00.
/// </summary>
public class Enviro1SunKeyboardController : MonoBehaviour
{
    [Header("Tenkoku")]
    [SerializeField] private TenkokuModule tenkokuModule;
    [SerializeField] private bool stopAutomaticTime = true;

    [Header("Hour Range")]
    [SerializeField, Range(0, 23)] private int startHour = 9;
    [SerializeField, Range(0, 23)] private int endHour = 18;

    [Header("State")]
    [SerializeField, Range(1, 9)] private int currentSlot = 1;

#if ENABLE_INPUT_SYSTEM
    [Header("Steering Wheel Time Control")]
    [SerializeField] private bool useSteeringWheelTime = true;
    [SerializeField] private string wheelDeviceContains = "B696";
    [SerializeField] private string wheelAxisContains = "stick/x";
    [SerializeField] private Vector2 wheelAxisInputRange = new Vector2(-1f, 1f);
    [SerializeField, Range(0f, 0.95f)] private float wheelDeadzone = 0.03f;
    [SerializeField, Min(0f)] private float wheelChangeThreshold = 0.01f;
    [SerializeField, Min(0f)] private float wheelTimeUpdateInterval = 0.05f;
    [SerializeField] private bool invertWheel;

    private AxisControl steeringAxis;
    private bool hasWheelAxisSample;
    private float lastWheelAxisValue;
    private float nextWheelTimeUpdate;
#endif

    private void Awake()
    {
        ResolveTenkokuModule();
        currentSlot = Mathf.Clamp(currentSlot, 1, 9);
#if ENABLE_INPUT_SYSTEM
        ResolveSteeringAxis();
#endif
        SetSlot(currentSlot);
    }

    private void OnValidate()
    {
        currentSlot = Mathf.Clamp(currentSlot, 1, 9);
        startHour = Mathf.Clamp(startHour, 0, 23);
        endHour = Mathf.Clamp(endHour, 0, 23);
#if ENABLE_INPUT_SYSTEM
        if (Mathf.Approximately(wheelAxisInputRange.x, wheelAxisInputRange.y))
        {
            wheelAxisInputRange = new Vector2(-1f, 1f);
        }

        wheelChangeThreshold = Mathf.Max(0f, wheelChangeThreshold);
        wheelTimeUpdateInterval = Mathf.Max(0f, wheelTimeUpdateInterval);
#endif
        ResolveTenkokuModule();
    }

    private void Update()
    {
        for (int slot = 1; slot <= 9; slot++)
        {
            if (IsSlotPressed(slot))
            {
                SetSlot(slot);
                return;
            }
        }

#if ENABLE_INPUT_SYSTEM
        UpdateTimeFromSteeringWheel();
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private bool UpdateTimeFromSteeringWheel()
    {
        if (!useSteeringWheelTime || Time.unscaledTime < nextWheelTimeUpdate)
        {
            return false;
        }

        if (steeringAxis == null || !steeringAxis.device.added)
        {
            ResolveSteeringAxis();
        }

        if (steeringAxis == null)
        {
            return false;
        }

        float axisMinimum = Mathf.Min(wheelAxisInputRange.x, wheelAxisInputRange.y);
        float axisMaximum = Mathf.Max(wheelAxisInputRange.x, wheelAxisInputRange.y);
        float axisValue = Mathf.Clamp(steeringAxis.ReadValue(), axisMinimum, axisMaximum);
        if (invertWheel)
        {
            axisValue = axisMaximum - (axisValue - axisMinimum);
        }

        float axisCenter = Mathf.Lerp(axisMinimum, axisMaximum, 0.5f);
        float axisHalfRange = Mathf.Max(0.0001f, (axisMaximum - axisMinimum) * 0.5f);
        if (Mathf.Abs(axisValue - axisCenter) / axisHalfRange < wheelDeadzone)
        {
            axisValue = axisCenter;
        }

        if (!hasWheelAxisSample)
        {
            hasWheelAxisSample = true;
            lastWheelAxisValue = axisValue;
            return false;
        }

        if (Mathf.Abs(axisValue - lastWheelAxisValue) < wheelChangeThreshold)
        {
            return false;
        }

        lastWheelAxisValue = axisValue;
        float normalized = Mathf.InverseLerp(axisMinimum, axisMaximum, axisValue);
        float hour = Mathf.Lerp(startHour, endHour, normalized);
        SetTime(hour);
        currentSlot = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(1f, 9f, normalized)), 1, 9);
        nextWheelTimeUpdate = Time.unscaledTime + wheelTimeUpdateInterval;
        return true;
    }

    private void ResolveSteeringAxis()
    {
        steeringAxis = null;

        foreach (InputDevice device in InputSystem.devices)
        {
            if (!ContainsText(device.name, wheelDeviceContains) &&
                !ContainsText(device.displayName, wheelDeviceContains) &&
                !ContainsText(device.layout, wheelDeviceContains) &&
                !ContainsText(device.description.product, wheelDeviceContains) &&
                !ContainsText(device.description.manufacturer, wheelDeviceContains))
            {
                continue;
            }

            AxisControl fallbackAxis = null;
            foreach (InputControl control in device.allControls)
            {
                if (!(control is AxisControl axis))
                {
                    continue;
                }

                if (ContainsText(axis.path, wheelAxisContains) ||
                    ContainsText(axis.name, wheelAxisContains) ||
                    ContainsText(axis.displayName, wheelAxisContains))
                {
                    steeringAxis = axis;
                    Debug.Log(
                        $"[{nameof(Enviro1SunKeyboardController)}] Using steering axis '{axis.path}' on '{device.displayName}'.",
                        this);
                    return;
                }

                if (fallbackAxis == null &&
                    !ContainsText(axis.path, "trigger") &&
                    !ContainsText(axis.path, "hat") &&
                    !ContainsText(axis.path, "button"))
                {
                    fallbackAxis = axis;
                }
            }

            if (fallbackAxis != null)
            {
                steeringAxis = fallbackAxis;
                Debug.Log(
                    $"[{nameof(Enviro1SunKeyboardController)}] Using fallback steering axis '{fallbackAxis.path}' on '{device.displayName}'.",
                    this);
                return;
            }
        }
    }
#endif

    public void SetSlot(int slot)
    {
        currentSlot = Mathf.Clamp(slot, 1, 9);
        if (tenkokuModule == null)
        {
            Debug.LogWarning(
                $"[{nameof(Enviro1SunKeyboardController)}] Assign a Tenkoku Module.",
                this);
            return;
        }

        if (stopAutomaticTime)
        {
            tenkokuModule.useAutoTime = false;
            tenkokuModule.autoTimeSync = false;
        }

        float slotPosition = (currentSlot - 1f) / 8f;
        SetTime(Mathf.Lerp(startHour, endHour, slotPosition));
    }

    private void SetTime(float hour)
    {
        if (tenkokuModule == null)
        {
            Debug.LogWarning(
                $"[{nameof(Enviro1SunKeyboardController)}] Assign a Tenkoku Module.",
                this);
            return;
        }

        if (stopAutomaticTime)
        {
            tenkokuModule.useAutoTime = false;
            tenkokuModule.autoTimeSync = false;
        }

        float wrappedHour = Mathf.Repeat(hour, 24f);
        tenkokuModule.currentHour = Mathf.FloorToInt(wrappedHour);
        tenkokuModule.currentMinute = Mathf.FloorToInt((wrappedHour - tenkokuModule.currentHour) * 60f);
        tenkokuModule.currentSecond = 0;
    }

    private void ResolveTenkokuModule()
    {
        if (tenkokuModule == null)
        {
            tenkokuModule = FindAnyObjectByType<TenkokuModule>();
        }
    }

    private static bool IsSlotPressed(int slot)
    {
#if ENABLE_INPUT_SYSTEM
        Key mainKey = slot switch
        {
            1 => Key.Digit1,
            2 => Key.Digit2,
            3 => Key.Digit3,
            4 => Key.Digit4,
            5 => Key.Digit5,
            6 => Key.Digit6,
            7 => Key.Digit7,
            8 => Key.Digit8,
            9 => Key.Digit9,
            _ => Key.None
        };

        Key numpadKey = slot switch
        {
            1 => Key.Numpad1,
            2 => Key.Numpad2,
            3 => Key.Numpad3,
            4 => Key.Numpad4,
            5 => Key.Numpad5,
            6 => Key.Numpad6,
            7 => Key.Numpad7,
            8 => Key.Numpad8,
            9 => Key.Numpad9,
            _ => Key.None
        };

        bool mainPressed = mainKey != Key.None && ControllerKeyboardBinder.GetKeyDown(mainKey);
        bool numpadPressed = numpadKey != Key.None && ControllerKeyboardBinder.GetKeyDown(numpadKey);
        return mainPressed || numpadPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return ControllerKeyboardBinder.GetKeyDown(KeyCode.Alpha0 + slot) ||
               ControllerKeyboardBinder.GetKeyDown(KeyCode.Keypad0 + slot);
#else
        return false;
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private static bool ContainsText(string value, string text)
    {
        return string.IsNullOrWhiteSpace(text) ||
               !string.IsNullOrEmpty(value) &&
               value.IndexOf(text, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
#endif
}
