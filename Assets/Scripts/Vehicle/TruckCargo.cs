using UnityEngine;

public class TruckCargo : MonoBehaviour
{
    [Header("Cargo")]
    public ResourceType carriedResource = ResourceType.None;

    public int amount = 0;
    public int maxCapacity = 1;

    [Header("Cargo Visuals")]
    public GameObject steelCargo;
    public GameObject oilCargo;

    void Start()
    {
        UpdateVisual();
    }

    void Update()
    {
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
