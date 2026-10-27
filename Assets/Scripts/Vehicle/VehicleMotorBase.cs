using UnityEngine;
using System.Collections.Generic;

public abstract class VehicleMotorBase : MonoBehaviour
{
    [Header("Motor State")]
    [SerializeField]
    protected bool movementEnabled = true;

    protected Rigidbody rb;
    float environmentalSpeedMultiplier = 1f;
    static int resolvedFrame = -1;
    static readonly HashSet<ulong> resolvedCollisionPairs = new HashSet<ulong>();

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

    public void SetEnvironmentalSpeedMultiplier(float multiplier)
    {
        environmentalSpeedMultiplier = Mathf.Clamp(multiplier, 0.1f, 1f);
    }

    protected float EnvironmentalSpeedMultiplier => environmentalSpeedMultiplier;

    protected void ResolveVehicleCollision(Collision collision)
    {
        if (rb == null || collision == null || collision.rigidbody == null)
            return;

        VehicleMotorBase otherMotor = collision.rigidbody.GetComponentInParent<VehicleMotorBase>();
        if (otherMotor == null || otherMotor.rb == null || otherMotor.rb == rb)
            return;

        if (resolvedFrame != Time.frameCount)
        {
            resolvedFrame = Time.frameCount;
            resolvedCollisionPairs.Clear();
        }

        int first = Mathf.Min(rb.GetInstanceID(), otherMotor.rb.GetInstanceID());
        int second = Mathf.Max(rb.GetInstanceID(), otherMotor.rb.GetInstanceID());
        ulong pair = ((ulong)(uint)first << 32) | (uint)second;
        if (!resolvedCollisionPairs.Add(pair))
            return;

        Rigidbody heavy = rb.mass >= otherMotor.rb.mass ? rb : otherMotor.rb;
        Rigidbody light = heavy == rb ? otherMotor.rb : rb;
        Vector3 heavyToLight = light.position - heavy.position;
        heavyToLight.y = 0f;
        if (heavyToLight.sqrMagnitude < 0.001f)
            return;
        heavyToLight.Normalize();

        Vector3 heavyVelocity = Vector3.ProjectOnPlane(heavy.linearVelocity, Vector3.up);
        Vector3 lightVelocity = Vector3.ProjectOnPlane(light.linearVelocity, Vector3.up);
        float closingSpeed = Vector3.Dot(heavyVelocity - lightVelocity, heavyToLight);
        if (closingSpeed <= 0f)
            return;

        float totalMass = Mathf.Max(1f, heavy.mass + light.mass);
        light.linearVelocity += heavyToLight * (closingSpeed * (heavy.mass / totalMass) * 1.15f);
        heavy.linearVelocity -= heavyToLight * (closingSpeed * (light.mass / totalMass) * 0.15f);
    }
}
