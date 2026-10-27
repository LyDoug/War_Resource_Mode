using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ArtilleryShell : MonoBehaviour
{
    [Header("Damage")]
    public float damage = 100f;

    [Header("Explosion")]
    public float explosionRadius = 8f;

    public float explosionForce = 3000f;

    [Header("Ballistics")]
    public float speed = 120f;

    [Header("Lifetime")]
    public float lifeTime = 20f;

    [Header("VFX")]
    public GameObject impactVFX;

    Rigidbody rb;
    Transform shooterRoot;
    Team shooterTeam = Team.Neutral;
    ArtilleryAmmoType ammoType = ArtilleryAmmoType.HE;
    float revealRadius;
    float revealDuration;
    bool launched;
    Vector3 lastVelocity;

    bool exploded = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Shells move fast enough to cross thin structure colliders between
        // physics frames. Continuous collision keeps the impact reliable.
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public void ConfigureShooter(Transform shooter)
    {
        shooterRoot = shooter;

        TeamMember member = shooterRoot != null
            ? shooterRoot.GetComponentInChildren<TeamMember>(true)
            : null;
        shooterTeam = member != null ? member.team : Team.Neutral;

        if (shooterRoot == null)
            return;

        Collider[] shellColliders = GetComponentsInChildren<Collider>(true);
        Collider[] shooterColliders = shooterRoot.GetComponentsInChildren<Collider>(true);

        foreach (Collider shellCollider in shellColliders)
        {
            foreach (Collider shooterCollider in shooterColliders)
            {
                if (shellCollider != null && shooterCollider != null)
                    Physics.IgnoreCollision(shellCollider, shooterCollider, true);
            }
        }
    }

    public void ConfigureAmmo(ArtilleryAmmoType type, float configuredDamage,
        float configuredRadius, float configuredRevealRadius, float configuredRevealDuration,
        Team team)
    {
        ammoType = type;
        damage = configuredDamage;
        explosionRadius = configuredRadius;
        revealRadius = configuredRevealRadius;
        revealDuration = configuredRevealDuration;
        shooterTeam = team;
    }

    bool BelongsToShooter(Collider collider)
    {
        return shooterRoot != null && collider != null &&
            collider.transform.IsChildOf(shooterRoot);
    }

    // =====================================================
    // START
    // =====================================================
    void Start()
    {
        if (!launched)
        {
            rb.linearVelocity = transform.forward * speed;
            lastVelocity = rb.linearVelocity;
        }

        Destroy(gameObject, lifeTime);
    }

    public void Launch(Vector3 initialVelocity)
    {
        launched = true;
        rb.linearVelocity = initialVelocity;
        lastVelocity = initialVelocity;
    }

    // =====================================================
    // COLLISION
    // =====================================================
    void OnCollisionEnter(Collision collision)
    {
        if (!IsGround(collision.collider))
        {
            IgnoreCollider(collision.collider);
            rb.linearVelocity = lastVelocity;
            return;
        }

        Vector3 hitPoint = collision.contactCount > 0
            ? collision.contacts[0].point
            : transform.position;

        Explode(hitPoint);
    }

    void Explode(Vector3 hitPoint)
    {
        if (exploded)
            return;

        exploded = true;

        if (ammoType == ArtilleryAmmoType.Smoke)
        {
            SpotterSmokeMarker.Create(shooterTeam, hitPoint, revealDuration, revealRadius);
            Destroy(gameObject);
            return;
        }

        if (impactVFX != null)
            Instantiate(impactVFX, hitPoint, Quaternion.identity);

        // Capture and delivery zones are trigger colliders. Explicitly
        // include them so a shell can damage the structure that owns them.
        Collider[] hits = Physics.OverlapSphere(
            hitPoint,
            explosionRadius,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide);

        HashSet<BaseDestructibleWithHealthVisual> damagedStructures = new HashSet<BaseDestructibleWithHealthVisual>();
        HashSet<Health> damagedVehicles = new HashSet<Health>();
        foreach (Collider col in hits)
        {
            // A close explosion must never damage the vehicle that fired it.
            if (BelongsToShooter(col))
                continue;

            // =============================
            // DAMAGE FALLOFF
            // =============================
            float distance = Vector3.Distance(hitPoint, col.ClosestPoint(hitPoint));

            float multiplier =
                1f - Mathf.Clamp01(
                    distance / explosionRadius
                );

            float finalDamage =
                damage * multiplier;

            // =============================
            // BASE DAMAGE
            // =============================
            BaseDestructibleWithHealthVisual baseHealth =
                BaseDestructibleWithHealthVisual.FindInHierarchy(col);

            if (baseHealth != null && damagedStructures.Add(baseHealth))
            {
                baseHealth.TakeDamage(finalDamage);
            }

            // =============================
            // VEHICLE DAMAGE
            // =============================
            Health vh = Health.FindInHierarchy(col);

            if (vh != null && damagedVehicles.Add(vh))
            {
                vh.TakeDamage(finalDamage);
            }

            // =============================
            // EXPLOSION FORCE
            // =============================
            Rigidbody hitRb = col.attachedRigidbody;

            if (hitRb != null)
            {
                hitRb.AddExplosionForce(
                    explosionForce,
                    hitPoint,
                    explosionRadius,
                    1f,
                    ForceMode.Impulse
                );
            }
        }

        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        // Capture zones are triggers. Artillery intentionally flies through
        // them and detonates only after reaching physical terrain.
    }

    void FixedUpdate()
    {
        if (!exploded && rb != null)
            lastVelocity = rb.linearVelocity;
    }

    bool IsGround(Collider collider)
    {
        if (collider == null)
            return false;
        if (collider is TerrainCollider)
            return true;

        string name = collider.gameObject.name.ToLowerInvariant();
        if (name.Contains("plane") || name.Contains("ground") || name.Contains("terrain"))
            return true;

        // Static level geometry is valid terrain, but gameplay structures are
        // deliberately excluded so shells land beside them instead of on them.
        return collider.gameObject.isStatic &&
            BaseDestructibleWithHealthVisual.FindInHierarchy(collider) == null;
    }

    void IgnoreCollider(Collider other)
    {
        if (other == null)
            return;

        foreach (Collider shellCollider in GetComponentsInChildren<Collider>(true))
            if (shellCollider != null)
                Physics.IgnoreCollision(shellCollider, other, true);
    }

    // =====================================================
    // DEBUG GIZMO
    // =====================================================
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            explosionRadius
        );
    }
}
