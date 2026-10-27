using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TruckCargo : MonoBehaviour
{
    public static string PlayerPrompt { get; private set; }
    public static bool HasPlayerInteractionPrompt => promptedTruck != null;
    static TruckCargo promptedTruck;

    [Header("Cargo")]
    public ResourceType carriedResource = ResourceType.None;

    public int amount = 0;
    public int maxCapacity = 1;

    [Header("Cargo Visuals")]
    public GameObject steelCargo;
    public GameObject oilCargo;

    Coroutine safeSpawnRoutine;

    // Trucks frequently appear near the Jeep or a mine exit. Give physics a
    // short grace period so an overlapping spawn cannot launch either vehicle.
    public void BeginSafeSpawn(float duration = 1.5f)
    {
        if (safeSpawnRoutine != null)
            StopCoroutine(safeSpawnRoutine);
        safeSpawnRoutine = StartCoroutine(SafeSpawnRoutine(duration));
    }

    IEnumerator SafeSpawnRoutine(float duration)
    {
        Transform root = transform.root;
        Collider[] truckColliders = root.GetComponentsInChildren<Collider>(true);
        List<Collider> ignoredVehicleColliders = new List<Collider>();

        foreach (Rigidbody body in root.GetComponentsInChildren<Rigidbody>(true))
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        foreach (VehicleController vehicle in FindObjectsByType<VehicleController>(FindObjectsSortMode.None))
        {
            if (vehicle == null || vehicle.transform.root == root)
                continue;

            foreach (Collider otherCollider in vehicle.transform.root.GetComponentsInChildren<Collider>(true))
            {
                if (otherCollider == null)
                    continue;

                ignoredVehicleColliders.Add(otherCollider);
                foreach (Collider truckCollider in truckColliders)
                    if (truckCollider != null)
                        Physics.IgnoreCollision(truckCollider, otherCollider, true);
            }
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            bool visible = Mathf.FloorToInt(elapsed / 0.12f) % 2 == 0;
            foreach (Renderer renderer in renderers)
                if (renderer != null)
                    renderer.enabled = visible;
            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (Collider otherCollider in ignoredVehicleColliders)
        {
            if (otherCollider == null)
                continue;
            foreach (Collider truckCollider in truckColliders)
                if (truckCollider != null)
                    Physics.IgnoreCollision(truckCollider, otherCollider, false);
        }

        foreach (Renderer renderer in renderers)
            if (renderer != null)
                renderer.enabled = true;

        safeSpawnRoutine = null;
    }

    void Start()
    {
        UpdateVisual();
    }

    void Update()
    {
        UpdateEnemyTruckInteraction();

        if (!Input.GetKeyDown(KeyCode.B) || GameManager.Instance == null)
            return;

        TeamMember teamMember = GetComponentInParent<TeamMember>();
        if (teamMember == null || teamMember.team != GameManager.Instance.PlayerTeam)
            return;

        VehicleController controller = GetComponentInParent<VehicleController>();
        if (controller != null)
            controller.SetControlled(false);

        teamMember.SetTeam(Team.Neutral);

        foreach (VehicleSpawnerFinal spawner in FindObjectsByType<VehicleSpawnerFinal>(FindObjectsSortMode.None))
        {
            if (spawner.playerTeam == GameManager.Instance.PlayerTeam &&
                spawner.gameObject.name.IndexOf("MainBase", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                spawner.TrySpawn(VehicleRole.Jeep);
                Debug.Log("Caminhão abandonado. Novo jipe enviado da base principal.");
                break;
            }
        }
    }

    void UpdateEnemyTruckInteraction()
    {
        if (GameManager.Instance == null || !GameManager.Instance.MatchStarted)
            return;

        VehicleController interdictor = null;
        foreach (VehicleController candidate in FindObjectsByType<VehicleController>(FindObjectsSortMode.None))
        {
            if (candidate != null && candidate.IsControlled)
            {
                interdictor = candidate;
                break;
            }
        }

        TeamMember truckTeam = GetComponentInParent<TeamMember>() ?? GetComponent<TeamMember>();
        TeamMember playerTeam = interdictor != null
            ? interdictor.GetComponentInParent<TeamMember>() ?? interdictor.transform.root.GetComponentInChildren<TeamMember>(true)
            : null;
        bool valid = interdictor != null && truckTeam != null && playerTeam != null &&
            truckTeam.team != Team.Neutral && playerTeam.team != Team.Neutral &&
            truckTeam.team != playerTeam.team && IsJeepOrIFV(interdictor.transform.root) &&
            Vector3.SqrMagnitude(interdictor.transform.root.position - transform.root.position) <= 9f * 9f;

        if (!valid)
        {
            if (promptedTruck == this)
                ClearInteractionPrompt();
            return;
        }

        promptedTruck = this;
        PlayerPrompt = "TRUCK INIMIGO: [E] capturar e redirecionar para a base | [R] capturar e controlar";

        if (Input.GetKeyDown(KeyCode.E))
        {
            CaptureFor(playerTeam.team, false);
            ClearInteractionPrompt();
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            CaptureFor(playerTeam.team, true);
            ClearInteractionPrompt();
        }
    }

    bool IsJeepOrIFV(Transform vehicleRoot)
    {
        VehicleMotor motor = vehicleRoot.GetComponentInChildren<VehicleMotor>(true);
        if (motor != null && motor.blueprint != null)
            return motor.blueprint.role == VehicleRole.Jeep || motor.blueprint.role == VehicleRole.APC;

        return vehicleRoot.GetComponentInChildren<IFVAutocannon>(true) != null;
    }

    public bool TryCaptureForAI(Team newTeam)
    {
        TeamMember member = GetComponentInParent<TeamMember>() ?? GetComponent<TeamMember>();
        if (member == null || newTeam == Team.Neutral || member.team == Team.Neutral || member.team == newTeam)
            return false;

        CaptureFor(newTeam, false);
        return true;
    }

    void CaptureFor(Team newTeam, bool takeControl)
    {
        TeamMember member = GetComponentInParent<TeamMember>() ?? GetComponent<TeamMember>();
        if (member == null)
            return;
        member.SetTeam(newTeam);

        GameObject truckRoot = transform.root.gameObject;
        VehicleSimpleAI ai = truckRoot.GetComponent<VehicleSimpleAI>() ?? truckRoot.AddComponent<VehicleSimpleAI>();
        AITruckDelivery delivery = truckRoot.GetComponent<AITruckDelivery>() ?? truckRoot.AddComponent<AITruckDelivery>();

        if (!takeControl)
        {
            VehicleMotor motor = truckRoot.GetComponentInChildren<VehicleMotor>();
            if (motor != null) motor.usePlayerInput = false;
            TankMotor tankMotor = truckRoot.GetComponentInChildren<TankMotor>();
            if (tankMotor != null) tankMotor.usePlayerInput = false;
            ai.enabled = true;
            delivery.enabled = true;
            delivery.RedirectToMainBase(ai, newTeam);
            Debug.Log("Truck inimigo capturado e redirecionado para a base.");
            return;
        }

        Destroy(delivery);
        ai.target = null;
        ai.enabled = false;
        VehicleMotor playerMotor = truckRoot.GetComponentInChildren<VehicleMotor>();
        if (playerMotor != null) playerMotor.usePlayerInput = true;
        TankMotor playerTankMotor = truckRoot.GetComponentInChildren<TankMotor>();
        if (playerTankMotor != null) playerTankMotor.usePlayerInput = true;

        foreach (VehicleController controller in FindObjectsByType<VehicleController>(FindObjectsSortMode.None))
            controller.SetControlled(false);
        VehicleController truckController = truckRoot.GetComponentInChildren<VehicleController>();
        if (truckController != null) truckController.SetControlled(true);

        VehicleOrbitCamera camera = FindFirstObjectByType<VehicleOrbitCamera>();
        if (camera != null)
        {
            Transform pivot = truckRoot.transform.Find("CameraPivot");
            camera.target = pivot != null ? pivot : truckRoot.transform;
        }
        Debug.Log("Truck inimigo capturado e sob controle do jogador.");
    }

    static void ClearInteractionPrompt()
    {
        promptedTruck = null;
        PlayerPrompt = null;
    }

    // =====================================================
    // VISUAL
    // =====================================================
    void UpdateVisual()
    {
        // desliga tudo
        if (steelCargo != null)
            steelCargo.SetActive(false);

        if (oilCargo != null)
            oilCargo.SetActive(false);

        // sem carga
        if (!HasCargo())
            return;

        // aço
        if (carriedResource == ResourceType.Steel)
        {
            if (steelCargo != null)
                steelCargo.SetActive(true);
        }

        // petróleo
        if (carriedResource == ResourceType.Oil)
        {
            if (oilCargo != null)
                oilCargo.SetActive(true);
        }
    }

    // =====================================================
    // COLETA NA MINA
    // =====================================================
    void OnTriggerEnter(Collider other)
    {
        ResourceNode node =
            other.GetComponent<ResourceNode>() ??
            other.GetComponentInParent<ResourceNode>() ??
            other.GetComponentInChildren<ResourceNode>();

        if (node == null)
            return;

        TeamMember tm = GetComponent<TeamMember>();

        if (tm == null)
            return;

        node.TryCollect(this, tm.team);
    }

    // =====================================================
    // TEM CARGA
    // =====================================================
    public bool HasCargo()
    {
        return carriedResource != ResourceType.None && amount > 0;
    }

    // =====================================================
    // CARREGAR
    // =====================================================
    public bool Load(ResourceType type, int value)
    {
        if (HasCargo())
        {
            Debug.Log("🚫 Truck já possui carga");
            return false;
        }

        if (type == ResourceType.None || value <= 0 || maxCapacity <= 0)
        {
            Debug.LogWarning("Carga inválida para o caminhão.");
            return false;
        }

        carriedResource = type;

        amount = Mathf.Min(value, maxCapacity);

        Debug.Log(
            $"📦 Truck carregou {amount} de {type}"
        );

        UpdateVisual();

        return true;
    }

    // =====================================================
    // DESCARREGAR
    // =====================================================
    public bool Unload(BaseInventory inventory)
    {
        if (inventory == null)
        {
            Debug.LogWarning("Inventário de destino não encontrado.");
            return false;
        }

        if (!HasCargo())
        {
            Debug.Log("🚫 Truck vazio");
            return false;
        }

        // adiciona recurso
        inventory.AddResource(
            carriedResource,
            amount
        );

        Debug.Log(
            $"🏭 Entregue {amount} de {carriedResource}"
        );

        // limpa carga
        carriedResource = ResourceType.None;
        amount = 0;

        UpdateVisual();

        return true;
    }
}
