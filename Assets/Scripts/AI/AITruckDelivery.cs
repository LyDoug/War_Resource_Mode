using UnityEngine;

// Moves an AI-owned loaded truck to the nearest friendly factory and then
// retires it, allowing EnemySpawner to field its next single vehicle.
public class AITruckDelivery : MonoBehaviour
{
    VehicleSimpleAI ai;
    Team team;
    bool delivered;
    bool returnToMainBase;
    BaseInventory destinationInventory;

    public void Configure(VehicleSimpleAI vehicleAI, Team owner)
    {
        ai = vehicleAI;
        team = owner;
        returnToMainBase = false;
        FindDelivery();
    }

    public void RedirectToMainBase(VehicleSimpleAI vehicleAI, Team owner)
    {
        ai = vehicleAI;
        team = owner;
        delivered = false;
        returnToMainBase = true;
        FindDelivery();
    }

    void Start()
    {
        if (ai == null)
            ai = GetComponent<VehicleSimpleAI>();
        if (team == Team.Neutral)
        {
            TeamMember member = GetComponentInChildren<TeamMember>();
            if (member != null)
                team = member.team;
        }
        FindDelivery();
    }

    void FindDelivery()
    {
        if (ai == null || team == Team.Neutral)
            return;

        if (returnToMainBase)
        {
            foreach (TeamMember member in FindObjectsByType<TeamMember>(FindObjectsSortMode.None))
            {
                if (member.team != team || member.gameObject.name.IndexOf("MainBase", System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                destinationInventory = member.GetComponent<BaseInventory>() ??
                    member.GetComponentInChildren<BaseInventory>() ?? member.gameObject.AddComponent<BaseInventory>();
                ai.target = member.transform;
                return;
            }
            return;
        }

        VehicleSpawnerFinal best = null;
        float bestDistance = Mathf.Infinity;
        foreach (VehicleSpawnerFinal factory in FindObjectsByType<VehicleSpawnerFinal>(FindObjectsSortMode.None))
        {
            if (factory.inventory == null || factory.baseCapture == null ||
                factory.baseCapture.structureType != StructureType.Factory || factory.baseCapture.GetOwner() != team)
                continue;
            float distance = Vector3.SqrMagnitude(transform.position - factory.transform.position);
            if (distance < bestDistance) { bestDistance = distance; best = factory; }
        }
        if (best != null)
        {
            ai.target = best.transform;
            destinationInventory = best.inventory;
            return;
        }

        // No allied factory yet: preserve the cargo by returning it to the
        // faction main base, where it is stored for later use.
        foreach (TeamMember member in FindObjectsByType<TeamMember>(FindObjectsSortMode.None))
        {
            if (member.team != team || member.gameObject.name.IndexOf("MainBase", System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            destinationInventory = member.GetComponent<BaseInventory>() ??
                member.GetComponentInChildren<BaseInventory>() ?? member.gameObject.AddComponent<BaseInventory>();
            ai.target = member.transform;
            return;
        }
    }

    void Update()
    {
        TruckCargo cargo = GetComponentInChildren<TruckCargo>();
        if (cargo != null && cargo.HasCargo() && destinationInventory != null && ai != null && ai.target != null &&
            Vector3.SqrMagnitude(transform.position - ai.target.position) < 144f)
            cargo.Unload(destinationInventory);
        if (cargo != null && !cargo.HasCargo() && !delivered)
        {
            delivered = true;
            Destroy(gameObject, 1f);
        }
        if (ai != null && ai.target == null)
            FindDelivery();
    }
}
