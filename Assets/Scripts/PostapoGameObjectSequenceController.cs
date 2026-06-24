using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class PostapoGameObjectSequenceController : MonoBehaviour
{
    [Header("Postapo Objects")]
    [SerializeField] private GameObject[] sequenceObjects;
    [SerializeField] private bool turnAllObjectsOffOnStart = true;
    [SerializeField, Min(0f)] private float delayBetweenObjects = 0.1f;

    [Header("Input")]
#if ENABLE_INPUT_SYSTEM
    [SerializeField] private Key sequenceKey = Key.None;
#elif ENABLE_LEGACY_INPUT_MANAGER
    [SerializeField] private KeyCode sequenceKey = KeyCode.None;
#endif

    private bool objectsAreOn;
    private bool sequenceIsRunning;

    private void Awake()
    {
        if (turnAllObjectsOffOnStart)
        {
            SetAllObjects(false);
            objectsAreOn = false;
            return;
        }

        objectsAreOn = AreAllObjectsActive();
    }

    private void Update()
    {
        if (WasSequenceKeyPressed())
        {
            PlayObjectSequence();
        }
    }

    public void PlayObjectSequence()
    {
        if (sequenceIsRunning || sequenceObjects == null || sequenceObjects.Length == 0)
        {
            return;
        }

        StartCoroutine(SetObjectsSequentially(!objectsAreOn));
    }

    public void SetAllObjects(bool activeState)
    {
        if (sequenceObjects == null)
        {
            return;
        }

        for (int i = 0; i < sequenceObjects.Length; i++)
        {
            if (sequenceObjects[i] != null)
            {
                sequenceObjects[i].SetActive(activeState);
            }
        }
    }

    private IEnumerator SetObjectsSequentially(bool activeState)
    {
        sequenceIsRunning = true;

        for (int i = 0; i < sequenceObjects.Length; i++)
        {
            if (sequenceObjects[i] == null)
            {
                continue;
            }

            sequenceObjects[i].SetActive(activeState);

            if (delayBetweenObjects > 0f)
            {
                yield return new WaitForSeconds(delayBetweenObjects);
            }
        }

        objectsAreOn = activeState;
        sequenceIsRunning = false;
    }

    private bool AreAllObjectsActive()
    {
        if (sequenceObjects == null || sequenceObjects.Length == 0)
        {
            return false;
        }

        bool foundAssignedObject = false;
        for (int i = 0; i < sequenceObjects.Length; i++)
        {
            if (sequenceObjects[i] == null)
            {
                continue;
            }

            foundAssignedObject = true;
            if (!sequenceObjects[i].activeSelf)
            {
                return false;
            }
        }

        return foundAssignedObject;
    }

    private bool WasSequenceKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return sequenceKey != Key.None && ControllerKeyboardBinder.GetKeyDown(sequenceKey);
#elif ENABLE_LEGACY_INPUT_MANAGER
        return sequenceKey != KeyCode.None && ControllerKeyboardBinder.GetKeyDown(sequenceKey);
#else
        return false;
#endif
    }
}
