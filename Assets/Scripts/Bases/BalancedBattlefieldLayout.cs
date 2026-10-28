using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Builds a symmetric, neutral objective layout from the authored structure
// templates. Main bases are deliberately never moved or duplicated.
public class BalancedBattlefieldLayout : MonoBehaviour
{
    [Header("Structures per side")]
    [Min(1)] public int factoriesPerSide = 1;
    [Min(1)] public int citiesPerSide = 1;
    [Min(1)] public int steelMinesPerSide = 1;
    [Min(1)] public int oilMinesPerSide = 1;

    [Header("Layout")]
    [Min(20f)] public float minimumStructureSpacing = 50f;
    [Range(0.1f, 0.48f)] public float mapUsage = 0.40f;

    readonly List<Vector3> occupiedPositions = new List<Vector3>();
    bool built;
    bool radarQueued;
    Vector3 radarCenter;
    Vector3 radarTeamAxis;
    Vector3 radarSideAxis;
    float radarReach;

    public void Build()
    {
        if (built)
            return;
        built = true;

        FindBaseAxis(out Vector3 center, out Vector3 teamAxis, out float baseDistance);
        Vector3 sideAxis = Vector3.Cross(Vector3.up, teamAxis).normalized;
        float reach = Mathf.Clamp(baseDistance * mapUsage, 90f, 180f);

        foreach (TeamMember member in FindObjectsByType<TeamMember>(FindObjectsSortMode.None))
            if (member.gameObject.name.IndexOf("MainBase", StringComparison.OrdinalIgnoreCase) >= 0)
                occupiedPositions.Add(member.transform.position);

        CaptureTriggerAdvanced factory = FindTemplate(StructureType.Factory, ResourceType.None);
        CaptureTriggerAdvanced city = FindTemplate(StructureType.City, ResourceType.None);
        CaptureTriggerAdvanced steel = FindTemplate(StructureType.Mine, ResourceType.Steel);
        CaptureTriggerAdvanced oil = FindTemplate(StructureType.Mine, ResourceType.Oil);

        // Legacy mine templates keep their visible model and capture trigger
        // as separate scene roots. Group them before cloning/placing so every
        // runtime copy, destruction and respawn moves the complete mine.
        EnsureMineCompoundRoot(steel);
        EnsureMineCompoundRoot(oil);

        // Mines are placed first, so the resource economy always has the
        // best-separated positions. Other objectives fill the remaining lanes.
        PlacePairs(steel, steelMinesPerSide, center, teamAxis, sideAxis, reach, 2);
        PlacePairs(oil, oilMinesPerSide, center, teamAxis, sideAxis, reach, 3);
        PlacePairs(factory, factoriesPerSide, center, teamAxis, sideAxis, reach, 0);
        PlacePairs(city, citiesPerSide, center, teamAxis, sideAxis, reach, 1);
        // GameManager builds the battlefield in Awake. Delay the prefab
        // instantiation until Start so an imported structure can never block
        // the initial loadout screen from being created.
        radarQueued = true;
        radarCenter = center;
        radarTeamAxis = teamAxis;
        radarSideAxis = sideAxis;
        radarReach = reach;

        Debug.Log("Mapa equilibrado criado: " +
            (factoriesPerSide * 2) + " fábricas, " +
            (citiesPerSide * 2) + " cidades, " +
            (steelMinesPerSide * 2) + " minas de aço e " +
            (oilMinesPerSide * 2) + " minas de petróleo.");
    }

    void Start()
    {
        if (!radarQueued)
            return;

        radarQueued = false;
        CreateRadarStation(radarCenter, radarTeamAxis, radarSideAxis, radarReach);
    }

    void CreateRadarStation(Vector3 center, Vector3 teamAxis, Vector3 sideAxis, float reach)
    {
        // The radar is a single central objective, rather than a starting
        // asset. Its owner gains extended artillery observation range.
        foreach (RadarStation existing in FindObjectsByType<RadarStation>(FindObjectsSortMode.None))
            if (existing != null)
                return;

        GameObject prefab = null;
#if UNITY_EDITOR
        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/CartoonMilitaryModelPack/Prefebs/Building_Prefebs/Radar_Prefeb.prefab");
#endif
        if (prefab == null)
        {
            Debug.LogWarning("Radar_Prefeb não foi encontrado. Adicione-o à cena para habilitar o objetivo de radar.");
            return;
        }

        GameObject root = Instantiate(prefab);
        root.name = "Radar Station - Neutral";
        // The imported radar is authored much smaller than the battlefield
        // structures. Make it a deliberate landmark without changing its
        // world-space capture radius below.
        root.transform.localScale *= 7f;

        // Keep the capture volume on a separate child. Putting the capture
        // component on the prefab root turns every model collider into a
        // trigger, which prevents projectiles from ever damaging the radar.
        GameObject captureObject = new GameObject("Radar Capture Zone");
        captureObject.transform.SetParent(root.transform, false);
        SphereCollider area = captureObject.AddComponent<SphereCollider>();
        area.isTrigger = true;
        float scale = Mathf.Max(0.001f, Mathf.Abs(root.transform.lossyScale.x));
        area.radius = 14f / scale;
        CaptureTriggerAdvanced capture = captureObject.AddComponent<CaptureTriggerAdvanced>();
        capture.structureType = StructureType.Radar;
        capture.currentOwner = Team.Neutral;
        RadarStation radar = root.GetComponent<RadarStation>() ?? root.AddComponent<RadarStation>();
        radar.team = Team.Neutral;

        Vector3 desired = center + teamAxis * 32f + sideAxis * (reach * 0.06f);
        PlaceStructure(capture, FindValidGround(desired, center, teamAxis));
        occupiedPositions.Add(capture.transform.position);
    }

    CaptureTriggerAdvanced FindTemplate(StructureType type, ResourceType resource)
    {
        foreach (CaptureTriggerAdvanced candidate in FindObjectsByType<CaptureTriggerAdvanced>(FindObjectsSortMode.None))
        {
            if (candidate.structureType != type)
                continue;
            if (type != StructureType.Mine)
                return candidate;
            if (candidate.resourceNode != null && candidate.resourceNode.resourceType == resource)
                return candidate;
        }

        Debug.LogWarning("Template de estrutura ausente: " + type + " / " + resource);
        return null;
    }

    void PlacePairs(CaptureTriggerAdvanced template, int pairs, Vector3 center,
        Vector3 teamAxis, Vector3 sideAxis, float reach, int lane)
    {
        if (template == null)
            return;

        CaptureTriggerAdvanced current = template;
        for (int pair = 0; pair < pairs; pair++)
        {
            Vector3 offset = GetMirroredOffset(teamAxis, sideAxis, reach, lane, pair);
            PlaceStructure(current, FindValidGround(center + offset, center, teamAxis));
            occupiedPositions.Add(current.transform.position);

            GameObject cloneRoot = Instantiate(GetStructureRoot(template).gameObject);
            cloneRoot.name = template.structureType + " Runtime " + (pair + 1) + " B";
            CaptureTriggerAdvanced clone = cloneRoot.GetComponentInChildren<CaptureTriggerAdvanced>(true);
            if (clone == null)
            {
                Debug.LogWarning("A cópia de " + template.name + " não contém uma zona de captura.");
                Destroy(cloneRoot);
                continue;
            }

            PlaceStructure(clone, FindValidGround(center - offset, center, teamAxis));
            occupiedPositions.Add(clone.transform.position);
            current = clone;
        }
    }

    void EnsureMineCompoundRoot(CaptureTriggerAdvanced capture)
    {
        if (capture == null || capture.structureType != StructureType.Mine || capture.resourceNode == null)
            return;

        Transform captureRoot = GetStructureRoot(capture);
        Transform visualRoot = capture.resourceNode.transform.root;
        if (captureRoot == visualRoot)
            return;

        // Both source roots preserve their world transforms while becoming
        // children of a single gameplay root. Prefab cloning now remaps the
        // ResourceNode reference to the clone rather than the original mine.
        GameObject group = new GameObject(capture.resourceNode.resourceType + " Mine Structure");
        group.transform.position = captureRoot.position;
        group.transform.rotation = captureRoot.rotation;
        captureRoot.SetParent(group.transform, true);
        visualRoot.SetParent(group.transform, true);
    }

    Vector3 GetMirroredOffset(Vector3 teamAxis, Vector3 sideAxis, float reach, int lane, int pair)
    {
        // Each type receives its own mirrored lane, keeping its capture rings
        // far apart while preserving equal travel distances for both teams.
        // Factory pairs are deep on opposite banks, cities stay closer to
        // the river, and mines occupy the broad outer resource lanes.
        float longitudinal = new[] { 0.82f, 0.42f, 0.88f, 0.72f }[lane] * reach;
        float lateral = new[] { 0.25f, -0.18f, 0.46f, -0.36f }[lane] * reach;
        longitudinal += UnityEngine.Random.Range(-0.08f, 0.08f) * reach;
        lateral += UnityEngine.Random.Range(-0.12f, 0.12f) * reach;
        lateral += pair * minimumStructureSpacing * 1.25f;
        return teamAxis * longitudinal + sideAxis * lateral;
    }

    void PlaceStructure(CaptureTriggerAdvanced capture, Vector3 desiredCapturePosition)
    {
        Transform root = GetStructureRoot(capture);
        Vector3 originalRootPosition = root.position;
        root.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
        Vector3 delta = capture.transform.position - root.position;
        root.position = desiredCapturePosition - delta;
        SettleStructureOnGround(root);

        // Some legacy mine visuals are siblings of their capture zone rather
        // than children. Move/settle that visual root by the same displacement.
        if (capture.structureType == StructureType.Mine && capture.resourceNode != null)
        {
            Transform mineVisual = capture.resourceNode.transform.root;
            if (mineVisual != root)
            {
                mineVisual.position += root.position - originalRootPosition;
                SettleStructureOnGround(mineVisual);
            }
        }
        EnsureDestructible(capture, root);
        WireStructureReferences(capture, root);
    }

    void EnsureDestructible(CaptureTriggerAdvanced capture, Transform root)
    {
        if (capture.structureType == StructureType.MainBase)
            return;

        BaseDestructibleWithHealthVisual health =
            root.GetComponent<BaseDestructibleWithHealthVisual>() ??
            root.gameObject.AddComponent<BaseDestructibleWithHealthVisual>();
        health.endsMatchOnDestruction = false;
        health.respawnDelay = 5f;
        float maxHealth = capture.structureType == StructureType.Factory ? 800f :
            capture.structureType == StructureType.City ? 650f :
            capture.structureType == StructureType.Radar ? 550f : 450f;
        float armor = capture.structureType == StructureType.Factory ? 0.35f :
            capture.structureType == StructureType.City ? 0.30f :
            capture.structureType == StructureType.Radar ? 0.28f : 0.25f;
        health.ConfigureDurability(maxHealth, armor);
        capture.baseScript = health;
    }

    void SettleStructureOnGround(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        float lowest = float.PositiveInfinity;
        foreach (Renderer renderer in renderers)
            if (renderer.enabled)
                lowest = Mathf.Min(lowest, renderer.bounds.min.y);
        if (float.IsPositiveInfinity(lowest))
            return;

        Vector3 rayStart = root.position + Vector3.up * 180f;
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, 360f);
        bool foundGround = false;
        float bestGroundHeight = float.NegativeInfinity;
        foreach (RaycastHit ground in hits)
        {
            // A structure's own model collider is above the terrain. It must
            // not be treated as the floor for the settling calculation.
            if (ground.collider.transform.IsChildOf(root))
                continue;

            // Only accept a surface at or below the visual base. This avoids
            // placing a mine on an unrelated capture/helper collider that the
            // ray crossed above the actual ground.
            if (ground.point.y > lowest + 0.25f || ground.point.y <= bestGroundHeight)
                continue;

            bestGroundHeight = ground.point.y;
            foundGround = true;
        }

        if (!foundGround)
            return;

        Vector3 position = root.position;
        position.y += bestGroundHeight - lowest + 0.03f;
        root.position = position;
    }

    void WireStructureReferences(CaptureTriggerAdvanced capture, Transform root)
    {
        if (capture.structureType != StructureType.Factory)
            return;

        VehicleSpawnerFinal spawner = root.GetComponentInChildren<VehicleSpawnerFinal>(true);
        BaseInventory inventory = root.GetComponentInChildren<BaseInventory>(true);
        if (spawner != null)
        {
            spawner.baseCapture = capture;
            if (inventory != null)
                spawner.inventory = inventory;
        }

        foreach (StructureSpawnZone zone in root.GetComponentsInChildren<StructureSpawnZone>(true))
            zone.spawner = spawner;

        foreach (FactoryDeliveryArea area in root.GetComponentsInChildren<FactoryDeliveryArea>(true))
            area.Configure(inventory, capture);
    }

    Vector3 FindValidGround(Vector3 desired, Vector3 mapCenter, Vector3 teamAxis)
    {
        Vector3 candidate = desired;
        const float riverHalfWidth = 12f;
        for (int attempt = 0; attempt < 40; attempt++)
        {
            bool clear = Mathf.Abs(Vector3.Dot(candidate - mapCenter, teamAxis)) > riverHalfWidth + 16f;
            foreach (Vector3 occupied in occupiedPositions)
            {
                Vector3 flat = candidate - occupied;
                flat.y = 0f;
                if (flat.sqrMagnitude < minimumStructureSpacing * minimumStructureSpacing)
                {
                    clear = false;
                    Vector2 shift = UnityEngine.Random.insideUnitCircle.normalized * minimumStructureSpacing;
                    candidate += new Vector3(shift.x, 0f, shift.y);
                    candidate.y = desired.y;
                    break;
                }
            }
            if (clear)
                break;

            // A candidate can be clear of other structures but still land on
            // the river reservation. In either failure case, sample again.
            Vector2 retryShift = UnityEngine.Random.insideUnitCircle.normalized * minimumStructureSpacing;
            candidate += new Vector3(retryShift.x, 0f, retryShift.y);
            candidate.y = desired.y;
        }

        Vector3 rayStart = candidate + Vector3.up * 160f;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 320f))
            candidate.y = hit.point.y;
        return candidate;
    }

    Transform GetStructureRoot(CaptureTriggerAdvanced capture)
    {
        Transform root = capture.transform;
        while (root.parent != null &&
               root.parent.GetComponentsInChildren<CaptureTriggerAdvanced>(true).Length == 1)
        {
            root = root.parent;
        }
        return root;
    }

    void FindBaseAxis(out Vector3 center, out Vector3 axis, out float distance)
    {
        TeamMember teamA = null;
        TeamMember teamB = null;
        foreach (TeamMember member in FindObjectsByType<TeamMember>(FindObjectsSortMode.None))
        {
            if (member.gameObject.name.IndexOf("MainBase", StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            if (member.team == Team.TeamA) teamA = member;
            if (member.team == Team.TeamB) teamB = member;
        }

        if (teamA != null && teamB != null)
        {
            center = (teamA.transform.position + teamB.transform.position) * 0.5f;
            Vector3 flat = teamA.transform.position - teamB.transform.position;
            flat.y = 0f;
            distance = flat.magnitude;
            axis = distance > 0.01f ? flat / distance : Vector3.right;
            return;
        }

        center = Vector3.zero;
        axis = Vector3.right;
        distance = 180f;
        Debug.LogWarning("Main bases não encontradas; usando o centro do mapa como referência.");
    }
}
