using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TankMotor : VehicleMotorBase
{
    [Header("Blueprint")]
    public VehicleBlueprint blueprint;

    [Header("Physics")]
    public Transform centerOfMass;

    [Header("Grip")]
    [Range(0f, 1f)]
    public float sideGrip = 0.85f;

    float moveForce;
    float turnSpeed;
    float maxSpeed;

    float moveInput;
    float turnInput;

    // =====================================================
    // AWAKE
    // =====================================================
    protected override void Awake()
    {
        base.Awake();

        ApplyPhysics();

        ApplyBlueprint();
    }

    // =====================================================
    // UPDATE
    // =====================================================
    void Update()
    {
        moveInput =
            Input.GetAxis("Vertical");

        turnInput =
            Input.GetAxis("Horizontal");
    }

    // =====================================================
    // FIXED UPDATE
    // =====================================================
    void FixedUpdate()
    {
        if (!CanMove())
            return;

        Move();

        Turn();

        ApplyGrip();

        LimitSpeed();
    }

    // =====================================================
    // APPLY PHYSICS
    // =====================================================
    void ApplyPhysics()
    {
        rb.mass = 5000f;

        rb.linearDamping = 1f;

        rb.angularDamping = 8f;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;

        rb.collisionDetectionMode =
            CollisionDetectionMode.Continuous;

        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;

        if (centerOfMass != null)
        {
            rb.centerOfMass =
                transform.InverseTransformPoint(
                    centerOfMass.position
                );
        }
    }

    // =====================================================
    // APPLY BLUEPRINT
    // =====================================================
    void ApplyBlueprint()
    {
        if (blueprint == null)
        {
            moveForce = 35f;
            turnSpeed = 45f;
            maxSpeed = 12f;
            return;
        }

        moveForce =
            blueprint.moveForce;

        turnSpeed =
            blueprint.turnTorque;

        maxSpeed =
            blueprint.maxSpeed;

        rb.mass =
            blueprint.mass;
    }

    // =====================================================
    // MOVE
    // =====================================================
    void Move()
    {
        if (!CanMove())
            return;

        if (Mathf.Abs(moveInput) < 0.01f)
            return;

        rb.AddForce(
            transform.forward *
            moveInput *
            moveForce,
            ForceMode.Acceleration
        );
    }

    // =====================================================
    // TURN
    // =====================================================
    void Turn()
    {
        if (!CanMove())
            return;

        if (Mathf.Abs(turnInput) < 0.01f)
            return;

        float turnAmount =
            turnInput *
            turnSpeed *
            Time.fixedDeltaTime;

        Quaternion turnRotation =
            Quaternion.Euler(
                0f,
                turnAmount,
                0f
            );

        rb.MoveRotation(
            rb.rotation *
            turnRotation
        );
    }

    // =====================================================
    // GRIP
    // =====================================================
    void ApplyGrip()
    {
        if (!CanMove())
            return;

        if (rb.isKinematic)
            return;

        Vector3 localVelocity =
            transform.InverseTransformDirection(
                rb.linearVelocity
            );

        localVelocity.x *= sideGrip;

        rb.linearVelocity =
            transform.TransformDirection(
                localVelocity
            );
    }

    // =====================================================
    // LIMIT SPEED
    // =====================================================
    void LimitSpeed()
    {
        if (!CanMove())
            return;

        if (rb.isKinematic)
            return;

        Vector3 flatVelocity =
            new Vector3(
                rb.linearVelocity.x,
                0f,
                rb.linearVelocity.z
            );

        if (flatVelocity.magnitude > maxSpeed)
        {
            Vector3 limited =
                flatVelocity.normalized *
                maxSpeed;

            rb.linearVelocity =
                new Vector3(
                    limited.x,
                    rb.linearVelocity.y,
                    limited.z
                );
        }
    }
}