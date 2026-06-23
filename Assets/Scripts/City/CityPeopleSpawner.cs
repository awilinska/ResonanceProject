using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class CityPeopleSpawner : MonoBehaviour
{
    [Header("People")]
    [SerializeField] private GameObject[] personPrefabs;
    [SerializeField] private Transform spawnedPeopleParent;

    [Header("Spawn And Wander Areas")]
    [SerializeField] private BoxCollider[] areas;
    [SerializeField] private bool useNavMesh;
    [SerializeField, Min(1)] private int navMeshSampleAttempts = 20;
    [SerializeField, Min(0.1f)] private float navMeshSampleDistance = 3f;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float minimumSpeed = 1.2f;
    [SerializeField, Min(0.1f)] private float maximumSpeed = 2.2f;
    [SerializeField, Min(0f)] private float minimumWaitTime = 0.5f;
    [SerializeField, Min(0f)] private float maximumWaitTime = 2f;
    [SerializeField, Min(0.01f)] private float destinationReachDistance = 0.25f;

    [Header("Car Hit")]
    [SerializeField, Min(0f)] private float carHitForce = 8f;
    [SerializeField, Min(0f)] private float carHitUpwardForce = 7f;
    [SerializeField, Min(0f)] private float carHitTorque = 8f;
    [SerializeField, Min(0f)] private float hitPersonDestroyDelay = 3f;
    [SerializeField, Min(0.1f)] private float personColliderHeight = 1.8f;
    [SerializeField, Min(0.05f)] private float personColliderRadius = 0.3f;

    [Header("Fire Response")]
    [SerializeField] private Transform environmentRoot;
    [SerializeField] private GameObject[] fireObjects;
    [SerializeField, Min(1f)] private float fireMovementMultiplier = 3f;
    [SerializeField, Min(0f)] private float minimumFireDestroyDelay = 2f;
    [SerializeField, Min(0f)] private float maximumFireDestroyDelay = 6f;

    [Header("Rain Response")]
    [SerializeField] private GameObject[] rainObjects;
    [SerializeField] private GameObject[] stormObjects;
    [SerializeField] private Color wetColor = new Color(0.65f, 0.8f, 1f, 1f);
    [SerializeField, Min(0f)] private float minimumWetRestoreDelay = 1f;
    [SerializeField, Min(0f)] private float maximumWetRestoreDelay = 4f;

    [Header("Input")]
#if ENABLE_INPUT_SYSTEM
    [SerializeField] private Key spawnKey = Key.P;
#elif ENABLE_LEGACY_INPUT_MANAGER
    [SerializeField] private KeyCode spawnKey = KeyCode.P;
#endif

    private void Update()
    {
        if (WasSpawnKeyPressed())
        {
            SpawnPerson();
        }
    }

    public void SpawnPerson()
    {
        GameObject prefab = GetRandomPrefab();
        BoxCollider area = GetRandomArea();

        if (prefab == null || area == null)
        {
            Debug.LogWarning(
                $"[{nameof(CityPeopleSpawner)}] Assign at least one person prefab and area.",
                this);
            return;
        }

        if (!TryGetSpawnPoint(area, out Vector3 spawnPosition))
        {
            Debug.LogWarning(
                $"[{nameof(CityPeopleSpawner)}] Could not find a spawn point inside {area.name}.",
                area);
            return;
        }

        GameObject person = Instantiate(
            prefab,
            spawnPosition,
            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
            spawnedPeopleParent);

        NavMeshAgent agent = person.GetComponent<NavMeshAgent>();
        if (useNavMesh && agent == null)
        {
            agent = person.AddComponent<NavMeshAgent>();
        }
        else if (!useNavMesh && agent != null)
        {
            agent.enabled = false;
        }

        Rigidbody personRigidbody = person.GetComponent<Rigidbody>();
        if (personRigidbody == null)
        {
            personRigidbody = person.AddComponent<Rigidbody>();
        }

        personRigidbody.isKinematic = true;
        personRigidbody.useGravity = false;
        personRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        personRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        if (person.GetComponent<Collider>() == null)
        {
            CapsuleCollider personCollider = person.AddComponent<CapsuleCollider>();
            personCollider.height = Mathf.Max(
                personColliderHeight,
                personColliderRadius * 2f);
            personCollider.radius = personColliderRadius;
            personCollider.center = Vector3.up * (personCollider.height * 0.5f);
        }

        CityPersonWander wander = person.GetComponent<CityPersonWander>();
        if (wander == null)
        {
            wander = person.AddComponent<CityPersonWander>();
        }

        float minimum = Mathf.Min(minimumSpeed, maximumSpeed);
        float maximum = Mathf.Max(minimumSpeed, maximumSpeed);
        float movementSpeed = Random.Range(minimum, maximum);
        if (useNavMesh)
        {
            agent.speed = movementSpeed;
        }

        wander.Configure(
            area,
            useNavMesh,
            movementSpeed,
            navMeshSampleAttempts,
            navMeshSampleDistance,
            minimumWaitTime,
            maximumWaitTime,
            destinationReachDistance,
            carHitForce,
            carHitUpwardForce,
            carHitTorque,
            hitPersonDestroyDelay);

        Transform resolvedEnvironmentRoot =
            environmentRoot != null ? environmentRoot : area.transform.root;
        wander.ConfigureEnvironmentResponse(
            resolvedEnvironmentRoot,
            ResolveEnvironmentObjects(resolvedEnvironmentRoot, fireObjects, "Fire"),
            fireMovementMultiplier,
            minimumFireDestroyDelay,
            maximumFireDestroyDelay,
            ResolveEnvironmentObjects(resolvedEnvironmentRoot, rainObjects, "Rain"),
            ResolveEnvironmentObjects(resolvedEnvironmentRoot, stormObjects, "Storm"),
            wetColor,
            minimumWetRestoreDelay,
            maximumWetRestoreDelay);
    }

    private GameObject GetRandomPrefab()
    {
        if (personPrefabs == null || personPrefabs.Length == 0)
        {
            return null;
        }

        int startIndex = Random.Range(0, personPrefabs.Length);
        for (int i = 0; i < personPrefabs.Length; i++)
        {
            GameObject prefab = personPrefabs[(startIndex + i) % personPrefabs.Length];
            if (prefab != null)
            {
                return prefab;
            }
        }

        return null;
    }

    private BoxCollider GetRandomArea()
    {
        if (areas == null || areas.Length == 0)
        {
            return null;
        }

        int startIndex = Random.Range(0, areas.Length);
        for (int i = 0; i < areas.Length; i++)
        {
            BoxCollider area = areas[(startIndex + i) % areas.Length];
            if (area != null && area.enabled && area.gameObject.activeInHierarchy)
            {
                return area;
            }
        }

        return null;
    }

    private bool TryGetSpawnPoint(BoxCollider area, out Vector3 point)
    {
        if (useNavMesh)
        {
            return TryGetNavMeshPoint(area, out point);
        }

        point = GetRandomAreaPoint(area);
        return true;
    }

    private bool TryGetNavMeshPoint(BoxCollider area, out Vector3 point)
    {
        for (int i = 0; i < navMeshSampleAttempts; i++)
        {
            Vector3 worldPoint = GetRandomAreaPoint(area);

            if (NavMesh.SamplePosition(
                    worldPoint,
                    out NavMeshHit hit,
                    navMeshSampleDistance,
                    NavMesh.AllAreas) &&
                IsInsideArea(area, hit.position))
            {
                point = hit.position;
                return true;
            }
        }

        point = default;
        return false;
    }

    private static Vector3 GetRandomAreaPoint(BoxCollider area)
    {
        Vector3 localPoint = area.center + new Vector3(
            Random.Range(-area.size.x * 0.5f, area.size.x * 0.5f),
            -area.size.y * 0.5f,
            Random.Range(-area.size.z * 0.5f, area.size.z * 0.5f));

        return area.transform.TransformPoint(localPoint);
    }

    private static bool IsInsideArea(BoxCollider area, Vector3 worldPoint)
    {
        Vector3 localPoint = area.transform.InverseTransformPoint(worldPoint) - area.center;
        Vector3 halfSize = area.size * 0.5f;

        return Mathf.Abs(localPoint.x) <= halfSize.x &&
               Mathf.Abs(localPoint.y) <= halfSize.y &&
               Mathf.Abs(localPoint.z) <= halfSize.z;
    }

    private GameObject[] ResolveEnvironmentObjects(
        Transform resolvedEnvironmentRoot,
        GameObject[] configuredObjects,
        string objectName)
    {
        if (configuredObjects != null && configuredObjects.Length > 0)
        {
            return configuredObjects;
        }

        if (resolvedEnvironmentRoot == null)
        {
            return new GameObject[0];
        }

        List<GameObject> resolvedObjects = new List<GameObject>();
        Transform[] children =
            resolvedEnvironmentRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child != null &&
                string.Equals(
                    child.name,
                    objectName,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                resolvedObjects.Add(child.gameObject);
            }
        }

        return resolvedObjects.ToArray();
    }

    private bool WasSpawnKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null &&
               spawnKey != Key.None &&
               keyboard[spawnKey].wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return spawnKey != KeyCode.None && Input.GetKeyDown(spawnKey);
#else
        return false;
#endif
    }
}
