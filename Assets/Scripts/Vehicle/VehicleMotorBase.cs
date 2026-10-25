using UnityEngine;

public abstract class VehicleMotorBase : MonoBehaviour
{
    [Header("Motor State")]
    [SerializeField]
    protected bool movementEnabled = true;

    protected Rigidbody rb;

    // =====================================================
    // AWAKE
    // =====================================================
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
            rb = GetComponentInChildren<Rigidbody>();
    }

    // =====================================================
    // ENABLE / DISABLE MOVEMENT
    // =====================================================
    public virtual void EnableMovement(bool enable)
    {
        movementEnabled = enable;

        if (rb == null)
            return;

        if (enable)
        {
            if (!rb.isKinematic)
            {
                rb.WakeUp();
            }
        }
        else
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.Sleep();
        }
    }

    // =====================================================
    // CAN MOVE?
    // =====================================================
    protected bool CanMove()
    {
        if (!movementEnabled)
            return false;

        if (rb == null)
            return false;

        if (rb.isKinematic)
            return false;

        return true;
    }

    // =====================================================
    // PUBLIC STATE
    // =====================================================
    public bool IsMovementEnabled()
    {
        return movementEnabled;
    }
}
