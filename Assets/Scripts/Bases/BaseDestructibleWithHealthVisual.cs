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

        currentHealth -= damage;

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

        rubble = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rubble.name = "Escombros - " + name;
        rubble.transform.position = new Vector3(bounds.center.x, bounds.min.y + Mathf.Max(0.35f, bounds.size.y * 0.18f), bounds.center.z);
        rubble.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 180f), 0f);
        rubble.transform.localScale = new Vector3(Mathf.Max(2f, bounds.size.x), Mathf.Max(0.35f, bounds.size.y * 0.175f), Mathf.Max(2f, bounds.size.z));
        Renderer rubbleRenderer = rubble.GetComponent<Renderer>();
        if (rubbleRenderer != null)
        {
            Color rubbleColor = visible.material.HasProperty("_BaseColor") ? visible.material.GetColor("_BaseColor") : visible.material.color;
            rubbleRenderer.material.color = rubbleColor;

            // Small uneven debris breaks the perfect-block silhouette.
            for (int i = 0; i < 4; i++)
            {
                GameObject debris = GameObject.CreatePrimitive(PrimitiveType.Cube);
                debris.name = "Detrito - " + name;
                debris.transform.position = rubble.transform.position + new Vector3(
                    Random.Range(-bounds.size.x * 0.4f, bounds.size.x * 0.4f),
                    Random.Range(0.12f, 0.35f), Random.Range(-bounds.size.z * 0.4f, bounds.size.z * 0.4f));
                debris.transform.rotation = Random.rotation;
                debris.transform.localScale = Vector3.one * Random.Range(0.35f, 0.9f);
                debris.GetComponent<Renderer>().material.color = rubbleColor;
            }
        }
    }
}

