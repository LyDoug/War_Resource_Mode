using UnityEngine;

// Lightweight cannon for the runtime-built APC/IFV prefab. A manually placed
// child named Firepoint is the authoritative muzzle location.
public class IFVAutocannon : MonoBehaviour
{
    public Transform firePoint;
    public ProjectileBallistic projectilePrefab;
    public float fireRate = 6f;
    public float projectileSpeed = 95f;
    public float damage = 18f;
    [Min(1)] public int shotsPerBurst = 3;
    public float burstShotInterval = 0.10f;
    public float burstCooldown = 1f;

    float nextFireTime;
    int shotsInBurst;

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
                GameObject point = new GameObject("IFV_GunPoint");
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
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            Fire();
            shotsInBurst++;
            if (shotsInBurst >= shotsPerBurst)
            {
                shotsInBurst = 0;
                nextFireTime = Time.time + burstCooldown;
            }
            else
            {
                nextFireTime = Time.time + burstShotInterval;
            }
        }
    }

    void Fire()
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

        Vector3 direction = (target - firePoint.position).normalized;
        // ProjectileBallistic is the normal shell used by the Tank. Start it
        // just ahead of the muzzle and ignore the IFV colliders, so a shell
        // can never collide with the vehicle that fired it.
        ProjectileBallistic projectile = Instantiate(projectilePrefab, firePoint.position + direction * 0.35f, Quaternion.LookRotation(direction));
        projectile.Init(projectileSpeed, damage);
        foreach (Collider ownCollider in GetComponentsInChildren<Collider>())
            foreach (Collider shellCollider in projectile.GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(ownCollider, shellCollider);
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
