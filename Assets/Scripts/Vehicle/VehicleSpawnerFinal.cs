using UnityEngine;

public class VehicleSpawnerFinal : MonoBehaviour
{
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

        if (Input.GetKeyDown(KeyCode.X))
        {
            Debug.Log("🚚 Spawn Truck");
            TrySpawn(VehicleRole.Truck);
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
    void TrySpawn(VehicleRole desiredRole)
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

        // =====================================================
// TEAM
// =====================================================
Team teamOwner = playerTeam;

// se existir capture usa o dono da base
if (baseCapture != null)
{
    Team capturedOwner =
        baseCapture.GetOwner();

    // só substitui se NÃO for neutro
    if (capturedOwner != Team.Neutral)
    {
        teamOwner = capturedOwner;
    }
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
        if (desiredRole == VehicleRole.Tank)
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
        if (desiredRole == VehicleRole.Artillery)
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

// =====================================
// DEPLOYED AO NASCER
// =====================================
DeployableVehicle deploy =
    vehicle.GetComponentInChildren<DeployableVehicle>();

if (deploy != null)
{
    deploy.canDeploy = true;

    deploy.Deploy();

    Debug.Log(
        "🏹 SPG nasceu DEPLOYED"
    );
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
            spawnPoint.rotation
        );

        Debug.Log(
            "✅ VEÍCULO INSTANCIADO: " +
            currentVehicle.name
        );

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