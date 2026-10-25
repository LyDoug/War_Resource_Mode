using UnityEngine;

public class VehicleSimpleAI : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    public float waypointReachDistance = 10f;

    VehicleMotor motor;
    TankMotor tankMotor;

    // =====================================================
    // START
    // =====================================================
    void Start()
    {
        motor =
            GetComponent<VehicleMotor>();

        if (motor == null)
        {
            motor =
                GetComponentInChildren<VehicleMotor>();
        }

        tankMotor = GetComponentInChildren<TankMotor>();
    }

    // =====================================================
    // UPDATE
    // =====================================================
    void Update()
    {
        if (target == null)
            return;

        if (motor == null && tankMotor == null)
            return;

        Vector3 dir =
            target.position -
            transform.position;

        dir.y = 0f;

        // =====================================
        // DISTÂNCIA
        // =====================================
        if (
            dir.magnitude <
            waypointReachDistance
        )
        {
            SetInput(0f, 0f);
            return;
        }

        // =====================================
        // ÂNGULO
        // =====================================
        float angle =
            Vector3.SignedAngle(
                transform.forward,
                dir.normalized,
                Vector3.up
            );

        float turn =
            Mathf.Clamp(
                angle / 45f,
                -1f,
                1f
            );

        float move = 1f;

        // desacelera em curvas fortes
        if (Mathf.Abs(angle) > 70f)
        {
            move = 0.3f;
        }

        // =====================================
        // ENVIA INPUT
        // =====================================
        SetInput(move, turn);
    }

    void SetInput(float move, float turn)
    {
        if (motor != null)
            motor.SetAIInput(move, turn);
        if (tankMotor != null)
            tankMotor.SetAIInput(move, turn);
    }
}
