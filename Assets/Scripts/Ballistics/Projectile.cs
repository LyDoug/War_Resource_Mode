using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class ProjectileBallistic : MonoBehaviour
{
    [Header("Damage")]
    public float damage = 25f;

    [Header("Impact VFX")]
    public GameObject impactEffectPrefab;

    public float impactEffectDuration = 2f;

    Rigidbody rb;

    bool hasImpacted;
    Vector3 previousPosition;
    readonly HashSet<Collider> ignoredColliders = new HashSet<Collider>();

    // =========================================================
    // AWAKE
    // =========================================================
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        previousPosition = transform.position;
    }

    // =========================================================
    // INIT
    // =========================================================
    public void Init(
        float speed,
        float newDamage
    )
    {
        damage = newDamage;

        rb.linearVelocity =
            transform.forward * speed;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        previousPosition = transform.position;
    }

    public void IgnoreVehicleCollisions(Transform vehicleRoot)
    {
        if (vehicleRoot == null)
            return;

        foreach (Collider ownCollider in vehicleRoot.GetComponentsInChildren<Collider>())
        {
            ignoredColliders.Add(ownCollider);
            foreach (Collider shellCollider in GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(ownCollider, shellCollider);
        }
    }

    void Update()
    {
        if (hasImpacted)
            return;

        Vector3 currentPosition = transform.position;
        Vector3 travel = currentPosition - previousPosition;
        float distance = travel.magnitude;
        if (distance > 0.001f)
        {
            foreach (RaycastHit hit in Physics.RaycastAll(previousPosition, travel / distance, distance,
                         Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (ignoredColliders.Contains(hit.collider))
                    continue;

                HandleImpact(hit.collider, hit.point, hit.normal);
                break;
            }
        }
        previousPosition = currentPosition;
    }

    // =========================================================
    // COLLISION
    // =========================================================
    void OnCollisionEnter(Collision collision)
    {
        if (hasImpacted)
            return;

        HandleImpact(collision.collider, collision.contacts[0].point, collision.contacts[0].normal);
    }

    void HandleImpact(Collider collider, Vector3 point, Vector3 normal)
    {
        if (hasImpacted)
            return;

        hasImpacted = true;

        // =====================================================
        // IMPACT VFX
        // =====================================================
        if (impactEffectPrefab)
        {
            GameObject impact =
                Instantiate(impactEffectPrefab);

            impact.transform.position =
                point;

            impact.transform.rotation =
                Quaternion.LookRotation(
                    normal
                );

            impact.transform.localScale =
                Vector3.one;

            Destroy(
                impact,
                impactEffectDuration
            );
        }

        // =====================================================
        // DAMAGE STRUCTURE
        // =====================================================
        BaseDestructibleWithHealthVisual baseScript =
            BaseDestructibleWithHealthVisual.FindInHierarchy(collider);

        if (baseScript != null)
        {
            baseScript.TakeDamage(damage);

            Destroy(gameObject);

            return;
        }

        // =====================================================
        // DAMAGE VEHICLE/PLAYER
        // =====================================================
        Health health = Health.FindInHierarchy(collider);

        if (health != null)
        {
            health.TakeDamage(damage);
        }

        // =====================================================
        // DESTROY PROJECTILE
        // =====================================================
        Destroy(gameObject);
    }

    // Resource mines use trigger colliders for collection/capture. A shell
    // still needs to damage the structure when it enters that volume.
    void OnTriggerEnter(Collider other)
    {
        if (hasImpacted)
            return;

        ResourceNode mine = other.GetComponent<ResourceNode>();
        if (mine == null)
            return;

        BaseDestructibleWithHealthVisual structure =
            BaseDestructibleWithHealthVisual.FindInHierarchy(other);
        if (structure == null)
            return;

        hasImpacted = true;
        structure.TakeDamage(damage);
        Destroy(gameObject);
    }
}
