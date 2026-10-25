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
    float targetMoveInput;
    float targetTurnInput;

    [Header("Input")]
    public bool usePlayerInput = true;

    [Header("Inertia")]
    [Min(0.1f)] public float accelerationResponse = 1.5f;
    [Min(0.1f)] public float brakingResponse = 3.5f;

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
        if (!usePlayerInput)
            return;

        targetMoveInput = Input.GetAxis("Vertical");
        targetTurnInput = Input.GetAxis("Horizontal");
    }

    public void SetAIInput(float move, float turn)
    {
        targetMoveInput = Mathf.Clamp(move, -1f, 1f);
        targetTurnInput = Mathf.Clamp(turn, -1f, 1f);
    }

    // =====================================================
    // FIXED UPDATE
    // =====================================================
    void FixedUpdate()
    {
        if (!CanMove())
            return;

        float response = Mathf.Abs(targetMoveInput) < Mathf.Abs(moveInput)
            ? brakingResponse : accelerationResponse;
        moveInput = Mathf.MoveTowards(moveInput, targetMoveInput, response * Time.fixedDeltaTime);
        turnInput = Mathf.MoveTowards(turnInput, targetTurnInput, 3f * Time.fixedDeltaTime);

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

        rb.constraints |= RigidbodyConstraints.FreezeRotationX |
                          RigidbodyConstraints.FreezeRotationZ;
        rb.maxAngularVelocity = 3f;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;

        rb.collisionDetectionMode =
            CollisionDetectionMode.Continuous;


        if (centerOfMass != null)
        {
            rb.centerOfMass =
                transform.InverseTransformPoint(
                    centerOfMass.position
                );
        }
        else
        {
            rb.centerOfMass = new Vector3(0f, -0.75f, 0f);
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

        localVelocity.x *= Mathf.Min(sideGrip, 0.55f);

        if (Mathf.Abs(moveInput) < 0.05f)
            localVelocity.z *= 0.92f;

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
