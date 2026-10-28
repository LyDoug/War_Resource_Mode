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
        exitPosition = FindSafeTruckSpawnPosition(exitPosition, exitRotation);
        GameObject truck = Instantiate(truckBlueprint.prefab, exitPosition, exitRotation);
        TeamMember member = truck.GetComponentInChildren<TeamMember>();
        if (member != null)
            member.SetTeam(team);
        TruckCargo cargo = truck.GetComponentInChildren<TruckCargo>();
        if (cargo != null)
        {
            cargo.Load(resource, 1);
            cargo.BeginSafeSpawn();
        }

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
        position = FindSafeTruckSpawnPosition(position, rotation);

        GameObject truck = Instantiate(truckBlueprint.prefab, position, rotation);
        TeamMember member = truck.GetComponentInChildren<TeamMember>();
        if (member != null)
            member.SetTeam(team);
        TruckCargo cargo = truck.GetComponentInChildren<TruckCargo>();
        if (cargo != null)
        {
            cargo.Load(resource, 1);
            cargo.BeginSafeSpawn();
        }

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
    public static Vector3 FindSafeTruckSpawnPosition(Vector3 preferred, Quaternion rotation)
    {
        Vector3[] localOffsets =
        {
            Vector3.zero,
            Vector3.forward * 6f,
            Vector3.right * 6f,
            Vector3.left * 6f,
            Vector3.back * 6f,
            (Vector3.forward + Vector3.right).normalized * 9f,
            (Vector3.forward + Vector3.left).normalized * 9f
        };

        foreach (Vector3 localOffset in localOffsets)
        {
            Vector3 candidate = preferred + rotation * localOffset;
            if (!HasVehicleNear(candidate, 5.5f))
                return SetTruckSpawnHeight(candidate);
        }

        // Spawn immunity still protects the rare case where every exit point
        // is occupied by a vehicle.
        return SetTruckSpawnHeight(preferred);
    }

    static bool HasVehicleNear(Vector3 position, float radius)
    {
        foreach (VehicleController vehicle in FindObjectsByType<VehicleController>(FindObjectsSortMode.None))
        {
            if (vehicle == null)
                continue;

            Vector3 flatOffset = vehicle.transform.root.position - position;
            flatOffset.y = 0f;
            if (flatOffset.sqrMagnitude < radius * radius)
                return true;
        }
        return false;
    }

    static Vector3 SetTruckSpawnHeight(Vector3 position)
    {
        if (Physics.Raycast(position + Vector3.up * 12f, Vector3.down, out RaycastHit hit,
                24f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            position.y = hit.point.y + 0.15f;
        return position;
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

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("🚙 Spawn Jeep");
            TrySpawn(VehicleRole.Jeep);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("🛡️ Spawn Tank");
            TrySpawn(GameManager.Instance.GetSelectedFactoryBlueprint(0));
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log("🚀 Spawn Artillery");
        TrySpawn(GameManager.Instance.GetSelectedFactoryBlueprint(1));
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
        bool canProduceAtMainBase = isMainBase && desiredRole == VehicleRole.Artillery;
        if (IsFactoryBlueprint(desiredRole) && !isFactory && !canProduceAtMainBase)
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
        if (selected == null && (isFactory || canProduceAtMainBase))
        {
            foreach (VehicleSpawnerFinal other in FindObjectsByType<VehicleSpawnerFinal>(FindObjectsSortMode.None))
            {
                if (other.availableBlueprints == null) continue;
                foreach (VehicleBlueprint bp in other.availableBlueprints)
                    if (bp != null && bp.role == desiredRole && bp.prefab != null) { selected = bp; break; }
                if (selected != null) break;
            }
        }
        if (selected == null && (isFactory || canProduceAtMainBase))
        {
            foreach (VehicleBlueprint bp in Resources.LoadAll<VehicleBlueprint>("Blueprints"))
                if (bp != null && bp.role == desiredRole && bp.prefab != null) { selected = bp; break; }
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
        if (IsFactoryBlueprint(desiredRole) && !isFactory && !canProduceAtMainBase)
        {
            Debug.Log("Tanques e SPGs só podem ser produzidos em uma fábrica capturada.");
            return;
        }
        if (IsFactoryBlueprint(desiredRole) && inventory == null)
        {
            Debug.Log("Esta fábrica não possui inventário local.");
            return;
        }
        ResourceType requiredResource = desiredRole == VehicleRole.Tank || desiredRole == VehicleRole.APC ? ResourceType.Steel :
            desiredRole == VehicleRole.Artillery || desiredRole == VehicleRole.UAV ? ResourceType.Oil : ResourceType.None;
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
        if (desiredRole == VehicleRole.Artillery && (artilleryManager != null || isMainBase))
{
    if (artilleryManager == null && isMainBase)
        artilleryManager = EnsureMainBaseArtilleryPositions();
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

    // Used by the hold-J ejection flow. The abandoned vehicle remains in the
    // world, so it must not be treated as the previous vehicle to neutralize.
    public void SpawnFreshVehicle(VehicleRole role)
    {
        currentVehicle = null;
        TrySpawn(role);
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
            return !isFactory && !isMainBase ? "SPG: apenas na fábrica/base principal" : "SPG: " + (inventory != null && inventory.HasResource(ResourceType.Oil, 1) ? "disponível" : "falta 1 óleo");
        }
        return "Caminhão: obtido apenas nas minas";
    }

    static bool IsFactoryBlueprint(VehicleRole role)
    {
        return role == VehicleRole.Tank || role == VehicleRole.Artillery ||
               role == VehicleRole.APC || role == VehicleRole.UAV;
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

    ConfigureVehicleDurability(vehicle, VehicleRole.Artillery);

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

    ArtilleryPositionManager EnsureMainBaseArtilleryPositions()
    {
        ArtilleryPositionManager existing = GetComponentInChildren<ArtilleryPositionManager>(true);
        if (existing != null)
            return existing;

        GameObject holder = new GameObject("Main Base Artillery Positions");
        holder.transform.SetParent(transform, false);
        ArtilleryPositionManager manager = holder.AddComponent<ArtilleryPositionManager>();
        manager.positions = new ArtilleryPosition[2];
        Vector3[] offsets = { new Vector3(-12f, 0f, 12f), new Vector3(12f, 0f, 12f) };
        for (int index = 0; index < offsets.Length; index++)
        {
            GameObject positionObject = new GameObject("SPG Position " + (index + 1));
            positionObject.transform.SetParent(holder.transform, false);
            positionObject.transform.position = transform.TransformPoint(offsets[index]);
            positionObject.transform.rotation = transform.rotation;
            if (Physics.Raycast(positionObject.transform.position + Vector3.up * 20f, Vector3.down, out RaycastHit hit, 40f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                positionObject.transform.position = hit.point;
            ArtilleryPosition position = positionObject.AddComponent<ArtilleryPosition>();
            position.spawnPoint = positionObject.transform;
            manager.positions[index] = position;
        }
        return manager;
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
        if (string.Equals(t.name, "DriverView", System.StringComparison.OrdinalIgnoreCase))
            cameraRig.driverView = t;

        if (string.Equals(t.name, "GunView", System.StringComparison.OrdinalIgnoreCase))
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
    static void RaiseVehicleVisualAboveGround(GameObject vehicle, float groundHeight, float clearance)
    {
        Renderer[] renderers = vehicle.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        float lowest = float.PositiveInfinity;
        foreach (Renderer renderer in renderers)
            if (renderer.enabled)
                lowest = Mathf.Min(lowest, renderer.bounds.min.y);
        if (float.IsPositiveInfinity(lowest))
            return;

        float desiredBottom = groundHeight + clearance;
        if (lowest < desiredBottom)
            vehicle.transform.position += Vector3.up * (desiredBottom - lowest);
    }

    void EnsureRuntimeVehicleComponents(GameObject vehicle, VehicleBlueprint blueprint)
    {
        if (vehicle.GetComponentInChildren<VehicleController>() != null)
            return;

        Renderer[] renderers = vehicle.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int index = 1; index < renderers.Length; index++)
            bounds.Encapsulate(renderers[index].bounds);

        // Unity components can be "fake null" after a prefab override. Do
        // not use ?? here: it only checks the CLR reference and can return a
        // destroyed BoxCollider, causing the MissingComponentException.
        BoxCollider collider = vehicle.GetComponent<BoxCollider>();
        if (collider == null)
            collider = vehicle.AddComponent<BoxCollider>();
        collider.center = vehicle.transform.InverseTransformPoint(bounds.center);
        Vector3 localSize = vehicle.transform.InverseTransformVector(bounds.size);
        collider.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));

        Rigidbody body = vehicle.GetComponent<Rigidbody>();
        if (body == null)
            body = vehicle.AddComponent<Rigidbody>();
        body.mass = blueprint.mass;
        body.linearDamping = 1.5f;
        body.angularDamping = 8f;
        body.maxDepenetrationVelocity = 3f;
        body.constraints = RigidbodyConstraints.FreezePositionY |
            RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        TeamMember teamMember = vehicle.GetComponent<TeamMember>();
        if (teamMember == null)
            teamMember = vehicle.AddComponent<TeamMember>();
        VehicleMotor motor = vehicle.GetComponent<VehicleMotor>();
        if (motor == null)
            motor = vehicle.AddComponent<VehicleMotor>();
        motor.blueprint = blueprint;
        VehicleController controller = vehicle.GetComponent<VehicleController>();
        if (controller == null)
            controller = vehicle.AddComponent<VehicleController>();
    }

    void ConfigureRoleEquipment(GameObject vehicle, VehicleBlueprint blueprint)
    {
        if (blueprint.role == VehicleRole.Tank)
        {
            foreach (TankGunBallistic tankGun in vehicle.GetComponentsInChildren<TankGunBallistic>(true))
            {
                bool secondary = tankGun.firePoint != null &&
                    tankGun.firePoint.name.IndexOf("FirePointSec", System.StringComparison.OrdinalIgnoreCase) >= 0;
                tankGun.useSecondaryInput = secondary;
                tankGun.enabled = true;

                if (secondary)
                {
                    // Coaxial .50 cal: light, sustained secondary fire.
                    tankGun.shellPrefab = Resources.Load<ProjectileBallistic>("Projectiles/Shell_50cal");
                    tankGun.fireRate = 4f;
                    tankGun.shellSpeed = 150f;
                    tankGun.currentShellDamage = 8f;
                    tankGun.recoilDistance = 0.02f;
                    tankGun.muzzleFlashScale = 0.35f;
                }
                else
                {
                    tankGun.fireRate = blueprint.fireRate;
                    tankGun.shellSpeed = blueprint.shellSpeed;
                    tankGun.currentShellDamage = blueprint.shellDamage;
                    tankGun.recoilDistance = blueprint.recoilDistance;
                }
            }
        }

        // The Jeep receives a defensive .50 cal. It fires a single low-damage
        // round every few seconds, so it can disengage without replacing its
        // scout role.
        if (blueprint.role == VehicleRole.Jeep)
        {
            // Jeeps are forward observers.  The component is added at spawn
            // time so every player-controlled Jeep can deploy a smoke marker
            // without requiring a hand-edited prefab.
            if (vehicle.GetComponent<ArtillerySpotter>() == null)
                vehicle.AddComponent<ArtillerySpotter>();

            IFVAutocannon jeepGun = vehicle.GetComponent<IFVAutocannon>();
            if (jeepGun == null)
                jeepGun = vehicle.AddComponent<IFVAutocannon>();
            jeepGun.generatedFirePointName = "Jeep_FirePoint";
            jeepGun.ConfigureFromVehicleBounds();
            jeepGun.rearFireOnly = true;
            jeepGun.fireRate = 1f;
            jeepGun.projectileSpeed = 125f;
            jeepGun.damage = 8f;
            jeepGun.projectileScale = 0.75f;
            jeepGun.shotsPerBurst = 3;
            jeepGun.burstShotInterval = 0.12f;
            jeepGun.burstCooldown = 3.5f;

            IFVTurretController jeepTurret = vehicle.GetComponent<IFVTurretController>();
            if (jeepTurret == null)
                jeepTurret = vehicle.AddComponent<IFVTurretController>();
            jeepTurret.Configure(jeepGun.firePoint, true, true, true);
            return;
        }

        // APC/IFV receives its own light autocannon. It never borrows a
        // GunPoint from the previously controlled Jeep or another vehicle.
        if (blueprint.role != VehicleRole.APC)
            return;

        // IFVs carry the same reconnaissance smoke launcher as the Jeep.
        if (vehicle.GetComponent<ArtillerySpotter>() == null)
            vehicle.AddComponent<ArtillerySpotter>();

        IFVAutocannon gun = vehicle.GetComponent<IFVAutocannon>();
        if (gun == null)
            gun = vehicle.AddComponent<IFVAutocannon>();
        gun.ConfigureFromVehicleBounds();

        IFVTurretController turret = vehicle.GetComponent<IFVTurretController>();
        if (turret == null)
            turret = vehicle.AddComponent<IFVTurretController>();
        turret.Configure(gun.firePoint);
    }

    public static void ConfigureVehicleDurability(GameObject vehicle, VehicleRole role)
    {
        Health health = vehicle.GetComponentInChildren<Health>();
        if (health == null)
            health = vehicle.AddComponent<Health>();

        float maxHealth = role == VehicleRole.Tank ? 520f :
            role == VehicleRole.Artillery ? 220f :
            role == VehicleRole.APC ? 240f :
            role == VehicleRole.Truck ? 110f :
            role == VehicleRole.UAV ? 55f : 100f;
        float armor = role == VehicleRole.Tank ? 0.55f :
            role == VehicleRole.Artillery ? 0.15f :
            role == VehicleRole.APC ? 0.20f : 0f;
        health.ConfigureDurability(maxHealth, armor);
    }

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
                    scriptName.Contains("Gun") ||
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
                10f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore
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

        // The IFV mesh was authored with a pivot below its wheels. Settle its
        // visible bounds just above terrain before creating its runtime
        // collider, so it does not begin half-buried in the ground.
        if (blueprint.role == VehicleRole.APC)
            RaiseVehicleVisualAboveGround(currentVehicle, spawnPos.y, 0.16f);

        EnsureRuntimeVehicleComponents(currentVehicle, blueprint);
        ConfigureVehicleDurability(currentVehicle, blueprint.role);
        ConfigureRoleEquipment(currentVehicle, blueprint);

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
            if (string.Equals(t.name, "DriverView", System.StringComparison.OrdinalIgnoreCase))
            {
                cameraRig.driverView = t;
            }

            if (string.Equals(t.name, "GunView", System.StringComparison.OrdinalIgnoreCase))
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
