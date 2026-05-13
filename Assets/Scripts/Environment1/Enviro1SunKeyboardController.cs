using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Maps keyboard keys 1-9 to a smooth sun rotation range.
/// 1 = noon (brightest / highest), 9 = evening / dark (near or below horizon).
/// </summary>
public class Enviro1SunKeyboardController : MonoBehaviour
{
    [Header("Sun")]
    [SerializeField] private Light directionalLight;

    [Header("Rotation Range")]
    [SerializeField] private Vector3 noonEuler = new Vector3(60f, -30f, 0f);
    [SerializeField] private Vector3 eveningDarkEuler = new Vector3(-10f, -30f, 0f);

    [Header("Smoothing")]
    [SerializeField, Min(1f)] private float rotationSpeedDegreesPerSecond = 35f;

    [Header("State")]
    [SerializeField, Range(1, 9)] private int currentSlot = 1;

    private Quaternion targetRotation;

    private void Awake()
    {
        ResolveDirectionalLight();
        currentSlot = Mathf.Clamp(currentSlot, 1, 9);
        SetSlot(currentSlot, true);
    }

    private void OnValidate()
    {
        currentSlot = Mathf.Clamp(currentSlot, 1, 9);
        ResolveDirectionalLight();
        targetRotation = GetRotationForSlot(currentSlot);
    }

    private void Update()
    {
        bool changed = false;
        for (int slot = 1; slot <= 9; slot++)
        {
            if (IsSlotPressed(slot))
            {
                SetSlot(slot, false);
                changed = true;
                break;
            }
        }

        if (!changed && directionalLight == null)
        {
            return;
        }

        if (directionalLight != null)
        {
            directionalLight.transform.rotation = Quaternion.RotateTowards(
                directionalLight.transform.rotation,
                targetRotation,
                rotationSpeedDegreesPerSecond * Time.deltaTime);
        }
    }

    public void SetSlot(int slot, bool snap)
    {
        currentSlot = Mathf.Clamp(slot, 1, 9);
        targetRotation = GetRotationForSlot(currentSlot);

        if (snap && directionalLight != null)
        {
            directionalLight.transform.rotation = targetRotation;
        }
    }

    private Quaternion GetRotationForSlot(int slot)
    {
        float t = (Mathf.Clamp(slot, 1, 9) - 1f) / 8f;
        return Quaternion.Euler(Vector3.Lerp(noonEuler, eveningDarkEuler, t));
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
        return Input.GetKeyDown(KeyCode.Alpha0 + slot) || Input.GetKeyDown(KeyCode.Keypad0 + slot);
#else
        return false;
#endif
    }

    private void ResolveDirectionalLight()
    {
        if (directionalLight != null)
        {
            return;
        }

        Light ownLight = GetComponent<Light>();
        if (ownLight != null && ownLight.type == LightType.Directional)
        {
            directionalLight = ownLight;
            return;
        }

        directionalLight = RenderSettings.sun;
    }
}
