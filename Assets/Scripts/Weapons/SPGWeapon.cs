using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum ArtilleryAmmoType
{
    HE,
    Smoke
}

public class SPGWeapon : MonoBehaviour
{
    [Header("Projectile")]
    public GameObject shellPrefab;

    public Transform firePoint;

    [Header("VFX")]
    public GameObject muzzleFlashVFX;

    public GameObject smokeVFX;

    [Header("Ballistics")]
    public float launchForce = 40f;

    public float arcForce = 15f;

    [Tooltip("Initial velocity used by the indirect-fire trajectory solver.")]
    public float ballisticSpeed = 80f;

    [Range(45f, 75f)] public float mortarElevation = 60f;
    public float minimumLaunchSpeed = 18f;

    public float aimDistance = 1000f;

    [Header("Tactical Targeting")]
    public float traverseSpeed = 38f;
    [Min(0.2f)] public float deploymentDuration = 1.5f;
    [Min(10f)] public float tacticalVisionRange = 120f;
    [Min(0f)] public float minimumFiringRange = 30f;
    [Tooltip("Maximum landing error for a map target outside observation range.")]
    [Min(0f)] public float blindFireScatterRadius = 28f;

    [Header("Fire Rate")]
    public float reloadTime = 5f;
    [Header("Area Damage")]
    public float shellDamage = 260f;
    public float explosionRadius = 2.5f;

    [Header("Ammunition")]
    public ArtilleryAmmoType selectedAmmo = ArtilleryAmmoType.HE;
    public float smokeRevealRadius = 70f;
    public float smokeRevealDuration = 45f;

    float reloadTimer;
    Vector3 solutionVelocity;
    Vector3 mapTarget;
    bool hasMapTarget;
    float deploymentCompleteTime;
    DeploymentState deploymentState;
    float lastShotTime = -99f;
    Vector3 lastShotTarget;

    enum DeploymentState
    {
        Mobile,
        Deploying,
        Deployed,
        Retracting
    }

    public float CooldownRemaining => Mathf.Max(0f, reloadTimer);
    public Vector3 AimPoint { get; private set; }
    public float AimRange { get; private set; }
    public float ElevationDegrees { get; private set; }
    public float AzimuthDegrees { get; private set; }
    public float ChargeSpeed { get; private set; }
    public bool HasFireSolution { get; private set; }
    public bool HasMapTarget => hasMapTarget;
    public bool IsAimed { get; private set; }
    public bool FireComputerActive => deploymentState != DeploymentState.Mobile;
    public bool IsDeploying => deploymentState == DeploymentState.Deploying;
    public bool IsRetracting => deploymentState == DeploymentState.Retracting;
    public bool IsDeployed => deploymentState == DeploymentState.Deployed;
    public Team OwningTeam => GetOwningTeam();
    public float EffectiveTacticalVisionRange => RadarStation.IsRadarOnline(OwningTeam)
        ? tacticalVisionRange * 1.75f : tacticalVisionRange;
    public string AmmoName => selectedAmmo == ArtilleryAmmoType.HE ? "HE" : "SMOKE";
    public bool HasRecentShot => Time.time - lastShotTime < 3.5f;
    public Vector3 LastShotTarget => lastShotTarget;
    public string DeploymentMessage => IsDeploying ? "DEPLOYING..." :
        IsRetracting ? "RETRACTING..." : string.Empty;

    // =====================================================
    // UPDATE
    // =====================================================
    void Update()
    {
        reloadTimer -= Time.deltaTime;

        if (!VehicleController.IsLocallyControlled(this))
            return;

        if (Input.GetKeyDown(KeyCode.F1)) selectedAmmo = ArtilleryAmmoType.HE;
        if (Input.GetKeyDown(KeyCode.F2)) selectedAmmo = ArtilleryAmmoType.Smoke;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (deploymentState == DeploymentState.Mobile || deploymentState == DeploymentState.Retracting)
                BeginDeployment();
            else
                BeginRetraction();
        }

        UpdateDeployment();

        if (!IsDeployed)
            return;

        UpdateFireSolution();
        SetComputerAiming(true);
        AlignPlatformToTarget();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryFire();
        }
    }

    // =====================================================
    // FIRE
    // =====================================================
    public void TryFire()
    {
        if (!IsDeployed)
            return;

        if (reloadTimer > 0f)
            return;

        if (shellPrefab == null)
        {
            Debug.LogWarning(
                "⚠️ Shell Prefab não atribuído"
            );

            return;
        }

        if (firePoint == null)
        {
            Debug.LogWarning(
                "⚠️ FirePoint não atribuído"
            );

            return;
        }

        if (hasMapTarget && !IsAimed)
            return;

        Fire();

        reloadTimer = reloadTime;
    }

    // =====================================================
    // FIRE LOGIC
    // =====================================================
    void Fire()
    {
        // =====================================
        // SHELL
        // =====================================
        GameObject shell =
            Instantiate(
                shellPrefab,
                firePoint.position,
                firePoint.rotation
            );

        ArtilleryShell artilleryShell = shell.GetComponent<ArtilleryShell>();
        if (artilleryShell != null)
        {
            artilleryShell.ConfigureShooter(transform.root);
            artilleryShell.ConfigureAmmo(
                selectedAmmo,
                selectedAmmo == ArtilleryAmmoType.HE ? shellDamage : 0f,
                selectedAmmo == ArtilleryAmmoType.HE ? explosionRadius : 0f,
                smokeRevealRadius,
                smokeRevealDuration,
                GetOwningTeam());
        }

        Rigidbody rb =
            shell.GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError(
                "❌ Shell sem Rigidbody"
            );

            Destroy(shell);

            return;
        }

        Vector3 firingTarget = GetFiringTarget();
        UpdateFireSolution(firingTarget);
        lastShotTarget = firingTarget;
        lastShotTime = Time.time;
        Vector3 velocity = solutionVelocity;
        if (!HasFireSolution)
        {
            // Preserve a usable shot if the point is beyond ballistic range.
            velocity = firePoint.forward * ballisticSpeed + Vector3.up * arcForce;
        }

        if (artilleryShell != null)
            artilleryShell.Launch(velocity);
        else
            rb.linearVelocity = velocity;

        // =====================================
        // MUZZLE FLASH
        // =====================================
        if (muzzleFlashVFX != null)
        {
            GameObject flash =
                Instantiate(
                    muzzleFlashVFX,
                    firePoint.position,
                    firePoint.rotation
                );

            Destroy(flash, 3f);
        }

        // =====================================
        // SMOKE
        // =====================================
        if (smokeVFX != null)
        {
            GameObject smoke =
                Instantiate(
                    smokeVFX,
                    firePoint.position,
                    firePoint.rotation
                );

            Destroy(smoke, 6f);
        }

        // =====================================
        // CAMERA SHAKE
        // =====================================
        Camera cam =
            Camera.main;

        if (cam != null)
        {
            cam.transform.position +=
                -firePoint.forward * 0.15f;
        }

        //Debug.Log("💥 SPG FIRED");
    }

    Vector3 GetAimPoint()
    {
        if (hasMapTarget)
            return mapTarget;

        Camera camera = Camera.main;
        if (camera == null)
            return firePoint.position + firePoint.forward * 100f;

        Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            aimDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        float nearestDistance = float.MaxValue;
        Vector3 nearestPoint = Vector3.zero;
        bool foundTarget = false;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform.root))
                continue;

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                nearestPoint = hit.point;
                foundTarget = true;
            }
        }

        if (foundTarget)
            return nearestPoint;

        Plane ground = new Plane(Vector3.up, Vector3.zero);
        if (ground.Raycast(ray, out float distance))
            return ray.GetPoint(Mathf.Min(distance, aimDistance));

        return firePoint.position + firePoint.forward * 100f;
    }

    Vector3 GetFiringTarget()
    {
        Vector3 intendedTarget = GetAimPoint();
        if (!hasMapTarget || IsTargetObserved(intendedTarget) || blindFireScatterRadius <= 0f)
            return intendedTarget;

        Vector3 flatOffset = intendedTarget - transform.root.position;
        flatOffset.y = 0f;
        float outsideVision = Mathf.Max(0f, flatOffset.magnitude - tacticalVisionRange);
        float scatter = blindFireScatterRadius * Mathf.Lerp(0.35f, 1f,
            Mathf.Clamp01(outsideVision / Mathf.Max(1f, tacticalVisionRange)));
        Vector2 randomOffset = Random.insideUnitCircle * scatter;
        Vector3 scatteredTarget = intendedTarget + new Vector3(randomOffset.x, 0f, randomOffset.y);
        if (Physics.Raycast(scatteredTarget + Vector3.up * 250f, Vector3.down, out RaycastHit hit, 500f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            scatteredTarget.y = hit.point.y;
        return scatteredTarget;
    }

    public bool IsTargetObserved(Vector3 worldPosition)
    {
        Vector3 offset = worldPosition - transform.root.position;
        offset.y = 0f;
        return offset.sqrMagnitude <= EffectiveTacticalVisionRange * EffectiveTacticalVisionRange ||
            TacticalRevealZone.IsRevealed(GetOwningTeam(), worldPosition);
    }

    void UpdateFireSolution()
    {
        UpdateFireSolution(GetAimPoint());
    }

    void UpdateFireSolution(Vector3 target)
    {
        AimPoint = target;
        AimRange = Vector3.Distance(firePoint.position, AimPoint);
        if (AimRange < minimumFiringRange)
        {
            HasFireSolution = false;
            return;
        }
        HasFireSolution = BallisticTrajectory.TryGetMortarVelocity(
            firePoint.position,
            AimPoint,
            mortarElevation,
            minimumLaunchSpeed,
            ballisticSpeed,
            1f,
            out solutionVelocity,
            out float chargeSpeed);

        if (!HasFireSolution)
            return;

        ChargeSpeed = chargeSpeed;
        Vector3 flatVelocity = new Vector3(solutionVelocity.x, 0f, solutionVelocity.z);
        ElevationDegrees = Mathf.Atan2(solutionVelocity.y, flatVelocity.magnitude) * Mathf.Rad2Deg;
        AzimuthDegrees = Mathf.Repeat(Mathf.Atan2(solutionVelocity.x, solutionVelocity.z) * Mathf.Rad2Deg, 360f);
    }

    public void SetMapTarget(Vector3 target)
    {
        mapTarget = target;
        hasMapTarget = true;
        UpdateFireSolution();
    }

    public void ClearMapTarget()
    {
        hasMapTarget = false;
        IsAimed = false;
    }

    public bool TryGetSpotterMarkerPosition(out Vector3 position)
    {
        if (SpotterSmokeMarker.TryGetActiveMarker(GetOwningTeam(), out SpotterSmokeMarker marker))
        {
            position = marker.transform.position;
            return true;
        }

        position = Vector3.zero;
        return false;
    }

    Team GetOwningTeam()
    {
        TeamMember member = transform.root.GetComponentInChildren<TeamMember>(true);
        return member != null ? member.team : Team.Neutral;
    }

    void AlignPlatformToTarget()
    {
        if (!hasMapTarget || !HasFireSolution)
        {
            IsAimed = true;
            return;
        }

        Transform vehicleRoot = transform.root;
        Vector3 desiredForward = AimPoint - vehicleRoot.position;
        desiredForward.y = 0f;
        if (desiredForward.sqrMagnitude < 0.01f)
        {
            IsAimed = true;
            return;
        }

        Quaternion desiredRotation = Quaternion.LookRotation(desiredForward.normalized, Vector3.up);
        float difference = Quaternion.Angle(vehicleRoot.rotation, desiredRotation);
        IsAimed = difference <= 2f;
        if (IsAimed)
            return;

        Rigidbody vehicleBody = vehicleRoot.GetComponent<Rigidbody>();
        Quaternion next = Quaternion.RotateTowards(
            vehicleRoot.rotation,
            desiredRotation,
            traverseSpeed * Time.deltaTime);
        if (vehicleBody != null && !vehicleBody.isKinematic)
            vehicleBody.MoveRotation(next);
        else
            vehicleRoot.rotation = next;
    }

    void SetComputerAiming(bool active)
    {
        foreach (TankCannonAim cannon in transform.root.GetComponentsInChildren<TankCannonAim>(true))
            cannon.SetComputerAim(active, mortarElevation);

        foreach (TurretController turret in transform.root.GetComponentsInChildren<TurretController>(true))
            turret.SetComputerAim(active, AimPoint);
    }

    void BeginDeployment()
    {
        deploymentState = DeploymentState.Deploying;
        deploymentCompleteTime = Time.time + deploymentDuration;
        SetVehicleMovement(false);
        SetComputerAiming(true);
    }

    void BeginRetraction()
    {
        deploymentState = DeploymentState.Retracting;
        deploymentCompleteTime = Time.time + deploymentDuration;
        SetComputerAiming(false);
        foreach (TankCannonAim cannon in transform.root.GetComponentsInChildren<TankCannonAim>(true))
            cannon.SetStowAim();
    }

    void UpdateDeployment()
    {
        if (!IsDeploying && !IsRetracting)
            return;

        if (Time.time < deploymentCompleteTime)
            return;

        if (IsDeploying)
        {
            deploymentState = DeploymentState.Deployed;
            return;
        }

        deploymentState = DeploymentState.Mobile;
        ReleaseComputerAiming();
        SetVehicleMovement(true);
    }

    void SetVehicleMovement(bool enabled)
    {
        foreach (VehicleMotorBase motor in transform.root.GetComponentsInChildren<VehicleMotorBase>(true))
            motor.EnableMovement(enabled);
    }

    void ReleaseComputerAiming()
    {
        foreach (TankCannonAim cannon in transform.root.GetComponentsInChildren<TankCannonAim>(true))
            cannon.ReleaseComputerAim();
    }

    void OnDisable()
    {
        if (deploymentState == DeploymentState.Mobile)
            return;

        deploymentState = DeploymentState.Mobile;
        SetComputerAiming(false);
        ReleaseComputerAiming();
        SetVehicleMovement(true);
    }
}

// Shared solver: future artillery and mortar weapons can use this directly.
public static class BallisticTrajectory
{
    // Mortar solution: constant practical elevation, while propellant charge
    // changes with range. This avoids near-vertical shots at close targets.
    public static bool TryGetMortarVelocity(
        Vector3 origin,
        Vector3 target,
        float elevationDegrees,
        float minimumSpeed,
        float maximumSpeed,
        float gravityMultiplier,
        out Vector3 velocity,
        out float launchSpeed)
    {
        velocity = Vector3.zero;
        launchSpeed = 0f;

        Vector3 offset = target - origin;
        Vector3 horizontal = new Vector3(offset.x, 0f, offset.z);
        float distance = horizontal.magnitude;
        float gravity = -Physics.gravity.y * Mathf.Clamp(gravityMultiplier, 0.05f, 1f);
        float elevation = elevationDegrees * Mathf.Deg2Rad;
        float cosine = Mathf.Cos(elevation);
        float denominator = 2f * cosine * cosine *
            (distance * Mathf.Tan(elevation) - offset.y);

        if (distance < 0.1f || gravity <= 0f || denominator <= 0.001f)
            return false;

        launchSpeed = Mathf.Sqrt(gravity * distance * distance / denominator);
        if (launchSpeed < minimumSpeed || launchSpeed > maximumSpeed)
            return false;

        velocity = horizontal.normalized * (launchSpeed * cosine) +
            Vector3.up * (launchSpeed * Mathf.Sin(elevation));
        return true;
    }

    public static bool TryGetHighArcVelocity(
        Vector3 origin,
        Vector3 target,
        float launchSpeed,
        out Vector3 velocity)
    {
        velocity = Vector3.zero;

        Vector3 offset = target - origin;
        Vector3 horizontal = new Vector3(offset.x, 0f, offset.z);
        float horizontalDistance = horizontal.magnitude;
        float gravity = -Physics.gravity.y;

        if (horizontalDistance < 0.1f || gravity <= 0f || launchSpeed <= 0f)
            return false;

        float speedSquared = launchSpeed * launchSpeed;
        float discriminant = speedSquared * speedSquared - gravity *
            (gravity * horizontalDistance * horizontalDistance + 2f * offset.y * speedSquared);

        if (discriminant < 0f)
            return false;

        // Positive root: high trajectory for indirect fire.
        float tangent = (speedSquared + Mathf.Sqrt(discriminant)) /
            (gravity * horizontalDistance);
        float horizontalSpeed = launchSpeed / Mathf.Sqrt(1f + tangent * tangent);

        velocity = horizontal.normalized * horizontalSpeed + Vector3.up * horizontalSpeed * tangent;
        return true;
    }
}

// Jeep and IFV forward-observer ability. The marker is a visual reference on
// the SPG tactical map and reveals nearby hostile units.
public class ArtillerySpotter : MonoBehaviour
{
    public float smokeDuration = 60f;
    public float revealRadius = 70f;
    public float cooldown = 5f;
    float nextAvailableTime;

    void Update()
    {
        if (!VehicleController.IsLocallyControlled(this) || !Input.GetKeyDown(KeyCode.G) ||
            Time.time < nextAvailableTime)
            return;

        VehicleMotor motor = GetComponentInChildren<VehicleMotor>(true);
        if (motor == null || motor.blueprint == null ||
            (motor.blueprint.role != VehicleRole.Jeep && motor.blueprint.role != VehicleRole.APC))
            return;

        TeamMember member = GetComponentInChildren<TeamMember>(true);
        if (member == null || member.team == Team.Neutral)
            return;

        Vector3 position = transform.position + transform.forward * 3f;
        if (Physics.Raycast(position + Vector3.up * 20f, Vector3.down, out RaycastHit hit, 40f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            position = hit.point;

        SpotterSmokeMarker.Create(member.team, position, smokeDuration, revealRadius);
        nextAvailableTime = Time.time + cooldown;
    }
}

public class SpotterSmokeMarker : MonoBehaviour
{
    static SpotterSmokeMarker teamAMarker;
    static SpotterSmokeMarker teamBMarker;

    public Team OwningTeam { get; private set; }
    public int CoordinateX { get; private set; }
    public int CoordinateZ { get; private set; }

    public static void Create(Team team, Vector3 position, float duration, float revealRadius = 70f)
    {
        SpotterSmokeMarker previous = team == global::Team.TeamA ? teamAMarker : teamBMarker;
        if (previous != null)
            Object.Destroy(previous.gameObject);

        GameObject markerObject = new GameObject("Orange Spotter Smoke");
        markerObject.transform.position = position + Vector3.up * 0.1f;
        SpotterSmokeMarker marker = markerObject.AddComponent<SpotterSmokeMarker>();
        marker.OwningTeam = team;
        marker.CoordinateX = Mathf.RoundToInt(position.x);
        marker.CoordinateZ = Mathf.RoundToInt(position.z);

        TacticalRevealZone revealZone = markerObject.AddComponent<TacticalRevealZone>();
        revealZone.Configure(team, revealRadius);

        if (team == global::Team.TeamA) teamAMarker = marker;
        else if (team == global::Team.TeamB) teamBMarker = marker;

        if (!TryCreateWarFxSmoke(markerObject.transform, duration))
            CreateFallbackSmoke(markerObject, duration);

        Light glow = markerObject.AddComponent<Light>();
        glow.color = new Color(1f, 0.28f, 0.02f);
        glow.range = 8f;
        glow.intensity = 2f;
        Object.Destroy(markerObject, duration);
    }

    static bool TryCreateWarFxSmoke(Transform parent, float duration)
    {
#if UNITY_EDITOR
        const string warFxSmokePath =
            "Assets/JMO Assets/WarFX/_Effects (Mobile)/Smoke/Color Variants/WFXMR_SmokeGrenade Red.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(warFxSmokePath);
        if (prefab == null)
            return false;

        GameObject effect = Object.Instantiate(prefab, parent.position, parent.rotation, parent);
        effect.name = "WarFX Orange Spotter Smoke";
        Color orange = new Color(1f, 0.30f, 0.02f, 0.82f);
        foreach (ParticleSystem system in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.duration = duration;
            main.startColor = orange;
        }
        return true;
#else
        return false;
#endif
    }

    static void CreateFallbackSmoke(GameObject markerObject, float duration)
    {
        ParticleSystem smoke = markerObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = smoke.main;
        main.duration = duration;
        main.loop = true;
        main.startLifetime = 3.5f;
        main.startSpeed = 1.2f;
        main.startSize = 3.5f;
        main.startColor = new Color(1f, 0.32f, 0.02f, 0.75f);
        ParticleSystem.EmissionModule emission = smoke.emission;
        emission.rateOverTime = 18f;
        ParticleSystem.ShapeModule shape = smoke.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.radius = 0.8f;
    }

    public static bool TryGetActiveMarker(Team team, out SpotterSmokeMarker marker)
    {
        marker = team == global::Team.TeamA ? teamAMarker : team == global::Team.TeamB ? teamBMarker : null;
        return marker != null;
    }

    void OnDestroy()
    {
        if (teamAMarker == this) teamAMarker = null;
        if (teamBMarker == this) teamBMarker = null;
    }
}

// Shared visibility source for smoke grenades and artillery flares.  The
// tactical map deliberately knows only these circles, not global enemy state.
public class TacticalRevealZone : MonoBehaviour
{
    static readonly System.Collections.Generic.HashSet<TacticalRevealZone> activeZones =
        new System.Collections.Generic.HashSet<TacticalRevealZone>();

    Team owningTeam;
    float radius;

    public void Configure(Team team, float range)
    {
        owningTeam = team;
        radius = Mathf.Max(1f, range);
    }

    void OnEnable()
    {
        activeZones.Add(this);
    }

    void OnDisable()
    {
        activeZones.Remove(this);
    }

    public static bool IsRevealed(Team observingTeam, Vector3 worldPosition)
    {
        foreach (TacticalRevealZone zone in activeZones)
        {
            if (zone == null || zone.owningTeam != observingTeam)
                continue;

            Vector3 offset = worldPosition - zone.transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude <= zone.radius * zone.radius)
                return true;
        }
        return false;
    }

}
