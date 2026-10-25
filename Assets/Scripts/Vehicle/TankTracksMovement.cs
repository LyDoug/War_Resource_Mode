using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TankTracksMovement : MonoBehaviour
{
    [Header("Blueprint")]
    public VehicleBlueprint blueprint;

    [Header("Tracks")]
    public Transform leftTrack;
    public Transform rightTrack;

    [Header("Stability")]
    public Transform centerOfMass;

    [Header("Extra Turning")]
    public float turnForceMultiplier = 1.5f;

    Rigidbody rb;

    float trackForce;
    float turnTorque;
    float maxSpeed;

    // =====================================================
    // AWAKE
    // =====================================================
    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // =========================================
        // APPLY BLUEPRINT
        // =========================================
        if (blueprint != null)
        {
            trackForce = blueprint.moveForce;
            turnTorque = blueprint.turnTorque;
            maxSpeed = blueprint.maxSpeed;

            rb.mass = blueprint.mass;
        }
        else
        {
            Debug.LogWarning(
                "⚠️ VehicleBlueprint não atribuído em: " +
                gameObject.name
            );

            // fallback
            trackForce = 15000f;
            turnTorque = 3500f;
            maxSpeed = 10f;
        }

        // =========================================
        // RIGIDBODY SETTINGS
        // =========================================
        rb.linearDamping = 1.5f;

        rb.angularDamping = 8f;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;

        rb.collisionDetectionMode =
            CollisionDetectionMode.Continuous;

        rb.maxAngularVelocity = 2f;
        rb.constraints |= RigidbodyConstraints.FreezeRotationX |
                          RigidbodyConstraints.FreezeRotationZ;

        // =========================================
        // CENTER OF MASS
        // =========================================
        if (centerOfMass != null)
        {
            rb.centerOfMass =
                centerOfMass.localPosition;
        }
        else
        {
            rb.centerOfMass = new Vector3(0f, -0.75f, 0f);
        }
    }

    // =====================================================
    // FIXED UPDATE
    // =====================================================
    void FixedUpdate()
    {

        float forward =
            Input.GetAxis("Vertical");

        float turn =
            Input.GetAxis("Horizontal");

        // =========================================
        // TRACK INPUT
        // =========================================
        float left =
            Mathf.Clamp(
                forward - turn,
                -1f,
                1f
            );

        float right =
            Mathf.Clamp(
                forward + turn,
                -1f,
                1f
            );

        // =========================================
        // SPEED LIMIT
        // =========================================
        Vector3 flatVelocity =
            new Vector3(
                rb.linearVelocity.x,
                0f,
                rb.linearVelocity.z
            );

        if (
        flatVelocity.magnitude < maxSpeed ||
        Vector3.Dot(
        flatVelocity,
        transform.forward
        ) < 0
            )
        {
            Vector3 forceLeft =
                transform.forward *
                left *
                trackForce *
                Time.fixedDeltaTime;

            Vector3 forceRight =
                transform.forward *
                right *
                trackForce *
                Time.fixedDeltaTime;

            // TRACK FORCES
            if (leftTrack != null)
            {
                rb.AddForceAtPosition(
                    forceLeft,
                    leftTrack.position
                );
            }

            if (rightTrack != null)
            {
                rb.AddForceAtPosition(
                    forceRight,
                    rightTrack.position
                );
            }
        }

        // =========================================
        // EXTRA TURN HELP
        // =========================================
        rb.AddTorque(
            Vector3.up *
            turn *
            turnTorque *
            turnForceMultiplier *
            Time.fixedDeltaTime
        );

        // =========================================
        // SIDEWAYS GRIP
        // =========================================
        Vector3 localVelocity =
            transform.InverseTransformDirection(
                rb.linearVelocity
            );

        localVelocity.x *= 0.55f;

        if (Mathf.Abs(forward) < 0.05f)
            localVelocity.z *= 0.92f;

        rb.linearVelocity =
            transform.TransformDirection(
                localVelocity
            );

        // =========================================
        // ANTI FLIP
        // =========================================
        rb.AddForce(
    Vector3.down * rb.mass * 0.5f
        );
    }
}
