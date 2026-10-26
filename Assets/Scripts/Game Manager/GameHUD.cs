using TMPro;
using UnityEngine;

public class GameHUD : MonoBehaviour
{
    [HideInInspector]
    public bool showSpawnPanel;
    int loadoutStep;

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

        DrawBattleStatus();
        DrawIntelClipboard();
        DrawProductionPopupNew();

        if (!string.IsNullOrEmpty(ResourceNode.PlayerPrompt))
        {
            GUI.Box(new Rect(20f, Screen.height - 245f, 560f, 44f), ResourceNode.PlayerPrompt);
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
        Rect panel = new Rect(Screen.width * 0.5f - width * 0.5f, 60f, width, 470f);
        GUI.Box(panel, "PREPARACAO DA PARTIDA");

        string[] steps = { "1  EQUIPE", "2  BLUEPRINTS", "3  CONFIRMAR" };
        for (int i = 0; i < steps.Length; i++)
        {
            Color old = GUI.color;
            GUI.color = i == loadoutStep ? new Color(0.3f, 0.7f, 1f) : Color.white;
            GUI.Label(new Rect(panel.x + 35f + i * 205f, panel.y + 38f, 180f, 24f), steps[i],
                new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
            GUI.color = old;
        }

        Rect content = new Rect(panel.x + 38f, panel.y + 78f, panel.width - 76f, 290f);
        if (loadoutStep == 0)
            DrawTeamStep(content);
        else if (loadoutStep == 1)
            DrawBlueprintStep(content);
        else
            DrawConfirmStep(content);

        if (loadoutStep > 0 && GUI.Button(new Rect(panel.x + 38f, panel.y + 400f, 130f, 38f), "VOLTAR"))
            loadoutStep--;

        if (loadoutStep < 2)
        {
            GUI.enabled = loadoutStep != 0 || GameManager.Instance.PlayerTeam != Team.Neutral;
            if (GUI.Button(new Rect(panel.x + panel.width - 168f, panel.y + 400f, 130f, 38f), "PROXIMO"))
                loadoutStep++;
            GUI.enabled = true;
        }
        else
        {
            GUI.enabled = GameManager.Instance.SelectedFactoryBlueprintCount() == 2;
            if (GUI.Button(new Rect(panel.x + panel.width - 208f, panel.y + 400f, 170f, 38f), "INICIAR PARTIDA"))
                GameManager.Instance.StartMatch();
            GUI.enabled = true;
        }
    }

    void DrawTeamStep(Rect content)
    {
        GUI.Label(new Rect(content.x, content.y, content.width, 44f),
            "Escolha a equipe. Ambos os times usam o mesmo conjunto inicial de unidades e recursos.",
            new GUIStyle(GUI.skin.label) { wordWrap = true, alignment = TextAnchor.MiddleCenter, fontSize = 15 });

        if (GUI.Button(new Rect(content.x + 35f, content.y + 76f, 255f, 70f), "TEAM AZUL\nDefesa e reconhecimento"))
            GameManager.Instance.SelectPlayerTeam(Team.TeamA);
        if (GUI.Button(new Rect(content.x + content.width - 290f, content.y + 76f, 255f, 70f), "TEAM VERMELHO\nDefesa e reconhecimento"))
            GameManager.Instance.SelectPlayerTeam(Team.TeamB);

        string selected = GameManager.Instance.PlayerTeam == Team.Neutral ? "Nenhum" : GetTeamName(GameManager.Instance.PlayerTeam);
        GUI.Label(new Rect(content.x, content.y + 185f, content.width, 28f), "Equipe selecionada: " + selected,
            new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 16 });
        GUI.Label(new Rect(content.x + 40f, content.y + 228f, content.width - 80f, 45f),
            "Unidade inicial: Jeep Scout (gratuito). Tanques, SPGs e apoio exigem fabrica capturada e recursos locais.",
            new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true });
    }

    void DrawBlueprintStep(Rect content)
    {
        GUI.Label(new Rect(content.x, content.y, content.width, 38f),
            "Marque os blueprints que sua equipe podera fabricar durante esta batalha.",
            new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 15 });

        DrawBlueprintToggle(new Rect(content.x + 8f, content.y + 52f, 275f, 72f), VehicleRole.Tank,
            "TANK", "1 ACO", "Blindado de linha de frente");
        DrawBlueprintToggle(new Rect(content.x + content.width - 283f, content.y + 52f, 275f, 72f), VehicleRole.Artillery,
            "SPG", "1 OLEO", "Artilharia de longo alcance");
        DrawBlueprintToggle(new Rect(content.x + 8f, content.y + 142f, 275f, 72f), VehicleRole.APC,
            "APC / IFV", "1 ACO", "Transporte de infantaria e captura");
        DrawBlueprintToggle(new Rect(content.x + content.width - 283f, content.y + 142f, 275f, 72f), VehicleRole.UAV,
            "UNIDADE UAV", "1 OLEO", "Plataforma de reconhecimento aereo");

        GUI.Label(new Rect(content.x + 20f, content.y + 238f, content.width - 40f, 40f),
            "Selecione exatamente duas blueprints de fabrica. Jeep Scout sempre fica disponivel na Base Principal.",
            new GUIStyle(GUI.skin.label) { wordWrap = true, alignment = TextAnchor.MiddleCenter });
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

    void DrawConfirmStep(Rect content)
    {
        string team = GameManager.Instance.PlayerTeam == Team.Neutral ? "NENHUMA" : GetTeamName(GameManager.Instance.PlayerTeam);
        GUI.Label(new Rect(content.x, content.y, content.width, 30f), "PRONTO PARA A BATALHA",
            new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 18 });
        GUI.Label(new Rect(content.x + 70f, content.y + 60f, content.width - 140f, 170f),
            "EQUIPE: " + team + "\n\nBLUEPRINTS ATIVOS\n" +
            "Jeep Scout - gratuito\n" +
            "Tank - " + BlueprintState(GameManager.Instance.tankBlueprintSelected) + " (1 Aco)\n" +
            "SPG - " + BlueprintState(GameManager.Instance.spgBlueprintSelected) + " (1 Oleo)\n" +
            "APC / IFV - " + BlueprintState(GameManager.Instance.apcBlueprintSelected) + " (1 Aco)\n" +
            "Unidade UAV - " + BlueprintState(GameManager.Instance.uavBlueprintSelected) + " (1 Oleo)",
            new GUIStyle(GUI.skin.label) { fontSize = 14 });
    }

    string BlueprintState(bool selected) => selected ? "SELECIONADO" : "NAO SELECIONADO";

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
