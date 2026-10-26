using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Player Team")]
    [SerializeField]
    Team playerTeam = Team.Neutral;

    public Team PlayerTeam => playerTeam;
    public bool MatchStarted { get; private set; }
    public bool GameEnded => gameEnded;
    public Team WinningTeam { get; private set; } = Team.Neutral;
    public bool tankBlueprintSelected = false;
    public bool spgBlueprintSelected = false;
    public bool apcBlueprintSelected = false;
    public bool uavBlueprintSelected = false;
    public VehicleSpawnerFinal ActiveSpawner { get; private set; }

    [Header("Tickets")]
    public float teamAResources = 500;
    public float teamBResources = 500;

    // =====================================================
    // ESTRUTURAS CAPTURADAS
    // =====================================================

    [Header("Cities")]
    public int teamACities = 0;
    public int teamBCities = 0;

    [Header("Bases")]
    public int teamABases = 0;
    public int teamBBases = 0;

    [Header("Mines")]
    public int teamAMines = 0;
    public int teamBMines = 0;

    [Header("Factories")]
    public int teamAFactories = 0;
    public int teamBFactories = 0;

    // =====================================================
    // RECURSOS
    // =====================================================

    [Header("Resources")]

    // OIL
    public int teamAOil = 0;
    public int teamBOil = 0;

    // STEEL
    public int teamASteel = 0;
    public int teamBSteel = 0;

    // =====================================================
    // DRAIN
    // =====================================================

    [Header("Drain por segundo")]
    public float drainPerSecond = 5f;

    // =====================================================
    // UNLOCKS
    // =====================================================

    [Header("Unlocks")]
    public bool teamATruckUnlocked = false;
    public bool teamBTruckUnlocked = false;

    public bool teamATankUnlocked = false;
    public bool teamBTankUnlocked = false;

    // =====================================================
    // TRUCK CARGO
    // =====================================================

    [Header("Truck Cargo")]
    public ResourceType teamATruckResource =
        ResourceType.None;

    public ResourceType teamBTruckResource =
        ResourceType.None;

    bool gameEnded = false;

    // =====================================================
    // RADARSTATION
    // =====================================================
    public RadarStation teamARadar;
    public RadarStation teamBRadar;

    // =====================================================
    // AWAKE
    // =====================================================

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Every new battle starts with an empty factory loadout. Players must
        // explicitly choose the two blueprints they want before StartMatch.
        tankBlueprintSelected = false;
        spgBlueprintSelected = false;
        apcBlueprintSelected = false;
        uavBlueprintSelected = false;

        // Build the neutral objective layout before the structures run Start.
        // The authored objects are templates; the runtime map is mirrored so
        // neither team receives a positional resource advantage.
        BalancedBattlefieldLayout layout = GetComponent<BalancedBattlefieldLayout>();
        if (layout == null)
            layout = gameObject.AddComponent<BalancedBattlefieldLayout>();
        layout.Build();

        BattlefieldEnvironment environment = GetComponent<BattlefieldEnvironment>();
        if (environment == null)
            environment = gameObject.AddComponent<BattlefieldEnvironment>();
        environment.Build();

        SetupMainBaseObjectives();

        GameHUD spawnHud = GetComponent<GameHUD>();

        if (spawnHud == null)
            spawnHud = gameObject.AddComponent<GameHUD>();

        spawnHud.showSpawnPanel = true;
    }

    void Start()
    {
        // One commander represents one opposing player. Earlier test builds
        // duplicated this component at runtime, which made the AI dispatch
        // duplicate scouts and resource trucks.
        EnemySpawner[] enemies = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
        for (int index = 1; index < enemies.Length; index++)
            enemies[index].enabled = false;
    }

    void SetupMainBaseObjectives()
    {
        TeamMember[] members = FindObjectsByType<TeamMember>(FindObjectsSortMode.None);
        foreach (TeamMember member in members)
        {
            if (member == null ||
                member.gameObject.name.IndexOf("MainBase", System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            BaseDestructibleWithHealthVisual objective =
                member.GetComponent<BaseDestructibleWithHealthVisual>();
            if (objective == null)
                objective = member.gameObject.AddComponent<BaseDestructibleWithHealthVisual>();

            objective.endsMatchOnDestruction = true;
            objective.mainBaseTeam = member.team;
            objective.maxHealth = 500f;

            FlagController baseFlag = member.GetComponent<FlagController>();
            if (baseFlag == null)
                baseFlag = member.gameObject.AddComponent<FlagController>();
            baseFlag.SetOwner(member.team);

            MainBaseResourceDispatcher dispatcher = member.GetComponent<MainBaseResourceDispatcher>();
            if (dispatcher == null)
                dispatcher = member.gameObject.AddComponent<MainBaseResourceDispatcher>();
            dispatcher.team = member.team;
            dispatcher.inventory = member.GetComponent<BaseInventory>() ??
                member.GetComponentInChildren<BaseInventory>() ?? member.gameObject.AddComponent<BaseInventory>();
            VehicleSpawnerFinal spawner = member.GetComponent<VehicleSpawnerFinal>() ?? member.GetComponentInChildren<VehicleSpawnerFinal>();
            if (spawner != null)
                dispatcher.spawnPoint = spawner.spawnPoint;
        }
    }

    public void MainBaseDestroyed(Team destroyedTeam)
    {
        if (gameEnded || destroyedTeam == Team.Neutral)
            return;

        Team winner = destroyedTeam == Team.TeamA ? Team.TeamB : Team.TeamA;
        Debug.Log("BASE PRINCIPAL " + destroyedTeam + " destruída. " + winner + " venceu!");
        EndGame(winner);
    }

    // =====================================================
    // UPDATE
    // =====================================================

    void Update()
    {
        HandleTeamSelection();

        if (!MatchStarted)
            return;

        HandleTicketDrain();

        CheckVictory();
    }

    void HandleTeamSelection()
    {
        if (MatchStarted)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
            SelectPlayerTeam(Team.TeamA);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            SelectPlayerTeam(Team.TeamB);
    }

    public void SelectPlayerTeam(Team team)
    {
        if (team != Team.TeamA && team != Team.TeamB)
            return;

        playerTeam = team;
        Debug.Log("Equipe selecionada: " + team);
    }

    public bool IsBlueprintSelected(VehicleRole role)
    {
        return role == VehicleRole.Jeep || role == VehicleRole.Truck ||
               (role == VehicleRole.Tank && tankBlueprintSelected) ||
               (role == VehicleRole.Artillery && spgBlueprintSelected) ||
               (role == VehicleRole.APC && apcBlueprintSelected) ||
               (role == VehicleRole.UAV && uavBlueprintSelected);
    }

    public int SelectedFactoryBlueprintCount()
    {
        int count = 0;
        if (tankBlueprintSelected) count++;
        if (spgBlueprintSelected) count++;
        if (apcBlueprintSelected) count++;
        if (uavBlueprintSelected) count++;
        return count;
    }

    public bool TrySetFactoryBlueprint(VehicleRole role, bool selected)
    {
        bool current = IsBlueprintSelected(role);
        if (current == selected)
            return true;

        if (selected && SelectedFactoryBlueprintCount() >= 2)
        {
            Debug.Log("Escolha no maximo duas blueprints para as fabricas.");
            return false;
        }

        switch (role)
        {
            case VehicleRole.Tank: tankBlueprintSelected = selected; break;
            case VehicleRole.Artillery: spgBlueprintSelected = selected; break;
            case VehicleRole.APC: apcBlueprintSelected = selected; break;
            case VehicleRole.UAV: uavBlueprintSelected = selected; break;
            default: return false;
        }
        return true;
    }

    public VehicleRole GetSelectedFactoryBlueprint(int slot)
    {
        VehicleRole[] order = { VehicleRole.Tank, VehicleRole.Artillery, VehicleRole.APC, VehicleRole.UAV };
        int selectedIndex = 0;
        foreach (VehicleRole role in order)
        {
            if (!IsBlueprintSelected(role))
                continue;
            if (selectedIndex == slot)
                return role;
            selectedIndex++;
        }
        return VehicleRole.Jeep;
    }

    public void StartMatch()
    {
        if (playerTeam == Team.Neutral || MatchStarted)
            return;

        if (SelectedFactoryBlueprintCount() != 2)
        {
            Debug.Log("Escolha exatamente duas blueprints para as fabricas antes de iniciar.");
            return;
        }

        MatchStarted = true;

        VehicleSpawnerFinal[] spawners =
            FindObjectsByType<VehicleSpawnerFinal>(FindObjectsSortMode.None);
        foreach (VehicleSpawnerFinal spawner in spawners)
        {
            if (spawner.baseCapture == null &&
                spawner.playerTeam == playerTeam &&
                spawner.gameObject.name.IndexOf("MainBase", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                spawner.TrySpawn(VehicleRole.Jeep);
                break;
            }
        }
    }

    public void RestartMatch()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void SetActiveSpawner(VehicleSpawnerFinal spawner)
    {
        ActiveSpawner = spawner;
    }


    // =====================================================
    // RADAR
    // =====================================================
    public bool HasRadar(Team team)
    {
    if(team == Team.TeamA)
        return teamARadar != null &&
               teamARadar.IsOperational();

    if(team == Team.TeamB)
        return teamBRadar != null &&
               teamBRadar.IsOperational();

    return false;
    }

    // =====================================================
    // DRENO DE TICKETS
    // =====================================================

    void HandleTicketDrain()
    {
        int diff = teamACities - teamBCities;

        // Team A domina mais cidades
        if (diff > 0)
        {
            teamBResources -=
                diff *
                drainPerSecond *
                Time.deltaTime;
        }

        // Team B domina mais cidades
        else if (diff < 0)
        {
            teamAResources -=
                Mathf.Abs(diff) *
                drainPerSecond *
                Time.deltaTime;
        }

        ClampResources();
    }

    // =====================================================
    // REGISTRO DE ESTRUTURAS
    // =====================================================

    public void RegisterStructure(
        StructureType type,
        Team newOwner,
        Team oldOwner
    )
    {
        // remove do dono antigo
        RemoveStructure(type, oldOwner);

        // adiciona ao novo dono
        AddStructure(type, newOwner);

        Debug.Log(
            $"🏴 {type} capturada por {newOwner}"
        );

        PrintStructureStatus();
    }

    // =====================================================
    // ADD STRUCTURE
    // =====================================================

    void AddStructure(
        StructureType type,
        Team owner
    )
    {
        if (owner == Team.TeamA)
        {
            switch (type)
            {
                case StructureType.City:
                    teamACities++;
                    break;

                case StructureType.Mine:
                    teamAMines++;
                    break;

                case StructureType.Factory:
                    teamAFactories++;
                    break;

                case StructureType.MainBase:
                    teamABases++;
                    break;
            }
        }

        else if (owner == Team.TeamB)
        {
            switch (type)
            {
                case StructureType.City:
                    teamBCities++;
                    break;

                case StructureType.Mine:
                    teamBMines++;
                    break;

                case StructureType.Factory:
                    teamBFactories++;
                    break;

                case StructureType.MainBase:
                    teamBBases++;
                    break;
            }
        }
    }

    // =====================================================
    // REMOVE STRUCTURE
    // =====================================================

    void RemoveStructure(
        StructureType type,
        Team owner
    )
    {
        if (owner == Team.TeamA)
        {
            switch (type)
            {
                case StructureType.City:
                    teamACities =
                        Mathf.Max(0, teamACities - 1);
                    break;

                case StructureType.Mine:
                    teamAMines =
                        Mathf.Max(0, teamAMines - 1);
                    break;

                case StructureType.Factory:
                    teamAFactories =
                        Mathf.Max(0, teamAFactories - 1);
                    break;

                case StructureType.MainBase:
                    teamABases =
                        Mathf.Max(0, teamABases - 1);
                    break;
            }
        }

        else if (owner == Team.TeamB)
        {
            switch (type)
            {
                case StructureType.City:
                    teamBCities =
                        Mathf.Max(0, teamBCities - 1);
                    break;

                case StructureType.Mine:
                    teamBMines =
                        Mathf.Max(0, teamBMines - 1);
                    break;

                case StructureType.Factory:
                    teamBFactories =
                        Mathf.Max(0, teamBFactories - 1);
                    break;

                case StructureType.MainBase:
                    teamBBases =
                        Mathf.Max(0, teamBBases - 1);
                    break;
            }
        }
    }

    // =====================================================
    // RECURSOS
    // =====================================================

    public void AddResource(
        Team team,
        ResourceType type,
        int amount
    )
    {
        if (team == Team.TeamA)
        {
            switch (type)
            {
                case ResourceType.Oil:
                    teamAOil += amount;
                    break;

                case ResourceType.Steel:
                    teamASteel += amount;
                    break;
            }
        }

        else if (team == Team.TeamB)
        {
            switch (type)
            {
                case ResourceType.Oil:
                    teamBOil += amount;
                    break;

                case ResourceType.Steel:
                    teamBSteel += amount;
                    break;
            }
        }

        Debug.Log(
            $"📦 {team} recebeu {amount} de {type}"
        );
    }

    // =====================================================
    // GASTAR RECURSOS
    // =====================================================

    public bool TrySpend(
        Team team,
        float amount
    )
    {
        if (team == Team.TeamA)
        {
            if (teamAResources >= amount)
            {
                teamAResources -= amount;
                return true;
            }
        }

        else if (team == Team.TeamB)
        {
            if (teamBResources >= amount)
            {
                teamBResources -= amount;
                return true;
            }
        }

        return false;
    }

    // =====================================================
    // DEBUG HUD STATUS
    // =====================================================

    void PrintStructureStatus()
    {
        Debug.Log(
            $"🏙 Cities A:{teamACities} | B:{teamBCities}"
        );

        Debug.Log(
            $"⛏ Mines A:{teamAMines} | B:{teamBMines}"
        );

        Debug.Log(
            $"🏭 Factories A:{teamAFactories} | B:{teamBFactories}"
        );

        Debug.Log(
            $"🛢 Oil A:{teamAOil} | B:{teamBOil}"
        );

        Debug.Log(
            $"🔩 Steel A:{teamASteel} | B:{teamBSteel}"
        );
    }

    // =====================================================
    // VITÓRIA
    // =====================================================

    void CheckVictory()
    {
        if (gameEnded)
            return;

        if (teamAResources <= 0.1f)
        {
            Debug.Log("🏆 TEAM B VENCEU!");
            EndGame(Team.TeamB);
        }

        if (teamBResources <= 0.1f)
        {
            Debug.Log("🏆 TEAM A VENCEU!");
            EndGame(Team.TeamA);
        }
    }

    // =====================================================
    // END GAME
    // =====================================================

    void EndGame(Team winner)
    {
        gameEnded = true;
        WinningTeam = winner;

        Time.timeScale = 0f;

        Debug.Log("⛔ FIM DE JOGO");
    }

    // =====================================================
    // CLAMP
    // =====================================================

    void ClampResources()
    {
        teamAResources =
            Mathf.Max(teamAResources, 0f);

        teamBResources =
            Mathf.Max(teamBResources, 0f);

        teamAOil =
            Mathf.Max(0, teamAOil);

        teamBOil =
            Mathf.Max(0, teamBOil);

        teamASteel =
            Mathf.Max(0, teamASteel);

        teamBSteel =
            Mathf.Max(0, teamBSteel);
    }
}
