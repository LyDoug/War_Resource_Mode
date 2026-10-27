using UnityEngine;

public class ResourceNode : MonoBehaviour
{
    [Header("Resource")]
    public ResourceType resourceType = ResourceType.Steel;

    [Header("Owner")]
    public Team owner = Team.Neutral;

    [Header("Jeep Collection")]
    public static string PlayerPrompt { get; private set; }
    Transform extractionMarker;
    LineRenderer markerRenderer;
    CaptureTriggerAdvanced captureZone;
    [Header("Resource cooldown")]
    [Min(0.5f)] public float truckCooldown = 5f;
    float nextTruckAvailableTime;
    Transform cooldownBar;

    void Awake()
    {
        // Mines are fixed interaction volumes, not movable physics props.
        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = true;
            body.constraints = RigidbodyConstraints.FreezeAll;
        }

        foreach (Collider mineCollider in GetComponents<Collider>())
            mineCollider.isTrigger = true;

        GameObject marker = new GameObject("Extraction Area Outline");
        marker.name = "Extraction Area Indicator";
        marker.transform.position = transform.position + Vector3.up * 0.1f;
        markerRenderer = marker.AddComponent<LineRenderer>();
        markerRenderer.useWorldSpace = false;
        markerRenderer.loop = true;
        markerRenderer.positionCount = 64;
        markerRenderer.startWidth = 0.2f;
        markerRenderer.endWidth = 0.2f;
        markerRenderer.material = new Material(Shader.Find("Sprites/Default"));
        for (int i = 0; i < 64; i++)
        {
            float angle = i * Mathf.PI * 2f / 64f;
            markerRenderer.SetPosition(i, new Vector3(Mathf.Cos(angle) * 14f, 0f, Mathf.Sin(angle) * 14f));
        }
        extractionMarker = marker.transform;
        foreach (CaptureTriggerAdvanced candidate in FindObjectsByType<CaptureTriggerAdvanced>(FindObjectsSortMode.None))
            if (candidate.resourceNode == this) { captureZone = candidate; break; }
        UpdateMarkerColor();

        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.name = "Mine Resource Cooldown";
        bar.transform.position = transform.position + Vector3.up * 3.2f;
        Collider barCollider = bar.GetComponent<Collider>();
        if (barCollider != null) Destroy(barCollider);
        Renderer barRenderer = bar.GetComponent<Renderer>();
        barRenderer.material = new Material(Shader.Find("Sprites/Default"));
        barRenderer.material.color = new Color(1f, 0.8f, 0.05f);
        cooldownBar = bar.transform;
    }

    void Update()
    {
        if (extractionMarker != null)
            extractionMarker.position = GetRingCenter() + Vector3.up * 0.1f;
        UpdateCooldownBar();

        // Extraction is automatic. A friendly vehicle entering a ready mine
        // dispatches a loaded truck while keeping the vehicle under control.
    }

    void OnTriggerEnter(Collider other)
    {
        OfferCollection(other.transform.root.gameObject);
    }

    void OnTriggerStay(Collider other)
    {
        OfferCollection(other.transform.root.gameObject);
    }

    public void OfferCollection(GameObject vehicle)
    {
        if (vehicle == null || vehicle.GetComponentInChildren<TruckCargo>() != null ||
            Time.time < nextTruckAvailableTime)
            return;

        if (vehicle.GetComponentInChildren<VehicleMotor>() == null)
            return;

        TeamMember member = vehicle.GetComponentInChildren<TeamMember>();
        if (member == null || owner == Team.Neutral || member.team != owner ||
            GameManager.Instance == null)
            return;

        // AI is limited to one concurrent delivery; player mines are limited
        // only by their own cooldown and can work in parallel.
        if (member.team != GameManager.Instance.PlayerTeam && HasAutomaticDeliveryInProgress(member.team))
            return;

        BeginTruckCooldown();
        VehicleSpawnerFinal.SpawnAutoDeliveryTruck(vehicle, member.team, resourceType);
    }

    static bool HasAutomaticDeliveryInProgress(Team team)
    {
        foreach (AITruckDelivery delivery in FindObjectsByType<AITruckDelivery>(FindObjectsSortMode.None))
        {
            if (delivery == null || !delivery.gameObject.activeInHierarchy)
                continue;

            TeamMember truckTeam = delivery.GetComponentInChildren<TeamMember>();
            if (truckTeam != null && truckTeam.team == team)
                return true;
        }

        return false;
    }

    void BeginTruckCooldown()
    {
        nextTruckAvailableTime = Time.time + truckCooldown;
    }

    void UpdateCooldownBar()
    {
        if (cooldownBar == null)
            return;

        cooldownBar.position = GetRingCenter() + Vector3.up * 3.2f;
        float remaining = Mathf.Max(0f, nextTruckAvailableTime - Time.time);
        float ready = 1f - remaining / truckCooldown;
        cooldownBar.localScale = new Vector3(Mathf.Max(0.05f, ready * 4f), 0.2f, 0.25f);
        cooldownBar.gameObject.SetActive(remaining > 0f);
    }

    public Vector3 GetVisualCenter()
    {
        // The authored mine meshes are not guaranteed to share the root
        // pivot. Use the visible bounds, so the interaction/capture rings
        // stay centred even when a model was made with an offset pivot.
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        bool foundRenderer = false;
        Bounds visualBounds = default;

        foreach (Renderer renderer in renderers)
        {
            if (!renderer.enabled)
                continue;

            if (!foundRenderer)
            {
                visualBounds = renderer.bounds;
                foundRenderer = true;
            }
            else
            {
                visualBounds.Encapsulate(renderer.bounds);
            }
        }

        if (!foundRenderer)
            return transform.position;

        // Keep the outline on the terrain while using the mesh centre in X/Z.
        return new Vector3(visualBounds.center.x, transform.position.y, visualBounds.center.z);
    }

    public void SetWorldIndicatorsVisible(bool visible)
    {
        if (extractionMarker != null)
            extractionMarker.gameObject.SetActive(visible);
        if (cooldownBar != null)
            cooldownBar.gameObject.SetActive(visible && Time.time < nextTruckAvailableTime);
    }

    Vector3 GetRingCenter()
    {
        return GetVisualCenter();
    }

    // ================= DONO =================
    public void SetOwner(Team newOwner)
    {
        owner = newOwner;
        UpdateMarkerColor();

        Debug.Log("⛏️ Mina agora pertence a: " + owner);
    }

    void UpdateMarkerColor()
    {
        if (markerRenderer == null)
            return;

        Color color = owner == Team.TeamA
            ? new Color(0.15f, 0.45f, 1f)
            : owner == Team.TeamB ? new Color(1f, 0.18f, 0.18f)
            : new Color(0.75f, 0.75f, 0.75f);
        markerRenderer.startColor = color;
        markerRenderer.endColor = color;
    }

    // ================= COLETA =================
    public bool TryCollect(TruckCargo truck, Team collectorTeam)
    {
        // sem dono
        if (owner == Team.Neutral)
        {
            Debug.Log("⚪ Mina neutra");
            return false;
        }

        // time errado
        if (collectorTeam != owner)
        {
            Debug.Log("⛔ Mina inimiga");
            return false;
        }

        // truck inválido
        if (truck == null)
        {
            Debug.Log("❌ Truck NULL");
            return false;
        }

        // 🔥 USA LOAD CORRETAMENTE
        bool loaded = truck.Load(resourceType, 1);

        if (loaded)
        {
            Debug.Log($"📦 Coletado 1 de {resourceType}");
        }

        return loaded;
    }
}
