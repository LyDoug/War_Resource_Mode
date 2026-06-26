using UnityEngine;

public class RadarStation : MonoBehaviour
{
    public Team team;

    public bool operational = true;

    public bool IsOperational()
    {
        return operational;
    }

    public void DestroyRadar()
    {
        operational = false;

        Debug.Log("📡 Radar destruído: " + team);
    }

    public void RepairRadar()
    {
        operational = true;

        Debug.Log("📡 Radar restaurado");
    }
}