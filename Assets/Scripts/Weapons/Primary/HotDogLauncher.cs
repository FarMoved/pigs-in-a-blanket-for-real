using UnityEngine;
using Photon.Pun;

/// <summary>
/// Primary weapon: Launches hot dog projectiles that arc and explode on impact.
/// </summary>
public class HotDogLauncher : WeaponBase
{
    [Header("Hot Dog Launcher Settings")]
    [SerializeField] private GameObject hotDogProjectilePrefab;
    [SerializeField] private float projectileSpeed = 20f;
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private int explosionDamage = 40;

    protected override void Awake()
    {
        base.Awake();

        // Set default values for Hot Dog Launcher
        weaponName = "Hot Dog Launcher";
        weaponSlot = WeaponSlot.Primary;
        maxAmmo = 6;
        currentAmmo = 6;
        reserveAmmo = 18;
        reloadTime = 2.5f;
        damage = explosionDamage;
        fireRate = 0.8f;
        isAutomatic = false;
    }

    protected override void PerformAttack()
    {
        if (muzzlePoint != null)
        {
            SpawnProjectile(muzzlePoint.position, muzzlePoint.rotation);
        }
        else
        {
            // No muzzle point: use raycast fallback (works without projectile prefab too)
            FallbackRaycastFire();
        }
    }

    private void SpawnProjectile(Vector3 position, Quaternion rotation)
    {
        if (hotDogProjectilePrefab != null)
        {
            // Spawn networked projectile
            GameObject projectile = PhotonNetwork.Instantiate(
                hotDogProjectilePrefab.name,
                position,
                rotation
            );

            // Initialize projectile
            HotDogProjectile proj = projectile.GetComponent<HotDogProjectile>();
            if (proj != null)
            {
                proj.Initialize(projectileSpeed, explosionDamage, explosionRadius, GetOwnerViewID(), GetOwnerActorNumber());
            }
        }
        else
        {
            // Fallback: use raycast with splash damage
            Debug.Log("Hot Dog Launcher fired! (Projectile prefab not assigned)");
            FallbackRaycastFire();
        }
    }

    /// <summary>
    /// Fallback firing method if projectile prefab isn't set up yet.
    /// Ignores the shooter's own colliders (weapon, body) so the ray doesn't hit itself.
    /// </summary>
    private void FallbackRaycastFire()
    {
        Camera cam = GetShootCamera();
        if (cam == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        PhotonView ownerView = GetComponentInParent<PhotonView>();
        if (ownerView == null) return;

        RaycastHit[] hits = Physics.RaycastAll(ray, range);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            // Skip hits on ourselves (weapon, body, etc.)
            if (hit.collider.GetComponentInParent<PhotonView>() == ownerView)
                continue;

            // First hit that isn't us is the real target
            Debug.Log($"Hot Dog hit: {hit.collider.name} at {hit.point}");

            int ownerViewID = GetOwnerViewID();
            PlayerHealth directTarget = hit.collider.GetComponentInParent<PlayerHealth>();
            if (directTarget != null && directTarget.photonView.ViewID != ownerViewID)
            {
                // Direct target hit (typically in air): apply direct damage only, no AoE splash.
                PlayerController targetController = directTarget.GetComponent<PlayerController>();
                bool validDirectHit = targetController == null || targetController.IsValidDamageHit(hit.collider, hit.point);
                if (validDirectHit)
                {
                    directTarget.TakeDamage(explosionDamage, ownerViewID, GetOwnerActorNumber());
                    ShowHitIndicatorOnHUD();
                }
                return;
            }

            // Environment impact: apply AoE splash around contact point.
            Collider[] hitColliders = Physics.OverlapSphere(hit.point, explosionRadius);
            System.Collections.Generic.HashSet<int> damagedViews = new System.Collections.Generic.HashSet<int>();
            foreach (Collider col in hitColliders)
            {
                PlayerHealth targetHealth = col.GetComponentInParent<PlayerHealth>();
                if (targetHealth != null &&
                    targetHealth.photonView.ViewID != ownerViewID &&
                    !damagedViews.Contains(targetHealth.photonView.ViewID))
                {
                    PlayerController targetController = targetHealth.GetComponent<PlayerController>();
                    Vector3 samplePoint = col.ClosestPoint(hit.point);
                    bool validHit = targetController == null || targetController.IsValidDamageHit(col, samplePoint, 0.08f);
                    if (!validHit) continue;

                    float distance = Vector3.Distance(hit.point, col.transform.position);
                    float damageMultiplier = 1f - (distance / explosionRadius);
                    int finalDamage = Mathf.RoundToInt(explosionDamage * damageMultiplier);

                    targetHealth.TakeDamage(finalDamage, ownerViewID, GetOwnerActorNumber());
                    ShowHitIndicatorOnHUD();
                    damagedViews.Add(targetHealth.photonView.ViewID);
                }
            }
            return;
        }
    }
}
