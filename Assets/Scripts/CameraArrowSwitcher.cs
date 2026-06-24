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

    [Header("Split View")]
    [SerializeField] private bool includeSplitView = true;

    private Rect[] originalCameraRects;
    private AudioListener[] cameraAudioListeners;
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

        CacheCameraState();
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
        int viewCount = GetViewCount();
        if (viewCount == 0)
        {
            return -1;
        }

        int index = WrapIndex(startIndex);
        for (int i = 0; i < viewCount; i++)
        {
            if (IsValidViewIndex(index))
            {
                return index;
            }

            index = WrapIndex(index + direction);
        }

        return -1;
    }

    private void ActivateCamera(int activeIndex)
    {
        bool isSplitView = IsSplitViewIndex(activeIndex);
        int splitCameraCount = isSplitView ? GetAssignedCameraCount() : 0;
        int splitCameraIndex = 0;

        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null)
            {
                bool isActive = isSplitView || i == activeIndex;
                cameras[i].gameObject.SetActive(isActive);
                cameras[i].rect =
                    isSplitView
                        ? GetSplitViewport(splitCameraIndex++, splitCameraCount)
                        : originalCameraRects[i];

                if (cameraAudioListeners[i] != null)
                {
                    cameraAudioListeners[i].enabled =
                        isActive && (!isSplitView || splitCameraIndex == 1);
                }
            }
        }
    }

    private int WrapIndex(int index)
    {
        int viewCount = GetViewCount();
        return (index % viewCount + viewCount) % viewCount;
    }

    private void CacheCameraState()
    {
        originalCameraRects = new Rect[cameras.Length];
        cameraAudioListeners = new AudioListener[cameras.Length];

        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] == null)
            {
                continue;
            }

            originalCameraRects[i] = cameras[i].rect;
            cameraAudioListeners[i] = cameras[i].GetComponent<AudioListener>();
        }
    }

    private int GetViewCount()
    {
        int cameraCount = cameras != null ? cameras.Length : 0;
        return cameraCount + (HasSplitView() ? 1 : 0);
    }

    private bool IsValidViewIndex(int index)
    {
        if (IsSplitViewIndex(index))
        {
            return true;
        }

        return cameras != null &&
               index >= 0 &&
               index < cameras.Length &&
               cameras[index] != null;
    }

    private bool IsSplitViewIndex(int index)
    {
        return HasSplitView() && index == cameras.Length;
    }

    private bool HasSplitView()
    {
        return includeSplitView && GetAssignedCameraCount() > 1;
    }

    private int GetAssignedCameraCount()
    {
        if (cameras == null)
        {
            return 0;
        }

        int assignedCameraCount = 0;
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null)
            {
                assignedCameraCount++;
            }
        }

        return assignedCameraCount;
    }

    private static Rect GetSplitViewport(int index, int count)
    {
        float width = 1f / Mathf.Max(1, count);
        return new Rect(width * index, 0f, width, 1f);
    }

    private static bool WasRightArrowPressed()
    {
        return ControllerKeyboardBinder.GetKeyDown(KeyCode.RightArrow);
    }

    private static bool WasLeftArrowPressed()
    {
        return ControllerKeyboardBinder.GetKeyDown(KeyCode.LeftArrow);
    }
}
