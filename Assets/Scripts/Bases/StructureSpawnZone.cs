using UnityEngine;

public class StructureSpawnZone : MonoBehaviour
{
    public VehicleSpawnerFinal spawner;
    readonly System.Collections.Generic.Dictionary<int, int> occupants =
        new System.Collections.Generic.Dictionary<int, int>();

    void Awake()
    {
        // Production areas are interaction volumes, never solid geometry.
        foreach (Collider zoneCollider in GetComponentsInChildren<Collider>(true))
            zoneCollider.isTrigger = true;

        // This object may inherit the factory model's scale. Keep the
        // interaction trigger at the same 12 m radius as the visible factory
        // perimeter instead of allowing it to cover another factory.
        SphereCollider zoneSphere = GetComponent<SphereCollider>();
        if (zoneSphere != null)
        {
            Vector3 scale = transform.lossyScale;
            float largestAxis = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z), 0.001f);
            zoneSphere.radius = 15f / largestAxis;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        TeamMember unit = other.GetComponent<TeamMember>() ??
            other.GetComponentInParent<TeamMember>() ??
            other.GetComponentInChildren<TeamMember>();
        if (unit == null)
            return;

        int unitId = unit.transform.root.GetInstanceID();
        occupants.TryGetValue(unitId, out int colliderCount);
        occupants[unitId] = colliderCount + 1;

        // Any vehicle can capture a factory, regardless of who controls it.
        if (spawner != null && spawner.baseCapture != null && spawner.baseCapture.structureType == StructureType.Factory)
        {
            if (!spawner.baseCapture.CanUnitCapture(unit))
                return;
            spawner.baseCapture.RegisterExternalUnit(unit);
        }

        // Only the human team opens the production UI.
        if (GameManager.Instance != null && unit.team != GameManager.Instance.PlayerTeam)
            return;

        Debug.Log("🏭 Entrou na zona");

        // 🔥 desativa somente os conhecidos
        VehicleSpawnerFinal[] allSpawners =
            FindObjectsOfType<VehicleSpawnerFinal>();

        foreach (VehicleSpawnerFinal s in allSpawners)
        {
            s.isPlayerSpawner = false;
        }

        if (spawner != null)
        {
            spawner.isPlayerSpawner = true;

            if (GameManager.Instance != null)
                GameManager.Instance.SetActiveSpawner(spawner);

            Debug.Log(
                "✅ Spawner ativado: " +
                spawner.name
            );
        }
    }

    void OnTriggerExit(Collider other)
    {
        TeamMember unit = other.GetComponent<TeamMember>() ??
            other.GetComponentInParent<TeamMember>() ??
            other.GetComponentInChildren<TeamMember>();
        if (unit == null || spawner == null)
            return;

        int unitId = unit.transform.root.GetInstanceID();
        if (!occupants.TryGetValue(unitId, out int colliderCount))
            return;

        if (colliderCount > 1)
        {
            occupants[unitId] = colliderCount - 1;
            return;
        }

        occupants.Remove(unitId);

        if (spawner.baseCapture != null && spawner.baseCapture.structureType == StructureType.Factory)
            spawner.baseCapture.UnregisterExternalUnit(unit);

        if (GameManager.Instance != null && unit.team != GameManager.Instance.PlayerTeam)
            return;

        // A vehicle can have several colliders. Keep the menu visible until
        // its last collider leaves the zone.
        if (occupants.Count > 0)
            return;

        spawner.isPlayerSpawner = false;

        if (GameManager.Instance != null && GameManager.Instance.ActiveSpawner == spawner)
            GameManager.Instance.SetActiveSpawner(null);
    }
}
