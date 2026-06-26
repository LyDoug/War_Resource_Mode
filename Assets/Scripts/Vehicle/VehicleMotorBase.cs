using UnityEngine;

public abstract class VehicleMotorBase : MonoBehaviour
{
    [HideInInspector]
    public bool movementEnabled = true;

    protected Rigidbody rb;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
            rb = GetComponentInChildren<Rigidbody>();
    }

    public virtual void EnableMovement(bool enable)
    {
        movementEnabled = enable;
    }

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
}