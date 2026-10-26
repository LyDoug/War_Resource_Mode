using UnityEngine;

// Runtime controller for the Turret_09_Prefeb added to the APC model.
// The root turns horizontally; its MainGun child supplies vertical aiming.
public class IFVTurretController : MonoBehaviour
{
    public float yawSpeed = 160f;
    public float pitchSpeed = 110f;
    public float minPitch = -12f;
    public float maxPitch = 38f;

    Transform turretYaw;
    Transform gunPitch;
    Quaternion gunInitialLocalRotation;

    public void Configure(Transform firePoint)
    {
        turretYaw = FindChild("Turret_09_Prefeb");
        if (turretYaw == null)
            return;

        gunPitch = FindChild("Turret_09_MainGun");
        if (gunPitch == null)
            return;

        gunInitialLocalRotation = gunPitch.localRotation;

        // Firepoint was authored on the APC body. Parent it to the cannon so
        // the muzzle follows both yaw and pitch without changing its position.
        if (firePoint != null && !firePoint.IsChildOf(gunPitch))
            firePoint.SetParent(gunPitch, true);
    }

    void Update()
    {
        if (turretYaw == null || gunPitch == null || Camera.main == null)
            return;

        VehicleMotor motor = GetComponent<VehicleMotor>();
        if (motor != null && !motor.usePlayerInput)
            return;

        Vector3 aim = Camera.main.transform.forward;
        Vector3 flatAim = Vector3.ProjectOnPlane(aim, Vector3.up);
        if (flatAim.sqrMagnitude > 0.001f)
        {
            Quaternion desiredYaw = Quaternion.LookRotation(flatAim.normalized, Vector3.up);
            turretYaw.rotation = Quaternion.RotateTowards(turretYaw.rotation, desiredYaw, yawSpeed * Time.deltaTime);
        }

        Vector3 localAim = turretYaw.InverseTransformDirection(aim.normalized);
        float desiredPitch = -Mathf.Atan2(localAim.y, Mathf.Max(0.001f, new Vector2(localAim.x, localAim.z).magnitude)) * Mathf.Rad2Deg;
        desiredPitch = Mathf.Clamp(desiredPitch, minPitch, maxPitch);
        Quaternion targetPitch = gunInitialLocalRotation * Quaternion.Euler(desiredPitch, 0f, 0f);
        gunPitch.localRotation = Quaternion.RotateTowards(gunPitch.localRotation, targetPitch, pitchSpeed * Time.deltaTime);
    }

    Transform FindChild(string name)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
            if (string.Equals(child.name, name, System.StringComparison.OrdinalIgnoreCase))
                return child;
        return null;
    }
}
