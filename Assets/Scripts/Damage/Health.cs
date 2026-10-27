using UnityEngine;

public class Health : MonoBehaviour
{
    public static event System.Action<Health> Destroyed;

    public float maxHealth = 100f;
    [Range(0f, 0.9f)] public float armorDamageReduction;
    private float currentHealth;
    private bool isDestroyed;

    public float CurrentHealth => currentHealth;
    public float HealthFraction => maxHealth <= 0f ? 0f : Mathf.Clamp01(currentHealth / maxHealth);

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void ConfigureMaxHealth(float value)
    {
        maxHealth = value;
        currentHealth = value;
    }

    public void ConfigureDurability(float health, float armorReduction)
    {
        maxHealth = health;
        armorDamageReduction = Mathf.Clamp(armorReduction, 0f, 0.9f);
        currentHealth = health;
    }

    public void TakeDamage(float damage)
    {
        float appliedDamage = Mathf.Max(0f, damage) * (1f - armorDamageReduction);
        currentHealth -= appliedDamage;
        DamageFeedback.Show(transform.position, appliedDamage, armorDamageReduction > 0f);

        Debug.Log(gameObject.name + " tomou " + appliedDamage + " de dano. Vida: " + currentHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDestroyed)
            return;

        isDestroyed = true;
        Debug.Log(gameObject.name + " destruído!");
        Destroyed?.Invoke(this);

        // Imported prefabs may keep Health on a mesh child. Remove the
        // complete vehicle rather than leaving the remaining hierarchy alive.
        VehicleController vehicle = GetComponentInParent<VehicleController>();
        GameObject target = vehicle != null ? vehicle.transform.root.gameObject : gameObject;
        Destroy(target);
    }

    // Vehicle colliders are often placed on a mesh child while Health lives
    // on the vehicle root (or another sibling). Resolve from the root too so
    // a hit on any tank mesh can never bypass its health component.
    public static Health FindInHierarchy(Collider collider)
    {
        if (collider == null)
            return null;

        Health health = collider.GetComponentInParent<Health>();
        if (health != null)
            return health;

        Transform root = collider.transform.root;
        return root.GetComponent<Health>() ?? root.GetComponentInChildren<Health>(true);
    }
}
