using UnityEngine;
using System.Collections.Generic;

public class CaptureTriggerAdvanced : MonoBehaviour
{
    [Header("Flag")]
    public FlagController flagController;

    [Header("Base")]
    public BaseDestructibleWithHealthVisual baseScript;

    [Header("Resource Node")]
    public ResourceNode resourceNode;

    [Header("Capture Settings")]
    public float captureTime = 8f;

    [Header("Capture Type")]
    public bool countsAsCity = true;

    [Header("Structure")]
    public StructureType structureType;

    [Header("Debug")]
    public Team currentOwner = Team.Neutral;

    float captureProgress = 0f;

    bool captured = false;

    Team lastCapturingTeam = Team.Neutral;

    List<TeamMember> unitsInside =
        new List<TeamMember>();

    readonly Dictionary<TeamMember, int> colliderCounts =
        new Dictionary<TeamMember, int>();
    readonly HashSet<TeamMember> externalUnits = new HashSet<TeamMember>();
    readonly HashSet<TeamMember> externalInjectedUnits = new HashSet<TeamMember>();
    Transform captureMarker;
    LineRenderer captureMarkerRenderer;

    // =====================================================
    // START
    // =====================================================
    void Start()
    {
        // Main bases are pre-owned; all other world structures start neutral.
        captured = structureType == StructureType.MainBase && currentOwner != Team.Neutral;
        captureProgress = captured ? 1f : 0f;
        if (!captured)
            currentOwner = Team.Neutral;

        // AUTO RESOURCE NODE
        if (resourceNode == null)
        {
            resourceNode =
                GetComponent<ResourceNode>();
        }

        if (flagController == null)
        {
            flagController =
                GetComponentInChildren<FlagController>();
        }

        if (flagController == null)
        {
            flagController =
                gameObject.AddComponent<FlagController>();
        }

        // Factory models are taller than their capture centre. Make their
        // world-space flag read clearly above the roof and in front of it.
        if (structureType == StructureType.Factory)
        {
            flagController.raisedHeight = 5f;
            flagController.forwardOffset = 4f;
        }

        flagController.SetOwner(currentOwner);
        CreateCaptureMarker();
        UpdateCaptureMarker();
        Invoke(nameof(EnsureDestructible), 0.05f);
    }

    void Awake()
    {
        // Capture volumes must not push a vehicle or block its route.
        foreach (Collider captureCollider in GetComponentsInChildren<Collider>(true))
            captureCollider.isTrigger = true;

        // The capture object inherits the model's large scale. Collider radii
        // are local-space values, so convert the intended world radius back
        // into local space and avoid gigantic overlapping capture areas.
        SphereCollider captureSphere = GetComponent<SphereCollider>();
        if (captureSphere != null)
        {
            float worldRadius = structureType == StructureType.Factory ? 15f : 14f;
            Vector3 scale = transform.lossyScale;
            float largestAxis = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z), 0.001f);
            captureSphere.isTrigger = true;
            captureSphere.radius = worldRadius / largestAxis;
        }
    }

    void CreateCaptureMarker()
    {
        GameObject marker = new GameObject("Capture Area Outline");
        marker.name = "Capture Area Indicator";
        marker.transform.position = GetWorldCenter() + Vector3.up * 0.06f;
        float radius = structureType == StructureType.Factory ? 15f : 14f;
        captureMarkerRenderer = marker.AddComponent<LineRenderer>();
        captureMarkerRenderer.useWorldSpace = false;
        captureMarkerRenderer.loop = true;
        captureMarkerRenderer.positionCount = 64;
        captureMarkerRenderer.startWidth = 0.22f;
        captureMarkerRenderer.endWidth = 0.22f;
        captureMarkerRenderer.material = new Material(Shader.Find("Sprites/Default"));
        for (int i = 0; i < 64; i++)
        {
            float angle = i * Mathf.PI * 2f / 64f;
            captureMarkerRenderer.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }
        captureMarker = marker.transform;
    }

    void UpdateCaptureMarker()
    {
        if (captureMarkerRenderer == null) return;
        Color color = currentOwner == Team.TeamA ? new Color(0.15f, 0.45f, 1f) :
            currentOwner == Team.TeamB ? new Color(1f, 0.18f, 0.18f) : new Color(0.55f, 0.55f, 0.55f);
        captureMarkerRenderer.startColor = color;
        captureMarkerRenderer.endColor = color;
    }

    public void SetWorldMarkerVisible(bool visible)
    {
        if (captureMarker != null)
            captureMarker.gameObject.SetActive(visible);
    }

    void EnsureDestructible()
    {
        if (baseScript != null || structureType == StructureType.MainBase)
            return;

        // Only use a real structure model as the damage target. Adding health
        // directly to a capture volume made its helper mesh look like a grey
        // respawn bubble and generated false rubble.
        baseScript = GetComponentInParent<BaseDestructibleWithHealthVisual>();
    }

    // =====================================================
    // UPDATE
    // =====================================================
    void Update()
    {
        if (captureMarker != null)
            captureMarker.position = GetWorldCenter() + Vector3.up * 0.06f;
        // DEBUG
        if (
            captured &&
            Input.GetKeyDown(KeyCode.T)
        )
        {
            Debug.Log(
                "🚗 Spawn liberado via tecla T"
            );

            if (baseScript != null)
            {
                baseScript.OnCaptured();
            }
        }

        // LIMPA NULOS
        unitsInside.RemoveAll(
            u => u == null
        );

        if (structureType == StructureType.Mine)
            RefreshMinePresence();

        HandleCapture();
    }

    // =====================================================
    // HANDLE CAPTURE
    // =====================================================
    void HandleCapture()
    {
        int teamACount = 0;
        int teamBCount = 0;

        foreach (var unit in unitsInside)
        {
            // Trigger enter/exit can be produced by oversized helper
            // colliders. The visible perimeter is the source of truth.
            if (unit == null || !IsVehicleInCaptureArea(unit))
                continue;

            if (unit.team == Team.TeamA)
            {
                teamACount++;
            }

            if (unit.team == Team.TeamB)
            {
                teamBCount++;
            }
        }

        // =================================================
        // CONTESTADO
        // =================================================
        if (
            teamACount > 0 &&
            teamBCount > 0
        )
        {
            Debug.Log("⚔️ CONTESTADO");
            return;
        }

        // =================================================
        // NINGUÉM
        // =================================================
        if (
            teamACount == 0 &&
            teamBCount == 0
        )
        {
            return;
        }

        Team capturingTeam =
            teamACount > 0
            ? Team.TeamA
            : Team.TeamB;

        // =================================================
        // JÁ DONO
        // =================================================
        if (
            captured &&
            currentOwner == capturingTeam
        )
        {
            return;
        }

        // =================================================
        // NOVO TIME CAPTURANDO
        // =================================================
        if (
            capturingTeam !=
            lastCapturingTeam
        )
        {
            captureProgress = 0f;

            captured = false;

            lastCapturingTeam =
                capturingTeam;

            Debug.Log(
                "🔄 Novo time capturando"
            );
        }

        // =================================================
        // PROGRESSO
        // =================================================
        int count =
            Mathf.Max(
                teamACount,
                teamBCount
            );

        float speed = count;

        captureProgress +=
            (
                Time.deltaTime /
                captureTime
            ) * speed;

        captureProgress =
            Mathf.Clamp01(
                captureProgress
            );

        Debug.Log(
            $"🏙️ Capturando {capturingTeam} → {captureProgress * 100f:F0}%"
        );

        // =================================================
        // CAPTURA FINAL
        // =================================================
        if (
            captureProgress >= 1f &&
            !captured
        )
        {
            Team oldOwner =
                currentOwner;

            // EVITA RECAPTURA
            if (
                oldOwner ==
                capturingTeam
            )
            {
                captured = true;

                captureProgress = 1f;

                return;
            }

            captured = true;

            captureProgress = 1f;

            currentOwner =
                capturingTeam;

            UpdateCaptureMarker();

            lastCapturingTeam =
                Team.Neutral;

            Debug.Log(
                $"🏙️ BASE CAPTURADA: {currentOwner}"
            );

            Debug.Log(
                $"🔥 FLOW: {oldOwner} → {currentOwner}"
            );

            // =================================================
            // RESOURCE NODE
            // =================================================
            if (resourceNode != null)
            {
                resourceNode.SetOwner(
                    currentOwner
                );

                // A Jeep that stayed in the capture zone can collect
                // immediately after the mine changes ownership.
                foreach (TeamMember unit in unitsInside)
                {
                    if (unit != null)
                        resourceNode.OfferCollection(unit.transform.root.gameObject);
                }

                // =============================================
                // REMOVE RECURSO DO ANTIGO DONO
                // =============================================
                if (
                    GameManager.Instance != null
                )
                {
                    // ---------- TEAM A ----------
                    if (
                        oldOwner ==
                        Team.TeamA
                    )
                    {
                        if (
                            resourceNode.resourceType ==
                            ResourceType.Steel
                        )
                        {
                            GameManager.Instance.teamASteel--;
                        }

                        if (
                            resourceNode.resourceType ==
                            ResourceType.Oil
                        )
                        {
                            GameManager.Instance.teamAOil--;
                        }
                    }

                    // ---------- TEAM B ----------
                    if (
                        oldOwner ==
                        Team.TeamB
                    )
                    {
                        if (
                            resourceNode.resourceType ==
                            ResourceType.Steel
                        )
                        {
                            GameManager.Instance.teamBSteel--;
                        }

                        if (
                            resourceNode.resourceType ==
                            ResourceType.Oil
                        )
                        {
                            GameManager.Instance.teamBOil--;
                        }
                    }

                    // =========================================
                    // ADICIONA AO NOVO DONO
                    // =========================================
                    if (
                        currentOwner ==
                        Team.TeamA
                    )
                    {
                        if (
                            resourceNode.resourceType ==
                            ResourceType.Steel
                        )
                        {
                            GameManager.Instance.teamASteel++;
                        }

                        if (
                            resourceNode.resourceType ==
                            ResourceType.Oil
                        )
                        {
                            GameManager.Instance.teamAOil++;
                        }

                        GameManager.Instance
                            .teamATruckUnlocked = true;
                    }

                    if (
                        currentOwner ==
                        Team.TeamB
                    )
                    {
                        if (
                            resourceNode.resourceType ==
                            ResourceType.Steel
                        )
                        {
                            GameManager.Instance.teamBSteel++;
                        }

                        if (
                            resourceNode.resourceType ==
                            ResourceType.Oil
                        )
                        {
                            GameManager.Instance.teamBOil++;
                        }

                        GameManager.Instance
                            .teamBTruckUnlocked = true;
                    }
                }

                Debug.Log(
                    "⛏ Mina agora pertence a: " +
                    currentOwner
                );

                Debug.Log(
                    "🚚 Truck desbloqueado!"
                );
            }

            // =================================================
            // GAME MANAGER
            // =================================================
            if (GameManager.Instance != null)
            {
                GameManager.Instance
                    .RegisterStructure(
                        structureType,
                        currentOwner,
                        oldOwner
                    );
            }

            // =================================================
            // FLAG
            // =================================================
            if (flagController != null)
            {
                flagController.SetOwner(
                    currentOwner
                );
            }

            // =================================================
            // BASE SCRIPT
            // =================================================
            if (baseScript != null)
            {
                baseScript.OnCaptured();
            }

            lastCapturingTeam =
                Team.Neutral;
        }
    }

    void RefreshMinePresence()
    {
        foreach (TeamMember unit in FindObjectsByType<TeamMember>(FindObjectsSortMode.None))
        {
            if (!IsVehicleUnit(unit) || !CanUnitCapture(unit))
                continue;

            bool inside = IsVehicleInCaptureArea(unit);
            if (inside && !colliderCounts.ContainsKey(unit))
            {
                colliderCounts[unit] = 1;
                unitsInside.Add(unit);
            }
            else if (!inside && colliderCounts.ContainsKey(unit) && !externalUnits.Contains(unit))
            {
                colliderCounts.Remove(unit);
                unitsInside.Remove(unit);
            }
        }
    }

    // =====================================================
    // TRIGGER ENTER
    // =====================================================
    void OnTriggerEnter(Collider other)
    {
        // Factory ownership is driven by its dedicated StructureSpawnZone.
        // This avoids unrelated model/area colliders falsely contesting it.
        if (structureType == StructureType.Factory)
            return;

        TeamMember unit =
            other.GetComponent<TeamMember>() ??
            other.GetComponentInParent<TeamMember>() ??
            other.GetComponentInChildren<TeamMember>();

        if (unit == null || !IsVehicleUnit(unit) || !CanUnitCapture(unit))
            return;

        if (colliderCounts.TryGetValue(unit, out int count))
            colliderCounts[unit] = count + 1;
        else
        {
            colliderCounts[unit] = 1;
            unitsInside.Add(unit);

            Debug.Log(
                "➡️ Entrou: " +
                unit.name +
                " | " +
                unit.team
            );
        }

        if (resourceNode != null)
            resourceNode.OfferCollection(other.transform.root.gameObject);
    }

    // =====================================================
    // TRIGGER EXIT
    // =====================================================
    void OnTriggerExit(Collider other)
    {
        if (structureType == StructureType.Factory)
            return;

        TeamMember unit =
            other.GetComponent<TeamMember>() ??
            other.GetComponentInParent<TeamMember>() ??
            other.GetComponentInChildren<TeamMember>();

        if (unit == null || !IsVehicleUnit(unit) || !CanUnitCapture(unit))
            return;

        if (!colliderCounts.TryGetValue(unit, out int count))
            return;

        if (count > 1)
        {
            colliderCounts[unit] = count - 1;
            return;
        }

        colliderCounts.Remove(unit);
        unitsInside.Remove(unit);

        Debug.Log(
            "⬅️ Saiu: " +
            unit.name
        );
    }

    // =====================================================
    // RESET
    // =====================================================
    public void ResetCapture()
    {
        Team oldOwner = currentOwner;
        if (GameManager.Instance != null && oldOwner != Team.Neutral)
        {
            GameManager.Instance.RegisterStructure(structureType, Team.Neutral, oldOwner);

            if (resourceNode != null)
            {
                if (oldOwner == Team.TeamA)
                {
                    if (resourceNode.resourceType == ResourceType.Steel)
                        GameManager.Instance.teamASteel = Mathf.Max(0, GameManager.Instance.teamASteel - 1);
                    else if (resourceNode.resourceType == ResourceType.Oil)
                        GameManager.Instance.teamAOil = Mathf.Max(0, GameManager.Instance.teamAOil - 1);
                }
                else if (oldOwner == Team.TeamB)
                {
                    if (resourceNode.resourceType == ResourceType.Steel)
                        GameManager.Instance.teamBSteel = Mathf.Max(0, GameManager.Instance.teamBSteel - 1);
                    else if (resourceNode.resourceType == ResourceType.Oil)
                        GameManager.Instance.teamBOil = Mathf.Max(0, GameManager.Instance.teamBOil - 1);
                }
            }
        }

        if (resourceNode != null)
            resourceNode.SetOwner(Team.Neutral);

        captureProgress = 0f;

        captured = false;

        currentOwner =
            Team.Neutral;

        UpdateCaptureMarker();

        lastCapturingTeam =
            Team.Neutral;

        Debug.Log(
            "🔄 Capture RESETADO"
        );

        if (flagController != null)
        {
            flagController.ResetFlag();
        }
    }

    // =====================================================
    // GET OWNER
    // =====================================================
    public Team GetOwner()
    {
        return currentOwner;
    }

    public bool IsVehicleInCaptureArea(TeamMember unit)
    {
        if (unit == null)
            return false;

        Vector3 offset = unit.transform.root.position - GetWorldCenter();
        offset.y = 0f;
        return offset.sqrMagnitude <= GetCaptureRadius() * GetCaptureRadius();
    }

    float GetCaptureRadius()
    {
        return structureType == StructureType.Factory ? 15f : 14f;
    }

    public Vector3 GetWorldCenter()
    {
        // Mine capture helpers are often offset from their visible model.
        // ResourceNode is the model-side anchor, so use it for both physics
        // checks and every world-space marker.
        if (structureType == StructureType.Mine && resourceNode != null)
            return resourceNode.GetVisualCenter();
        return transform.position;
    }

    public void RegisterExternalUnit(TeamMember unit)
    {
        if (unit == null || !IsVehicleUnit(unit) || !CanUnitCapture(unit) || !externalUnits.Add(unit)) return;
        if (!colliderCounts.ContainsKey(unit))
        {
            colliderCounts[unit] = 1;
            unitsInside.Add(unit);
            externalInjectedUnits.Add(unit);
        }
    }

    public void UnregisterExternalUnit(TeamMember unit)
    {
        if (unit == null || !externalUnits.Remove(unit)) return;
        if (!externalInjectedUnits.Remove(unit) || !colliderCounts.ContainsKey(unit)) return;
        if (colliderCounts[unit] > 1)
            colliderCounts[unit]--;
        else
        {
            colliderCounts.Remove(unit);
            unitsInside.Remove(unit);
        }
    }

    bool IsVehicleUnit(TeamMember unit)
    {
        Transform root = unit.transform.root;
        return root.GetComponentInChildren<VehicleMotor>(true) != null ||
            root.GetComponentInChildren<TankMotor>(true) != null ||
            root.GetComponentInChildren<TruckCargo>(true) != null ||
            root.GetComponentInChildren<VehicleController>(true) != null;
    }

    public bool CanUnitCapture(TeamMember unit)
    {
        if (structureType == StructureType.City || structureType == StructureType.MainBase)
            return true;

        Transform root = unit.transform.root;
        bool tank = root.GetComponentInChildren<TankMotor>(true) != null;
        bool spg = root.GetComponentInChildren<DeployableVehicle>(true) != null;
        return !tank && !spg;
    }
}
