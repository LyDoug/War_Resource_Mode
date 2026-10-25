using UnityEngine;

public class RadarStation : MonoBehaviour
{
    public static RadarStation TeamARadar;
    public static RadarStation TeamBRadar;

    public Team team;

    public bool operational = true;

    void Awake()
    {
        if (team == Team.TeamA)
            TeamARadar = this;

        if (team == Team.TeamB)
            TeamBRadar = this;
    }

    public bool IsOperational()
    {
        return operational;
    }

    public static bool IsRadarOnline(Team team)
    {
        if (team == Team.TeamA)
        {
            return TeamARadar != null &&
                   TeamARadar.operational;
        }

        if (team == Team.TeamB)
        {
            return TeamBRadar != null &&
                   TeamBRadar.operational;
        }

        return false;
    }

    public void DestroyRadar()
    {
        operational = false;

        Debug.Log("📡 Radar destruído: " + team);
    }

    public void RepairRadar()
    {
        operational = true;

        Debug.Log("📡 Radar restaurado: " + team);
    }
}