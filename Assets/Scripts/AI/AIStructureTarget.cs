using UnityEngine;

public class AIStructureTarget : MonoBehaviour
{
    [Header("Search")]
    public float refreshRate = 5f;

    VehicleSimpleAI ai;

    TeamMember teamMember;

    // =====================================================
    // START
    // =====================================================
    void Start()
    {
        ai =
            GetComponent<VehicleSimpleAI>();

        // TeamMember is placed on the visual child of the vehicle prefabs.
        teamMember = GetComponentInChildren<TeamMember>();

        InvokeRepeating(
            nameof(FindTarget),
            1f,
            refreshRate
        );
    }

    // =====================================================
    // FIND TARGET
    // =====================================================
    void FindTarget()
    {
        if (ai == null)
            return;

        if (teamMember == null)
            return;

        bool combatVehicle = GetComponentInChildren<TankMotor>() != null ||
            GetComponentInChildren<DeployableVehicle>() != null;

        // A Scout with factory stock returns to manufacture first. Combat
        // vehicles skip economy and pressure the opposing main base.
        if (!combatVehicle)
        {
            foreach (VehicleSpawnerFinal factory in FindObjectsByType<VehicleSpawnerFinal>(FindObjectsSortMode.None))
            {
                if (factory.baseCapture != null && factory.baseCapture.structureType == StructureType.Factory &&
                    factory.baseCapture.GetOwner() == teamMember.team && factory.inventory != null &&
                    (factory.inventory.HasResource(ResourceType.Steel, 1) || factory.inventory.HasResource(ResourceType.Oil, 1)))
                {
                    ai.target = factory.baseCapture.transform;
                    return;
                }
            }
        }

        CaptureTriggerAdvanced[] allBases =
            FindObjectsOfType<CaptureTriggerAdvanced>();

        CaptureTriggerAdvanced bestMine = null;
        CaptureTriggerAdvanced bestStructure = null;
        CaptureTriggerAdvanced enemyMainBase = null;
        float bestMineDistance = Mathf.Infinity;
        float bestStructureDistance = Mathf.Infinity;

        foreach (var baseTarget in allBases)
        {
            if (baseTarget == null)
                continue;

            if (baseTarget.baseScript != null && baseTarget.baseScript.IsDestroyed())
                continue;

            // ignora bases do próprio time
            if (
                baseTarget.currentOwner ==
                teamMember.team
            )
            {
                continue;
            }

            float dist = Vector3.Distance(transform.position, baseTarget.transform.position);

            if (baseTarget.structureType == StructureType.Mine && dist < bestMineDistance)
            {
                bestMineDistance = dist;
                bestMine = baseTarget;
            }
            else if (baseTarget.structureType == StructureType.MainBase)
            {
                enemyMainBase = baseTarget;
            }
            else if (dist < bestStructureDistance)
            {
                bestStructureDistance = dist;
                bestStructure = baseTarget;
            }
        }

        // Economy first: secure a mine, then a factory/city. Once those are
        // owned, push towards the opponent's main base.
        CaptureTriggerAdvanced bestTarget = bestMine ?? bestStructure ?? enemyMainBase;

        if (combatVehicle && enemyMainBase != null)
            bestTarget = enemyMainBase;

        // aplica target
        if (bestTarget != null)
        {
            ai.target =
                bestTarget.transform;

            Debug.Log(
                "🎯 Novo alvo AI: " +
                bestTarget.name
            );
        }
    }
}
