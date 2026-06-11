using Tenkoku.Core;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
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

    private void Awake()
    {
        ResolveTenkokuModule();
        currentSlot = Mathf.Clamp(currentSlot, 1, 9);
        SetSlot(currentSlot);
    }

    private void OnValidate()
    {
        currentSlot = Mathf.Clamp(currentSlot, 1, 9);
        startHour = Mathf.Clamp(startHour, 0, 23);
        endHour = Mathf.Clamp(endHour, 0, 23);
        ResolveTenkokuModule();
    }

    private void Update()
    {
        for (int slot = 1; slot <= 9; slot++)
        {
            if (IsSlotPressed(slot))
            {
                SetSlot(slot);
                break;
            }
        }
    }

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
        tenkokuModule.currentHour = Mathf.RoundToInt(
            Mathf.Lerp(startHour, endHour, slotPosition));
        tenkokuModule.currentMinute = 0;
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
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

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

        bool mainPressed = mainKey != Key.None && keyboard[mainKey].wasPressedThisFrame;
        bool numpadPressed = numpadKey != Key.None && keyboard[numpadKey].wasPressedThisFrame;
        return mainPressed || numpadPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Alpha0 + slot) ||
               Input.GetKeyDown(KeyCode.Keypad0 + slot);
#else
        return false;
#endif
    }
}
