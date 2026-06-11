using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Toggles environment FX objects by keyboard:
/// F = fire, G = fog, R = rain, S = storm (rain + lightning), W = wind.
/// </summary>
public class Enviro1FogToggleController : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private GameObject fireObject;
    [SerializeField] private GameObject fogObject;
    [SerializeField] private GameObject rainObject;
    [SerializeField] private GameObject lightningObject;
    [SerializeField] private GameObject windObject;

    private void Start()
    {
        if (fireObject == null)
        {
            Debug.LogWarning("[Enviro1FogToggleController] Fire Object is not assigned.");
        }
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
        if (windObject == null)
        {
            Debug.LogWarning("[Enviro1FogToggleController] Wind Object is not assigned.");
        }
    }

    private void Update()
    {
        if (WasFireTogglePressed() &&
            fireObject != null &&
            !fireObject.activeSelf &&
            (rainObject == null || !rainObject.activeSelf))
        {
            fireObject.SetActive(true);
        }

        if (WasFogTogglePressed() && fogObject != null)
        {
            fogObject.SetActive(!fogObject.activeSelf);
        }

        if (WasRainTogglePressed() && rainObject != null)
        {
            bool nextRainState = !rainObject.activeSelf;
            rainObject.SetActive(nextRainState);

            if (nextRainState)
            {
                TurnOffFire();
            }
        }

        if (WasStormTogglePressed())
        {
            ToggleStorm();
        }

        if (WasWindTogglePressed() && windObject != null)
        {
            windObject.SetActive(!windObject.activeSelf);
        }
    }

    public void SetFireObject(GameObject target) => fireObject = target;
    public void SetFogObject(GameObject target) => fogObject = target;
    public void SetRainObject(GameObject target) => rainObject = target;
    public void SetLightningObject(GameObject target) => lightningObject = target;
    public void SetWindObject(GameObject target) => windObject = target;

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

        if (nextState)
        {
            TurnOffFire();
        }
    }

    private void TurnOffFire()
    {
        if (fireObject != null && fireObject.activeSelf)
        {
            fireObject.SetActive(false);
        }
    }

    private static bool WasFireTogglePressed()
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

    private static bool WasFogTogglePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.gKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.G);
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

    private static bool WasWindTogglePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.wKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.W);
#else
        return false;
#endif
    }
}
