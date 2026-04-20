using UnityEngine;
using Photon.Pun;

/// <summary>
/// Secondary weapon: Fast, accurate hitscan pistol that shoots ketchup.
/// </summary>
public class KetchupPistol : WeaponBase
{
    [Header("Ketchup Pistol Settings")]
    [SerializeField] private GameObject bulletHolePrefab;
    [SerializeField] private GameObject bloodSplatPrefab; // Ketchup splat!
    [SerializeField] private LineRenderer bulletTrail;
    [SerializeField] private float trailDuration = 0.1f;

    protected override void Awake()
    {
        base.Awake();

        // Set default values for Ketchup Pistol
        weaponName = "Ketchup Pistol";
        weaponSlot = WeaponSlot.Secondary;
        maxAmmo = 12;
        currentAmmo = 12;
        reserveAmmo = 36;
        reloadTime = 1.5f;
        damage = 20;
        fireRate = 0.3f;
        range = 100f;
        isAutomatic = false;
    }

    protected override void PerformAttack()
    {
        Camera cam = GetShootCamera();
        if (cam == null) return;

        // Raycast from center of screen
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        Vector3 endPoint;

        if (Physics.Raycast(ray, out hit, range))
        {
            endPoint = hit.point;

            // Check if we hit a player (use GetComponentInParent in case collider is on a child)
            PlayerHealth targetHealth = hit.collider.GetComponentInParent<PlayerHealth>();
            PlayerController targetController = targetHealth != null ? targetHealth.GetComponent<PlayerController>() : null;
            bool validHit = targetController == null || targetController.IsValidDamageHit(hit.collider, hit.point);
            if (targetHealth != null && !targetHealth.photonView.IsMine && validHit)
            {
                targetHealth.TakeDamage(damage, GetOwnerViewID(), GetOwnerActorNumber());
                ShowHitIndicatorOnHUD();
                SpawnHitEffect(hit.point, hit.normal, true);
            }
            else
            {
                SpawnHitEffect(hit.point, hit.normal, false);
            }

            Debug.Log($"Ketchup Pistol hit: {hit.collider.name}");
        }
        else
        {
            endPoint = ray.origin + ray.direction * range;
        }

        // Muzzle position for trail (use camera if no muzzle point)
        Vector3 startPos = muzzlePoint != null ? muzzlePoint.position : cam.transform.position + cam.transform.forward;

        // Show bullet trail
        if (bulletTrail != null)
        {
            StartCoroutine(ShowBulletTrail(startPos, endPoint));
        }

        // Sync hit effect to other players
        photonView.RPC("RPC_ShowHitEffect", RpcTarget.Others, startPos, endPoint);
    }

    /// <summary>
    /// Spawns a hit effect at the impact point.
    /// </summary>
    private void SpawnHitEffect(Vector3 position, Vector3 normal, bool hitPlayer)
    {
        if (hitPlayer && bloodSplatPrefab != null)
        {
            // Spawn ketchup splat on player hit
            GameObject splat = Instantiate(bloodSplatPrefab, position, Quaternion.LookRotation(normal));
            Destroy(splat, 5f);
        }
        else if (!hitPlayer && bulletHolePrefab != null)
        {
            // Spawn bullet hole on environment hit
            GameObject hole = Instantiate(bulletHolePrefab, position + normal * 0.01f, Quaternion.LookRotation(normal));
            Destroy(hole, 10f);
        }
    }

    /// <summary>
    /// Coroutine to show bullet trail briefly.
    /// </summary>
    private System.Collections.IEnumerator ShowBulletTrail(Vector3 start, Vector3 end)
    {
        if (bulletTrail == null) yield break;

        bulletTrail.enabled = true;
        bulletTrail.SetPosition(0, start);
        bulletTrail.SetPosition(1, end);

        yield return new WaitForSeconds(trailDuration);

        bulletTrail.enabled = false;
    }

    [PunRPC]
    private void RPC_ShowHitEffect(Vector3 start, Vector3 end)
    {
        // Show bullet trail for other players
        if (bulletTrail != null)
        {
            StartCoroutine(ShowBulletTrail(start, end));
        }
    }
}
