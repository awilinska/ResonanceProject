using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Spawns a flock of animated birds on B key press and moves them from spawn to end point.
/// Each bird is destroyed once it reaches its destination.
/// </summary>
public class Enviro1BirdFlockController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject birdPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform endPoint;
    [SerializeField] private Transform runtimeParent;

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
        if (birdPrefab == null || spawnPoint == null || endPoint == null)
        {
            return;
        }

        EnsureRuntimeParent();

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

