using UnityEngine;

// Lightweight cannon for the runtime-built APC/IFV prefab. A manually placed
// child named Firepoint is the authoritative muzzle location.
public class IFVAutocannon : MonoBehaviour
{
    public Transform firePoint;
    [HideInInspector] public string generatedFirePointName = "IFV_GunPoint";
    [Header("Firing Arc")]
    public bool rearFireOnly;
    [Header("VFX")]
    public GameObject muzzleFlashPrefab;
    [Min(0.05f)] public float muzzleFlashLifetime = 0.75f;
    public ProjectileBallistic projectilePrefab;
    public float fireRate = 6f;
    public float projectileSpeed = 95f;
    public float damage = 8f;
    [Range(0.1f, 1f)] public float projectileScale = 1f;
    [Min(1)] public int shotsPerBurst = 3;
    public float burstShotInterval = 0.10f;
    public float burstCooldown = 1f;

    float nextFireTime;
    int shotsInBurst;
    float nextAutoTargetScan;

    public float CooldownRemaining => Mathf.Max(0f, nextFireTime - Time.time);
    public int ShotsRemainingInBurst => shotsPerBurst - shotsInBurst;

    public void ConfigureFromVehicleBounds()
    {
        if (firePoint == null)
        {
            Transform existing = FindChildIgnoreCase(transform, "Firepoint") ??
                FindChildIgnoreCase(transform, "IFV_GunPoint");
            if (existing != null)
                firePoint = existing;
            else
            {
                Bounds bounds = GetRendererBounds();
                GameObject point = new GameObject(generatedFirePointName);
                point.transform.SetParent(transform, false);
                // The APC's light cannon is roof-mounted; a manually placed
                // IFV_GunPoint still overrides this generated position.
                point.transform.position = bounds.center + transform.forward * 0.15f + Vector3.up * (bounds.extents.y + 0.20f);
                point.transform.rotation = transform.rotation;
                firePoint = point.transform;
            }
        }

        if (projectilePrefab == null)
        {
            projectilePrefab = Resources.Load<ProjectileBallistic>("Projectiles/Shell_50cal");
        }

        if (projectilePrefab == null)
        {
            foreach (TankGunBallistic gun in Resources.FindObjectsOfTypeAll<TankGunBallistic>())
            {
                if (gun != null && gun.shellPrefab != null)
                {
                    projectilePrefab = gun.shellPrefab;
                    break;
                }
            }
        }
    }

    void Start()
    {
        ConfigureFromVehicleBounds();
        if (projectilePrefab == null)
        {
            Debug.LogWarning("IFV sem prefab de projetil; o canhao foi desativado.");
            enabled = false;
        }
    }

    void Update()
    {
        if (VehicleController.IsLocallyControlled(this) && Input.GetMouseButton(0))
        {
            TryFire();
        }
        else if (rearFireOnly && Time.time >= nextAutoTargetScan)
        {
            nextAutoTargetScan = Time.time + 0.15f;
            TryAutoFireAtEnemyInLine();
        }
    }

    void TryAutoFireAtEnemyInLine()
    {
        TeamMember owner = GetComponentInParent<TeamMember>() ?? transform.root.GetComponentInChildren<TeamMember>(true);
        if (owner == null || owner.team == Team.Neutral || firePoint == null)
            return;

        Health bestTarget = null;
        float bestDistance = 65f * 65f;
        foreach (Health candidate in FindObjectsByType<Health>(FindObjectsSortMode.None))
        {
            if (candidate == null || candidate.transform.root == transform.root)
                continue;
            TeamMember targetTeam = candidate.GetComponentInParent<TeamMember>() ?? candidate.transform.root.GetComponentInChildren<TeamMember>(true);
            if (targetTeam == null || targetTeam.team == Team.Neutral || targetTeam.team == owner.team)
                continue;

            Vector3 offset = candidate.transform.position - firePoint.position;
            float distance = offset.sqrMagnitude;
            if (distance >= bestDistance || offset.sqrMagnitude < 0.01f || Vector3.Dot(firePoint.forward.normalized, offset.normalized) < 0.96f)
                continue;

            if (Physics.Raycast(firePoint.position + firePoint.forward * 0.2f, offset.normalized, out RaycastHit hit,
                Mathf.Sqrt(distance), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                !hit.collider.transform.IsChildOf(candidate.transform.root))
                continue;

            bestDistance = distance;
            bestTarget = candidate;
        }

        if (bestTarget != null)
            TryFire();
    }

    void TryFire()
    {
        if (Time.time < nextFireTime)
            return;

        Fire();
        shotsInBurst++;
        if (shotsInBurst >= shotsPerBurst)
        {
            shotsInBurst = 0;
            nextFireTime = Time.time + burstCooldown;
        }
        else
            nextFireTime = Time.time + burstShotInterval;
    }

    void Fire()
    {
        Vector3 direction;
        if (rearFireOnly)
        {
            // The Jeep mount itself is limited to the rear arc by its turret
            // controller. Fire along the animated barrel, not a fixed hull
            // direction, so every projectile matches the visible aim.
            direction = firePoint.forward;
        }
        else
        {
            Camera camera = Camera.main;
            Ray ray = camera != null ? camera.ViewportPointToRay(new Vector3(0.5f, 0.5f)) : new Ray(firePoint.position, firePoint.forward);
            Vector3 target = ray.GetPoint(1000f);
            foreach (RaycastHit hit in Physics.RaycastAll(ray, 1000f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.transform.IsChildOf(transform))
                {
                    target = hit.point;
                    break;
                }
            }
            direction = (target - firePoint.position).normalized;
        }

        if (direction.sqrMagnitude < 0.001f)
            direction = firePoint.forward;

        Quaternion shotRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        if (muzzleFlashPrefab != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, shotRotation, firePoint);
            Destroy(flash, muzzleFlashLifetime);
        }

        // ProjectileBallistic is the normal shell used by the Tank. Start it
        // just ahead of the muzzle and ignore the IFV colliders, so a shell
        // can never collide with the vehicle that fired it.
        ProjectileBallistic projectile = Instantiate(projectilePrefab, firePoint.position + direction * 0.35f, shotRotation);
        projectile.transform.localScale *= projectileScale;
        projectile.Init(projectileSpeed, damage);
        projectile.IgnoreVehicleCollisions(transform.root);
    }

    Bounds GetRendererBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
        for (int index = 1; index < renderers.Length; index++)
            bounds.Encapsulate(renderers[index].bounds);
        return bounds;
    }

    Transform FindChildIgnoreCase(Transform root, string childName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (string.Equals(child.name, childName, System.StringComparison.OrdinalIgnoreCase))
                return child;
        return null;
    }
}
