using System.Collections.Generic;
using UnityEngine;

public class BaseDestructibleWithHealthVisual : MonoBehaviour
{
    [Header("Match Objective")]
    public bool endsMatchOnDestruction;
    public Team mainBaseTeam = Team.Neutral;
    [Header("Spawner Reference")]
    public VehicleSpawnerFinal spawner;

    [Header("Health Settings")]
    public float maxHealth = 200f;
    [Range(0f, 0.9f)] public float armorDamageReduction = 0.3f;

    [SerializeField]
    private float currentHealth;

    bool destroyed = false;

    [Header("Respawn Settings")]
    public Transform[] respawnPoints;

    public float respawnDelay = 5f;

    [Header("Visuals")]
    public Renderer baseRenderer;

    public Color fullHealthColor = Color.green;

    public Color lowHealthColor = Color.red;

    CaptureTriggerAdvanced capture;

    Rigidbody[] rigidbodies;
    Vector3 originalPosition;
    Quaternion originalRotation;
    GameObject rubble;
    readonly Dictionary<Renderer, bool> rendererStates = new Dictionary<Renderer, bool>();
    readonly Dictionary<Collider, bool> colliderStates = new Dictionary<Collider, bool>();

    // =========================================================
    // START
    // =========================================================
    void Start()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        currentHealth = maxHealth;

        // CAPTURE
        capture =
            GetComponentInChildren<CaptureTriggerAdvanced>(true);

        if (capture == null)
        {
            float nearest = 28f;
            foreach (CaptureTriggerAdvanced candidate in FindObjectsByType<CaptureTriggerAdvanced>(FindObjectsSortMode.None))
            {
                if (candidate.baseScript != null)
                    continue;
                float distance = Vector3.Distance(transform.position, candidate.transform.position);
                if (distance < nearest)
                {
                    nearest = distance;
                    capture = candidate;
                }
            }
        }
        if (capture != null && capture.baseScript == null)
            capture.baseScript = this;

        // Preserve the authored state. Capture helper meshes are intentionally
        // hidden, and must not turn into a grey bubble after a respawn.
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            rendererStates[renderer] = renderer.enabled;
        foreach (Collider collider in GetComponentsInChildren<Collider>(true))
            colliderStates[collider] = collider.enabled;

        // RIGIDBODIES
        rigidbodies =
            GetComponentsInChildren<Rigidbody>(true);

        // Bases and factories are scenery. Dynamic rigidbodies made them
        // react to vehicle impacts and bounce across the map.
        foreach (Rigidbody rb in rigidbodies)
        {
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        // AUTO RENDERER
        if (baseRenderer == null)
        {
            baseRenderer =
                GetComponentInChildren<Renderer>();

            if (baseRenderer == null)
            {
                Debug.LogWarning(
                    "⚠️ Renderer não encontrado na base!"
                );
            }
        }

        UpdateColor();
    }

    // =========================================================
    // DAMAGE
    // =========================================================
    public void TakeDamage(float damage)
    {
        if (destroyed)
            return;

        float appliedDamage = Mathf.Max(0f, damage) * (1f - armorDamageReduction);
        currentHealth -= appliedDamage;
        DamageFeedback.Show(transform.position, appliedDamage, armorDamageReduction > 0f);

        currentHealth =
            Mathf.Clamp(
                currentHealth,
                0f,
                maxHealth
            );

        UpdateColor();

        Debug.Log(
            "💥 Base tomou dano: " +
            currentHealth +
            "/" +
            maxHealth
        );

        if (currentHealth <= 0f)
        {
            DestroyBase();
        }
    }

    // =========================================================
    // VISUAL
    // =========================================================
    void UpdateColor()
    {
        if (baseRenderer == null)
            return;

        float t =
            1f - (currentHealth / maxHealth);

        Color finalColor =
            Color.Lerp(
                fullHealthColor,
                lowHealthColor,
                t
            );

        // URP/HDRP
        if (
            baseRenderer.material.HasProperty(
                "_BaseColor"
            )
        )
        {
            baseRenderer.material.SetColor(
                "_BaseColor",
                finalColor
            );
        }
        else
        {
            baseRenderer.material.color =
                finalColor;
        }
    }

    // =========================================================
    // DESTROY
    // =========================================================
    void DestroyBase()
    {
        if (destroyed)
            return;

        destroyed = true;

        BaseInventory inventory = GetComponentInChildren<BaseInventory>(true);
        if (inventory != null)
            inventory.ClearResources();

        // Rubble is neutral: remove all ownership, score and mine-resource
        // contribution as soon as the structure is destroyed.
        if (!endsMatchOnDestruction && capture != null)
            capture.ResetCapture();

        Debug.Log("💥 Base destruída!");

        if (endsMatchOnDestruction)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.MainBaseDestroyed(mainBaseTeam);
            return;
        }

        // =====================================
        // DESLIGA RENDERERS
        // =====================================
        foreach (Renderer r in rendererStates.Keys)
            if (r != null) r.enabled = false;

        // =====================================
        // DESLIGA COLLIDERS
        // =====================================
        foreach (Collider c in colliderStates.Keys)
            if (c != null) c.enabled = false;

        // =====================================
        // DESLIGA CAPTURE
        // =====================================
        if (capture != null)
        {
            capture.enabled = false;
            capture.SetWorldMarkerVisible(false);
        }

        SetStructureSystemsEnabled(false);

        CreateRubble();

        // =====================================
        // DESLIGA RIGIDBODIES
        // =====================================
        foreach (Rigidbody rb in rigidbodies)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.isKinematic = true;
        }

        // =====================================
        // RESPAWN
        // =====================================
        Invoke(
            nameof(RespawnBase),
            respawnDelay
        );
    }

    // =========================================================
    // RESPAWN
    // =========================================================
    void RespawnBase()
    {
        Debug.Log("🔄 Respawnando base...");

        // =====================================
        // POSIÇÃO NOVA
        // =====================================
        // Structures no longer rely on editor-authored point lists. Pick a
        // new location across the playable center of the battlefield.
        transform.position = FindSafeRespawnPosition();
        transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        // The destroyed structure respawns elsewhere. Its rubble remains at
        // the former location as physical cover for the rest of the match.
        rubble = null;

        // =====================================
        // RESET HEALTH
        // =====================================
        currentHealth = maxHealth;

        destroyed = false;

        // =====================================
        // REATIVA RENDERERS
        // =====================================
        foreach (KeyValuePair<Renderer, bool> state in rendererStates)
            if (state.Key != null) state.Key.enabled = state.Value;
        SettleOnGround();

        // =====================================
        // REATIVA COLLIDERS
        // =====================================
        foreach (KeyValuePair<Collider, bool> state in colliderStates)
            if (state.Key != null) state.Key.enabled = state.Value;

        // =====================================
        // REATIVA RIGIDBODIES
        // =====================================
        foreach (Rigidbody rb in rigidbodies)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // =====================================
        // RESET CAPTURE
        // =====================================
        if (capture != null)
        {
            capture.enabled = true;
            capture.SetWorldMarkerVisible(true);

            capture.ResetCapture();

            Debug.Log(
                "🏳️ Capture resetado"
            );
        }
        else
        {
            Debug.LogWarning(
                "⚠️ CaptureTrigger não encontrado!"
            );
        }

        // =====================================
        // RESET VISUAL
        // =====================================
        UpdateColor();
        SetStructureSystemsEnabled(true);

        Debug.Log("🏗️ Base reapareceu");
    }

    // =========================================================
    // MANUAL RESPAWN
    // =========================================================
    public void Respawn()
    {
        RespawnBase();
    }

    // =========================================================
    // CAPTURE
    // =========================================================
    public void OnCaptured()
    {
        Debug.Log("🏭 Base capturada");
    }

    // =========================================================
    // GETTERS
    // =========================================================
    public float GetHealth()
    {
        return currentHealth;
    }

    public void ConfigureDurability(float health, float armorReduction)
    {
        maxHealth = health;
        armorDamageReduction = Mathf.Clamp(armorReduction, 0f, 0.9f);
        currentHealth = maxHealth;
    }

    public static BaseDestructibleWithHealthVisual FindInHierarchy(Collider collider)
    {
        if (collider == null)
            return null;

        BaseDestructibleWithHealthVisual structure = collider.GetComponentInParent<BaseDestructibleWithHealthVisual>();
        if (structure != null)
            return structure;

        Transform root = collider.transform.root;
        return root.GetComponent<BaseDestructibleWithHealthVisual>() ??
            root.GetComponentInChildren<BaseDestructibleWithHealthVisual>(true);
    }

    public bool IsDestroyed()
    {
        return destroyed;
    }

    void SetStructureSystemsEnabled(bool enabled)
    {
        foreach (ResourceNode node in GetComponentsInChildren<ResourceNode>(true))
        {
            node.SetWorldIndicatorsVisible(enabled);
            node.enabled = enabled;
        }
        foreach (VehicleSpawnerFinal vehicleSpawner in GetComponentsInChildren<VehicleSpawnerFinal>(true))
            vehicleSpawner.enabled = enabled;
        foreach (StructureSpawnZone zone in GetComponentsInChildren<StructureSpawnZone>(true))
            zone.enabled = enabled;
        foreach (FactoryDeliveryArea area in GetComponentsInChildren<FactoryDeliveryArea>(true))
            area.enabled = enabled;
    }

    Vector3 FindSafeRespawnPosition()
    {
        const float minimumDistance = 42f;
        for (int attempt = 0; attempt < 80; attempt++)
        {
            Vector2 point = Random.insideUnitCircle.normalized * Random.Range(45f, 125f);
            Vector3 candidate = new Vector3(point.x, 180f, point.y);
            if (!TryFindGround(candidate, out float groundY))
                continue;

            candidate.y = groundY;
            bool clear = true;
            foreach (CaptureTriggerAdvanced other in FindObjectsByType<CaptureTriggerAdvanced>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (other == null || other == capture || other.baseScript == this ||
                    (other.baseScript != null && other.baseScript.IsDestroyed()))
                    continue;
                Vector3 offset = candidate - other.GetWorldCenter();
                offset.y = 0f;
                if (offset.sqrMagnitude < minimumDistance * minimumDistance)
                {
                    clear = false;
                    break;
                }
            }
            if (clear)
                return candidate;
        }

        return originalPosition;
    }

    bool TryFindGround(Vector3 rayStart, out float groundY)
    {
        groundY = 0f;
        bool found = false;
        float lowest = float.PositiveInfinity;
        foreach (RaycastHit hit in Physics.RaycastAll(rayStart, Vector3.down, 360f))
        {
            if (hit.collider.transform.IsChildOf(transform))
                continue;
            if (hit.point.y < lowest)
            {
                lowest = hit.point.y;
                found = true;
            }
        }
        groundY = lowest;
        return found;
    }

    void SettleOnGround()
    {
        float visualBottom = float.PositiveInfinity;
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            if (renderer.enabled)
                visualBottom = Mathf.Min(visualBottom, renderer.bounds.min.y);
        if (float.IsPositiveInfinity(visualBottom) || !TryFindGround(transform.position + Vector3.up * 180f, out float groundY))
            return;

        transform.position += Vector3.up * (groundY - visualBottom + 0.03f);
    }

    void CreateRubble()
    {
        if (rubble != null)
            return;

        Bounds bounds = new Bounds(transform.position, new Vector3(5f, 2f, 5f));
        Renderer visible = GetComponentInChildren<Renderer>();
        if (visible == null)
            return;
        bounds = visible.bounds;

        // A compact, low rubble mound remains as cover. Keep its base on the
        // terrain instead of centering a tall cube above the former model.
        float rubbleHeight = Mathf.Max(0.22f, bounds.size.y * 0.08f);
        float rubbleWidth = Mathf.Max(1.4f, bounds.size.x * 0.55f);
        float rubbleDepth = Mathf.Max(1.4f, bounds.size.z * 0.55f);
        Color rubbleColor = new Color(0.34f, 0.31f, 0.27f);
        Material rubbleMaterial = CreateRubbleMaterial(rubbleColor);

        rubble = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rubble.name = "Escombros - " + name;
        rubble.transform.position = new Vector3(bounds.center.x, bounds.min.y + rubbleHeight * 0.5f + 0.02f, bounds.center.z);
        rubble.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 180f), 0f);
        rubble.transform.localScale = new Vector3(rubbleWidth, rubbleHeight, rubbleDepth);
        Renderer rubbleRenderer = rubble.GetComponent<Renderer>();
        if (rubbleRenderer != null)
        {
            rubbleRenderer.material = rubbleMaterial;

            // Flattened, rotated pieces make the pile read as construction
            // debris instead of a single black square.
            for (int i = 0; i < 6; i++)
            {
                GameObject debris = GameObject.CreatePrimitive(PrimitiveType.Cube);
                debris.name = "Detrito - " + name;
                debris.transform.position = rubble.transform.position + new Vector3(
                    Random.Range(-rubbleWidth * 0.45f, rubbleWidth * 0.45f),
                    rubbleHeight * 0.65f + Random.Range(0.03f, 0.16f),
                    Random.Range(-rubbleDepth * 0.45f, rubbleDepth * 0.45f));
                debris.transform.rotation = Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(0f, 180f), Random.Range(-12f, 12f));
                debris.transform.localScale = new Vector3(Random.Range(0.25f, 0.7f), Random.Range(0.12f, 0.28f), Random.Range(0.25f, 0.7f));
                debris.GetComponent<Renderer>().material = CreateRubbleMaterial(Color.Lerp(rubbleColor, Color.gray, Random.Range(0f, 0.45f)));
            }
        }

        CreateRubbleSmoke(rubble.transform, rubbleHeight);
    }

    Material CreateRubbleMaterial(Color color)
    {
        Shader shader = Shader.Find("Sprites/Default");
        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    void CreateRubbleSmoke(Transform parent, float rubbleHeight)
    {
        GameObject smoke = new GameObject("Fumaca - " + name);
        smoke.transform.SetParent(parent, false);
        smoke.transform.localPosition = Vector3.up * (rubbleHeight * 0.6f);
        ParticleSystem particles = smoke.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.55f, 1.2f);
        main.maxParticles = 28;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new Color(0.28f, 0.28f, 0.28f, 0.48f);

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 5f;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.radius = 0.25f;
        shape.angle = 9f;

        ParticleSystem.ColorOverLifetimeModule fade = particles.colorOverLifetime;
        fade.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(new Color(0.22f, 0.22f, 0.22f), 0f), new GradientColorKey(new Color(0.45f, 0.45f, 0.45f), 1f) },
            new[] { new GradientAlphaKey(0.45f, 0f), new GradientAlphaKey(0f, 1f) });
        fade.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystemRenderer renderer = smoke.GetComponent<ParticleSystemRenderer>();
        Shader smokeShader = Shader.Find("Particles/Standard Unlit");
        if (smokeShader == null)
            smokeShader = Shader.Find("Sprites/Default");
        renderer.material = new Material(smokeShader);
    }
}

