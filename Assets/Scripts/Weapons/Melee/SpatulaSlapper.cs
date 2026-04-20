using UnityEngine;
using Photon.Pun;
using System.Collections;

/// <summary>
/// Melee weapon: A spatula used for slapping enemies in close-range combat.
/// High damage, short range.
/// </summary>
public class SpatulaSlapper : WeaponBase
{
    [Header("Spatula Slapper Settings")]
    [SerializeField] private float meleeRange = 2.5f;
    [SerializeField] private float swingAngle = 60f;
    [SerializeField] private float swingDuration = 0.3f;
    [SerializeField] private Transform spatulaModel;

    [Header("Effects")]
    [SerializeField] private AudioClip swingSound;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private GameObject hitEffectPrefab;

    private bool isSwinging = false;
    private Quaternion originalRotation;

    protected override void Awake()
    {
        base.Awake();

        // Set default values for Spatula Slapper
        weaponName = "Spatula Slapper";
        weaponSlot = WeaponSlot.Melee;
        maxAmmo = 1; // Unlimited - melee
        currentAmmo = 1;
        reserveAmmo = 999;
        reloadTime = 0f;
        damage = 50;
        fireRate = 0.6f; // Time between swings
        range = meleeRange;
        isAutomatic = false;
    }

    protected override void Start()
    {
        base.Start();

        if (spatulaModel != null)
        {
            originalRotation = spatulaModel.localRotation;
        }
    }

    /// <summary>
    /// Override HandleInput since melee doesn't use ammo traditionally.
    /// </summary>
    public override void HandleInput()
    {
        if (isSwinging) return;

        if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime)
        {
            Fire();
        }
    }

    public override bool ShowAmmo => false;

    /// <summary>
    /// Override Fire so melee never uses or consumes ammo (base Fire would block after first swing).
    /// </summary>
    protected override void Fire()
    {
        nextFireTime = Time.time + fireRate;
        PerformAttack();
    }

    protected override void PerformAttack()
    {
        StartCoroutine(SwingAnimation());

        // Play swing sound
        PlaySound(swingSound);

        // Check for hits in front of player (start slightly in front of camera to avoid self-hit)
        Camera cam = GetShootCamera();
        if (cam == null) return;

        PhotonView ownerView = GetComponentInParent<PhotonView>();
        Vector3 origin = cam.transform.position + cam.transform.forward * 0.5f;
        Vector3 direction = cam.transform.forward;

        RaycastHit[] hits = Physics.SphereCastAll(origin, 0.6f, direction, meleeRange);

        bool hitEnemy = false;
        bool hitAnything = false;
        string hitName = "";

        foreach (RaycastHit hit in hits)
        {
            // Skip our own colliders (player, weapon)
            if (hit.collider.GetComponentInParent<PhotonView>() == ownerView)
                continue;

            hitAnything = true;
            hitName = hit.collider.name;

            PlayerHealth targetHealth = hit.collider.GetComponentInParent<PlayerHealth>();
            PlayerController targetController = targetHealth != null ? targetHealth.GetComponent<PlayerController>() : null;
            bool validHit = targetController == null || targetController.IsValidDamageHit(hit.collider, hit.point);
            if (targetHealth != null && !targetHealth.photonView.IsMine && validHit)
            {
                targetHealth.TakeDamage(damage, GetOwnerViewID(), GetOwnerActorNumber());
                ShowHitIndicatorOnHUD();
                hitEnemy = true;
                SpawnHitEffect(hit.point);
                Debug.Log($"Spatula Slapper hit: {hit.collider.name} for {damage} damage!");
            }
        }

        if (hitAnything)
        {
            if (!hitEnemy)
            {
                Debug.Log($"Spatula hit: {hitName}");
            }
            PlaySound(hitSound);
            OnMeleeHit?.Invoke(hitName);
        }

        // Sync swing to other players (weapon is child of player; use player's PhotonView)
        if (ownerView != null)
        {
            ownerView.RPC("RPC_TriggerMeleeSwing", RpcTarget.Others);
        }
    }

    /// <summary>
    /// Animates the spatula swing. Respects fire rate even when spatulaModel is null.
    /// </summary>
    private IEnumerator SwingAnimation()
    {
        isSwinging = true;

        if (spatulaModel != null)
        {
            float elapsed = 0f;
            float halfDuration = swingDuration / 2f;

            // Swing forward
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                float angle = Mathf.Lerp(0, swingAngle, t);
                spatulaModel.localRotation = originalRotation * Quaternion.Euler(-angle, 0, 0);
                yield return null;
            }

            // Swing back
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                float angle = Mathf.Lerp(swingAngle, 0, t);
                spatulaModel.localRotation = originalRotation * Quaternion.Euler(-angle, 0, 0);
                yield return null;
            }

            spatulaModel.localRotation = originalRotation;
        }
        else
        {
            yield return new WaitForSeconds(swingDuration);
        }

        isSwinging = false;
    }

    /// <summary>
    /// Called by WeaponManager RPC so remote clients see/hear the swing.
    /// </summary>
    public void PlaySwingEffectForRemote()
    {
        PlaySound(swingSound);
        StartCoroutine(SwingAnimation());
    }

    /// <summary>
    /// Spawns a hit effect at the impact point.
    /// </summary>
    private void SpawnHitEffect(Vector3 position)
    {
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }

    /// <summary>
    /// Melee doesn't need reloading.
    /// </summary>
    protected override void StartReload()
    {
        // No reload for melee
    }
}
