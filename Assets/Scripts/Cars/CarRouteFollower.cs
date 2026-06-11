using UnityEngine;

[DisallowMultipleComponent]
public class CarRouteFollower : MonoBehaviour
{
    private Transform[] route;
    private float moveSpeed;
    private float turnSpeedDegreesPerSecond;
    private float pointReachDistance;
    private Quaternion modelRotationOffset;
    private GameObject explosionPrefab;
    private float explosionForce;
    private float explosionRadius;
    private float explosionUpwardModifier;
    private float crashTorque;
    private float carDestroyDelay;
    private float explosionDestroyDelay;
    private Rigidbody carRigidbody;
    private int targetIndex;
    private bool isConfigured;
    private bool hasCrashed;

    private void Awake()
    {
        carRigidbody = GetComponent<Rigidbody>();
    }

    public void Configure(
        Transform[] newRoute,
        float newMoveSpeed,
        float newTurnSpeedDegreesPerSecond,
        float newPointReachDistance,
        Vector3 newModelRotationOffset,
        GameObject newExplosionPrefab,
        float newExplosionForce,
        float newExplosionRadius,
        float newExplosionUpwardModifier,
        float newCrashTorque,
        float newCarDestroyDelay,
        float newExplosionDestroyDelay)
    {
        route = newRoute;
        moveSpeed = Mathf.Max(0.01f, newMoveSpeed);
        turnSpeedDegreesPerSecond = Mathf.Max(0f, newTurnSpeedDegreesPerSecond);
        pointReachDistance = Mathf.Max(0.01f, newPointReachDistance);
        modelRotationOffset = Quaternion.Euler(newModelRotationOffset);
        explosionPrefab = newExplosionPrefab;
        explosionForce = Mathf.Max(0f, newExplosionForce);
        explosionRadius = Mathf.Max(0.01f, newExplosionRadius);
        explosionUpwardModifier = Mathf.Max(0f, newExplosionUpwardModifier);
        crashTorque = Mathf.Max(0f, newCrashTorque);
        carDestroyDelay = Mathf.Max(0f, newCarDestroyDelay);
        explosionDestroyDelay = Mathf.Max(0f, newExplosionDestroyDelay);
        carRigidbody = GetComponent<Rigidbody>();
        targetIndex = 0;
        hasCrashed = false;
        isConfigured = route != null && route.Length > 0;
    }

    private void FixedUpdate()
    {
        if (!isConfigured || hasCrashed)
        {
            return;
        }

        SkipMissingPoints();
        if (targetIndex >= route.Length)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPosition = route[targetIndex].position;
        Vector3 currentPosition = carRigidbody != null
            ? carRigidbody.position
            : transform.position;
        Vector3 direction = targetPosition - currentPosition;

        if (direction.sqrMagnitude <= pointReachDistance * pointReachDistance)
        {
            targetIndex++;
            if (targetIndex >= route.Length)
            {
                Destroy(gameObject);
            }

            return;
        }

        Vector3 nextPosition = Vector3.MoveTowards(
            currentPosition,
            targetPosition,
            moveSpeed * Time.fixedDeltaTime);

        if (carRigidbody != null)
        {
            carRigidbody.MovePosition(nextPosition);
        }
        else
        {
            transform.position = nextPosition;
        }

        if (turnSpeedDegreesPerSecond > 0f && direction.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction.normalized, Vector3.up) * modelRotationOffset;

            Quaternion currentRotation = carRigidbody != null
                ? carRigidbody.rotation
                : transform.rotation;
            Quaternion nextRotation = Quaternion.RotateTowards(
                currentRotation,
                targetRotation,
                turnSpeedDegreesPerSecond * Time.fixedDeltaTime);

            if (carRigidbody != null)
            {
                carRigidbody.MoveRotation(nextRotation);
            }
            else
            {
                transform.rotation = nextRotation;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasCrashed)
        {
            return;
        }

        CarRouteFollower otherCar = collision.collider.GetComponentInParent<CarRouteFollower>();
        if (otherCar == null || otherCar == this || otherCar.hasCrashed)
        {
            return;
        }

        Vector3 collisionPoint = collision.contactCount > 0
            ? collision.GetContact(0).point
            : (transform.position + otherCar.transform.position) * 0.5f;

        hasCrashed = true;
        otherCar.hasCrashed = true;

        SpawnExplosion(collisionPoint);
        ApplyCrashPhysics(collisionPoint);
        otherCar.ApplyCrashPhysics(collisionPoint);
    }

    private void SpawnExplosion(Vector3 collisionPoint)
    {
        if (explosionPrefab == null)
        {
            return;
        }

        GameObject explosion = Instantiate(explosionPrefab, collisionPoint, Quaternion.identity);
        ParticleSystem[] particleSystems = explosion.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = particleSystems[i];
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particleSystem.main;
            main.startDelay = 0f;

            particleSystem.Play(true);
        }

        if (explosionDestroyDelay > 0f)
        {
            Destroy(explosion, explosionDestroyDelay);
        }
    }

    private void ApplyCrashPhysics(Vector3 collisionPoint)
    {
        isConfigured = false;

        if (carRigidbody != null)
        {
            carRigidbody.isKinematic = false;
            carRigidbody.useGravity = true;
            carRigidbody.constraints = RigidbodyConstraints.None;
            carRigidbody.AddExplosionForce(
                explosionForce,
                collisionPoint,
                explosionRadius,
                explosionUpwardModifier,
                ForceMode.Impulse);
            carRigidbody.AddTorque(Random.insideUnitSphere * crashTorque, ForceMode.Impulse);
        }

        Destroy(gameObject, carDestroyDelay);
    }

    private void SkipMissingPoints()
    {
        while (targetIndex < route.Length && route[targetIndex] == null)
        {
            targetIndex++;
        }
    }
}
