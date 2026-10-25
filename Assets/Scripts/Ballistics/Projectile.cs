using UnityEngine;

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

    // =========================================================
    // AWAKE
    // =========================================================
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
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
    }

    // =========================================================
    // COLLISION
    // =========================================================
    void OnCollisionEnter(Collision collision)
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
                collision.contacts[0].point;

            impact.transform.rotation =
                Quaternion.LookRotation(
                    collision.contacts[0].normal
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
            collision.collider.GetComponentInParent<BaseDestructibleWithHealthVisual>();

        if (baseScript != null)
        {
            baseScript.TakeDamage(damage);

            Destroy(gameObject);

            return;
        }

        // =====================================================
        // DAMAGE VEHICLE/PLAYER
        // =====================================================
        Health health =
            collision.collider.GetComponentInParent<Health>();

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
            mine.GetComponentInParent<BaseDestructibleWithHealthVisual>();
        if (structure == null)
            return;

        hasImpacted = true;
        structure.TakeDamage(damage);
        Destroy(gameObject);
    }
}
