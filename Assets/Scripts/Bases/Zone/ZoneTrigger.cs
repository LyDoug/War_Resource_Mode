using UnityEngine;

public class ZoneTrigger : MonoBehaviour
{
    public ZoneType zoneType;

    private void OnTriggerEnter(Collider other)
    {
        VehicleZoneState state =
            other.GetComponentInParent<VehicleZoneState>();

        if (state != null)
        {
            state.EnterZone(zoneType);
        }
    }
}