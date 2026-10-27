using UnityEngine;

// Converts stock held at a Main Base into automatic deliveries whenever the
// owning team controls at least one factory.
public class MainBaseResourceDispatcher : MonoBehaviour
{
    public Team team;
    public BaseInventory inventory;
    public Transform spawnPoint;
    public float checkInterval = 3f;

    void Start()
    {
        if (inventory == null)
            inventory = GetComponent<BaseInventory>() ?? gameObject.AddComponent<BaseInventory>();
        if (spawnPoint == null)
        {
            VehicleSpawnerFinal spawner = GetComponent<VehicleSpawnerFinal>() ?? GetComponentInChildren<VehicleSpawnerFinal>();
            if (spawner != null) spawnPoint = spawner.spawnPoint;
        }
        InvokeRepeating(nameof(DispatchOneResource), checkInterval, checkInterval);
    }

    void DispatchOneResource()
    {
        if (GameManager.Instance == null || !GameManager.Instance.MatchStarted ||
            team == Team.Neutral || !HasFriendlyFactory() || spawnPoint == null)
            return;

        ResourceType resource = inventory.HasResource(ResourceType.Steel, 1) ? ResourceType.Steel :
            inventory.HasResource(ResourceType.Oil, 1) ? ResourceType.Oil : ResourceType.None;
        if (resource == ResourceType.None)
            return;

        VehicleBlueprint blueprint = FindTruckBlueprint();
        if (blueprint == null || !inventory.ConsumeResource(resource, 1))
            return;

        Vector3 position = VehicleSpawnerFinal.FindSafeTruckSpawnPosition(
            spawnPoint.position,
            spawnPoint.rotation);
        GameObject truck = Instantiate(blueprint.prefab, position, spawnPoint.rotation);
        TeamMember member = truck.GetComponentInChildren<TeamMember>();
        if (member != null) member.SetTeam(team);
        TruckCargo cargo = truck.GetComponentInChildren<TruckCargo>();
        if (cargo != null)
        {
            cargo.Load(resource, 1);
            cargo.BeginSafeSpawn();
        }
        VehicleMotor motor = truck.GetComponentInChildren<VehicleMotor>();
        if (motor != null) motor.usePlayerInput = false;
        TankMotor tankMotor = truck.GetComponentInChildren<TankMotor>();
        if (tankMotor != null) tankMotor.usePlayerInput = false;
        VehicleSimpleAI ai = truck.GetComponent<VehicleSimpleAI>() ?? truck.AddComponent<VehicleSimpleAI>();
        (truck.GetComponent<AITruckDelivery>() ?? truck.AddComponent<AITruckDelivery>()).Configure(ai, team);
    }

    bool HasFriendlyFactory()
    {
        foreach (VehicleSpawnerFinal factory in FindObjectsByType<VehicleSpawnerFinal>(FindObjectsSortMode.None))
            if (factory.baseCapture != null && factory.baseCapture.structureType == StructureType.Factory &&
                factory.baseCapture.GetOwner() == team && factory.inventory != null)
                return true;
        return false;
    }

    VehicleBlueprint FindTruckBlueprint()
    {
        foreach (VehicleSpawnerFinal spawner in FindObjectsByType<VehicleSpawnerFinal>(FindObjectsSortMode.None))
            if (spawner.availableBlueprints != null)
                foreach (VehicleBlueprint blueprint in spawner.availableBlueprints)
                    if (blueprint != null && blueprint.role == VehicleRole.Truck && blueprint.prefab != null)
                        return blueprint;
        return null;
    }
}
