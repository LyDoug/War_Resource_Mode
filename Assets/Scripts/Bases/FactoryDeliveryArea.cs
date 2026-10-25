using UnityEngine;

// Broad delivery ring around a factory: any friendly loaded truck entering
// the perimeter deposits directly into that factory's local inventory.
public class FactoryDeliveryArea : MonoBehaviour
{
    const float DeliveryRadius = 4f;
    public BaseInventory inventory;
    public CaptureTriggerAdvanced capture;
    LineRenderer marker;

    public void Configure(BaseInventory target, CaptureTriggerAdvanced owner)
    {
        inventory = target;
        capture = owner;
    }

    void Awake()
    {
        GameObject ring = new GameObject("Factory Delivery Outline");
        ring.name = "Factory Delivery Area";
        // Delivery is a smaller, centred inner ring; the larger outer ring is
        // the capture perimeter.
        ring.transform.position = (capture == null ? transform.position : capture.GetWorldCenter()) + Vector3.up * 0.06f;
        marker = ring.AddComponent<LineRenderer>();
        marker.useWorldSpace = false;
        marker.loop = true;
        marker.positionCount = 64;
        marker.startWidth = 0.22f;
        marker.endWidth = 0.22f;
        marker.material = new Material(Shader.Find("Sprites/Default"));
        for (int i = 0; i < 64; i++)
        {
            float angle = i * Mathf.PI * 2f / 64f;
            marker.SetPosition(i, new Vector3(Mathf.Cos(angle) * DeliveryRadius, 0f, Mathf.Sin(angle) * DeliveryRadius));
        }
    }

    void Update()
    {
        if (marker == null) return;
        marker.transform.position = (capture == null ? transform.position : capture.GetWorldCenter()) + Vector3.up * 0.06f;
        Team owner = capture == null ? Team.Neutral : capture.GetOwner();
        Color color = owner == Team.TeamA ? new Color(0.15f, 0.45f, 1f) :
            owner == Team.TeamB ? new Color(1f, 0.18f, 0.18f) : new Color(0.65f, 0.65f, 0.65f);
        marker.startColor = color;
        marker.endColor = color;

        // The factory capture object inherits the model's scale. A distance
        // check keeps delivery exactly inside the visible world-space ring.
        foreach (TruckCargo cargo in FindObjectsByType<TruckCargo>(FindObjectsSortMode.None))
        {
            TeamMember truck = cargo.GetComponentInParent<TeamMember>() ?? cargo.GetComponent<TeamMember>();
            if (truck == null || inventory == null || capture == null || truck.team != capture.GetOwner())
                continue;

            Vector3 offset = cargo.transform.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude <= DeliveryRadius * DeliveryRadius && cargo.Unload(inventory))
            {
                AITruckDelivery autoDelivery = cargo.GetComponentInParent<AITruckDelivery>() ??
                    cargo.GetComponentInChildren<AITruckDelivery>();
                if (autoDelivery != null)
                    Destroy(autoDelivery.gameObject, 0.5f);
            }
        }
    }
}
