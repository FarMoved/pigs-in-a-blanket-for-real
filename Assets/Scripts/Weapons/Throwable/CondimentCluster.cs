using UnityEngine;
using Photon.Pun;

/// <summary>
/// Throwable weapon: A cluster grenade that explodes into multiple condiment splashes.
/// </summary>
public class CondimentCluster : WeaponBase
{
    [Header("Condiment Cluster Settings")]
    [SerializeField] private GameObject grenadePrefab;
    [SerializeField] private float throwForce = 15f;
    [SerializeField] private float upwardForce = 5f;
    [SerializeField] private float fuseTime = 3f;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private int clusterCount = 5;

    protected override void Awake()
    {
        base.Awake();

        // Set default values for Condiment Cluster
        weaponName = "Condiment Cluster";
        weaponSlot = WeaponSlot.Throwable;
        maxAmmo = 1;
        currentAmmo = 1;
        reserveAmmo = 3;
        reloadTime = 0f; // Instant "reload" - just grab next grenade
        damage = 35;
        fireRate = 1f;
        isAutomatic = false;
    }

    protected override void PerformAttack()
    {
        Camera cam = GetShootCamera();
        if (cam == null) return;

        // Calculate throw direction
        Vector3 throwDirection = cam.transform.forward;
        Vector3 spawnPosition = muzzlePoint != null ? muzzlePoint.position : cam.transform.position + cam.transform.forward;

        // Spawn grenade
        if (grenadePrefab != null)
        {
            GameObject grenade = PhotonNetwork.Instantiate(
                grenadePrefab.name,
                spawnPosition,
                Quaternion.identity
            );

            // Initialize grenade
            CondimentGrenade grenadeScript = grenade.GetComponent<CondimentGrenade>();
            if (grenadeScript != null)
            {
                Vector3 velocity = throwDirection * throwForce + Vector3.up * upwardForce;
                grenadeScript.Initialize(velocity, damage, explosionRadius, fuseTime, clusterCount, GetOwnerViewID(), GetOwnerActorNumber());
            }
        }
        else
        {
            Debug.Log("Condiment Cluster thrown! (Prefab not assigned)");
        }

        // Auto-reload if we have reserve
        if (reserveAmmo > 0)
        {
            reserveAmmo--;
            currentAmmo = 1;
            OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
        }
    }

    /// <summary>
    /// Override to handle the unique ammo system for grenades.
    /// </summary>
    protected override void Fire()
    {
        if (isReloading || currentAmmo <= 0) return;

        currentAmmo--;
        nextFireTime = Time.time + fireRate;

        // Play effects
        PlaySound(fireSound);

        // Throw the grenade
        PerformAttack();

        // Update UI
        OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
    }
}
