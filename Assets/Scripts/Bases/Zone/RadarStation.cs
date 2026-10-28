using UnityEngine;

public class RadarStation : MonoBehaviour
{
    public static RadarStation TeamARadar;
    public static RadarStation TeamBRadar;

    public Team team;

    public bool operational = true;
    CaptureTriggerAdvanced capture;
    BaseDestructibleWithHealthVisual health;
    Transform dish;
    [Min(1f)] public float dishRotationSpeed = 48f;

    void Awake()
    {
        capture = GetComponentInChildren<CaptureTriggerAdvanced>(true);
        health = GetComponentInParent<BaseDestructibleWithHealthVisual>() ??
            GetComponent<BaseDestructibleWithHealthVisual>();
        foreach (Transform candidate in GetComponentsInChildren<Transform>(true))
        {
            if (candidate != transform && candidate.name.IndexOf("Radar", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                dish = candidate;
                break;
            }
        }
        RefreshStatus();
    }

    void Update()
    {
        RefreshStatus();
        if (operational && dish != null)
            dish.Rotate(Vector3.up, dishRotationSpeed * Time.deltaTime, Space.Self);
    }

    void RefreshStatus()
    {
        if (health == null)
            health = GetComponentInParent<BaseDestructibleWithHealthVisual>() ??
                GetComponent<BaseDestructibleWithHealthVisual>();
        if (TeamARadar == this && team != Team.TeamA)
            TeamARadar = null;
        if (TeamBRadar == this && team != Team.TeamB)
            TeamBRadar = null;

        if (capture != null)
            team = capture.GetOwner();

        operational = team != Team.Neutral && (health == null || !health.IsDestroyed());
        if (team == Team.TeamA)
            TeamARadar = this;
        else if (team == Team.TeamB)
            TeamBRadar = this;

        if (GameManager.Instance != null)
        {
            if (team == Team.TeamA)
                GameManager.Instance.teamARadar = this;
            else if (team == Team.TeamB)
                GameManager.Instance.teamBRadar = this;
        }
    }

    public bool IsOperational()
    {
        return operational;
    }

    public static bool IsRadarOnline(Team team)
    {
        return team == Team.TeamA ? TeamARadar != null && TeamARadar.operational :
            team == Team.TeamB && TeamBRadar != null && TeamBRadar.operational;
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
