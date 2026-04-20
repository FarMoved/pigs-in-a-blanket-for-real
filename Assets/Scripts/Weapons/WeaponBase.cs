using UnityEngine;
using Photon.Pun;

/// <summary>
/// Base class for all weapons in the game.
/// Handles common functionality like ammo, firing rates, and animations.
/// </summary>
public abstract class WeaponBase : MonoBehaviourPunCallbacks
{
    [Header("Weapon Info")]
    [SerializeField] protected string weaponName;
    [SerializeField] protected WeaponSlot weaponSlot;
    [SerializeField] protected Sprite weaponIcon;

    [Header("Ammo Settings")]
    [SerializeField] protected int maxAmmo = 30;
    [SerializeField] protected int currentAmmo = 30;
    [SerializeField] protected int reserveAmmo = 90;
    [SerializeField] protected float reloadTime = 2f;

    [Header("Firing Settings")]
    [SerializeField] protected int damage = 25;
    [SerializeField] protected float fireRate = 0.1f; // Time between shots
    [SerializeField] protected float range = 100f;
    [SerializeField] protected bool isAutomatic = true;

    [Header("Audio")]
    [SerializeField] protected AudioClip fireSound;
    [SerializeField] protected AudioClip reloadSound;
    [SerializeField] protected AudioClip emptySound;

    [Header("Visual")]
    [SerializeField] protected Transform muzzlePoint;
    [SerializeField] protected ParticleSystem muzzleFlash;

    // State
    protected bool isReloading = false;
    protected float nextFireTime = 0f;
    protected AudioSource audioSource;

    // Events
    public System.Action<int, int> OnAmmoChanged; // current, reserve
    public System.Action OnStartReload;
    public System.Action OnEndReload;
    /// <summary>Optional: fired when melee hits something (passes hit object name).</summary>
    public System.Action<string> OnMeleeHit;

    // Properties
    public string WeaponName => weaponName;
    public WeaponSlot Slot => weaponSlot;
    public Sprite Icon => weaponIcon;
    public int CurrentAmmo => currentAmmo;
    public int ReserveAmmo => reserveAmmo;
    public bool IsReloading => isReloading;
    /// <summary>If false, HUD shows "—" instead of ammo count (e.g. melee).</summary>
    public virtual bool ShowAmmo => true;

    protected virtual void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    protected virtual void Start()
    {
        currentAmmo = maxAmmo;
        OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
    }

    /// <summary>
    /// Called every frame by WeaponManager when this weapon is active.
    /// Handles input for firing and reloading.
    /// </summary>
    public virtual void HandleInput()
    {
        if (isReloading) return;

        // Fire input
        bool fireInput = isAutomatic ? Input.GetButton("Fire1") : Input.GetButtonDown("Fire1");

        if (fireInput && Time.time >= nextFireTime)
        {
            if (currentAmmo > 0)
            {
                Fire();
            }
            else
            {
                // Click on empty - play sound and auto-start reload if we have reserve
                PlaySound(emptySound);
                nextFireTime = Time.time + 0.2f;
                if (reserveAmmo > 0 && !isReloading)
                {
                    StartReload();
                }
            }
        }

        // Reload input
        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < maxAmmo && reserveAmmo > 0 && !isReloading)
        {
            StartReload();
        }
    }

    /// <summary>
    /// Fires the weapon. Override in derived classes for specific behavior.
    /// </summary>
    protected virtual void Fire()
    {
        if (isReloading || currentAmmo <= 0) return;

        currentAmmo--;
        nextFireTime = Time.time + fireRate;

        // Play effects locally
        PlaySound(fireSound);
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        // Perform the actual attack (implemented by derived classes)
        PerformAttack();

        // Update UI
        OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);

        // Sync firing effect to other players (weapon is child; use player's PhotonView and RPC on WeaponManager)
        PhotonView playerView = GetComponentInParent<PhotonView>();
        WeaponManager wm = GetComponentInParent<WeaponManager>();
        if (playerView != null && wm != null)
        {
            int slot = wm.GetSlotForWeapon(this);
            playerView.RPC("RPC_FireEffect", RpcTarget.Others, slot);
        }
    }

    /// <summary>
    /// Override this to implement the specific attack behavior (raycast, projectile, etc.)
    /// </summary>
    protected abstract void PerformAttack();

    /// <summary>
    /// Play fire sound and muzzle flash only. Called on remote clients via WeaponManager.RPC_FireEffect.
    /// </summary>
    public virtual void PlayFireEffect()
    {
        PlaySound(fireSound);
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }
    }

    /// <summary>
    /// Starts the reload process.
    /// </summary>
    protected virtual void StartReload()
    {
        if (isReloading || currentAmmo >= maxAmmo || reserveAmmo <= 0) return;

        isReloading = true;
        OnStartReload?.Invoke();
        PlaySound(reloadSound);

        Invoke(nameof(FinishReload), reloadTime);
    }

    /// <summary>
    /// Completes the reload process.
    /// </summary>
    protected virtual void FinishReload()
    {
        int ammoNeeded = maxAmmo - currentAmmo;
        int ammoToAdd = Mathf.Min(ammoNeeded, reserveAmmo);

        currentAmmo += ammoToAdd;
        reserveAmmo -= ammoToAdd;

        isReloading = false;
        OnEndReload?.Invoke();
        OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
    }

    /// <summary>
    /// Adds ammo to reserve.
    /// </summary>
    public void AddAmmo(int amount)
    {
        reserveAmmo += amount;
        OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
    }

    /// <summary>
    /// Plays a sound effect.
    /// </summary>
    protected void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Called when this weapon is equipped.
    /// </summary>
    public virtual void OnEquip()
    {
        gameObject.SetActive(true);
        CancelInvoke(nameof(FinishReload));
        isReloading = false;
    }

    /// <summary>
    /// Called when this weapon is unequipped.
    /// </summary>
    public virtual void OnUnequip()
    {
        CancelInvoke(nameof(FinishReload));
        isReloading = false;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Gets the PhotonView ID of the owner for kill credit.
    /// </summary>
    protected int GetOwnerViewID()
    {
        PhotonView parentView = GetComponentInParent<PhotonView>();
        return parentView != null ? parentView.ViewID : -1;
    }

    /// <summary>
    /// Gets the owner's ActorNumber so the victim can report the kill correctly (no reliance on PhotonView.Find on victim).
    /// </summary>
    protected int GetOwnerActorNumber()
    {
        PhotonView parentView = GetComponentInParent<PhotonView>();
        return (parentView != null && parentView.Owner != null) ? parentView.Owner.ActorNumber : -1;
    }

    /// <summary>
    /// Call after dealing damage to show the hit indicator on the local player's HUD.
    /// </summary>
    protected void ShowHitIndicatorOnHUD()
    {
        PhotonView pv = GetComponentInParent<PhotonView>();
        if (pv == null || !pv.IsMine) return;
        var hud = FindObjectOfType<HUDController>();
        if (hud != null)
            hud.ShowHitIndicator();
    }

    /// <summary>
    /// Gets the camera used for shooting (player's first-person camera).
    /// Uses the camera in our parent (CameraHolder) so it works even when not tagged MainCamera.
    /// </summary>
    protected Camera GetShootCamera()
    {
        if (transform.parent != null)
        {
            Camera cam = transform.parent.GetComponentInChildren<Camera>();
            if (cam != null && cam.enabled) return cam;
        }
        return Camera.main;
    }
}

/// <summary>
/// Weapon slot categories.
/// </summary>
public enum WeaponSlot
{
    Primary,    // 1 key - Hot Dog Launcher
    Secondary,  // 2 key - Ketchup Pistol
    Melee,      // 3 key - Spatula Slapper
    Throwable   // 4 key - Condiment Cluster
}
