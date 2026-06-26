using UnityEngine;

public class DeployableVehicle : MonoBehaviour
{
    public bool transportMode = true;

    public bool deployed = false;

    public bool canDeploy = false;

    public MonoBehaviour[] weaponSystems;

    VehicleMotorBase motor;
    Rigidbody rb;

 void Awake()
{
    motor =
        GetComponent<VehicleMotorBase>();

    if (motor == null)
    {
        motor =
            GetComponentInChildren<VehicleMotorBase>();
    }

    rb =
        GetComponent<Rigidbody>();

    if (rb == null)
    {
        rb =
            GetComponentInChildren<Rigidbody>();
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
        transportMode = value;

        foreach (MonoBehaviour weapon in weaponSystems)
        {
            if (weapon != null)
            {
                weapon.enabled = !value;
            }
        }

        Debug.Log(
            gameObject.name +
            (value
                ? " entrou em TRANSPORT MODE"
                : " saiu do TRANSPORT MODE")
        );
    }

    // =====================================
    // DEPLOY
    // =====================================

public void Deploy()
{
    if (deployed)
        return;

    transportMode = false;
    deployed = true;

    foreach (MonoBehaviour weapon in weaponSystems)
    {
        if (weapon != null)
            weapon.enabled = true;
    }

    if (motor != null)
    {
        motor.EnableMovement(false);
    }

    if (rb != null)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        motor.EnableMovement(true);
    }

    Debug.Log(gameObject.name + " DEPLOYED");
}

public void DeployInstant()
{
    transportMode = false;
    deployed = true;

    foreach (MonoBehaviour weapon in weaponSystems)
    {
        if (weapon != null)
        {
            weapon.enabled = true;
        }
    }

    if (motor != null)
    {
         motor.EnableMovement(false);
    }

    if (rb != null)
    {
        rb.constraints =
            RigidbodyConstraints.FreezePositionX |
            RigidbodyConstraints.FreezePositionZ |
            RigidbodyConstraints.FreezeRotation;
    }

    Debug.Log(
        gameObject.name +
        " nasceu DEPLOYED"
    );
}

    // =====================================
    // UNDEPLOY
    // =====================================

    public void Undeploy()
{
    if (!deployed)
        return;

    transportMode = true;
    deployed = false;

    foreach (MonoBehaviour weapon in weaponSystems)
    {
        if (weapon != null)
            weapon.enabled = false;
    }

    if (rb != null)
    {
        motor.EnableMovement(false);
    }
    
    Debug.Log(
        gameObject.name +
        " voltou para TRANSPORT MODE"
    );
}
}