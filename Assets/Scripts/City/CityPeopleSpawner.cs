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

    private void Update()
    {
        if (WasSpawnPressed())
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

    private static bool WasSpawnPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.pKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.P);
#else
        return false;
#endif
    }
}
