using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CarRouteSpawner : MonoBehaviour
{
    [Header("Car")]
    [SerializeField] private GameObject carPrefab;
    [SerializeField] private Transform spawnedCarsParent;

    [Header("Route")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private List<Transform> routePoints = new List<Transform>();
    [SerializeField] private Transform endPoint;

    [Header("Movement")]
    [SerializeField, Min(0.01f)] private float moveSpeed = 8f;
    [SerializeField, Min(0f)] private float turnSpeedDegreesPerSecond = 180f;
    [SerializeField, Min(0.01f)] private float pointReachDistance = 0.25f;
    [SerializeField] private Vector3 modelRotationOffset;

    [Header("Collision")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField, Min(0f)] private float explosionForce = 12f;
    [SerializeField, Min(0.01f)] private float explosionRadius = 5f;
    [SerializeField, Min(0f)] private float explosionUpwardModifier = 1.5f;
    [SerializeField, Min(0f)] private float crashTorque = 8f;
    [SerializeField, Min(0f)] private float carDestroyDelay = 3f;
    [SerializeField, Min(0f)] private float explosionDestroyDelay = 5f;

    [Header("Input")]
#if ENABLE_INPUT_SYSTEM
    [SerializeField] private Key spawnKey = Key.C;
#elif ENABLE_LEGACY_INPUT_MANAGER
    [SerializeField] private KeyCode spawnKey = KeyCode.C;
#endif
    [SerializeField, Min(0f)] private float spawnCooldown = 1f;

    private float nextSpawnTime;

    private void Update()
    {
        if (WasSpawnKeyPressed())
        {
            SpawnCar();
        }
    }

    public void SpawnCar()
    {
        if (Time.time < nextSpawnTime)
        {
            return;
        }

        if (carPrefab == null || startPoint == null || endPoint == null)
        {
            Debug.LogWarning(
                $"[{nameof(CarRouteSpawner)}] Assign a car prefab, start point, and end point.",
                this);
            return;
        }

        GameObject car = Instantiate(
            carPrefab,
            startPoint.position,
            startPoint.rotation * Quaternion.Euler(modelRotationOffset),
            spawnedCarsParent);

        CarRouteFollower follower = car.GetComponent<CarRouteFollower>();
        if (follower == null)
        {
            follower = car.AddComponent<CarRouteFollower>();
        }

        follower.Configure(
            BuildRoute(),
            moveSpeed,
            turnSpeedDegreesPerSecond,
            pointReachDistance,
            modelRotationOffset,
            explosionPrefab,
            explosionForce,
            explosionRadius,
            explosionUpwardModifier,
            crashTorque,
            carDestroyDelay,
            explosionDestroyDelay);

        nextSpawnTime = Time.time + spawnCooldown;
    }

    private Transform[] BuildRoute()
    {
        List<Transform> route = new List<Transform>(routePoints.Count + 1);

        for (int i = 0; i < routePoints.Count; i++)
        {
            if (routePoints[i] != null)
            {
                route.Add(routePoints[i]);
            }
        }

        route.Add(endPoint);
        return route.ToArray();
    }

    private bool WasSpawnKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return spawnKey != Key.None && ControllerKeyboardBinder.GetKeyDown(spawnKey);
#elif ENABLE_LEGACY_INPUT_MANAGER
        return spawnKey != KeyCode.None && ControllerKeyboardBinder.GetKeyDown(spawnKey);
#else
        return false;
#endif
    }

    private void OnDrawGizmosSelected()
    {
        if (startPoint == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(startPoint.position, 0.5f);

        Vector3 previousPosition = startPoint.position;
        for (int i = 0; i < routePoints.Count; i++)
        {
            Transform routePoint = routePoints[i];
            if (routePoint == null)
            {
                continue;
            }

            Gizmos.DrawLine(previousPosition, routePoint.position);
            Gizmos.DrawWireSphere(routePoint.position, 0.35f);
            previousPosition = routePoint.position;
        }

        if (endPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(previousPosition, endPoint.position);
            Gizmos.DrawWireSphere(endPoint.position, 0.5f);
        }
    }
}
