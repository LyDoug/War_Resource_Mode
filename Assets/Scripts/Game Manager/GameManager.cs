using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Tickets")]
    public float teamAResources = 500;
    public float teamBResources = 500;

    // =====================================================
    // ESTRUTURAS CAPTURADAS
    // =====================================================

    [Header("Cities")]
    public int teamACities = 0;
    public int teamBCities = 0;

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
    }

    // =====================================================
    // UPDATE
    // =====================================================

    void Update()
    {
        HandleTicketDrain();

        CheckVictory();
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
            EndGame();
        }

        if (teamBResources <= 0.1f)
        {
            Debug.Log("🏆 TEAM A VENCEU!");
            EndGame();
        }
    }

    // =====================================================
    // END GAME
    // =====================================================

    void EndGame()
    {
        gameEnded = true;

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