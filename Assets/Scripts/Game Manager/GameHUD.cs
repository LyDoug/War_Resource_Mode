using TMPro;
using UnityEngine;

public class GameHUD : MonoBehaviour
{
    [HideInInspector]
    public bool showSpawnPanel;

    void Start()
    {
        // Older Canvas/TMP labels are authored in the scene and otherwise
        // remain visible as orphaned floating text. The current HUD is drawn
        // exclusively by OnGUI below.
        foreach (TMP_Text legacyText in FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
            legacyText.gameObject.SetActive(false);
    }

    #if false // Legacy TMP HUD removed; the runtime OnGUI HUD below is authoritative.
    void Update()
    {
        if (GameManager.Instance == null)
            return;

        Team team = GetDisplayedTeam();

        // =====================================
        // RESOURCES
        // =====================================
        if (oilText != null)
        {
            oilText.text =
                "OIL " +
                GetDisplayedResource(ResourceType.Oil, team);
        }

        if (steelText != null)
        {
            steelText.text =
                "STEEL " +
                GetDisplayedResource(ResourceType.Steel, team);
        }

        // =====================================
        // STRUCTURES
        // =====================================
        if (mineText != null)
        {
            mineText.text =
                "MINE " +
                GetStructureCount(StructureType.Mine, team);
        }

        if (factoryText != null)
        {
            factoryText.text =
                "FACTORY " +
                GetStructureCount(StructureType.Factory, team);
        }

        if (cityText != null)
        {
            cityText.text =
                "CITY " +
                GetStructureCount(StructureType.City, team);
        }

        // =====================================
        // TICKETS
        // =====================================
        if (ticketsText != null)
        {
            ticketsText.text =
    "A: " +
    Mathf.RoundToInt(GameManager.Instance.teamAResources)
    +
    " | B: " +
    Mathf.RoundToInt(GameManager.Instance.teamBResources);
        }
    }

    Team GetDisplayedTeam()
    {
        return GameManager.Instance.PlayerTeam == Team.Neutral
            ? Team.TeamA
            : GameManager.Instance.PlayerTeam;
    }

    int GetDisplayedResource(ResourceType type, Team team)
    {
        VehicleSpawnerFinal spawner = GameManager.Instance.ActiveSpawner;

        if (spawner != null && spawner.inventory != null)
            return spawner.inventory.GetResource(type);

        if (team == Team.TeamA)
            return type == ResourceType.Oil
                ? GameManager.Instance.teamAOil
                : GameManager.Instance.teamASteel;

        return type == ResourceType.Oil
            ? GameManager.Instance.teamBOil
            : GameManager.Instance.teamBSteel;
    }

    #endif

    int GetStructureCount(StructureType type, Team team)
    {
        if (team == Team.TeamA)
        {
            if (type == StructureType.Mine)
                return GameManager.Instance.teamAMines;
            if (type == StructureType.Factory)
                return GameManager.Instance.teamAFactories;

            return GameManager.Instance.teamACities;
        }

        if (type == StructureType.Mine)
            return GameManager.Instance.teamBMines;
        if (type == StructureType.Factory)
            return GameManager.Instance.teamBFactories;

        return GameManager.Instance.teamBCities;
    }

    void OnGUI()
    {
        if (!showSpawnPanel || GameManager.Instance == null)
            return;

        if (GameManager.Instance.GameEnded)
        {
            DrawEndScreen();
            return;
        }

        if (!GameManager.Instance.MatchStarted)
        {
            DrawLoadoutScreenNew();
            return;
        }

        DrawCentralTickets();
        DrawCombatHUD();

        DrawBattleStatus();
        DrawIntelClipboard();
        DrawProductionPopupNew();

        string interactionPrompt = !string.IsNullOrEmpty(TruckCargo.PlayerPrompt)
            ? TruckCargo.PlayerPrompt : !string.IsNullOrEmpty(MainBaseResourceDispatcher.PlayerPrompt)
                ? MainBaseResourceDispatcher.PlayerPrompt : !string.IsNullOrEmpty(VehicleController.PlayerPrompt)
                    ? VehicleController.PlayerPrompt : ResourceNode.PlayerPrompt;
        if (!string.IsNullOrEmpty(interactionPrompt))
        {
            GUI.Box(new Rect(20f, Screen.height - 245f, 680f, 44f), interactionPrompt);
        }

        /* Legacy lateral production/status block intentionally removed.
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };

        GUIStyle bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            wordWrap = true
        };

        GUI.Box(new Rect(20f, 20f, 400f, 158f), string.Empty);

        Team team = GameManager.Instance.PlayerTeam;
        GUI.color = GetTeamColor(team);
        GUI.Label(
            new Rect(34f, 30f, 372f, 28f),
            team == Team.Neutral
                ? "ESCOLHA SUA EQUIPE"
                : "EQUIPE: " + GetTeamName(team),
            titleStyle
        );

        GUI.color = Color.white;

        if (team == Team.Neutral)
        {
            GUI.Label(
                new Rect(34f, 64f, 372f, 70f),
                "[1]  ■  AZUL\n[2]  ●  VERMELHO\n" + GetScoreStatus() + "\n" + GetBasesStatus(),
                bodyStyle
            );
            return;
        }

        if (GameManager.Instance.ActiveSpawner == null)
        {
            GUI.Label(
                new Rect(34f, 64f, 372f, 70f),
                "Entre na zona de uma base ou fábrica\npara abrir o menu de produção.\n" + GetScoreStatus() + "\n" + GetBasesStatus(),
                bodyStyle
            );
            return;
        }

        VehicleSpawnerFinal spawner = GameManager.Instance.ActiveSpawner;
        GUI.Label(
            new Rect(34f, 64f, 440f, 130f),
            "PRODUÇÃO ATIVA\n[Z] Jipe   [C] Tanque   [V] SPG   [F] Coletar" + GetTruckHint() + "\n" +
            spawner.GetSpawnStatus(VehicleRole.Jeep) + "\n" +
            spawner.GetSpawnStatus(VehicleRole.Tank) + "\n" +
            spawner.GetSpawnStatus(VehicleRole.Artillery) + "\n" +
            GetFactoryStatus(spawner) + "\n" +
            GetScoreStatus() + "\n" +
            GetBasesStatus(),
            bodyStyle
        );
        */
    }

    void DrawIntelClipboard()
    {
        Team enemy = GameManager.Instance.PlayerTeam == Team.TeamA ? Team.TeamB : Team.TeamA;
        int oil = enemy == Team.TeamA ? GameManager.Instance.teamAOil : GameManager.Instance.teamBOil;
        int steel = enemy == Team.TeamA ? GameManager.Instance.teamASteel : GameManager.Instance.teamBSteel;
        int cities = enemy == Team.TeamA ? GameManager.Instance.teamACities : GameManager.Instance.teamBCities;
        string enemyName = enemy == Team.TeamA ? "AZUL" : "VERMELHO";
        Rect clipboard = new Rect(Screen.width - 258f, 198f, 238f, 138f);
        GUI.Box(clipboard, string.Empty);
        GUI.Box(new Rect(clipboard.x + 8f, clipboard.y + 8f, clipboard.width - 16f, 30f), string.Empty);
        GUI.Label(new Rect(clipboard.x + 12f, clipboard.y + 10f, clipboard.width - 24f, 24f), "INTEL",
            new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 15, alignment = TextAnchor.MiddleCenter });
        GUI.Label(new Rect(clipboard.x + 18f, clipboard.y + 42f, clipboard.width - 36f, 82f),
            "INIMIGO: " + enemyName + "\n\nCIDADES CONTROLADAS: " + cities + "\nAÇO ESTIMADO: " + steel + "\nÓLEO ESTIMADO: " + oil,
            new GUIStyle(GUI.skin.label) { fontSize = 13 });
    }

    void DrawBattleStatus()
    {
        Team team = GameManager.Instance.PlayerTeam;
        int cities = GetStructureCount(StructureType.City, team);
        int mines = GetStructureCount(StructureType.Mine, team);
        int factories = GetStructureCount(StructureType.Factory, team);
        Color previous = GUI.color;
        GUI.color = new Color(GetTeamColor(team).r, GetTeamColor(team).g, GetTeamColor(team).b, 0.92f);
        Rect panel = new Rect(Screen.width - 258f, 72f, 238f, 116f);
        GUI.Box(panel, string.Empty);
        GUI.color = Color.white;
        GUI.Label(new Rect(panel.x + 12f, panel.y + 10f, panel.width - 24f, 22f), "SITUAÇÃO TÁTICA",
            new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, fontSize = 15 });
        GUI.Label(new Rect(panel.x + 18f, panel.y + 40f, panel.width - 36f, 64f),
            "CIDADES  " + cities + "     MINAS  " + mines + "\n" +
            "FÁBRICAS  " + factories + "\n" +
            (GameManager.Instance.ActiveSpawner == null ? "Objetivo: capture uma estrutura" : "Produção: zona ativa"),
            new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleLeft });
        GUI.color = previous;
    }

    void DrawProductionPopup()
    {
        VehicleSpawnerFinal spawner = GameManager.Instance.ActiveSpawner;
        if (spawner == null)
            return;

        bool factory = spawner.baseCapture != null && spawner.baseCapture.structureType == StructureType.Factory;
        Rect panel = new Rect(Screen.width * 0.5f - 210f, Screen.height - 175f, 420f, 130f);
        GUI.Box(panel, string.Empty);
        string title = factory ? "FÁBRICA — PRODUÇÃO" : "BASE PRINCIPAL — PRODUÇÃO";
        string steel = spawner.inventory == null ? "—" : spawner.inventory.GetResource(ResourceType.Steel).ToString();
        string oil = spawner.inventory == null ? "—" : spawner.inventory.GetResource(ResourceType.Oil).ToString();
        GUI.Label(new Rect(panel.x + 12f, panel.y + 10f, panel.width - 24f, 22f), title,
            new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 16 });
        GUI.Label(new Rect(panel.x + 24f, panel.y + 40f, panel.width - 48f, 76f),
            "[Z] JEEP SCOUT — gratuito\n" +
            "[C] TANK — 1 AÇO " + (factory ? (steel == "0" ? "(SEM ESTOQUE)" : "(DISPONÍVEL)") : "(SÓ FÁBRICA)") + "\n" +
            "[V] SPG — 1 ÓLEO " + (factory ? (oil == "0" ? "(SEM ESTOQUE)" : "(DISPONÍVEL)") : "(SÓ FÁBRICA)") +
            (factory ? "\nESTOQUE LOCAL: AÇO " + steel + " | ÓLEO " + oil : ""),
            new GUIStyle(GUI.skin.label) { fontSize = 13 });
    }

    void DrawCentralTickets()
    {
        const float width = 440f;
        const float height = 18f;
        float x = Screen.width * 0.5f - width * 0.5f;
        float maxTickets = 500f;
        float blue = Mathf.Clamp01(GameManager.Instance.teamAResources / maxTickets);
        float red = Mathf.Clamp01(GameManager.Instance.teamBResources / maxTickets);

        // The inner edges stay locked to the centre. Each team's territory
        // retracts from the outer edge toward the centre as tickets are lost.
        Color previous = GUI.color;
        GUI.color = new Color(0.15f, 0.48f, 1f);
        GUI.DrawTexture(new Rect(x + width * 0.5f * (1f - blue), 18f, width * 0.5f * blue, height), Texture2D.whiteTexture);
        GUI.color = new Color(1f, 0.18f, 0.18f);
        GUI.DrawTexture(new Rect(x + width * 0.5f, 18f, width * 0.5f * red, height), Texture2D.whiteTexture);
        GUI.color = previous;
        GUI.Label(new Rect(x, 38f, width, 22f),
            "AZUL " + Mathf.CeilToInt(GameManager.Instance.teamAResources) +
            "     CIDADES " + GameManager.Instance.teamACities + " × " + GameManager.Instance.teamBCities +
            "     " + Mathf.CeilToInt(GameManager.Instance.teamBResources) + " VERMELHO",
            new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
    }

    void DrawCombatHUD()
    {
        VehicleController controlled = null;
        foreach (VehicleController candidate in FindObjectsByType<VehicleController>(FindObjectsSortMode.None))
        {
            if (candidate != null && candidate.IsControlled)
            {
                controlled = candidate;
                break;
            }
        }

        if (controlled == null)
        {
            if (GameManager.Instance.PlayerRespawnPending)
                DrawRespawnNotice();
            return;
        }

        Transform root = controlled.transform.root;
        Health health = root.GetComponentInChildren<Health>(true);
        if (health == null)
            return;

        Rect panel = new Rect(20f, Screen.height - 176f, 294f, 142f);
        GUI.Box(panel, string.Empty);
        GUI.Label(new Rect(panel.x + 12f, panel.y + 10f, panel.width - 24f, 22f), GetVehicleLabel(root),
            new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold });

        GUI.Label(new Rect(panel.x + 12f, panel.y + 38f, 64f, 20f), "VIDA");
        DrawMeter(new Rect(panel.x + 78f, panel.y + 42f, 196f, 14f), health.HealthFraction,
            health.HealthFraction > 0.35f ? new Color(0.22f, 0.86f, 0.30f) : new Color(1f, 0.30f, 0.18f));
        GUI.Label(new Rect(panel.x + 78f, panel.y + 59f, 196f, 18f),
            Mathf.CeilToInt(health.CurrentHealth) + " / " + Mathf.CeilToInt(health.maxHealth) +
            "     BLINDAGEM " + Mathf.CeilToInt(health.CurrentArmorIntegrity) +
            " / " + Mathf.CeilToInt(health.MaxArmorIntegrity),
            new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter });

        string primary = GetPrimaryWeaponStatus(root);
        string secondary = GetSecondaryWeaponStatus(root);
        GUI.Label(new Rect(panel.x + 12f, panel.y + 84f, panel.width - 24f, 22f), primary,
            new GUIStyle(GUI.skin.label) { fontSize = 12 });
        if (!string.IsNullOrEmpty(secondary))
            GUI.Label(new Rect(panel.x + 12f, panel.y + 108f, panel.width - 24f, 22f), secondary,
                new GUIStyle(GUI.skin.label) { fontSize = 12 });

        SPGWeapon spg = root.GetComponentInChildren<SPGWeapon>(true);
        if (spg != null && spg.FireComputerActive)
        {
            DrawIndirectFireControl(spg);
            DrawArtilleryMap(spg);
            DrawDeploymentMessage(spg);
        }
    }

    void DrawIndirectFireControl(SPGWeapon spg)
    {
        Rect panel = new Rect(Screen.width * 0.5f - 165f, 74f, 330f, 114f);
        GUI.Box(panel, string.Empty);

        GUI.Label(new Rect(panel.x + 10f, panel.y + 7f, panel.width - 20f, 20f),
            "COMPUTADOR DE TIRO  [Q] FECHAR",
            new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 12 });

        string solution = !spg.HasFireSolution ? "FORA DE ALCANCE" :
            spg.HasMapTarget && !spg.IsAimed ? "ALINHANDO" : "PRONTO";
        GUI.Label(new Rect(panel.x + 12f, panel.y + 31f, panel.width - 24f, 20f),
            "DIST " + spg.AimRange.ToString("0") + "m    ELEV " + spg.ElevationDegrees.ToString("0.0") + " deg    AZ " + spg.AzimuthDegrees.ToString("0") + " deg",
            new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 12 });
        GUI.Label(new Rect(panel.x + 12f, panel.y + 53f, panel.width - 24f, 20f),
            "CARGA " + spg.ChargeSpeed.ToString("0") + " m/s     " + solution,
            new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 12 });
        GUI.Label(new Rect(panel.x + 10f, panel.y + 77f, panel.width - 20f, 20f),
            "MUNICAO: " + spg.AmmoName + "   [F1] HE  [F2] SMOKE",
            new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 11 });

        GUI.Label(new Rect(Screen.width * 0.5f - 8f, Screen.height * 0.5f - 12f, 16f, 24f), "+",
            new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 22 });
    }

    void DrawDeploymentMessage(SPGWeapon spg)
    {
        if (string.IsNullOrEmpty(spg.DeploymentMessage))
            return;

        Color previous = GUI.color;
        float pulse = 0.35f + Mathf.PingPong(Time.time * 2.8f, 0.65f);
        GUI.color = new Color(1f, 0.66f, 0.08f, pulse);
        GUI.Label(new Rect(Screen.width * 0.5f - 150f, 46f, 300f, 24f), spg.DeploymentMessage,
            new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 16
            });
        GUI.color = previous;
    }

    void DrawArtilleryMap(SPGWeapon spg)
    {
        float size = Mathf.Min(480f, Screen.height * 0.60f);
        // Keep the tactical map in the same central fire-control column so
        // it cannot be covered by the right-side battlefield UI.
        float mapY = Mathf.Min(260f, Screen.height - size - 30f);
        Rect map = new Rect(Screen.width * 0.5f - size * 0.5f, mapY, size, size);
        Rect mapContent = new Rect(map.x + 4f, map.y + 23f, map.width - 8f, map.height - 27f);
        GetArtilleryMapBounds(out Vector3 center, out float halfSize);

        GUI.Box(map, "FCS // SITUACAO TATICA // CLIQUE: MARCAR IMPACTO");
        Color previous = GUI.color;
        GUI.color = new Color(0.13f, 0.34f, 0.13f, 0.95f);
        GUI.DrawTexture(mapContent, Texture2D.whiteTexture);
        DrawMapGrid(mapContent);

        // Draw the actual river orientation rather than assuming it is a
        // vertical line on the world X/Z map.
        GameObject river = GameObject.Find("Central River");
        if (river != null)
        {
            GUI.color = new Color(0.08f, 0.30f, 0.75f, 0.8f);
            float riverLength = river.transform.lossyScale.z;
            for (int step = 0; step <= 48; step++)
            {
                float t = step / 48f - 0.5f;
                Vector3 worldPoint = river.transform.position + river.transform.forward * (riverLength * t);
                Vector2 riverPoint = WorldToMap(worldPoint, mapContent, center, halfSize);
                if (mapContent.Contains(riverPoint))
                    GUI.DrawTexture(new Rect(riverPoint.x - 2f, riverPoint.y - 2f, 4f, 4f), Texture2D.whiteTexture);
            }
        }

        // Apply the veil after terrain features: nothing beyond the tactical
        // vision circle (unless illuminated by friendly smoke) leaks through.
        DrawMapFog(spg, mapContent, center, halfSize);

        foreach (CaptureTriggerAdvanced structure in FindObjectsByType<CaptureTriggerAdvanced>(FindObjectsSortMode.None))
        {
            if (!IsMapVisible(spg, structure.transform.position))
                continue;

            Vector2 point = WorldToMap(structure.transform.position, mapContent, center, halfSize);
            if (!mapContent.Contains(point))
                continue;

            TeamMember team = structure.GetComponentInParent<TeamMember>();
            GUI.color = team != null ? GetTeamColor(team.team) : Color.white;
            GUI.Label(new Rect(point.x - 7f, point.y - 7f, 14f, 14f), GetStructureMapSymbol(structure),
                new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 12 });
        }

        // Enemy units are intentionally absent until a friendly smoke or
        // flare observation circle covers their world position.
        foreach (VehicleMotor enemyMotor in FindObjectsByType<VehicleMotor>(FindObjectsSortMode.None))
        {
            if (enemyMotor == null || enemyMotor.transform.root == spg.transform.root)
                continue;

            TeamMember enemyTeam = enemyMotor.GetComponentInChildren<TeamMember>(true);
            if (enemyTeam == null || enemyTeam.team == Team.Neutral || enemyTeam.team == spg.OwningTeam ||
                !IsMapVisible(spg, enemyMotor.transform.position))
                continue;

            Vector2 enemyPoint = WorldToMap(enemyMotor.transform.position, mapContent, center, halfSize);
            if (!mapContent.Contains(enemyPoint))
                continue;

            GUI.color = Color.red;
            GUI.Label(new Rect(enemyPoint.x - 7f, enemyPoint.y - 7f, 14f, 14f), "X",
                new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 });
        }

        // Local player's SPG position: compact green triangle.
        Vector2 playerPoint = WorldToMap(spg.transform.root.position, mapContent, center, halfSize);
        float scale = mapContent.width / (halfSize * 2f);
        DrawMapRangeRing(playerPoint, spg.tacticalVisionRange * scale, mapContent, new Color(0.20f, 0.95f, 0.35f, 0.72f));
        DrawMapRangeRing(playerPoint, spg.minimumFiringRange * scale, mapContent, new Color(1f, 0.35f, 0.12f, 0.88f));
        GUI.color = Color.green;
        GUI.Label(new Rect(playerPoint.x - 7f, playerPoint.y - 9f, 14f, 14f), "▲",
            new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14, fontStyle = FontStyle.Bold });

        // The observer smoke is represented in the tactical view by a red X.
        if (spg.TryGetSpotterMarkerPosition(out Vector3 spotterPosition))
        {
            Vector2 spotterPoint = WorldToMap(spotterPosition, mapContent, center, halfSize);
            if (mapContent.Contains(spotterPoint))
            {
                GUI.color = Color.red;
                GUI.Label(new Rect(spotterPoint.x - 8f, spotterPoint.y - 10f, 16f, 18f), "X",
                    new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold });
            }
        }

        if (spg.HasMapTarget)
        {
            Vector2 target = WorldToMap(spg.AimPoint, mapContent, center, halfSize);
            DrawMapDashedLine(playerPoint, target, mapContent, new Color(1f, 0.84f, 0.18f, 0.85f));
            GUI.color = Color.yellow;
            GUI.DrawTexture(new Rect(target.x - 7f, target.y - 1f, 14f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(target.x - 1f, target.y - 7f, 2f, 14f), Texture2D.whiteTexture);
        }

        if (spg.HasRecentShot)
        {
            Vector2 impact = WorldToMap(spg.LastShotTarget, mapContent, center, halfSize);
            if (mapContent.Contains(impact))
            {
                GUI.color = new Color(1f, 0.45f, 0.05f, 1f);
                DrawMapRangeRing(impact, 14f, mapContent, GUI.color);
            }
        }

        GUI.color = previous;
        GUI.Label(new Rect(map.x, map.yMax + 3f, map.width, 18f),
            "F FABRICA | C CIDADE | S MINA ACO | O MINA OLEO | ▲ SPG | X OBSERVADOR/INIMIGO",
            new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 11 });

        Event current = Event.current;
        if (current.type != EventType.MouseDown || !mapContent.Contains(current.mousePosition))
            return;

        if (current.button == 0)
        {
            spg.SetMapTarget(MapToWorld(current.mousePosition, mapContent, center, halfSize));
            current.Use();
        }
        else if (current.button == 1)
        {
            spg.ClearMapTarget();
            current.Use();
        }
    }

    bool IsMapVisible(SPGWeapon spg, Vector3 worldPosition)
    {
        return spg.IsTargetObserved(worldPosition);
    }

    void DrawMapFog(SPGWeapon spg, Rect mapContent, Vector3 center, float halfSize)
    {
        const int cells = 24;
        Color previous = GUI.color;
        GUI.color = new Color(0.015f, 0.025f, 0.02f, 0.82f);
        float width = mapContent.width / cells;
        float height = mapContent.height / cells;
        for (int row = 0; row < cells; row++)
        {
            for (int column = 0; column < cells; column++)
            {
                Vector2 mapPoint = new Vector2(mapContent.x + (column + 0.5f) * width,
                    mapContent.y + (row + 0.5f) * height);
                float worldX = Mathf.Lerp(center.x - halfSize, center.x + halfSize,
                    Mathf.InverseLerp(mapContent.x, mapContent.xMax, mapPoint.x));
                float worldZ = Mathf.Lerp(center.z + halfSize, center.z - halfSize,
                    Mathf.InverseLerp(mapContent.y, mapContent.yMax, mapPoint.y));
                if (!spg.IsTargetObserved(new Vector3(worldX, center.y, worldZ)))
                    GUI.DrawTexture(new Rect(mapContent.x + column * width, mapContent.y + row * height,
                        width + 1f, height + 1f), Texture2D.whiteTexture);
            }
        }
        GUI.color = previous;
    }

    string GetStructureMapSymbol(CaptureTriggerAdvanced structure)
    {
        if (structure.structureType == StructureType.Factory) return "F";
        if (structure.structureType == StructureType.City) return "C";
        if (structure.resourceNode != null && structure.resourceNode.resourceType == ResourceType.Oil) return "O";
        return "S";
    }

    void DrawMapGrid(Rect mapContent)
    {
        const int cells = 8;
        Color previous = GUI.color;
        GUI.color = new Color(0.55f, 0.82f, 0.55f, 0.22f);
        for (int index = 1; index < cells; index++)
        {
            float x = mapContent.x + mapContent.width * index / cells;
            float y = mapContent.y + mapContent.height * index / cells;
            GUI.DrawTexture(new Rect(x, mapContent.y, 1f, mapContent.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(mapContent.x, y, mapContent.width, 1f), Texture2D.whiteTexture);
        }

        GUI.color = new Color(0.75f, 0.95f, 0.75f, 0.8f);
        GUIStyle style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 9, fontStyle = FontStyle.Bold };
        for (int index = 0; index < cells; index++)
        {
            float x = mapContent.x + mapContent.width * (index + 0.5f) / cells;
            float y = mapContent.y + mapContent.height * (index + 0.5f) / cells;
            GUI.Label(new Rect(x - 7f, mapContent.y + 1f, 14f, 12f), ((char)('A' + index)).ToString(), style);
            GUI.Label(new Rect(mapContent.x + 1f, y - 6f, 14f, 12f), (index + 1).ToString(), style);
        }
        GUI.color = previous;
    }

    void DrawMapRangeRing(Vector2 center, float radius, Rect clip, Color color)
    {
        if (radius < 2f)
            return;

        Color previous = GUI.color;
        GUI.color = color;
        const int segments = 64;
        for (int index = 0; index < segments; index++)
        {
            float angle = index * Mathf.PI * 2f / segments;
            Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            if (clip.Contains(point))
                GUI.DrawTexture(new Rect(point.x - 1f, point.y - 1f, 2f, 2f), Texture2D.whiteTexture);
        }
        GUI.color = previous;
    }

    void DrawMapDashedLine(Vector2 start, Vector2 end, Rect clip, Color color)
    {
        Vector2 offset = end - start;
        float distance = offset.magnitude;
        if (distance < 1f)
            return;

        Vector2 direction = offset / distance;
        Color previous = GUI.color;
        GUI.color = color;
        const float dashLength = 7f;
        const float gapLength = 5f;
        for (float traveled = 4f; traveled < distance - 4f; traveled += dashLength + gapLength)
        {
            Vector2 dash = start + direction * traveled;
            if (clip.Contains(dash))
                GUI.DrawTexture(new Rect(dash.x - 1.5f, dash.y - 1.5f, 3f, 3f), Texture2D.whiteTexture);
        }
        GUI.color = previous;
    }

    void GetArtilleryMapBounds(out Vector3 center, out float halfSize)
    {
        TeamMember teamA = null;
        TeamMember teamB = null;
        foreach (TeamMember member in FindObjectsByType<TeamMember>(FindObjectsSortMode.None))
        {
            if (member.gameObject.name.IndexOf("MainBase", System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            if (member.team == Team.TeamA) teamA = member;
            if (member.team == Team.TeamB) teamB = member;
        }

        if (teamA != null && teamB != null)
        {
            center = (teamA.transform.position + teamB.transform.position) * 0.5f;
            halfSize = Mathf.Max(170f, Vector3.Distance(teamA.transform.position, teamB.transform.position) * 0.75f);
            return;
        }

        center = Vector3.zero;
        halfSize = 180f;
    }

    Vector2 WorldToMap(Vector3 world, Rect map, Vector3 center, float halfSize)
    {
        float x = Mathf.InverseLerp(center.x - halfSize, center.x + halfSize, world.x);
        float y = Mathf.InverseLerp(center.z + halfSize, center.z - halfSize, world.z);
        return new Vector2(map.x + x * map.width, map.y + y * map.height);
    }

    Vector3 MapToWorld(Vector2 point, Rect map, Vector3 center, float halfSize)
    {
        float x = Mathf.Lerp(center.x - halfSize, center.x + halfSize, Mathf.InverseLerp(map.x, map.xMax, point.x));
        float z = Mathf.Lerp(center.z + halfSize, center.z - halfSize, Mathf.InverseLerp(map.y, map.yMax, point.y));
        Vector3 target = new Vector3(x, center.y, z);
        if (Physics.Raycast(target + Vector3.up * 250f, Vector3.down, out RaycastHit hit, 500f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            target.y = hit.point.y;
        return target;
    }

    void DrawMeter(Rect rect, float value, Color fill)
    {
        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = fill;
        GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * Mathf.Clamp01(value), rect.height - 4f), Texture2D.whiteTexture);
        GUI.color = previous;
    }

    void DrawRespawnNotice()
    {
        Rect notice = new Rect(Screen.width * 0.5f - 180f, Screen.height * 0.5f - 28f, 360f, 56f);
        GUI.Box(notice, string.Empty);
        GUI.Label(new Rect(notice.x + 8f, notice.y + 16f, notice.width - 16f, 24f), "VEICULO DESTRUIDO - RETORNANDO A BASE...",
            new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 15 });
    }

    string GetVehicleLabel(Transform root)
    {
        VehicleMotor motor = root.GetComponentInChildren<VehicleMotor>(true);
        if (motor != null && motor.blueprint != null)
        {
            switch (motor.blueprint.role)
            {
                case VehicleRole.Jeep: return "JEEP SCOUT - METRALHADORA TRASEIRA";
                case VehicleRole.Tank: return "TANK";
                case VehicleRole.Artillery: return "SPG";
                case VehicleRole.APC: return "APC / IFV";
                case VehicleRole.UAV: return "UAV";
            }
        }
        return root.name.ToUpperInvariant();
    }

    string GetPrimaryWeaponStatus(Transform root)
    {
        TankGunBallistic[] tankGuns = root.GetComponentsInChildren<TankGunBallistic>(true);
        foreach (TankGunBallistic gun in tankGuns)
            if (!gun.useSecondaryInput)
                return "[LMB] CANHAO PRINCIPAL  " + CooldownText(gun.CooldownRemaining);

        SPGWeapon spg = root.GetComponentInChildren<SPGWeapon>(true);
        if (spg != null)
            return "[ESPACO] ARTILHARIA  " + CooldownText(spg.CooldownRemaining);

        IFVAutocannon autocannon = root.GetComponentInChildren<IFVAutocannon>(true);
        if (autocannon != null)
            return "[LMB] .50 RAJADA  " + CooldownText(autocannon.CooldownRemaining);

        return "SEM ARMA PRIMARIA";
    }

    string GetSecondaryWeaponStatus(Transform root)
    {
        foreach (TankGunBallistic gun in root.GetComponentsInChildren<TankGunBallistic>(true))
            if (gun.useSecondaryInput)
                return "[ESPACO] .50 SECUNDARIA  " + CooldownText(gun.CooldownRemaining);

        VehicleMotor motor = root.GetComponentInChildren<VehicleMotor>(true);
        if (motor != null && motor.blueprint != null &&
            (motor.blueprint.role == VehicleRole.Jeep || motor.blueprint.role == VehicleRole.APC))
            return "[G] FUMACA MARCADORA";

        return string.Empty;
    }

    string CooldownText(float cooldown)
    {
        return cooldown > 0.05f ? "RECARGA " + cooldown.ToString("0.0") + "s" : "PRONTO";
    }

    Color GetTeamColor(Team team)
    {
        if (team == Team.TeamA)
            return new Color(0.35f, 0.65f, 1f);

        if (team == Team.TeamB)
            return new Color(1f, 0.35f, 0.35f);

        return Color.white;
    }

    string GetTeamName(Team team)
    {
        return team == Team.TeamA ? "AZUL" : "VERMELHO";
    }

    string GetBasesStatus()
    {
        return "Bases capturadas  Azul: " +
               GameManager.Instance.teamABases +
               "  |  Vermelho: " +
               GameManager.Instance.teamBBases;
    }

    string GetScoreStatus()
    {
        return "PLACAR  Azul: " + Mathf.CeilToInt(GameManager.Instance.teamAResources) +
               "  |  Vermelho: " + Mathf.CeilToInt(GameManager.Instance.teamBResources);
    }

    string GetTruckHint()
    {
        foreach (TruckCargo truck in FindObjectsByType<TruckCargo>(FindObjectsSortMode.None))
        {
            TeamMember member = truck.GetComponentInParent<TeamMember>();
            Rigidbody body = truck.GetComponentInParent<Rigidbody>();
            if (member != null && body != null && !body.isKinematic &&
                member.team == GameManager.Instance.PlayerTeam)
                return "   [B] Abandonar caminhão";
        }
        return string.Empty;
    }

    void DrawLoadoutScreen()
    {
        GUI.Box(new Rect(Screen.width * 0.5f - 260f, 80f, 520f, 400f), "PREPARAÇÃO DA PARTIDA");
        GUI.Label(new Rect(Screen.width * 0.5f - 230f, 120f, 460f, 70f),
            "Escolha seu time e os blueprints liberados nas fábricas.\nJeep Scout é gratuito; tanque e SPG consomem recursos locais.");

        if (GUI.Button(new Rect(Screen.width * 0.5f - 230f, 205f, 210f, 40f), "TEAM AZUL"))
            GameManager.Instance.SelectPlayerTeam(Team.TeamA);
        if (GUI.Button(new Rect(Screen.width * 0.5f + 20f, 205f, 210f, 40f), "TEAM VERMELHO"))
            GameManager.Instance.SelectPlayerTeam(Team.TeamB);

        string selected = GameManager.Instance.PlayerTeam == Team.Neutral ? "Nenhum" : GetTeamName(GameManager.Instance.PlayerTeam);
        GUI.Label(new Rect(Screen.width * 0.5f - 230f, 260f, 460f, 24f), "Time selecionado: " + selected);
        GUI.Label(new Rect(Screen.width * 0.5f - 230f, 290f, 460f, 55f),
            "Veículos\nJeep Scout — gratuito | Tank — 1 Aço | SPG — 1 Oil");

        GameManager.Instance.tankBlueprintSelected = GUI.Toggle(
            new Rect(Screen.width * 0.5f - 230f, 355f, 210f, 24f),
            GameManager.Instance.tankBlueprintSelected, " Tank — 1 Aço");
        GameManager.Instance.spgBlueprintSelected = GUI.Toggle(
            new Rect(Screen.width * 0.5f + 20f, 355f, 210f, 24f),
            GameManager.Instance.spgBlueprintSelected, " SPG — 1 Oil");

        GUI.enabled = GameManager.Instance.PlayerTeam != Team.Neutral;
        if (GUI.Button(new Rect(Screen.width * 0.5f - 110f, 410f, 220f, 42f), "INICIAR PARTIDA"))
            GameManager.Instance.StartMatch();
        GUI.enabled = true;
    }

    void DrawLoadoutScreenNew()
    {
        const float width = 680f;
        Rect panel = new Rect(Screen.width * 0.5f - width * 0.5f, 60f, width, 440f);
        GUI.Box(panel, "PREPARACAO DA PARTIDA");

        DrawTeamSlider(new Rect(panel.x + panel.width * 0.5f - 130f, panel.y + 48f, 260f, 34f));
        DrawBlueprintStep(new Rect(panel.x + 38f, panel.y + 116f, panel.width - 76f, 230f));

        GUI.enabled = GameManager.Instance.PlayerTeam != Team.Neutral &&
            GameManager.Instance.SelectedFactoryBlueprintCount() == 2;
        if (GUI.Button(new Rect(panel.x + panel.width * 0.5f - 110f, panel.y + 370f, 220f, 42f), "INICIAR PARTIDA"))
            GameManager.Instance.StartMatch();
        GUI.enabled = true;
    }

    void DrawTeamSlider(Rect rect)
    {
        Color old = GUI.color;
        bool blueSelected = GameManager.Instance.PlayerTeam == Team.TeamA;
        bool redSelected = GameManager.Instance.PlayerTeam == Team.TeamB;

        GUI.color = blueSelected ? new Color(0.15f, 0.48f, 1f) : new Color(0.15f, 0.48f, 1f, 0.38f);
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * 0.5f, rect.height), Texture2D.whiteTexture);
        GUI.color = redSelected ? new Color(1f, 0.18f, 0.18f) : new Color(1f, 0.18f, 0.18f, 0.38f);
        GUI.DrawTexture(new Rect(rect.x + rect.width * 0.5f, rect.y, rect.width * 0.5f, rect.height), Texture2D.whiteTexture);
        GUI.color = old;

        Color oldBackground = GUI.backgroundColor;
        GUI.backgroundColor = blueSelected ? new Color(0.15f, 0.48f, 1f) : new Color(0.15f, 0.48f, 1f, 0.55f);
        if (GUI.Button(new Rect(rect.x, rect.y, rect.width * 0.5f, rect.height), "AZUL"))
            GameManager.Instance.SelectPlayerTeam(Team.TeamA);
        GUI.backgroundColor = redSelected ? new Color(1f, 0.18f, 0.18f) : new Color(1f, 0.18f, 0.18f, 0.55f);
        if (GUI.Button(new Rect(rect.x + rect.width * 0.5f, rect.y, rect.width * 0.5f, rect.height), "VERMELHO"))
            GameManager.Instance.SelectPlayerTeam(Team.TeamB);
        GUI.backgroundColor = oldBackground;
    }

    void DrawBlueprintStep(Rect content)
    {
        DrawBlueprintToggle(new Rect(content.x + 8f, content.y + 8f, 275f, 72f), VehicleRole.Tank,
            "TANK", "1 ACO", "Blindado de linha de frente");
        DrawBlueprintToggle(new Rect(content.x + content.width - 283f, content.y + 8f, 275f, 72f), VehicleRole.Artillery,
            "SPG", "1 OLEO", "Artilharia de longo alcance");
        DrawBlueprintToggle(new Rect(content.x + 8f, content.y + 98f, 275f, 72f), VehicleRole.APC,
            "APC / IFV", "1 ACO", "Transporte de infantaria e captura");
        DrawBlueprintToggle(new Rect(content.x + content.width - 283f, content.y + 98f, 275f, 72f), VehicleRole.UAV,
            "UNIDADE UAV", "1 OLEO", "Plataforma de reconhecimento aereo");
    }

    void DrawBlueprintToggle(Rect rect, VehicleRole blueprintRole, string name, string cost, string role)
    {
        GUI.Box(rect, string.Empty);
        bool value = GameManager.Instance.IsBlueprintSelected(blueprintRole);
        bool requested = GUI.Toggle(new Rect(rect.x + 12f, rect.y + 10f, rect.width - 24f, 22f), value, name + " - " + cost,
            new GUIStyle(GUI.skin.toggle) { fontStyle = FontStyle.Bold });
        if (requested != value)
            GameManager.Instance.TrySetFactoryBlueprint(blueprintRole, requested);
        GUI.Label(new Rect(rect.x + 30f, rect.y + 38f, rect.width - 42f, 22f), role,
            new GUIStyle(GUI.skin.label) { fontSize = 12 });
    }

    void DrawProductionPopupNew()
    {
        VehicleSpawnerFinal spawner = GameManager.Instance.ActiveSpawner;
        if (spawner == null)
            return;

        bool factory = spawner.baseCapture != null && spawner.baseCapture.structureType == StructureType.Factory;
        Rect panel = new Rect(Screen.width * 0.5f - 210f, Screen.height - 175f, 420f, 130f);
        GUI.Box(panel, string.Empty);
        string title = factory ? "FABRICA - PRODUCAO" : "BASE PRINCIPAL - PRODUCAO";
        string steel = spawner.inventory == null ? "-" : spawner.inventory.GetResource(ResourceType.Steel).ToString();
        string oil = spawner.inventory == null ? "-" : spawner.inventory.GetResource(ResourceType.Oil).ToString();
        GUI.Label(new Rect(panel.x + 12f, panel.y + 10f, panel.width - 24f, 22f), title,
            new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 16 });
        GUI.Label(new Rect(panel.x + 24f, panel.y + 40f, panel.width - 48f, 76f),
            "[1] JEEP SCOUT - gratuito\n" +
            "[2] " + GetProductionSlotText(0, steel, oil, factory) + "\n" +
            "[3] " + GetProductionSlotText(1, steel, oil, factory) +
            (factory ? "\nESTOQUE LOCAL: ACO " + steel + " | OLEO " + oil : ""),
            new GUIStyle(GUI.skin.label) { fontSize = 13 });
    }

    string GetProductionSlotText(int slot, string steel, string oil, bool factory)
    {
        VehicleRole role = GameManager.Instance.GetSelectedFactoryBlueprint(slot);
        string name = role == VehicleRole.Tank ? "TANK" : role == VehicleRole.Artillery ? "SPG" :
            role == VehicleRole.APC ? "APC / IFV" : "UNIDADE UAV";
        bool needsSteel = role == VehicleRole.Tank || role == VehicleRole.APC;
        string resource = needsSteel ? "1 ACO" : "1 OLEO";
        string stock = needsSteel ? steel : oil;
        return name + " - " + resource + " " +
               (factory ? (stock == "0" ? "(SEM ESTOQUE)" : "(DISPONIVEL)") : "(SO FABRICA)");
    }

    void DrawEndScreen()
    {
        string winner = GameManager.Instance.WinningTeam == Team.TeamA ? "Team Azul venceu" : "Team Vermelho venceu";
        GUI.Box(new Rect(Screen.width * 0.5f - 200f, Screen.height * 0.5f - 100f, 400f, 200f), string.Empty);
        GUI.Label(new Rect(Screen.width * 0.5f - 150f, Screen.height * 0.5f - 55f, 300f, 35f), winner,
            new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
        if (GUI.Button(new Rect(Screen.width * 0.5f - 100f, Screen.height * 0.5f + 20f, 200f, 42f), "REINICIAR"))
            GameManager.Instance.RestartMatch();
    }

    string GetFactoryStatus(VehicleSpawnerFinal spawner)
    {
        if (spawner.inventory == null)
            return "Estoque da fábrica: indisponível";
        return "FÁBRICA  Aço: " + spawner.inventory.GetResource(ResourceType.Steel) +
               " | Óleo: " + spawner.inventory.GetResource(ResourceType.Oil) +
               " | Blueprints: Tank " + (GameManager.Instance.tankBlueprintSelected ? "✓" : "—") +
               ", SPG " + (GameManager.Instance.spgBlueprintSelected ? "✓" : "—");
    }
}
