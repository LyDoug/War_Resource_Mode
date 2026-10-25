using UnityEngine;

public class BaseDeliveryZone : MonoBehaviour
{
    public BaseInventory inventory;
    Transform deliveryMarker;

    void Awake()
    {
        foreach (Collider deliveryCollider in GetComponentsInChildren<Collider>(true))
            deliveryCollider.isTrigger = true;

        GameObject marker = new GameObject("Delivery Point Outline");
        marker.name = "Delivery Point Indicator";
        marker.transform.position = transform.position + Vector3.up * 0.12f;
        LineRenderer outline = marker.AddComponent<LineRenderer>();
        outline.useWorldSpace = false;
        outline.loop = true;
        outline.positionCount = 64;
        outline.startWidth = 0.2f;
        outline.endWidth = 0.2f;
        outline.material = new Material(Shader.Find("Sprites/Default"));
        outline.startColor = new Color(1f, 0.78f, 0.1f);
        outline.endColor = new Color(1f, 0.78f, 0.1f);
        for (int i = 0; i < 64; i++)
        {
            float angle = i * Mathf.PI * 2f / 64f;
            outline.SetPosition(i, new Vector3(Mathf.Cos(angle) * 18f, 0f, Mathf.Sin(angle) * 18f));
        }
        deliveryMarker = marker.transform;

        // Attach the practical delivery perimeter to the factory capture
        // object, not just to this small doorway trigger.
        foreach (VehicleSpawnerFinal spawner in FindObjectsByType<VehicleSpawnerFinal>(FindObjectsSortMode.None))
        {
            if (spawner.inventory != inventory || spawner.baseCapture == null || spawner.baseCapture.structureType != StructureType.Factory)
                continue;
            FactoryDeliveryArea area = spawner.baseCapture.GetComponent<FactoryDeliveryArea>() ?? spawner.baseCapture.gameObject.AddComponent<FactoryDeliveryArea>();
            area.Configure(inventory, spawner.baseCapture);
            break;
        }
    }

    void LateUpdate()
    {
        if (deliveryMarker != null)
            deliveryMarker.position = transform.position + Vector3.up * 0.12f;
    }

    void OnTriggerEnter(Collider other)
    {
        TruckCargo cargo =
            other.GetComponent<TruckCargo>() ??
            other.GetComponentInParent<TruckCargo>();

        if (cargo == null)
            return;

        if (inventory == null)
            return;

        TeamMember truckTeam =
            cargo.GetComponent<TeamMember>() ??
            cargo.GetComponentInParent<TeamMember>();

        CaptureTriggerAdvanced capture =
            inventory.GetComponent<CaptureTriggerAdvanced>() ??
            inventory.GetComponentInParent<CaptureTriggerAdvanced>();

        if (
            capture != null &&
            capture.GetOwner() != Team.Neutral &&
            (truckTeam == null || truckTeam.team != capture.GetOwner())
        )
        {
            Debug.Log("⛔ Entrega bloqueada: base pertence ao outro time.");
            return;
        }

        if (cargo.Unload(inventory))
            Debug.Log("🏭 Recurso entregue!");
    }
}
