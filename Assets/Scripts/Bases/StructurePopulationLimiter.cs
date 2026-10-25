using System.Collections.Generic;
using UnityEngine;

// Keeps the prototype battlefield readable: one city, one factory, one steel
// mine and one oil mine. Extra authoring copies are disabled as whole groups.
public class StructurePopulationLimiter : MonoBehaviour
{
    void Start() => Invoke(nameof(ApplyLayout), 0.2f);

    void ApplyLayout()
    {
        HashSet<string> kept = new HashSet<string>();
        foreach (CaptureTriggerAdvanced capture in FindObjectsByType<CaptureTriggerAdvanced>(FindObjectsSortMode.None))
        {
            string key = GetKey(capture);
            if (key == null || kept.Add(key))
                continue;

            foreach (VehicleSpawnerFinal spawner in FindObjectsByType<VehicleSpawnerFinal>(FindObjectsSortMode.None))
                if (spawner.baseCapture == capture)
                    spawner.gameObject.SetActive(false);

            if (capture.baseScript != null)
                capture.baseScript.gameObject.SetActive(false);
            if (capture.resourceNode != null && capture.resourceNode.gameObject != capture.gameObject)
                capture.resourceNode.gameObject.SetActive(false);
            capture.gameObject.SetActive(false);
        }
    }

    string GetKey(CaptureTriggerAdvanced capture)
    {
        if (capture.structureType == StructureType.City) return "city";
        if (capture.structureType == StructureType.Factory) return "factory";
        if (capture.structureType != StructureType.Mine || capture.resourceNode == null) return null;
        return capture.resourceNode.resourceType == ResourceType.Steel ? "steel" :
            capture.resourceNode.resourceType == ResourceType.Oil ? "oil" : null;
    }
}
