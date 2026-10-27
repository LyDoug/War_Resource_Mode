using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn")]
    public GameObject vehiclePrefab;

    public Transform spawnPoint;

    [Header("Team")]
    public Team enemyTeam = Team.TeamB;

    [Header("Timer")]
    public float spawnInterval = 20f;
    [Min(1f)] public float commanderInterval = 3f;

    public int maxVehicles = 5;
    bool started;
    GameObject activeEnemy;
    float nextScoutSpawnTime;

    // =====================================================
    // START
    // =====================================================
    void Start()
    {
        // The player chooses a side first; this component then controls the
        // opposing faction.  This prevents both factions being player input.
    }

    void OnEnable()
    {
        VehicleSpawnerFinal.VehicleReplaced += HandleVehicleReplacement;
    }

    void OnDisable()
    {
        VehicleSpawnerFinal.VehicleReplaced -= HandleVehicleReplacement;
    }

    void HandleVehicleReplacement(GameObject previous, GameObject replacement)
    {
        if (previous == activeEnemy)
            activeEnemy = replacement;
    }

    // =====================================================
    // SPAWN
    // =====================================================
    void SpawnEnemy()
    {
        if (GameManager.Instance == null || !GameManager.Instance.MatchStarted ||
            GameManager.Instance.PlayerTeam == Team.Neutral)
            return;

        // One AI represents one opposing player. Its current vehicle can be a
        // scout, loaded truck, tank or SPG, but never more than one at once.
        if (activeEnemy != null)
        {
            TryUpgradeScoutAtFactory();
            return;
        }

        if (Time.time < nextScoutSpawnTime)
            return;

        enemyTeam = GameManager.Instance.PlayerTeam == Team.TeamA ? Team.TeamB : Team.TeamA;

        // A captured factory with stock takes priority over a free Scout.
        if (TrySpawnCombatUnit())
            return;

        if (vehiclePrefab == null)
        {
            Debug.LogWarning(
                "⚠️ Vehicle Prefab não atribuído"
            );

            return;
        }

        Transform resolvedSpawnPoint = GetSpawnPointForTeam(enemyTeam);
        if (resolvedSpawnPoint == null)
        {
            Debug.LogWarning(
                "⚠️ Spawn Point não atribuído"
            );

            return;
        }

        // =====================================
        // INSTANTIATE
        // =====================================
        GameObject obj =
            Instantiate(
                vehiclePrefab,
                resolvedSpawnPoint.position,
                resolvedSpawnPoint.rotation
            );

        activeEnemy = obj;
        nextScoutSpawnTime = Time.time + 2f;

        ConfigureAIUnit(obj, null);

        Debug.Log(
            "🤖 Enemy spawned: " +
            obj.name
        );
    }

    bool TrySpawnCombatUnit()
    {
        VehicleSpawnerFinal factory = null;
        VehicleRole role = VehicleRole.Jeep;

        foreach (VehicleSpawnerFinal candidate in FindObjectsByType<VehicleSpawnerFinal>(FindObjectsSortMode.None))
        {
            if (candidate.baseCapture == null ||
                candidate.baseCapture.structureType != StructureType.Factory ||
                candidate.baseCapture.GetOwner() != enemyTeam || candidate.inventory == null ||
                candidate.spawnPoint == null)
                continue;

            // Prefer a tank when both resources are ready; it keeps the
            // opposing force mobile. SPG is the fallback oil expenditure.
            if (candidate.inventory.HasResource(ResourceType.Steel, 1))
            {
                factory = candidate;
                role = VehicleRole.Tank;
                break;
            }
            if (factory == null && candidate.inventory.HasResource(ResourceType.Oil, 1))
            {
                factory = candidate;
                role = VehicleRole.Artillery;
            }
        }

        if (factory == null)
            return false;

        VehicleBlueprint blueprint = FindBlueprint(factory, role);
        ResourceType cost = role == VehicleRole.Tank ? ResourceType.Steel : ResourceType.Oil;
        if (blueprint == null || !factory.inventory.ConsumeResource(cost, 1))
            return false;

        GameObject unit = Instantiate(blueprint.prefab, factory.spawnPoint.position, factory.spawnPoint.rotation);
        activeEnemy = unit;
        ConfigureAIUnit(unit, blueprint);
        Debug.Log("IA produziu " + role + " na fábrica " + factory.name);
        return true;
    }

    void TryUpgradeScoutAtFactory()
    {
        if (activeEnemy == null || activeEnemy.GetComponentInChildren<TruckCargo>() != null)
            return;

        foreach (VehicleSpawnerFinal factory in FindObjectsByType<VehicleSpawnerFinal>(FindObjectsSortMode.None))
        {
            if (factory.baseCapture == null || factory.baseCapture.structureType != StructureType.Factory ||
                factory.baseCapture.GetOwner() != enemyTeam || factory.inventory == null)
                continue;

            Vector3 offset = activeEnemy.transform.position - factory.baseCapture.GetWorldCenter();
            offset.y = 0f;
            if (offset.sqrMagnitude > 15f * 15f ||
                (!factory.inventory.HasResource(ResourceType.Steel, 1) && !factory.inventory.HasResource(ResourceType.Oil, 1)))
                continue;

            Destroy(activeEnemy);
            activeEnemy = null;
            TrySpawnCombatUnit();
            return;
        }
    }

    VehicleBlueprint FindBlueprint(VehicleSpawnerFinal factory, VehicleRole role)
    {
        if (factory.availableBlueprints != null)
            foreach (VehicleBlueprint blueprint in factory.availableBlueprints)
                if (blueprint != null && blueprint.role == role && blueprint.prefab != null)
                    return blueprint;

        foreach (VehicleSpawnerFinal candidate in FindObjectsByType<VehicleSpawnerFinal>(FindObjectsSortMode.None))
        {
            if (candidate.availableBlueprints == null)
                continue;
            foreach (VehicleBlueprint blueprint in candidate.availableBlueprints)
                if (blueprint != null && blueprint.role == role && blueprint.prefab != null)
                    return blueprint;
        }
        return null;
    }

    void ConfigureAIUnit(GameObject unit, VehicleBlueprint blueprint)
    {
        if (blueprint != null)
            VehicleSpawnerFinal.ConfigureVehicleDurability(unit, blueprint.role);

        TeamMember tm = unit.GetComponentInChildren<TeamMember>();
        if (tm != null)
            tm.SetTeam(enemyTeam);

        if (unit.GetComponent<VehicleSimpleAI>() == null)
            unit.AddComponent<VehicleSimpleAI>();
        if (unit.GetComponent<AIStructureTarget>() == null)
            unit.AddComponent<AIStructureTarget>();

        // Scouts capture/extract; produced combat vehicles hunt enemy units,
        // then pressure cities and the opposing main base.
        bool combatRole = blueprint != null &&
            (blueprint.role == VehicleRole.Tank || blueprint.role == VehicleRole.Artillery ||
             blueprint.role == VehicleRole.APC);
        if (combatRole && unit.GetComponent<AICombatAttack>() == null)
        {
            AICombatAttack attack = unit.AddComponent<AICombatAttack>();
            if (unit.GetComponentInChildren<DeployableVehicle>() != null)
            {
                attack.attackRange = 55f;
                attack.damagePerShot = 60f;
            }
        }

        VehicleMotor motor = unit.GetComponentInChildren<VehicleMotor>();
        if (motor != null)
            motor.usePlayerInput = false;
        TankMotor tankMotor = unit.GetComponentInChildren<TankMotor>();
        if (tankMotor != null)
            tankMotor.usePlayerInput = false;
    }

    Transform GetSpawnPointForTeam(Team team)
    {
        foreach (VehicleSpawnerFinal spawner in FindObjectsByType<VehicleSpawnerFinal>(FindObjectsSortMode.None))
        {
            if (spawner.playerTeam == team && spawner.spawnPoint != null &&
                spawner.gameObject.name.IndexOf("MainBase", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return spawner.spawnPoint;
        }

        return spawnPoint;
    }

    void Update()
    {
        if (started || GameManager.Instance == null || !GameManager.Instance.MatchStarted ||
            GameManager.Instance.PlayerTeam == Team.Neutral)
            return;

        started = true;
        InvokeRepeating(nameof(SpawnEnemy), 1f, commanderInterval);
    }
}
