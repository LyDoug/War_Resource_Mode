using UnityEngine;

public class ArtilleryComputer : MonoBehaviour
{
    [Header("Controls")]
    public KeyCode openKey = KeyCode.F;

    public bool computerActive = false;

    private ArtilleryCamera artilleryCamera;
    private DeployableVehicle deploy;
    private TeamMember teamMember;

    // =====================================================
    // AWAKE
    // =====================================================

    void Awake()
    {
        deploy = GetComponent<DeployableVehicle>();

        if (deploy == null)
            deploy = GetComponentInChildren<DeployableVehicle>();

        teamMember = GetComponent<TeamMember>();

        if (teamMember == null)
            teamMember = GetComponentInChildren<TeamMember>();

        artilleryCamera =
            FindObjectOfType<ArtilleryCamera>();
    }

    // =====================================================
    // UPDATE
    // =====================================================

    void Update()
    {
        if (deploy == null)
            return;

        // Só funciona quando montada
        if (!deploy.deployed)
            return;

        // Verifica radar
        if (!IsRadarOnline())
        {
            if (computerActive)
            {
                computerActive = false;
                ExitComputer();
            }

            return;
        }

        if (Input.GetKeyDown(openKey))
        {
            computerActive = !computerActive;

            if (computerActive)
                EnterComputer();
            else
                ExitComputer();
        }
    }

    // =====================================================
    // RADAR
    // =====================================================

    bool IsRadarOnline()
    {
        if (teamMember == null)
            return false;

        RadarStation[] radars =
            FindObjectsOfType<RadarStation>();

        foreach (RadarStation radar in radars)
        {
            if (
                radar.team == teamMember.team &&
                radar.IsOperational()
            )
            {
                return true;
            }
        }

        return false;
    }

    // =====================================================
    // ENTER
    // =====================================================

    void EnterComputer()
    {
        Debug.Log("🛰 ARTILLERY COMPUTER ONLINE");

        if (artilleryCamera != null)
        {
            artilleryCamera.EnableComputer();
        }
    }

    // =====================================================
    // EXIT
    // =====================================================

    void ExitComputer()
    {
        Debug.Log("❌ ARTILLERY COMPUTER OFFLINE");

        if (artilleryCamera != null)
        {
            artilleryCamera.DisableComputer();
        }
    }
}