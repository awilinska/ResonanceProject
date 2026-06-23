using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class CityPersonWander : MonoBehaviour
{
    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    private BoxCollider wanderArea;
    private NavMeshAgent agent;
    private bool useNavMesh;
    private float baseMovementSpeed;
    private float movementSpeed;
    private int sampleAttempts;
    private float sampleDistance;
    private float minimumWaitTime;
    private float maximumWaitTime;
    private float destinationReachDistance;
    private float carHitForce;
    private float carHitUpwardForce;
    private float carHitTorque;
    private float hitPersonDestroyDelay;
    private Transform environmentRoot;
    private GameObject[] fireObjects;
    private GameObject[] rainObjects;
    private GameObject[] stormObjects;
    private float fireMovementMultiplier;
    private float minimumFireDestroyDelay;
    private float maximumFireDestroyDelay;
    private Color wetColor;
    private float minimumWetRestoreDelay;
    private float maximumWetRestoreDelay;
    private Vector3 directDestination;
    private bool isConfigured;
    private bool isWaiting;
    private bool wasHitByCar;
    private bool firePanicStarted;
    private bool wetTintActive;
    private Coroutine fireDestroyCoroutine;
    private Coroutine wetRestoreCoroutine;
    private RendererTintState[] originalRendererTintStates;

    private struct RendererTintState
    {
        public Renderer Renderer;
        public MaterialPropertyBlock PropertyBlock;
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public void Configure(
        BoxCollider newWanderArea,
        bool newUseNavMesh,
        float newMovementSpeed,
        int newSampleAttempts,
        float newSampleDistance,
        float newMinimumWaitTime,
        float newMaximumWaitTime,
        float newDestinationReachDistance,
        float newCarHitForce,
        float newCarHitUpwardForce,
        float newCarHitTorque,
        float newHitPersonDestroyDelay)
    {
        wanderArea = newWanderArea;
        useNavMesh = newUseNavMesh;
        baseMovementSpeed = Mathf.Max(0.1f, newMovementSpeed);
        movementSpeed = baseMovementSpeed;
        sampleAttempts = Mathf.Max(1, newSampleAttempts);
        sampleDistance = Mathf.Max(0.1f, newSampleDistance);
        minimumWaitTime = Mathf.Max(0f, Mathf.Min(newMinimumWaitTime, newMaximumWaitTime));
        maximumWaitTime = Mathf.Max(minimumWaitTime, newMaximumWaitTime);
        destinationReachDistance = Mathf.Max(0.01f, newDestinationReachDistance);
        carHitForce = Mathf.Max(0f, newCarHitForce);
        carHitUpwardForce = Mathf.Max(0f, newCarHitUpwardForce);
        carHitTorque = Mathf.Max(0f, newCarHitTorque);
        hitPersonDestroyDelay = Mathf.Max(0f, newHitPersonDestroyDelay);
        agent = GetComponent<NavMeshAgent>();
        if (useNavMesh && agent != null)
        {
            agent.stoppingDistance = destinationReachDistance;
        }
        isConfigured = wanderArea != null;
        wasHitByCar = false;
        firePanicStarted = false;
        wetTintActive = false;
        fireDestroyCoroutine = null;
        wetRestoreCoroutine = null;

        if (isConfigured)
        {
            SetNewDestination();
        }
    }

    public void ConfigureEnvironmentResponse(
        Transform newEnvironmentRoot,
        GameObject[] newFireObjects,
        float newFireMovementMultiplier,
        float newMinimumFireDestroyDelay,
        float newMaximumFireDestroyDelay,
        GameObject[] newRainObjects,
        GameObject[] newStormObjects,
        Color newWetColor,
        float newMinimumWetRestoreDelay,
        float newMaximumWetRestoreDelay)
    {
        environmentRoot =
            newEnvironmentRoot != null
                ? newEnvironmentRoot
                : wanderArea != null
                    ? wanderArea.transform.root
                    : null;
        fireObjects = newFireObjects;
        rainObjects = newRainObjects;
        stormObjects = newStormObjects;
        fireMovementMultiplier = Mathf.Max(1f, newFireMovementMultiplier);
        minimumFireDestroyDelay = Mathf.Max(
            0f,
            Mathf.Min(newMinimumFireDestroyDelay, newMaximumFireDestroyDelay));
        maximumFireDestroyDelay = Mathf.Max(
            minimumFireDestroyDelay,
            newMaximumFireDestroyDelay);
        wetColor = newWetColor;
        minimumWetRestoreDelay = Mathf.Max(
            0f,
            Mathf.Min(newMinimumWetRestoreDelay, newMaximumWetRestoreDelay));
        maximumWetRestoreDelay = Mathf.Max(
            minimumWetRestoreDelay,
            newMaximumWetRestoreDelay);
    }

    private void Update()
    {
        if (!isConfigured || wasHitByCar)
        {
            return;
        }

        bool isRaining = IsRainActiveInSameEnvironment();
        if (isRaining)
        {
            ApplyWetState();
        }
        else if (wetTintActive && wetRestoreCoroutine == null)
        {
            wetRestoreCoroutine = StartCoroutine(RestoreNormalColorAfterRain());
        }

        if (!isRaining && !firePanicStarted && IsFireActiveInSameEnvironment())
        {
            StartFirePanic();
        }

        if (isWaiting)
        {
            return;
        }

        if (!useNavMesh)
        {
            UpdateDirectMovement();
            return;
        }

        if (agent == null || !agent.isOnNavMesh || agent.pathPending)
        {
            return;
        }

        if (!agent.hasPath || agent.remainingDistance <= destinationReachDistance)
        {
            StartCoroutine(WaitAndChooseDestination());
        }
    }

    private IEnumerator WaitAndChooseDestination()
    {
        isWaiting = true;
        if (useNavMesh && agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }

        float waitTime = Random.Range(minimumWaitTime, maximumWaitTime);
        if (waitTime > 0f)
        {
            yield return new WaitForSeconds(waitTime);
        }

        SetNewDestination();
        isWaiting = false;
    }

    private void SetNewDestination()
    {
        if (wanderArea == null)
        {
            return;
        }

        if (!useNavMesh)
        {
            directDestination = GetRandomAreaPoint();
            return;
        }

        if (agent == null || !agent.isOnNavMesh)
        {
            return;
        }

        for (int i = 0; i < sampleAttempts; i++)
        {
            Vector3 worldPoint = GetRandomAreaPoint();

            if (!NavMesh.SamplePosition(
                    worldPoint,
                    out NavMeshHit hit,
                    sampleDistance,
                    NavMesh.AllAreas) ||
                !IsInsideArea(hit.position))
            {
                continue;
            }

            NavMeshPath path = new NavMeshPath();
            if (agent.CalculatePath(hit.position, path) &&
                path.status == NavMeshPathStatus.PathComplete)
            {
                agent.SetPath(path);
                return;
            }
        }
    }

    private void UpdateDirectMovement()
    {
        Vector3 toDestination = directDestination - transform.position;
        toDestination.y = 0f;

        if (toDestination.sqrMagnitude <=
            destinationReachDistance * destinationReachDistance)
        {
            StartCoroutine(WaitAndChooseDestination());
            return;
        }

        Vector3 direction = toDestination.normalized;
        transform.position = Vector3.MoveTowards(
            transform.position,
            directDestination,
            movementSpeed * Time.deltaTime);

        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                360f * Time.deltaTime);
        }
    }

    private Vector3 GetRandomAreaPoint()
    {
        Vector3 localPoint = wanderArea.center + new Vector3(
            Random.Range(-wanderArea.size.x * 0.5f, wanderArea.size.x * 0.5f),
            -wanderArea.size.y * 0.5f,
            Random.Range(-wanderArea.size.z * 0.5f, wanderArea.size.z * 0.5f));

        return wanderArea.transform.TransformPoint(localPoint);
    }

    private bool IsInsideArea(Vector3 worldPoint)
    {
        Vector3 localPoint =
            wanderArea.transform.InverseTransformPoint(worldPoint) - wanderArea.center;
        Vector3 halfSize = wanderArea.size * 0.5f;

        return Mathf.Abs(localPoint.x) <= halfSize.x &&
               Mathf.Abs(localPoint.y) <= halfSize.y &&
               Mathf.Abs(localPoint.z) <= halfSize.z;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (wasHitByCar)
        {
            return;
        }

        CarRouteFollower car = collision.collider.GetComponentInParent<CarRouteFollower>();
        if (car == null)
        {
            return;
        }

        wasHitByCar = true;
        isConfigured = false;
        StopAllCoroutines();

        if (agent != null && agent.enabled)
        {
            agent.enabled = false;
        }

        Animator[] animators = GetComponentsInChildren<Animator>();
        for (int i = 0; i < animators.Length; i++)
        {
            animators[i].enabled = false;
        }

        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody>();
        }

        body.isKinematic = false;
        body.useGravity = true;
        body.constraints = RigidbodyConstraints.None;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        Vector3 awayFromCar = transform.position - car.transform.position;
        awayFromCar.y = 0f;
        if (awayFromCar.sqrMagnitude < 0.0001f)
        {
            awayFromCar = car.transform.forward;
        }

        Vector3 launchImpulse =
            awayFromCar.normalized * carHitForce +
            Vector3.up * carHitUpwardForce;

        body.AddForce(launchImpulse, ForceMode.Impulse);
        body.AddTorque(Random.insideUnitSphere * carHitTorque, ForceMode.Impulse);

        Destroy(gameObject, hitPersonDestroyDelay);
    }

    private bool IsFireActiveInSameEnvironment()
    {
        return IsAnyEnvironmentObjectActive(fireObjects);
    }

    private bool IsRainActiveInSameEnvironment()
    {
        return IsAnyEnvironmentObjectActive(rainObjects) ||
               IsAnyEnvironmentObjectActive(stormObjects);
    }

    private void StartFirePanic()
    {
        if (wetRestoreCoroutine != null)
        {
            StopCoroutine(wetRestoreCoroutine);
            wetRestoreCoroutine = null;
        }

        wetTintActive = false;
        firePanicStarted = true;
        movementSpeed = baseMovementSpeed * fireMovementMultiplier;

        if (agent != null && agent.enabled)
        {
            agent.speed = movementSpeed;
        }

        if (isWaiting)
        {
            StopAllCoroutines();
            isWaiting = false;
        }

        SetNewDestination();
        ApplyTint(Color.red);
        fireDestroyCoroutine = StartCoroutine(DestroyAfterFireDelay());
    }

    private void StopFirePanic()
    {
        if (!firePanicStarted)
        {
            return;
        }

        firePanicStarted = false;
        movementSpeed = baseMovementSpeed;

        if (agent != null && agent.enabled)
        {
            agent.speed = movementSpeed;
        }

        if (fireDestroyCoroutine != null)
        {
            StopCoroutine(fireDestroyCoroutine);
            fireDestroyCoroutine = null;
        }
    }

    private void ApplyWetState()
    {
        StopFirePanic();

        if (wetRestoreCoroutine != null)
        {
            StopCoroutine(wetRestoreCoroutine);
            wetRestoreCoroutine = null;
        }

        if (wetTintActive)
        {
            return;
        }

        wetTintActive = true;
        ApplyTint(wetColor);
    }

    private IEnumerator DestroyAfterFireDelay()
    {
        float destroyDelay = Random.Range(
            minimumFireDestroyDelay,
            maximumFireDestroyDelay);
        if (destroyDelay > 0f)
        {
            yield return new WaitForSeconds(destroyDelay);
        }

        Destroy(gameObject);
    }

    private IEnumerator RestoreNormalColorAfterRain()
    {
        float restoreDelay = Random.Range(
            minimumWetRestoreDelay,
            maximumWetRestoreDelay);
        if (restoreDelay > 0f)
        {
            yield return new WaitForSeconds(restoreDelay);
        }

        wetTintActive = false;
        wetRestoreCoroutine = null;

        if (!firePanicStarted)
        {
            RestoreOriginalTint();
        }
    }

    private bool IsAnyEnvironmentObjectActive(GameObject[] environmentObjects)
    {
        if (environmentObjects == null || environmentObjects.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < environmentObjects.Length; i++)
        {
            GameObject environmentObject = environmentObjects[i];
            if (environmentObject == null || !environmentObject.activeInHierarchy)
            {
                continue;
            }

            if (environmentRoot != null &&
                !IsTransformInsideRoot(environmentObject.transform, environmentRoot))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private void ApplyTint(Color tintColor)
    {
        CacheOriginalRendererTintStates();

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer meshRenderer = renderers[i];
            if (meshRenderer == null)
            {
                continue;
            }

            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorProperty, tintColor);
            propertyBlock.SetColor(ColorProperty, tintColor);
            meshRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void RestoreOriginalTint()
    {
        if (originalRendererTintStates == null)
        {
            return;
        }

        for (int i = 0; i < originalRendererTintStates.Length; i++)
        {
            RendererTintState rendererTintState = originalRendererTintStates[i];
            if (rendererTintState.Renderer == null)
            {
                continue;
            }

            rendererTintState.Renderer.SetPropertyBlock(
                rendererTintState.PropertyBlock);
        }
    }

    private void CacheOriginalRendererTintStates()
    {
        if (originalRendererTintStates != null)
        {
            return;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        originalRendererTintStates = new RendererTintState[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            if (renderers[i] != null)
            {
                renderers[i].GetPropertyBlock(propertyBlock);
            }

            originalRendererTintStates[i] = new RendererTintState
            {
                Renderer = renderers[i],
                PropertyBlock = propertyBlock
            };
        }
    }

    private static bool IsTransformInsideRoot(Transform target, Transform root)
    {
        return target == root || target.IsChildOf(root);
    }
}
