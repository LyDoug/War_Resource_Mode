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

    bool exploded = false;

    // =====================================================
    // START
    // =====================================================
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // velocidade inicial
        rb.linearVelocity =
            transform.forward * speed;

        Destroy(gameObject, lifeTime);
    }

    // =====================================================
    // COLLISION
    // =====================================================
    void OnCollisionEnter(Collision collision)
    {
        if (exploded)
            return;

        exploded = true;

        Vector3 hitPoint =
            collision.contacts[0].point;

        // =====================================
        // IMPACT VFX
        // =====================================
        if (impactVFX != null)
        {
            Instantiate(
                impactVFX,
                hitPoint,
                Quaternion.identity
            );
        }

        // =====================================
        // AREA DAMAGE
        // =====================================
        Collider[] hits =
            Physics.OverlapSphere(
                hitPoint,
                explosionRadius
            );

        foreach (Collider col in hits)
        {
            // =============================
            // DAMAGE FALLOFF
            // =============================
            float distance =
                Vector3.Distance(
                    hitPoint,
                    col.transform.position
                );

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
                col.GetComponentInParent<BaseDestructibleWithHealthVisual>();

            if (baseHealth != null)
            {
                baseHealth.TakeDamage(finalDamage);
            }

            // =============================
            // VEHICLE DAMAGE
            // =============================
            Health vh =
                col.GetComponentInParent<Health>();

            if (vh != null)
            {
                vh.TakeDamage(finalDamage);
            }

            // =============================
            // EXPLOSION FORCE
            // =============================
            Rigidbody hitRb =
                col.GetComponent<Rigidbody>();

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

    // Oil/steel mine interaction volumes are triggers, so artillery needs a
    // trigger impact path in addition to its normal physical explosion path.
    void OnTriggerEnter(Collider other)
    {
        if (exploded)
            return;

        ResourceNode mine = other.GetComponent<ResourceNode>();
        if (mine == null)
            return;

        BaseDestructibleWithHealthVisual structure =
            mine.GetComponentInParent<BaseDestructibleWithHealthVisual>();
        if (structure == null)
            return;

        exploded = true;
        structure.TakeDamage(damage);
        Destroy(gameObject);
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
