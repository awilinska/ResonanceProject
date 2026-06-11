using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class CameraArrowSwitcher : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private Camera[] cameras;
    [SerializeField, Min(0)] private int startingCameraIndex;

    private int currentCameraIndex;

    private void Awake()
    {
        if (cameras == null || cameras.Length == 0)
        {
            Debug.LogWarning(
                $"[{nameof(CameraArrowSwitcher)}] Assign at least one camera in the Inspector.",
                this);
            enabled = false;
            return;
        }

        currentCameraIndex = FindValidCameraIndex(startingCameraIndex, 1);
        if (currentCameraIndex < 0)
        {
            Debug.LogWarning(
                $"[{nameof(CameraArrowSwitcher)}] The camera list contains no assigned cameras.",
                this);
            enabled = false;
            return;
        }

        ActivateCamera(currentCameraIndex);
    }

    private void Update()
    {
        if (WasRightArrowPressed())
        {
            ShowNextCamera();
        }
        else if (WasLeftArrowPressed())
        {
            ShowPreviousCamera();
        }
    }

    public void ShowNextCamera()
    {
        SwitchCamera(1);
    }

    public void ShowPreviousCamera()
    {
        SwitchCamera(-1);
    }

    private void SwitchCamera(int direction)
    {
        int nextIndex = FindValidCameraIndex(currentCameraIndex + direction, direction);
        if (nextIndex >= 0)
        {
            currentCameraIndex = nextIndex;
            ActivateCamera(currentCameraIndex);
        }
    }

    private int FindValidCameraIndex(int startIndex, int direction)
    {
        if (cameras == null || cameras.Length == 0)
        {
            return -1;
        }

        int index = WrapIndex(startIndex);
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[index] != null)
            {
                return index;
            }

            index = WrapIndex(index + direction);
        }

        return -1;
    }

    private void ActivateCamera(int activeIndex)
    {
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null)
            {
                cameras[i].gameObject.SetActive(i == activeIndex);
            }
        }
    }

    private int WrapIndex(int index)
    {
        return (index % cameras.Length + cameras.Length) % cameras.Length;
    }

    private static bool WasRightArrowPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.rightArrowKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.RightArrow);
#else
        return false;
#endif
    }

    private static bool WasLeftArrowPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.leftArrowKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.LeftArrow);
#else
        return false;
#endif
    }
}
