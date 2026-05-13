using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Toggles environment FX objects by keyboard:
/// F = fog, R = rain, S = storm (rain + lightning).
/// </summary>
public class Enviro1FogToggleController : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private GameObject fogObject;
    [SerializeField] private GameObject rainObject;
    [SerializeField] private GameObject lightningObject;

    private void Start()
    {
        if (fogObject == null)
        {
            Debug.LogWarning("[Enviro1FogToggleController] Fog Object is not assigned.");
        }
        if (rainObject == null)
        {
            Debug.LogWarning("[Enviro1FogToggleController] Rain Object is not assigned.");
        }
        if (lightningObject == null)
        {
            Debug.LogWarning("[Enviro1FogToggleController] Lightning Object is not assigned.");
        }
    }

    private void Update()
    {
        if (WasFogTogglePressed() && fogObject != null)
        {
            fogObject.SetActive(!fogObject.activeSelf);
        }

        if (WasRainTogglePressed() && rainObject != null)
        {
            rainObject.SetActive(!rainObject.activeSelf);
        }

        if (WasStormTogglePressed())
        {
            ToggleStorm();
        }
    }

    public void SetFogObject(GameObject target) => fogObject = target;
    public void SetRainObject(GameObject target) => rainObject = target;
    public void SetLightningObject(GameObject target) => lightningObject = target;

    private void ToggleStorm()
    {
        bool stormIsActive = (rainObject != null && rainObject.activeSelf) &&
                             (lightningObject != null && lightningObject.activeSelf);
        bool nextState = !stormIsActive;

        if (rainObject != null)
        {
            rainObject.SetActive(nextState);
        }
        if (lightningObject != null)
        {
            lightningObject.SetActive(nextState);
        }
    }

    private static bool WasFogTogglePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.fKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.F);
#else
        return false;
#endif
    }

    private static bool WasRainTogglePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.rKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.R);
#else
        return false;
#endif
    }

    private static bool WasStormTogglePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.sKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.S);
#else
        return false;
#endif
    }
}
