using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class BombProjectile : MonoBehaviour
{
    [Header("Damage")]
    public float damage = 150f;
    public float explosionRadius = 6f;

    [Header("Timing")]
    public float armDelay = 0.4f;     // tempo antes de poder explodir
    public float lifeTime = 12f;

    [Header("Effects")]
    public GameObject explosionPrefab;

    Rigidbody rb;
    Collider bombCollider;
    bool isArmed = false;
    bool hasExploded = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        bombCollider = GetComponent<Collider>();

        rb.useGravity = true;
        rb.isKinematic = false;

        bombCollider.isTrigger = false;
    }

    void Start()
    {
        // Evita explosão instantânea
        Invoke(nameof(ArmBomb), armDelay);

        // Segurança caso nunca colida
        Destroy(gameObject, lifeTime);
    }

    void ArmBomb()
    {
        isArmed = true;
    }

    // ================= IGNORAR COLISÃO COM AVIÃO =================
    public void IgnoreOwnerCollisions(Collider[] ownerColliders)
    {
        foreach (Collider col in ownerColliders)
        {
            if (col != null)
                Physics.IgnoreCollision(bombCollider, col);
        }
    }

    // ================= COLISÃO =================
    void OnCollisionEnter(Collision collision)
    {
        if (!isArmed || hasExploded) return;

        Explode();
    }

    // ================= EXPLOSÃO =================
    void Explode()
    {
        hasExploded = true;

        // Efeito visual
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        // Dano em área
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);

        HashSet<BaseDestructibleWithHealthVisual> damagedStructures = new HashSet<BaseDestructibleWithHealthVisual>();
        HashSet<Health> damagedVehicles = new HashSet<Health>();
        foreach (Collider hit in hits)
        {
            // Base
            BaseDestructibleWithHealthVisual baseHealth =
                BaseDestructibleWithHealthVisual.FindInHierarchy(hit);

            if (baseHealth != null && damagedStructures.Add(baseHealth))
            {
                baseHealth.TakeDamage(damage);
                continue;
            }

            Health vehicleHealth = Health.FindInHierarchy(hit);
            if (vehicleHealth != null && damagedVehicles.Add(vehicleHealth))
                vehicleHealth.TakeDamage(damage);
        }

        Destroy(gameObject);
    }

    // ================= DEBUG =================
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
