using UnityEngine;

public class VehicleOrbitCamera : MonoBehaviour
{
    [Header("Targets")]
    public Transform target;

    public Transform driverView;

    public Transform gunView;

    [Header("Camera")]
    public Transform cameraTransform;

    [Header("Mouse")]
    public float sensitivity = 3f;

    public float minPitch = -10f;

    public float maxPitch = 60f;

    [Header("Zoom")]
    public float zoomSpeed = 5f;

    public float minDistance = 0f;

    public float maxDistance = 10f;

    [Header("Follow")]
    public float followSmooth = 12f;

    public float rotationSmooth = 10f;

    [Header("Auto Align")]
    public bool autoAlign = true;

    public float alignSpeed = 3f;

    [Header("Height")]
    public float thirdPersonHeight = 2f;

    [Header("Modes")]
    public bool gunMode = false;

    float yaw;
    float pitch;
    float distance = 5f;
    bool wasIndirectFireView;

    bool HasSPGTarget => target != null && target.GetComponentInChildren<SPGWeapon>(true) != null;
    bool IsIndirectFireView => target != null &&
        target.GetComponentInChildren<SPGWeapon>(true) is SPGWeapon spg && spg.FireComputerActive;

    // =====================================================
    // START
    // =====================================================
    void Start()
    {
        if (cameraTransform == null)
        {
            Camera cam =
                GetComponentInChildren<Camera>();

            if (cam != null)
            {
                cameraTransform = cam.transform;
            }
        }

        yaw = transform.eulerAngles.y;
        pitch = 15f;
    }

    // =====================================================
    // UPDATE
    // =====================================================
    void Update()
    {
        // =====================================
        // GUN VIEW
        // =====================================
        // An SPG reserves Q for its fire computer. E remains the normal
        // GunView toggle for every other vehicle.
        if (Input.GetKeyDown(KeyCode.E) && !TruckCargo.HasPlayerInteractionPrompt && !HasSPGTarget)
        {
            gunMode = !gunMode;

            Debug.Log(
                gunMode
                ? "🎯 GunView ON"
                : "🚗 GunView OFF"
            );
        }
    }

    // =====================================================
    // LATE UPDATE
    // =====================================================
    void LateUpdate()
    {
        if (!target)
            return;

        bool indirectFireView = IsIndirectFireView;
        if (indirectFireView && !wasIndirectFireView)
            pitch = 67f;
        wasIndirectFireView = indirectFireView;

        // =====================================
        // INPUT
        // =====================================
        float mouseX =
            Input.GetAxis("Mouse X");

        float mouseY =
            Input.GetAxis("Mouse Y");

        yaw += mouseX * sensitivity;

        pitch -= mouseY * sensitivity;

        // =====================================
        // AUTO ALIGN
        // =====================================
        if (autoAlign && !gunMode && !indirectFireView)
        {
            bool movingMouse =
                Mathf.Abs(mouseX) > 0.01f ||
                Mathf.Abs(mouseY) > 0.01f;

            if (!movingMouse)
            {
                float targetYaw =
                    target.eulerAngles.y;

                yaw = Mathf.LerpAngle(
                    yaw,
                    targetYaw,
                    Time.deltaTime * alignSpeed
                );
            }
        }

        // =====================================
        // LIMITA PITCH
        // =====================================
        pitch = Mathf.Clamp(
            pitch,
            minPitch,
            maxPitch
        );

        // =====================================
        // ZOOM
        // =====================================
        float scroll =
            Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance -= scroll * zoomSpeed;

            distance = Mathf.Clamp(
                distance,
                minDistance,
                maxDistance
            );
        }

        // =====================================
        // FOLLOW
        // =====================================
        transform.position = Vector3.Lerp(
            transform.position,
            target.position,
            Time.deltaTime * followSmooth
        );

        // =====================================
        // ROTATION
        // =====================================
        Quaternion rotation =
            Quaternion.Euler(
                pitch,
                yaw,
                0f
            );

        if (indirectFireView)
        {
            Vector3 horizontalBack = Quaternion.Euler(0f, yaw, 0f) * Vector3.back;
            Vector3 indirectCameraPosition = target.position + Vector3.up * 32f + horizontalBack * 12f;

            cameraTransform.position = Vector3.Lerp(
                cameraTransform.position,
                indirectCameraPosition,
                Time.deltaTime * 8f);
            cameraTransform.rotation = Quaternion.Lerp(
                cameraTransform.rotation,
                rotation,
                Time.deltaTime * 10f);
            return;
        }

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            rotation,
            Time.deltaTime * rotationSmooth
        );

        // =====================================
        // GUN VIEW MODE
        // =====================================
        if (gunMode && gunView != null)
        {
            cameraTransform.position =
                Vector3.Lerp(
                    cameraTransform.position,
                    gunView.position,
                    Time.deltaTime * 15f
                );

            cameraTransform.rotation =
                Quaternion.Lerp(
                    cameraTransform.rotation,
                    // GunView provides the correct eye position, but it is
                    // an authored transform and does not rotate with mouse
                    // aim. Keeping the calculated yaw/pitch here prevents
                    // the sight from drifting away from screen centre.
                    rotation,
                    Time.deltaTime * 15f
                );

            return;
        }

        // =====================================
        // DRIVER VIEW
        // =====================================
        if (
            distance <= 0.2f &&
            driverView != null
        )
        {
            cameraTransform.position =
                Vector3.Lerp(
                    cameraTransform.position,
                    driverView.position,
                    Time.deltaTime * 15f
                );

            cameraTransform.rotation =
                Quaternion.Lerp(
                    cameraTransform.rotation,
                    // DriverView is only the seat position. Aim remains
                    // mouse-driven so every camera mode shares one reticle.
                    rotation,
                    Time.deltaTime * 15f
                );

            return;
        }

        // =====================================
        // THIRD PERSON
        // =====================================
        Vector3 offset =
            rotation *
            new Vector3(
                0f,
                thirdPersonHeight,
                -distance
            );

        Vector3 desiredPosition =
            target.position + offset;

        cameraTransform.position =
            Vector3.Lerp(
                cameraTransform.position,
                desiredPosition,
                Time.deltaTime * followSmooth
            );

        cameraTransform.rotation =
            Quaternion.Lerp(
                cameraTransform.rotation,
                rotation,
                Time.deltaTime * rotationSmooth
            );
    }

    // =====================================================
    // CONFIG EXTERNA
    // =====================================================
    public void SetDefaultDistance(float d)
    {
        distance =
            Mathf.Clamp(
                d,
                minDistance,
                maxDistance
            );
    }

    public void SetFOV(float fov)
    {
        Camera cam =
            cameraTransform.GetComponent<Camera>();

        if (cam != null)
        {
            cam.fieldOfView = fov;
        }
    }
}
