using UnityEngine;

public class TurretController : MonoBehaviour
{
    public float rotationSpeed = 60f;

    TankMotor tankMotor;
    bool computerControlled;
    Vector3 computerTarget;

    void Awake()
    {
        tankMotor = GetComponentInParent<TankMotor>();
    }

    void Update()
    {
        if (computerControlled)
        {
            Vector3 flatAim = computerTarget - transform.position;
            flatAim.y = 0f;
            if (flatAim.sqrMagnitude > 0.001f)
            {
                Quaternion desired = Quaternion.LookRotation(flatAim.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, desired, rotationSpeed * Time.deltaTime);
            }
            return;
        }

        // AI keeps its navigation-controlled rotation. Player tanks instead
        // align the turret with the centre of the active camera, matching the
        // same third-person/driver/gun sight behaviour.
        if (tankMotor != null && tankMotor.usePlayerInput && Camera.main != null)
        {
            Vector3 flatAim = Camera.main.transform.forward;
            flatAim.y = 0f;
            if (flatAim.sqrMagnitude > 0.001f)
            {
                Quaternion desired = Quaternion.LookRotation(flatAim.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, desired, rotationSpeed * Time.deltaTime);
            }
            return;
        }

        if (tankMotor != null && !tankMotor.usePlayerInput)
            return;

        float mouseX = Input.GetAxis("Mouse X");
        transform.Rotate(0f, mouseX * rotationSpeed * Time.deltaTime, 0f);
    }

    public void SetComputerAim(bool active, Vector3 target)
    {
        computerControlled = active;
        computerTarget = target;
    }
}
