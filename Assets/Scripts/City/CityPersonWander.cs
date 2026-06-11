using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class CityPersonWander : MonoBehaviour
{
    private BoxCollider wanderArea;
    private NavMeshAgent agent;
    private bool useNavMesh;
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
    private Vector3 directDestination;
    private bool isConfigured;
    private bool isWaiting;
    private bool wasHitByCar;

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
        movementSpeed = Mathf.Max(0.1f, newMovementSpeed);
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

        if (isConfigured)
        {
            SetNewDestination();
        }
    }

    private void Update()
    {
        if (!isConfigured || isWaiting || wasHitByCar)
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
}
