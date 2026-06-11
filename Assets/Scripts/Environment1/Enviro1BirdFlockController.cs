using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Spawns bird flocks on B key press using paired spawn and destination points.
/// Each bird is destroyed once it reaches its destination.
/// </summary>
public class Enviro1BirdFlockController : MonoBehaviour
{
    [System.Serializable]
    private sealed class RoutePair
    {
        public Transform SpawnPoint;
        public Transform EndPoint;
    }

    [Header("References")]
    [SerializeField] private GameObject birdPrefab;
    [SerializeField] private List<RoutePair> routePairs = new List<RoutePair>();
    [SerializeField] private Transform runtimeParent;

    [SerializeField, HideInInspector, FormerlySerializedAs("spawnPoint")]
    private Transform legacySpawnPoint;

    [SerializeField, HideInInspector, FormerlySerializedAs("endPoint")]
    private Transform legacyEndPoint;

    [Header("Flock")]
    [SerializeField, Min(1)] private int birdsPerFlock = 8;
    [SerializeField, Min(0f)] private float spawnRadius = 1.25f;
    [SerializeField, Min(0f)] private float targetRadius = 1.5f;

    [Header("Flight")]
    [SerializeField, Min(0.1f)] private float minSpeed = 4.5f;
    [SerializeField, Min(0.1f)] private float maxSpeed = 7.5f;
    [SerializeField, Min(0.01f)] private float arriveDistance = 0.35f;
    [SerializeField, Min(0.1f)] private float turnSpeed = 8f;

    private readonly List<BirdFlight> activeBirds = new List<BirdFlight>();

    private sealed class BirdFlight
    {
        public Transform Transform;
        public Vector3 Target;
        public float Speed;
    }

    private void Awake()
    {
        MigrateLegacyRoute();
    }

    private void OnValidate()
    {
        MigrateLegacyRoute();
    }

    private void Update()
    {
        if (WasSpawnPressed())
        {
            SpawnFlock();
        }

        UpdateBirdFlights();
    }

    private void SpawnFlock()
    {
        if (birdPrefab == null)
        {
            return;
        }

        EnsureRuntimeParent();

        for (int routeIndex = 0; routeIndex < routePairs.Count; routeIndex++)
        {
            RoutePair route = routePairs[routeIndex];
            if (route == null || route.SpawnPoint == null || route.EndPoint == null)
            {
                continue;
            }

            SpawnFlockOnRoute(route.SpawnPoint, route.EndPoint);
        }
    }

    private void SpawnFlockOnRoute(Transform spawnPoint, Transform endPoint)
    {
        for (int i = 0; i < birdsPerFlock; i++)
        {
            Vector3 spawnOffset = Random.insideUnitSphere * spawnRadius;
            spawnOffset.y *= 0.35f;
            Vector3 spawnPos = spawnPoint.position + spawnOffset;

            GameObject instance = Instantiate(birdPrefab, spawnPos, Quaternion.identity, runtimeParent);
            Transform bird = instance.transform;

            Vector3 targetOffset = Random.insideUnitSphere * targetRadius;
            targetOffset.y *= 0.5f;
            Vector3 target = endPoint.position + targetOffset;

            float speed = Random.Range(minSpeed, maxSpeed);
            activeBirds.Add(new BirdFlight
            {
                Transform = bird,
                Target = target,
                Speed = speed
            });
        }
    }

    private void UpdateBirdFlights()
    {
        for (int i = activeBirds.Count - 1; i >= 0; i--)
        {
            BirdFlight bird = activeBirds[i];
            if (bird.Transform == null)
            {
                activeBirds.RemoveAt(i);
                continue;
            }

            Vector3 toTarget = bird.Target - bird.Transform.position;
            float distance = toTarget.magnitude;
            if (distance <= arriveDistance)
            {
                Destroy(bird.Transform.gameObject);
                activeBirds.RemoveAt(i);
                continue;
            }

            Vector3 direction = toTarget / Mathf.Max(distance, 0.0001f);
            bird.Transform.position += direction * bird.Speed * Time.deltaTime;

            Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.up);
            bird.Transform.rotation = Quaternion.Slerp(
                bird.Transform.rotation,
                lookRotation,
                turnSpeed * Time.deltaTime);
        }
    }

    private void EnsureRuntimeParent()
    {
        if (runtimeParent != null)
        {
            return;
        }

        GameObject parent = new GameObject("Enviro1BirdFlockRuntime");
        runtimeParent = parent.transform;
    }

    private void MigrateLegacyRoute()
    {
        if (routePairs == null)
        {
            routePairs = new List<RoutePair>();
        }

        if (legacySpawnPoint == null && legacyEndPoint == null)
        {
            return;
        }

        if (routePairs.Count == 0)
        {
            routePairs.Add(new RoutePair
            {
                SpawnPoint = legacySpawnPoint,
                EndPoint = legacyEndPoint
            });
        }

        legacySpawnPoint = null;
        legacyEndPoint = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (routePairs == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        for (int i = 0; i < routePairs.Count; i++)
        {
            RoutePair route = routePairs[i];
            if (route == null || route.SpawnPoint == null || route.EndPoint == null)
            {
                continue;
            }

            Gizmos.DrawWireSphere(route.SpawnPoint.position, spawnRadius);
            Gizmos.DrawLine(route.SpawnPoint.position, route.EndPoint.position);
            Gizmos.DrawWireSphere(route.EndPoint.position, targetRadius);
        }
    }

    private static bool WasSpawnPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.bKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.B);
#else
        return false;
#endif
    }
}
