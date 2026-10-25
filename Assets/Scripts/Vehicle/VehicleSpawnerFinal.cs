using UnityEngine;

public class VehicleSpawnerFinal : MonoBehaviour
{
    public static System.Action<GameObject, GameObject> VehicleReplaced;

    public static GameObject ReplaceWithLoadedTruck(GameObject oldVehicle, Team team, ResourceType resource)
    {
        VehicleSpawnerFinal[] spawners = FindObjectsByType<VehicleSpawnerFinal>(FindObjectsSortMode.None);
        VehicleBlueprint truckBlueprint = null;
        foreach (VehicleSpawnerFinal spawner in spawners)
        {
            if (spawner.availableBlueprints == null)
                continue;
            foreach (VehicleBlueprint blueprint in spawner.availableBlueprints)
            {
                if (blueprint != null && blueprint.role == VehicleRole.Truck && blueprint.prefab != null)
                {
                    truckBlueprint = blueprint;
                    break;
                }
            }
            if (truckBlueprint != null)
                break;
        }

        if (truckBlueprint == null)
        {
            Debug.LogWarning("Nenhum blueprint de caminhão foi encontrado para a mina.");
            return null;
        }

        // Leave the extraction point facing away from the Jeep's approach and
        // outside the trigger/model, rather than creating a truck on top of it.
        Quaternion exitRotation = oldVehicle.transform.rotation * Quaternion.Euler(0f, 180f, 0f);
        Vector3 exitDirection = exitRotation * Vector3.forward;
        Vector3 exitPosition = oldVehicle.transform.position + exitDirection * 8f;
        if (Physics.Raycast(exitPosition + Vector3.up * 8f, Vector3.down, out RaycastHit groundHit, 16f))
            exitPosition.y = groundHit.point.y + 0.15f;
        GameObject truck = Instantiate(truckBlueprint.prefab, exitPosition, exitRotation);
        TeamMember member = truck.GetComponentInChildren<TeamMember>();
        if (member != null)
            member.SetTeam(team);
        TruckCargo cargo = truck.GetComponentInChildren<TruckCargo>();
        if (cargo != null)
            cargo.Load(resource, 1);

        if (GameManager.Instance != null && GameManager.Instance.PlayerTeam == team)
        {
            VehicleController[] controllers = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
            foreach (VehicleController controller in controllers)
                controller.SetControlled(false);

            VehicleController truckController = truck.GetComponentInChildren<VehicleController>();
            if (truckController != null)
                truckController.SetControlled(true);

            VehicleOrbitCamera camera = FindFirstObjectByType<VehicleOrbitCamera>();
            if (camera != null)
            {
                Transform pivot = truck.transform.Find("CameraPivot");
                camera.target = pivot != null ? pivot : truck.transform;
            }
        }

        VehicleReplaced?.Invoke(oldVehicle, truck);
        Destroy(oldVehicle);
        return truck;
    }

    // Sends a loaded truck from a mine without replacing or taking control
    // away from the player's Jeep.
    public static GameObject SpawnAutoDeliveryTruck(GameObject jeep, Team team, ResourceType resource)
    {
        if (jeep == null)
            return null;

        VehicleBlueprint truckBlueprint = FindTruckBlueprint();
        if (truckBlueprint == null)
            return null;

        Quaternion rotation = jeep.transform.rotation * Quaternion.Euler(0f, 180f, 0f);
        Vector3 position = jeep.transform.position + (rotation * Vector3.forward) * 8f;
        if (Physics.Raycast(position + Vector3.up * 8f, Vector3.down, out RaycastHit hit, 16f))
            position.y = hit.point.y + 0.15f;

        GameObject truck = Instantiate(truckBlueprint.prefab, position, rotation);
        TeamMember member = truck.GetComponentInChildren<TeamMember>();
        if (member != null)
            member.SetTeam(team);
        TruckCargo cargo = truck.GetComponentInChildren<TruckCargo>();
        if (cargo != null)
            cargo.Load(resource, 1);

        VehicleMotor motor = truck.GetComponentInChildren<VehicleMotor>();
        if (motor != null)
            motor.usePlayerInput = false;
        TankMotor tankMotor = truck.GetComponentInChildren<TankMotor>();
        if (tankMotor != null)
            tankMotor.usePlayerInput = false;

        VehicleSimpleAI ai = truck.GetComponent<VehicleSimpleAI>() ?? truck.AddComponent<VehicleSimpleAI>();
        AITruckDelivery delivery = truck.GetComponent<AITruckDelivery>() ?? truck.AddComponent<AITruckDelivery>();
        delivery.Configure(ai, team);
        return truck;
    }

    static VehicleBlueprint FindTruckBlueprint()
    {
        foreach (VehicleSpawnerFinal spawner in FindObjectsByType<VehicleSpawnerFinal>(FindObjectsSortMode.None))
        {
            if (spawner.availableBlueprints == null)
                continue;
            foreach (VehicleBlueprint blueprint in spawner.availableBlueprints)
                if (blueprint != null && blueprint.role == VehicleRole.Truck && blueprint.prefab != null)
                    return blueprint;
        }
        Debug.LogWarning("Nenhum blueprint de caminhão foi encontrado para a mina.");
        return null;
    }
    [Header("Spawner Settings")]
    public VehicleBlueprint[] availableBlueprints;

    public Transform spawnPoint;

    [Header("Player Control")]
    public bool isPlayerSpawner = false;

    [HideInInspector]
    public bool playerInside = false;

    [Header("Base (Opcional)")]
    public CaptureTriggerAdvanced baseCapture;

    [Header("Fallback Team")]
    public Team playerTeam = Team.TeamA;

    [Header("Inventory")]
    public BaseInventory inventory;

    [Header("Camera")]
    public VehicleOrbitCamera cameraRig;

    [Header("Truck Resource")]
    public ResourceType truckResourceType = ResourceType.None;

    [Header("Artillery Positions")]
    public ArtilleryPositionManager artilleryManager;

    GameObject currentVehicle;

    // =========================================================
    // START
    // =========================================================
void Start()
{
    VehicleController vc =
        FindObjectOfType<VehicleController>();

    if (vc != null)
    {
        currentVehicle =
            vc.transform.root.gameObject;

        Debug.Log(
            "🚗 Veículo inicial registrado: " +
            currentVehicle.name
        );
    }

    Debug.Log(
        gameObject.name +
        " possui " +
        availableBlueprints.Length +
        " blueprints"
    );
}
    // =========================================================
    // UPDATE
    // =========================================================
    void Update()
    {
        // somente spawner ativo recebe input
        if (!isPlayerSpawner)
            return;

        if (Input.GetKeyDown(KeyCode.Z))
        {
            Debug.Log("🚙 Spawn Jeep");
            TrySpawn(VehicleRole.Jeep);
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("🛡️ Spawn Tank");
            TrySpawn(VehicleRole.Tank);
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            Debug.Log("🚀 Spawn Artillery");
        TrySpawn(VehicleRole.Artillery);
        }
    }

    // =========================================================
    // TRY SPAWN
    // =========================================================
    public void TrySpawn(VehicleRole desiredRole)
    {
        if (availableBlueprints == null || availableBlueprints.Length == 0)
        {
            Debug.LogError("❌ Nenhum blueprint disponível!");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("❌ GameManager não encontrado!");
            return;
        }

        if (!GameManager.Instance.MatchStarted)
        {
            Debug.Log("Confirme a seleção de equipe e blueprints antes de fabricar.");
            return;
        }

        // =====================================================
// TEAM
// =====================================================
        Team teamOwner = GameManager.Instance.PlayerTeam;

        if (teamOwner == Team.Neutral)
        {
            Debug.Log("Escolha Azul [1] ou Vermelho [2] antes de fabricar.");
            return;
        }

        Team baseOwner = baseCapture != null
            ? baseCapture.GetOwner()
            : playerTeam;

        if (baseOwner == Team.Neutral)
        {
            Debug.Log("Essa estrutura precisa ser capturada antes de fabricar.");
            return;
        }

        if (baseOwner != teamOwner)
        {
            Debug.Log("Essa estrutura pertence ao outro time.");
            return;
        }

        bool isFactory = baseCapture != null && baseCapture.structureType == StructureType.Factory;
        bool isMainBase = baseCapture == null || baseCapture.structureType == StructureType.MainBase;
        if ((desiredRole == VehicleRole.Tank || desiredRole == VehicleRole.Artillery) && !isFactory)
        {
            Debug.Log("Tanques e SPGs só podem ser produzidos em uma fábrica capturada.");
            return;
        }

    // =====================================================
// PROCURA BLUEPRINT
// =====================================================
VehicleBlueprint selected = null;

Debug.Log("========== BLUEPRINTS ==========");

foreach (VehicleBlueprint bp in availableBlueprints)
{
    if (bp == null)
    {
        Debug.Log("❌ Blueprint NULL");
        continue;
    }

    Debug.Log(
        "Blueprint: " +
        bp.name +
        " | ROLE: " +
        bp.role +
        " | Desired: " +
        desiredRole
    );

    if (bp.role == desiredRole)
    {
        Debug.Log("✅ MATCH ENCONTRADO");

        selected = bp;
        break;
    }
}

Debug.Log("================================");
        if (selected == null && isFactory)
        {
            foreach (VehicleSpawnerFinal other in FindObjectsByType<VehicleSpawnerFinal>(FindObjectsSortMode.None))
            {
                if (other.availableBlueprints == null) continue;
                foreach (VehicleBlueprint bp in other.availableBlueprints)
                    if (bp != null && bp.role == desiredRole && bp.prefab != null) { selected = bp; break; }
                if (selected != null) break;
            }
        }
        // =====================================================
        // NÃO ENCONTROU
        // =====================================================
        if (selected == null)
        {
            Debug.LogError(
                "❌ Blueprint não encontrado para: " +
                desiredRole
            );

            return;
        }

        if (!GameManager.Instance.IsBlueprintSelected(desiredRole))
        {
            Debug.Log("Este blueprint não foi selecionado para a partida.");
            return;
        }

        if (desiredRole == VehicleRole.Jeep && !isMainBase)
        {
            Debug.Log("Jipes só podem ser obtidos na base principal.");
            return;
        }
        if ((desiredRole == VehicleRole.Tank || desiredRole == VehicleRole.Artillery) && !isFactory)
        {
            Debug.Log("Tanques e SPGs só podem ser produzidos em uma fábrica capturada.");
            return;
        }
        if ((desiredRole == VehicleRole.Tank || desiredRole == VehicleRole.Artillery) && inventory == null)
        {
            Debug.Log("Esta fábrica não possui inventário local.");
            return;
        }
        ResourceType requiredResource = desiredRole == VehicleRole.Tank ? ResourceType.Steel :
            desiredRole == VehicleRole.Artillery ? ResourceType.Oil : ResourceType.None;
        if (requiredResource != ResourceType.None && !inventory.ConsumeResource(requiredResource, 1))
        {
            Debug.Log("Falta " + requiredResource + " nesta fábrica.");
            return;
        }

        // =====================================================
        // TRUCK
        // =====================================================
        if (desiredRole == VehicleRole.Truck)
        {
            bool unlocked =
                (teamOwner == Team.TeamA)
                ? GameManager.Instance.teamATruckUnlocked
                : GameManager.Instance.teamBTruckUnlocked;

            if (!unlocked)
            {
                Debug.Log("🚫 Truck não desbloqueado");
                return;
            }
        }

        // =====================================================
        // TANK
        // =====================================================
        if (false && desiredRole == VehicleRole.Tank)
        {
            bool unlocked =
                (teamOwner == Team.TeamA)
                ? GameManager.Instance.teamATankUnlocked
                : GameManager.Instance.teamBTankUnlocked;

            if (!unlocked)
            {
                Debug.Log("🚫 Tank não desbloqueado");
                return;
            }

            if (inventory == null)
            {
                Debug.LogError("❌ Inventory não atribuída");
                return;
            }

            // =====================================
            // CHECK COSTS
            // =====================================
            foreach (var cost in selected.costs)
            {
                bool has =
                    inventory.HasResource(
                        cost.resource,
                        cost.amount
                    );

                if (!has)
                {
                    Debug.Log(
                        "🚫 Falta recurso: " +
                        cost.resource
                    );

                    return;
                }
            }

            // =====================================
            // CONSUME COSTS
            // =====================================
            foreach (var cost in selected.costs)
            {
                inventory.RemoveResource(
                    cost.resource,
                    cost.amount
                );

                Debug.Log(
                    "🧱 Consumido: " +
                    cost.resource
                );
            }

            Debug.Log("🛡️ Recursos do Tank consumidos");
        }

        // =====================================================
        // SPAWN
        // =====================================================
        if (desiredRole == VehicleRole.Artillery && artilleryManager != null)
{
    SpawnStaticArtillery(
        selected,
        teamOwner
    );
}
else
{
    SpawnVehicle(
        selected,
        teamOwner
    );
}
    }

    public string GetSpawnStatus(VehicleRole role)
    {
        bool isFactory = baseCapture != null && baseCapture.structureType == StructureType.Factory;
        bool isMainBase = baseCapture == null || baseCapture.structureType == StructureType.MainBase;
        if (role == VehicleRole.Jeep)
            return isMainBase ? "Jipe: disponível (sem custo)" : "Jipe: apenas na base principal";
        if (role == VehicleRole.Tank)
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsBlueprintSelected(role))
                return "Tanque: blueprint não selecionado";
            return !isFactory ? "Tanque: apenas na fábrica" : "Tanque: " + (inventory != null && inventory.HasResource(ResourceType.Steel, 1) ? "disponível" : "falta 1 aço");
        }
        if (role == VehicleRole.Artillery)
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsBlueprintSelected(role))
                return "SPG: blueprint não selecionado";
            return !isFactory ? "SPG: apenas na fábrica" : "SPG: " + (inventory != null && inventory.HasResource(ResourceType.Oil, 1) ? "disponível" : "falta 1 óleo");
        }
        return "Caminhão: obtido apenas nas minas";
    }

     // =====================================================
        // SPAWN ARTILERY ZONE
        // =====================================================
void SpawnStaticArtillery(
    VehicleBlueprint blueprint,
    Team team
)
{
    if (artilleryManager == null)
    {
        Debug.LogError(
            "❌ ArtilleryManager não atribuído"
        );
        return;
    }

    ArtilleryPosition pos =
        artilleryManager.GetFreePosition();

    if (pos == null)
    {
        Debug.Log("🚫 Nenhuma posição livre");
        return;
    }

    GameObject vehicle =
        Instantiate(
            blueprint.prefab,
            pos.spawnPoint.position,
            pos.spawnPoint.rotation
        );

    // =====================================
    // REGISTRA POSIÇÃO
    // =====================================
    pos.Occupy(vehicle);

    // =====================================
    // TEAM
    // =====================================
    TeamMember tm =
        vehicle.GetComponentInChildren<TeamMember>();

    if (tm != null)
    {
        tm.SetTeam(team);
    }

    // =====================================
// CONTROLE
// =====================================
VehicleController vc =
    vehicle.GetComponentInChildren<VehicleController>();

if (vc != null)
{
    vc.SetControlled(true);
}

DeployableVehicle deploy =
    vehicle.GetComponentInChildren<DeployableVehicle>();

if (deploy != null)
{
    deploy.canDeploy = true;

    deploy.DeployInstant();
}
else
{
    Debug.LogError(
        "❌ DeployableVehicle não encontrado!"
    );

}
    // =====================================
    // REFERÊNCIA
    // =====================================
    currentVehicle = vehicle;

    // =====================================
    // CAMERA
    // =====================================
    SetupCamera(
        vehicle,
        blueprint
    );

    Debug.Log(
        "🏹 Artilharia posicionada em " +
        pos.name
    );
}

void SetupCamera(
    GameObject vehicle,
    VehicleBlueprint blueprint
)
{
    if (cameraRig == null)
        return;

    Transform target = vehicle.transform;

    Transform pivot = null;

    foreach (
        Transform t
        in vehicle.GetComponentsInChildren<Transform>()
    )
    {
        if (t.name == "CameraPivot")
        {
            pivot = t;
            break;
        }
    }

    if (pivot != null)
    {
        target = pivot;
    }

    cameraRig.target = target;

    foreach (
        Transform t
        in vehicle.GetComponentsInChildren<Transform>()
    )
    {
        if (t.name == "DriverView")
            cameraRig.driverView = t;

        if (t.name == "GunView")
            cameraRig.gunView = t;
    }

    cameraRig.SetDefaultDistance(
        blueprint.cameraDistance
    );

    cameraRig.SetFOV(
        blueprint.cameraFOV
    );

    Debug.Log("🎯 Camera configurada");
}

    // =========================================================
    // SPAWN VEHICLE
    // =========================================================
    void SpawnVehicle(
        VehicleBlueprint blueprint,
        Team team
    )
    {
        Debug.Log("🚀 INÍCIO DO SPAWN");

        // =====================================================
        // REMOVE VEÍCULO ANTIGO
        // =====================================================
        if (currentVehicle != null)
        {
            // =========================
            // TEAM -> NEUTRAL
            // =========================
            TeamMember oldTeam =
                currentVehicle.GetComponentInChildren<TeamMember>();

            if (oldTeam != null)
            {
                oldTeam.SetTeam(Team.Neutral);

                Debug.Log("⚪ Veículo antigo virou Neutral");
            }

            // =========================
            // DESATIVA CONTROLE
            // =========================
            VehicleController oldController =
                currentVehicle.GetComponentInChildren<VehicleController>();

            if (oldController != null)
            {
                oldController.SetControlled(false);
            }

            // =========================
            // DESLIGA INPUTS/ARMAS
            // =========================
            MonoBehaviour[] scripts =
                currentVehicle.GetComponentsInChildren<MonoBehaviour>();

            foreach (MonoBehaviour mb in scripts)
            {
                if (mb == null)
                    continue;

                string scriptName = mb.GetType().Name;

                if (
                    scriptName.Contains("Controller") ||
                    scriptName.Contains("Weapon") ||
                    scriptName.Contains("Turret")
                )
                {
                    mb.enabled = false;
                }
            }

            Debug.Log(
                "💀 Veículo antigo removido: " +
                currentVehicle.name
            );
        }

        // =====================================================
        // VALIDAÇÕES
        // =====================================================
        if (spawnPoint == null)
        {
            Debug.LogError("❌ SpawnPoint não definido!");
            return;
        }

        if (blueprint == null || blueprint.prefab == null)
        {
            Debug.LogError("❌ Blueprint inválido!");
            return;
        }

        // =====================================================
        // POSIÇÃO
        // =====================================================
        Vector3 spawnPos = spawnPoint.position;
        Quaternion spawnRotation = spawnPoint.rotation;

        Vector3 outward = spawnPoint.position - transform.position;
        outward.y = 0f;
        if (outward.sqrMagnitude > 0.01f)
            spawnRotation = Quaternion.LookRotation(outward.normalized, Vector3.up);

        if (
            Physics.Raycast(
                spawnPos + Vector3.up * 5f,
                Vector3.down,
                out RaycastHit hit,
                10f
            )
        )
        {
            spawnPos.y = hit.point.y;
        }

        // =====================================================
        // INSTANTIATE
        // =====================================================
        currentVehicle = Instantiate(
            blueprint.prefab,
            spawnPos,
            spawnRotation
        );

        Debug.Log(
            "✅ VEÍCULO INSTANCIADO: " +
            currentVehicle.name
        );

        foreach (MonoBehaviour weapon in currentVehicle.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (weapon != null && weapon.GetType().Name.Contains("TankGun") &&
                weapon.gameObject.name.IndexOf("Sec", System.StringComparison.OrdinalIgnoreCase) >= 0)
                weapon.enabled = false;
        }

        // =====================================================
        // TEAM
        // =====================================================
        TeamMember tm =
            currentVehicle.GetComponentInChildren<TeamMember>();

        if (tm != null)
        {
            tm.SetTeam(team);

            Debug.Log("🎨 Team aplicado: " + team);
        }
        else
        {
            Debug.LogWarning("⚠️ TeamMember não encontrado");
        }

        // =====================================================
        // DESATIVA CONTROLE DE TODOS
        // =====================================================
        VehicleController[] allVehicles =
            FindObjectsOfType<VehicleController>();

        foreach (VehicleController v in allVehicles)
        {
            if (v != null)
            {
                v.SetControlled(false);
            }
        }

        // =====================================================
        // ATIVA CONTROLE DO NOVO
        // =====================================================
        VehicleController vc =
            currentVehicle.GetComponentInChildren<VehicleController>();

        if (vc != null)
        {
            vc.SetControlled(true);

            Debug.Log("🎮 Controle ativado");
        }
        else
        {
            Debug.LogWarning("⚠️ VehicleController não encontrado");
        }

        // =====================================================
        // TRUCK CARGO
        // =====================================================
        TruckCargo cargo =
            currentVehicle.GetComponentInChildren<TruckCargo>();

        if (
            cargo != null &&
            blueprint.role == VehicleRole.Truck
        )
        {
            ResourceType resource = truckResourceType;

            if (resource != ResourceType.None)
            {
                cargo.Load(resource, 1);

                Debug.Log(
                    "📦 Truck nasceu carregado: " +
                    resource
                );
            }
            else
            {
                Debug.LogWarning(
                    "⚠️ Nenhum recurso atribuído ao truck"
                );
            }
        }

        // =====================================================
        // CAMERA
        // =====================================================
        if (cameraRig == null)
        {
            Debug.LogError("❌ CameraRig não atribuída!");
            return;
        }

        Transform target = currentVehicle.transform;

        Transform pivot = null;

        foreach (
            Transform t
            in currentVehicle.GetComponentsInChildren<Transform>()
        )
        {
            if (t.name == "CameraPivot")
            {
                pivot = t;
                break;
            }
        }

        if (pivot != null)
        {
            target = pivot;

            Debug.Log("🎯 Usando CameraPivot");
        }
        else
        {
            Debug.LogWarning(
                "⚠️ CameraPivot não encontrado"
            );
        }

        cameraRig.target = target;

        // =====================================================
        // DRIVER + GUN VIEW
        // =====================================================
        foreach (
            Transform t
            in currentVehicle.GetComponentsInChildren<Transform>()
        )
        {
            if (t.name == "DriverView")
            {
                cameraRig.driverView = t;
            }

            if (t.name == "GunView")
            {
                cameraRig.gunView = t;
            }
        }

        // =====================================================
        // CAMERA SETTINGS
        // =====================================================
        cameraRig.SetDefaultDistance(
            blueprint.cameraDistance
        );

        cameraRig.SetFOV(
            blueprint.cameraFOV
        );

        Debug.Log("🎯 Camera configurada");
    }
}
