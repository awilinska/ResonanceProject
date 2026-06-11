using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class CityLightSequenceController : MonoBehaviour
{
    [Header("City Lights")]
    [SerializeField] private Light[] pointLights;
    [SerializeField] private bool turnAllLightsOffOnStart = true;
    [SerializeField, Min(0f)] private float delayBetweenLights = 0.1f;

    private bool lightsAreOn;
    private bool sequenceIsRunning;

    private void Awake()
    {
        if (turnAllLightsOffOnStart)
        {
            SetAllLights(false);
            lightsAreOn = false;
            return;
        }

        lightsAreOn = AreAllLightsOn();
    }

    private void Update()
    {
        if (WasLightSequencePressed())
        {
            PlayLightSequence();
        }
    }

    public void PlayLightSequence()
    {
        if (sequenceIsRunning || pointLights == null || pointLights.Length == 0)
        {
            return;
        }

        StartCoroutine(SetLightsSequentially(!lightsAreOn));
    }

    private IEnumerator SetLightsSequentially(bool enabledState)
    {
        sequenceIsRunning = true;

        for (int i = 0; i < pointLights.Length; i++)
        {
            if (pointLights[i] == null)
            {
                continue;
            }

            pointLights[i].enabled = enabledState;

            if (delayBetweenLights > 0f)
            {
                yield return new WaitForSeconds(delayBetweenLights);
            }
        }

        lightsAreOn = enabledState;
        sequenceIsRunning = false;
    }

    public void SetAllLights(bool enabledState)
    {
        if (pointLights == null)
        {
            return;
        }

        for (int i = 0; i < pointLights.Length; i++)
        {
            if (pointLights[i] != null)
            {
                pointLights[i].enabled = enabledState;
            }
        }
    }

    private bool AreAllLightsOn()
    {
        if (pointLights == null || pointLights.Length == 0)
        {
            return false;
        }

        bool foundAssignedLight = false;
        for (int i = 0; i < pointLights.Length; i++)
        {
            if (pointLights[i] == null)
            {
                continue;
            }

            foundAssignedLight = true;
            if (!pointLights[i].enabled)
            {
                return false;
            }
        }

        return foundAssignedLight;
    }

    private static bool WasLightSequencePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.lKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.L);
#else
        return false;
#endif
    }
}
