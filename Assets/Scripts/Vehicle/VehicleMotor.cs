using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VehicleMotor : VehicleMotorBase
{
    [Header("Blueprint")]
    public VehicleBlueprint blueprint;

    [Header("Center Of Mass")]
    public Transform centerOfMass;

    [Header("Input")]
    public bool usePlayerInput = true;


    float moveInput;
    float turnInput;
    float targetMoveInput;
    float targetTurnInput;

    [Header("Inertia")]
    [Min(0.1f)] public float accelerationResponse = 2.25f;
    [Min(0.1f)] public float brakingResponse = 4.5f;

    // =====================================================
    // AWAKE
    // =====================================================
    protected override void Awake()
    {
        base.Awake();

        rb = GetComponent<Rigidbody>();
        // =========================
        // RIGIDBODY SETTINGS
        // =========================
        rb.mass = 1200f;

        if (blueprint != null)
        {
            rb.mass = blueprint.mass;
        }

        rb.linearDamping = 1.5f;

        rb.angularDamping = 8f;

        rb.constraints |= RigidbodyConstraints.FreezeRotationX |
                          RigidbodyConstraints.FreezeRotationZ;
        rb.maxAngularVelocity = 4f;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;

        rb.collisionDetectionMode =
            CollisionDetectionMode.Continuous;
    

        // =========================
        // CENTER OF MASS
        // =========================
        if (centerOfMass != null)
        {
            rb.centerOfMass =
                transform.InverseTransformPoint(
                    centerOfMass.position
                );
        }
        else
        {
            rb.centerOfMass = new Vector3(0f, -0.55f, 0f);
        }

        LiftTruckVisuals();
    }

    void LiftTruckVisuals()
    {
        // Keep the rigidbody and colliders where they are; only the truck
        // mesh is raised slightly so its wheels no longer look half buried.
        if (GetComponent<TruckCargo>() == null)
            return;

        const float visualLift = 0.60f;
        foreach (Transform child in transform)
        {
            if (child.GetComponentInChildren<Renderer>(true) == null)
                continue;
            child.localPosition += Vector3.up * visualLift;
        }
    }


    // =====================================================
    // UPDATE
    // =====================================================
    void Update()
    {
        // =====================================
        // PLAYER INPUT
        // =====================================
        if (usePlayerInput)
        {
            targetMoveInput = Input.GetAxis("Vertical");
            targetTurnInput = Input.GetAxis("Horizontal");
        }
    }
    // =====================================================
    // AI INPUT
    // =====================================================
    public void SetAIInput(
        float move,
        float turn
    )
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
        if (blueprint == null)
            return;

        float response = Mathf.Abs(targetMoveInput) < Mathf.Abs(moveInput)
            ? brakingResponse : accelerationResponse;
        moveInput = Mathf.MoveTowards(moveInput, targetMoveInput, response * Time.fixedDeltaTime);
        turnInput = Mathf.MoveTowards(turnInput, targetTurnInput, 5f * Time.fixedDeltaTime);

        MoveVehicle();

        TurnVehicle();

        ApplySideGrip();

        ApplyDownforce();

        LimitSpeed();
    }

    // =====================================================
    // MOVE
    // =====================================================
    void MoveVehicle()
    { 
       if (!CanMove())
        return;
        rb.AddForce(
            transform.forward *
            moveInput *
            blueprint.moveForce,
            // Blueprint values (Jeep 85, Truck 75) were authored as
            // accelerations. Using Force divided them by multi-ton masses,
            // leaving vehicles able to turn but practically unable to move.
            ForceMode.Acceleration
        );
    }

    // =====================================================
    // TURN
    // =====================================================
    void TurnVehicle()
    {  
        if (!CanMove())
        return;

        // só vira se estiver andando
        if (Mathf.Abs(moveInput) < 0.05f)
            return;

        float turnAmount =
            turnInput *
            blueprint.turnTorque *
            Time.fixedDeltaTime;

        Quaternion turnRotation =
            Quaternion.Euler(
                0f,
                turnAmount,
                0f
            );

        rb.MoveRotation(
            rb.rotation * turnRotation
        );
    }

    // =====================================================
    // SIDE GRIP
    // =====================================================
    void ApplySideGrip()
    { 
         if (!CanMove())
        return;

        Vector3 localVelocity =
            transform.InverseTransformDirection(
                rb.linearVelocity
            );

        // reduz drift lateral
        localVelocity.x *= 0.55f;

        // Rolling resistance keeps a released vehicle from gliding like ice.
        if (Mathf.Abs(moveInput) < 0.05f)
            localVelocity.z *= 0.92f;

        rb.linearVelocity =
            transform.TransformDirection(
                localVelocity
            );
    }

    // =====================================================
    // DOWNFORCE
    // =====================================================
    void ApplyDownforce()
    {  
        if (!CanMove())
        return;

        float speed = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
        rb.AddForce(Vector3.down * (100f + speed * 35f), ForceMode.Force);
    }

    // =====================================================
    // LIMIT SPEED
    // =====================================================
    void LimitSpeed()
    {  
        if (!CanMove())
        return;
        Vector3 flatVelocity =
            new Vector3(
                rb.linearVelocity.x,
                0f,
                rb.linearVelocity.z
            );

        if (
            flatVelocity.magnitude >
            blueprint.maxSpeed
        )
        {
            Vector3 limitedVelocity =
                flatVelocity.normalized *
                blueprint.maxSpeed;

            rb.linearVelocity =
                new Vector3(
                    limitedVelocity.x,
                    rb.linearVelocity.y,
                    limitedVelocity.z
                );
         }
        }

}
