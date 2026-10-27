using UnityEngine;

public class Health : MonoBehaviour
{
    public static event System.Action<Health> Destroyed;

    public float maxHealth = 100f;
    [Tooltip("Fator usado para calcular a blindagem inicial quando nao houver um valor manual.")]
    [Range(0f, 0.9f)] public float armorDamageReduction;
    [Min(0f)] public float armorThresholdOverride;
    private float currentHealth;
    private float maxArmorIntegrity;
    private float currentArmorIntegrity;
    private bool isDestroyed;

    public float CurrentHealth => currentHealth;
    public float HealthFraction => maxHealth <= 0f ? 0f : Mathf.Clamp01(currentHealth / maxHealth);
    public float CurrentArmorIntegrity => currentArmorIntegrity;
    public float MaxArmorIntegrity => maxArmorIntegrity;
    public bool ArmorBroken => maxArmorIntegrity > 0f && currentArmorIntegrity <= 0f;

    void Awake()
    {
        currentHealth = maxHealth;
        ResetArmor();
    }

    public void ConfigureMaxHealth(float value)
    {
        maxHealth = value;
        currentHealth = value;
        ResetArmor();
    }

    public void ConfigureDurability(float health, float armorReduction)
    {
        maxHealth = health;
        armorDamageReduction = Mathf.Clamp(armorReduction, 0f, 0.9f);
        currentHealth = health;
        ResetArmor();
    }

    public void TakeDamage(float damage)
    {
        float incomingDamage = Mathf.Max(0f, damage);
        float absorbedDamage = Mathf.Min(incomingDamage, currentArmorIntegrity);
        float appliedDamage = incomingDamage - absorbedDamage;

        if (absorbedDamage > 0f)
        {
            // Armor has its own integrity. Each point absorbed costs half a
            // point of armor: 80 armor hit by 100 absorbs 80, leaks 20, and
            // becomes 40 armor for the next impact.
            currentArmorIntegrity = Mathf.Max(0f, currentArmorIntegrity - absorbedDamage * 0.5f);
            if (currentArmorIntegrity <= 0f)
                Debug.Log(gameObject.name + " blindagem quebrada!");
        }

        currentHealth -= appliedDamage;
        DamageFeedback.Show(transform.position, appliedDamage, absorbedDamage > 0f);

        Debug.Log(gameObject.name + " tomou " + appliedDamage + " de dano. Vida: " + currentHealth +
            " Blindagem: " + currentArmorIntegrity + "/" + maxArmorIntegrity);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void ResetArmor()
    {
        maxArmorIntegrity = armorThresholdOverride > 0f
            ? armorThresholdOverride
            : maxHealth * armorDamageReduction * 0.30f;
        currentArmorIntegrity = maxArmorIntegrity;
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
