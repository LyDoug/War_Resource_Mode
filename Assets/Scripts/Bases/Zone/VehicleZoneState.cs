using UnityEngine;

public class VehicleZoneState : MonoBehaviour
{
    public ZoneType currentZone;

    public bool transportMode;

    public void EnterZone(ZoneType zone)
    {
        if (currentZone == zone)
            return;

        currentZone = zone;

        Debug.Log(
            gameObject.name +
            " entrou em " +
            zone
        );

        HandleZone();
    }

   void HandleZone()
{
    DeployableVehicle deploy =
        GetComponent<DeployableVehicle>();

    if (deploy == null)
        return;

    switch (currentZone)
    {
        case ZoneType.Base:

            deploy.canDeploy = true;

            break;

        case ZoneType.Deployment:

            deploy.canDeploy = true;

            break;

        case ZoneType.Combat:

            deploy.canDeploy = false;

            deploy.Undeploy();

            break;
    }
}
}