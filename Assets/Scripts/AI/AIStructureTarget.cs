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

        bool combatVehicle = GetComponent<AICombatAttack>() != null;

        if (combatVehicle && TryTargetNearestEnemyVehicle())
            return;

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

        // Scouts secure the economy. Combat units pressure a city first so
        // the ticket drain can end a match; when no city is available they
        // proceed to the enemy main base.
        CaptureTriggerAdvanced bestTarget = bestMine ?? bestStructure ?? enemyMainBase;

        if (combatVehicle)
        {
            CaptureTriggerAdvanced enemyCity = null;
            foreach (CaptureTriggerAdvanced candidate in allBases)
            {
                if (candidate != null && candidate.structureType == StructureType.City &&
                    candidate.currentOwner != teamMember.team)
                {
                    enemyCity = candidate;
                    break;
                }
            }
            bestTarget = enemyCity ?? enemyMainBase ?? bestStructure;
        }

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

    bool TryTargetNearestEnemyVehicle()
    {
        Health closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (Health candidate in FindObjectsByType<Health>(FindObjectsSortMode.None))
        {
            if (candidate == null || candidate.transform.root == transform.root)
                continue;

            TeamMember candidateTeam = candidate.GetComponentInParent<TeamMember>() ??
                candidate.transform.root.GetComponentInChildren<TeamMember>(true);
            if (candidateTeam == null || candidateTeam.team == Team.Neutral ||
                candidateTeam.team == teamMember.team)
                continue;

            float distance = (candidate.transform.position - transform.position).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = candidate;
            }
        }

        if (closest == null)
            return false;

        ai.target = closest.transform;
        return true;
    }
}
