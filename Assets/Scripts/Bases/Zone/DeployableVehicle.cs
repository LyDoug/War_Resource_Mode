using UnityEngine;

public class DeployableVehicle : MonoBehaviour
{
    public bool transportMode = true;

    public bool deployed = false;

    public bool canDeploy = false;

    public MonoBehaviour[] weaponSystems;

    VehicleMotorBase motor;

 void Awake()
{
    motor =
        GetComponent<VehicleMotorBase>();

    if (motor == null)
    {
        motor =
            GetComponentInChildren<VehicleMotorBase>();
    }

}

    void Update()
    {
        // Deploy
        if (
            canDeploy &&
            transportMode &&
            Input.GetKeyDown(KeyCode.T)
        )
        {
            Deploy();
        }

        // Volta para transporte
        if (
            deployed &&
            Input.GetKeyDown(KeyCode.G)
        )
        {
            Undeploy();
        }
    }

    public void SetTransportMode(bool value)
    {
        SetMode(value, true);
    }

    // =====================================
    // DEPLOY
    // =====================================

public void Deploy()
{
    if (deployed)
        return;

    SetMode(false, false);
}

public void DeployInstant()
{
    SetMode(false, true);
}

    // =====================================
    // UNDEPLOY
    // =====================================

    public void Undeploy()
{
    if (!deployed)
        return;

    SetMode(true, false);
}

    void SetMode(bool enterTransportMode, bool force)
    {
        if (!force && transportMode == enterTransportMode)
            return;

        transportMode = enterTransportMode;
        deployed = !enterTransportMode;

        foreach (MonoBehaviour weapon in weaponSystems)
        {
            if (weapon != null)
                weapon.enabled = !enterTransportMode;
        }

        if (motor != null)
            motor.EnableMovement(enterTransportMode);

        Debug.Log(
            gameObject.name +
            (enterTransportMode
                ? " voltou para TRANSPORT MODE"
                : " DEPLOYED")
        );
    }
}
