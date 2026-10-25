using System.Collections.Generic;
using UnityEngine;

public class BaseInventory : MonoBehaviour
{
    [Header("Debug")]
    public int steel;
    public int oil;

    private Dictionary<ResourceType, int> resources =
        new Dictionary<ResourceType, int>();

    void Awake()
    {
        // inicia todos recursos com 0
        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            if (type == ResourceType.None)
                continue;

            resources[type] = 0;
        }

        UpdateDebugValues();
    }

    // ================= ADICIONAR =================
    public void AddResource(ResourceType type, int amount)
    {
        if (type == ResourceType.None || amount <= 0)
            return;

        if (!resources.ContainsKey(type))
            resources[type] = 0;

        resources[type] += amount;

        UpdateDebugValues();

        Debug.Log($"📦 Base recebeu {amount} de {type}");
        // ================= UNLOCK TANK =================
        if (type == ResourceType.Steel && GameManager.Instance != null)
        {
            CaptureTriggerAdvanced capture =
                GetComponent<CaptureTriggerAdvanced>() ??
                GetComponentInParent<CaptureTriggerAdvanced>();

            Team baseOwner =
                capture != null
                ? capture.GetOwner()
                : Team.Neutral;

            if (baseOwner == Team.TeamA)
                GameManager.Instance.teamATankUnlocked = true;
            else if (baseOwner == Team.TeamB)
                GameManager.Instance.teamBTankUnlocked = true;
            else
                return;

            Debug.Log("🛡️ Tank desbloqueado!");
        }
    }
    

    // ================= REMOVER =================
    public bool ConsumeResource(ResourceType type, int amount)
    {
        if (type == ResourceType.None)
            return false;

        if (!resources.ContainsKey(type))
            resources[type] = 0;

        if (resources[type] < amount)
        {
            Debug.Log($"❌ Recursos insuficientes: {type}");
            return false;
        }

        resources[type] -= amount;

        UpdateDebugValues();

        Debug.Log($"🧱 Consumido {amount} de {type}");

        return true;
    }

    // ================= CONSULTAR =================
    public int GetResource(ResourceType type)
    {
        if (resources.ContainsKey(type))
            return resources[type];

        return 0;
    }

    // ================= TEM RECURSO =================
    public bool HasResource(ResourceType type, int amount)
    {
        return GetResource(type) >= amount;
    }

    // ================= REMOVER DIRETO =================
    public void RemoveResource(ResourceType type, int amount)
    {
        if (!resources.ContainsKey(type))
            resources[type] = 0;

        resources[type] -= amount;

        if (resources[type] < 0)
            resources[type] = 0;

        UpdateDebugValues();

        Debug.Log($"🧱 Recurso removido: {type} -{amount}");
    }

    public void ClearResources()
    {
        resources[ResourceType.Steel] = 0;
        resources[ResourceType.Oil] = 0;
        UpdateDebugValues();
        Debug.Log("Inventário da fábrica perdido.");
    }

    // ================= DEBUG =================
    void UpdateDebugValues()
    {
        steel = GetResource(ResourceType.Steel);
        oil = GetResource(ResourceType.Oil);
    }
}
