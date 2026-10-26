using UnityEngine;

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

    [Header("Fire Rate")]
    public float reloadTime = 5f;
    [Header("Area Damage")]
    public float shellDamage = 260f;
    public float explosionRadius = 10f;

    float reloadTimer;

    // =====================================================
    // UPDATE
    // =====================================================
    void Update()
    {
        reloadTimer -= Time.deltaTime;

        // TEST FIRE
        if (Input.GetMouseButtonDown(0))
        {
            TryFire();
        }
    }

    // =====================================================
    // FIRE
    // =====================================================
    public void TryFire()
    {
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
            artilleryShell.damage = shellDamage;
            artilleryShell.explosionRadius = explosionRadius;
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

        // =====================================
        // FORCE
        // =====================================
        Vector3 force =
            firePoint.forward * launchForce +
            Vector3.up * arcForce;

        rb.AddForce(
            force,
            ForceMode.Impulse
        );

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
}
