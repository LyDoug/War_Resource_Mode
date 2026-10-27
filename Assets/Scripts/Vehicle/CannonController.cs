using UnityEngine;

public class TankCannonAim : MonoBehaviour
{
    [Header("Aim")]
    public float aimSpeed = 30f;

    [Header("Vertical Limits")]
    public float minAngle = -5f;

    public float maxAngle = 45f;

    [Header("Smoothing")]
    public float smoothSpeed = 8f;

    [Header("DeadZone")]
    public float deadZone = 0.02f;

    float targetAngle;
    float currentAngle;
    TankMotor tankMotor;
    bool computerControlled;

    // =====================================================
    // START
    // =====================================================
    void Start()
    {
        tankMotor = GetComponentInParent<TankMotor>();
        currentAngle =
            NormalizeAngle(
                transform.localEulerAngles.x
            );

        targetAngle = currentAngle;
    }

    // =====================================================
    // UPDATE
    // =====================================================
    void Update()
    {
        HandleInput();

        ApplyRotation();
    }

    // =====================================================
    // INPUT
    // =====================================================
    void HandleInput()
    {
        if (computerControlled)
            return;

        if (tankMotor != null && tankMotor.usePlayerInput && Camera.main != null)
        {
            Transform turret = transform.parent;
            Vector3 localAim = turret != null
                ? turret.InverseTransformDirection(Camera.main.transform.forward)
                : transform.InverseTransformDirection(Camera.main.transform.forward);
            float horizontal = new Vector2(localAim.x, localAim.z).magnitude;
            targetAngle = Mathf.Clamp(
                -Mathf.Atan2(localAim.y, Mathf.Max(0.001f, horizontal)) * Mathf.Rad2Deg,
                minAngle,
                maxAngle);
            return;
        }

        if (tankMotor != null && !tankMotor.usePlayerInput)
            return;

        float mouseY =
            Input.GetAxisRaw("Mouse Y");

        // evita tremedeira
        if (Mathf.Abs(mouseY) < deadZone)
            return;

        targetAngle -=
            mouseY *
            aimSpeed *
            Time.deltaTime;

        targetAngle =
            Mathf.Clamp(
                targetAngle,
                minAngle,
                maxAngle
            );
    }

    // =====================================================
    // ROTATION
    // =====================================================
    void ApplyRotation()
    {
        currentAngle =
            Mathf.Lerp(
                currentAngle,
                targetAngle,
                smoothSpeed *
                Time.deltaTime
            );

        transform.localRotation =
            Quaternion.Euler(
                currentAngle,
                0f,
                0f
            );
    }

    // =====================================================
    // NORMALIZE
    // =====================================================
    float NormalizeAngle(float angle)
    {
        while (angle > 180f)
            angle -= 360f;

        return angle;
    }

    // =====================================================
    // GET CURRENT ANGLE
    // =====================================================
    public float GetCurrentAngle()
    {
        return currentAngle;
    }

    public void SetComputerAim(bool active, float elevationDegrees)
    {
        computerControlled = active;
        if (computerControlled)
            // Model convention: negative local X raises this artillery barrel.
            targetAngle = Mathf.Clamp(-elevationDegrees, minAngle, maxAngle);
        else
            targetAngle = 0f;
    }

    public void SetStowAim()
    {
        computerControlled = true;
        targetAngle = 0f;
    }

    public void ReleaseComputerAim()
    {
        computerControlled = false;
    }
}
