using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Toggles environment FX objects by keyboard.
/// </summary>
public class Enviro1FogToggleController : MonoBehaviour
{
    [System.Serializable]
    private sealed class EnvironmentControls
    {
        public string label;
        public Transform environmentRoot;
        public GameObject fireObject;
        public GameObject fogObject;
        public GameObject rainObject;
        public GameObject[] stormObjects;
        public GameObject windObject;

#if ENABLE_INPUT_SYSTEM
        public Key fireKey = Key.None;
        public Key fogKey = Key.None;
        public Key rainKey = Key.None;
        public Key stormKey = Key.None;
        public Key windKey = Key.None;
#elif ENABLE_LEGACY_INPUT_MANAGER
        public KeyCode fireKey = KeyCode.None;
        public KeyCode fogKey = KeyCode.None;
        public KeyCode rainKey = KeyCode.None;
        public KeyCode stormKey = KeyCode.None;
        public KeyCode windKey = KeyCode.None;
#endif
    }

    [Header("Environment 1 Targets")]
    [SerializeField] private GameObject fireObject;
    [SerializeField] private GameObject fogObject;
    [SerializeField] private GameObject rainObject;
    [SerializeField] private GameObject lightningObject;
    [SerializeField] private GameObject windObject;

    [Header("Environment 1 Keys")]
#if ENABLE_INPUT_SYSTEM
    [SerializeField] private Key fireKey = Key.F;
    [SerializeField] private Key fogKey = Key.G;
    [SerializeField] private Key rainKey = Key.R;
    [SerializeField] private Key stormKey = Key.S;
    [SerializeField] private Key windKey = Key.W;
#elif ENABLE_LEGACY_INPUT_MANAGER
    [SerializeField] private KeyCode fireKey = KeyCode.F;
    [SerializeField] private KeyCode fogKey = KeyCode.G;
    [SerializeField] private KeyCode rainKey = KeyCode.R;
    [SerializeField] private KeyCode stormKey = KeyCode.S;
    [SerializeField] private KeyCode windKey = KeyCode.W;
#endif

    [Header("Additional Environments")]
    [SerializeField] private EnvironmentControls[] additionalEnvironments;

    [Header("Startup")]
    [SerializeField] private bool turnAllTargetsOffOnStart = true;

    private void Start()
    {
        if (turnAllTargetsOffOnStart)
        {
            SetEnvironmentActive(fireObject, fogObject, rainObject, new[] { lightningObject }, windObject, false);
        }

        ValidateEnvironment("Environment 1", fireObject, fogObject, rainObject, new[] { lightningObject }, windObject);

        if (additionalEnvironments == null)
        {
            return;
        }

        for (int i = 0; i < additionalEnvironments.Length; i++)
        {
            EnvironmentControls controls = additionalEnvironments[i];
            if (controls == null)
            {
                continue;
            }

            ResolveMissingTargets(controls);
            CreateMissingTargetsFromEnvironment1(controls);

            if (turnAllTargetsOffOnStart)
            {
                SetEnvironmentActive(
                    controls.fireObject,
                    controls.fogObject,
                    controls.rainObject,
                    controls.stormObjects,
                    controls.windObject,
                    false);
            }

            ValidateEnvironment(controls.label, controls.fireObject, controls.fogObject, controls.rainObject, controls.stormObjects, controls.windObject);
        }
    }

    private void Update()
    {
        HandleEnvironment(
            fireObject,
            fogObject,
            rainObject,
            new[] { lightningObject },
            windObject,
            fireKey,
            fogKey,
            rainKey,
            stormKey,
            windKey);

        if (additionalEnvironments == null)
        {
            return;
        }

        for (int i = 0; i < additionalEnvironments.Length; i++)
        {
            EnvironmentControls controls = additionalEnvironments[i];
            if (controls == null)
            {
                continue;
            }

            HandleEnvironment(
                controls.fireObject,
                controls.fogObject,
                controls.rainObject,
                controls.stormObjects,
                controls.windObject,
                controls.fireKey,
                controls.fogKey,
                controls.rainKey,
                controls.stormKey,
                controls.windKey);
        }
    }

    public void SetFireObject(GameObject target) => fireObject = target;
    public void SetFogObject(GameObject target) => fogObject = target;
    public void SetRainObject(GameObject target) => rainObject = target;
    public void SetLightningObject(GameObject target) => lightningObject = target;
    public void SetWindObject(GameObject target) => windObject = target;

    private static void HandleEnvironment(
        GameObject fireTarget,
        GameObject fogTarget,
        GameObject rainTarget,
        GameObject[] stormTargets,
        GameObject windTarget,
#if ENABLE_INPUT_SYSTEM
        Key fireToggleKey,
        Key fogToggleKey,
        Key rainToggleKey,
        Key stormToggleKey,
        Key windToggleKey)
#elif ENABLE_LEGACY_INPUT_MANAGER
        KeyCode fireToggleKey,
        KeyCode fogToggleKey,
        KeyCode rainToggleKey,
        KeyCode stormToggleKey,
        KeyCode windToggleKey)
#else
        int fireToggleKey,
        int fogToggleKey,
        int rainToggleKey,
        int stormToggleKey,
        int windToggleKey)
#endif
    {
        if (WasKeyPressed(fireToggleKey) &&
            fireTarget != null &&
            !fireTarget.activeSelf &&
            (rainTarget == null || !rainTarget.activeSelf))
        {
            fireTarget.SetActive(true);
        }

        if (WasKeyPressed(fogToggleKey) && fogTarget != null)
        {
            fogTarget.SetActive(!fogTarget.activeSelf);
        }

        if (WasKeyPressed(rainToggleKey) && rainTarget != null)
        {
            bool nextRainState = !rainTarget.activeSelf;
            rainTarget.SetActive(nextRainState);

            if (nextRainState)
            {
                TurnOffFire(fireTarget);
            }
        }

        if (WasKeyPressed(stormToggleKey))
        {
            ToggleStorm(fireTarget, rainTarget, stormTargets);
        }

        if (WasKeyPressed(windToggleKey) && windTarget != null)
        {
            windTarget.SetActive(!windTarget.activeSelf);
        }
    }

    private static void ToggleStorm(GameObject fireTarget, GameObject rainTarget, GameObject[] stormTargets)
    {
        bool stormIsActive = (rainTarget != null && rainTarget.activeSelf) &&
                             AnyActive(stormTargets);
        bool nextState = !stormIsActive;

        if (rainTarget != null)
        {
            rainTarget.SetActive(nextState);
        }

        SetAllActive(stormTargets, nextState);

        if (nextState)
        {
            TurnOffFire(fireTarget);
        }
    }

    private static bool AnyActive(GameObject[] targets)
    {
        if (targets == null)
        {
            return false;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null && targets[i].activeSelf)
            {
                return true;
            }
        }

        return false;
    }

    private static void SetAllActive(GameObject[] targets, bool active)
    {
        if (targets == null)
        {
            return;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                targets[i].SetActive(active);
            }
        }
    }

    private static void TurnOffFire(GameObject fireTarget)
    {
        if (fireTarget != null && fireTarget.activeSelf)
        {
            fireTarget.SetActive(false);
        }
    }

    private static void SetEnvironmentActive(
        GameObject fireTarget,
        GameObject fogTarget,
        GameObject rainTarget,
        GameObject[] stormTargets,
        GameObject windTarget,
        bool active)
    {
        if (fireTarget != null)
        {
            fireTarget.SetActive(active);
        }

        if (fogTarget != null)
        {
            fogTarget.SetActive(active);
        }

        if (rainTarget != null)
        {
            rainTarget.SetActive(active);
        }

        SetAllActive(stormTargets, active);

        if (windTarget != null)
        {
            windTarget.SetActive(active);
        }
    }

    private static void ValidateEnvironment(
        string label,
        GameObject fireTarget,
        GameObject fogTarget,
        GameObject rainTarget,
        GameObject[] stormTargets,
        GameObject windTarget)
    {
        string prefix = string.IsNullOrWhiteSpace(label) ? nameof(Enviro1FogToggleController) : label;

        WarnIfMissing(prefix, "Fire Object", fireTarget);
        WarnIfMissing(prefix, "Fog Object", fogTarget);
        WarnIfMissing(prefix, "Rain Object", rainTarget);
        WarnIfMissing(prefix, "Storm Object", FirstAssigned(stormTargets));
        WarnIfMissing(prefix, "Wind Object", windTarget);
    }

    private static void WarnIfMissing(string label, string targetName, GameObject target)
    {
        if (target == null)
        {
            Debug.LogWarning($"[{nameof(Enviro1FogToggleController)}] {label}: {targetName} is not assigned.");
        }
    }

    private static GameObject FirstAssigned(GameObject[] targets)
    {
        if (targets == null)
        {
            return null;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                return targets[i];
            }
        }

        return null;
    }

    private static void ResolveMissingTargets(EnvironmentControls controls)
    {
        if (controls.environmentRoot == null)
        {
            return;
        }

        controls.fireObject ??= FindChildObject(controls.environmentRoot, "Fire");
        controls.fogObject ??= FindChildObject(controls.environmentRoot, "Fog", "Whitish Fog");
        controls.rainObject ??= FindChildObject(controls.environmentRoot, "Rain");
        controls.windObject ??= FindChildObject(controls.environmentRoot, "Wind");

        if (controls.stormObjects == null || controls.stormObjects.Length == 0)
        {
            controls.stormObjects = FindChildObjects(controls.environmentRoot, "Storm");
        }
    }

    private void CreateMissingTargetsFromEnvironment1(EnvironmentControls controls)
    {
        if (controls.environmentRoot == null)
        {
            return;
        }

        controls.fireObject ??= CloneTarget(fireObject, controls.environmentRoot);
        controls.fogObject ??= CloneTarget(fogObject, controls.environmentRoot);
        controls.rainObject ??= CloneTarget(rainObject, controls.environmentRoot);
        controls.windObject ??= CloneTarget(windObject, controls.environmentRoot);

        if (controls.stormObjects == null || controls.stormObjects.Length == 0)
        {
            GameObject stormClone = CloneTarget(lightningObject, controls.environmentRoot);
            controls.stormObjects = stormClone == null
                ? System.Array.Empty<GameObject>()
                : new[] { stormClone };
        }
    }

    private static GameObject CloneTarget(GameObject template, Transform parent)
    {
        if (template == null || parent == null)
        {
            return null;
        }

        GameObject clone = Instantiate(template, parent, false);
        clone.name = template.name;
        clone.SetActive(false);
        return clone;
    }

    private static GameObject FindChildObject(Transform root, params string[] names)
    {
        GameObject[] matches = FindChildObjects(root, names);
        return matches.Length > 0 ? matches[0] : null;
    }

    private static GameObject[] FindChildObjects(Transform root, params string[] names)
    {
        if (root == null || names == null || names.Length == 0)
        {
            return System.Array.Empty<GameObject>();
        }

        System.Collections.Generic.List<GameObject> matches = new System.Collections.Generic.List<GameObject>();
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
            {
                if (child.name == names[nameIndex])
                {
                    matches.Add(child.gameObject);
                    break;
                }
            }
        }

        return matches.ToArray();
    }

#if ENABLE_INPUT_SYSTEM
    private static bool WasKeyPressed(Key key)
    {
        return key != Key.None && ControllerKeyboardBinder.GetKeyDown(key);
    }
#elif ENABLE_LEGACY_INPUT_MANAGER
    private static bool WasKeyPressed(KeyCode key)
    {
        return key != KeyCode.None && ControllerKeyboardBinder.GetKeyDown(key);
    }
#else
    private static bool WasKeyPressed(int key)
    {
        return false;
    }
#endif
}
