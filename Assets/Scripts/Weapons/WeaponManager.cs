using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

/// <summary>
/// Manages the player's weapons - switching, equipping, and input handling.
/// </summary>
public class WeaponManager : MonoBehaviourPunCallbacks
{
    [Header("Weapon Slots")]
    [SerializeField] private WeaponBase primaryWeapon;    // 1 key
    [SerializeField] private WeaponBase secondaryWeapon;  // 2 key
    [SerializeField] private WeaponBase meleeWeapon;      // 3 key
    [SerializeField] private WeaponBase throwableWeapon;  // 4 key

    [Header("Settings")]
    [SerializeField] private int startingWeaponSlot = 0;

    // Current state
    private WeaponBase currentWeapon;
    private int currentSlot = 0;
    private Dictionary<int, WeaponBase> weapons;

    // Events
    public System.Action<WeaponBase> OnWeaponSwitched;

    public WeaponBase CurrentWeapon => currentWeapon;

    private void Start()
    {
        // Initialize weapon dictionary
        weapons = new Dictionary<int, WeaponBase>
        {
            { 0, primaryWeapon },
            { 1, secondaryWeapon },
            { 2, meleeWeapon },
            { 3, throwableWeapon }
        };

        // Disable all weapons initially
        foreach (var weapon in weapons.Values)
        {
            if (weapon != null)
            {
                weapon.gameObject.SetActive(false);
            }
        }

        // Equip first available weapon (or starting slot if assigned)
        int slotToEquip = startingWeaponSlot;
        if (weapons[slotToEquip] == null)
        {
            slotToEquip = GetNextValidSlotFrom(-1);
        }
        if (weapons[slotToEquip] != null)
        {
            SwitchWeapon(slotToEquip);
        }
    }

    /// <summary>
    /// Gets the next slot that has a weapon assigned, starting after fromSlot.
    /// </summary>
    private int GetNextValidSlotFrom(int fromSlot)
    {
        for (int i = 1; i <= 4; i++)
        {
            int slot = ((fromSlot + i) % 4 + 4) % 4;
            if (weapons.ContainsKey(slot) && weapons[slot] != null)
                return slot;
        }
        return 0;
    }

    private void Update()
    {
        // Only process input for local player
        if (!photonView.IsMine) return;

        HandleWeaponSwitching();

        // Let current weapon handle its input
        if (currentWeapon != null)
        {
            currentWeapon.HandleInput();
        }
    }

    /// <summary>
    /// Handles weapon switching input (1-4 keys and scroll wheel).
    /// </summary>
    private void HandleWeaponSwitching()
    {
        // Number keys
        if (Input.GetKeyDown(KeyCode.Alpha1) && weapons.ContainsKey(0) && weapons[0] != null)
        {
            SwitchWeapon(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) && weapons.ContainsKey(1) && weapons[1] != null)
        {
            SwitchWeapon(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) && weapons.ContainsKey(2) && weapons[2] != null)
        {
            SwitchWeapon(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4) && weapons.ContainsKey(3) && weapons[3] != null)
        {
            SwitchWeapon(3);
        }

        // Scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            int newSlot = currentSlot;

            if (scroll > 0)
            {
                // Scroll up - previous weapon
                newSlot = GetPreviousValidSlot();
            }
            else
            {
                // Scroll down - next weapon
                newSlot = GetNextValidSlot();
            }

            if (newSlot != currentSlot)
            {
                SwitchWeapon(newSlot);
            }
        }
    }

    /// <summary>
    /// Switches to the specified weapon slot.
    /// </summary>
    public void SwitchWeapon(int slot)
    {
        if (!weapons.ContainsKey(slot) || weapons[slot] == null)
        {
            Debug.LogWarning($"No weapon in slot {slot}");
            return;
        }

        // Unequip current weapon
        if (currentWeapon != null)
        {
            currentWeapon.OnUnequip();
        }

        // Equip new weapon
        currentSlot = slot;
        currentWeapon = weapons[slot];
        currentWeapon.OnEquip();

        OnWeaponSwitched?.Invoke(currentWeapon);

        Debug.Log($"Switched to {currentWeapon.WeaponName}");

        // Sync to other players
        photonView.RPC("RPC_SwitchWeapon", RpcTarget.Others, slot);
    }

    [PunRPC]
    private void RPC_SwitchWeapon(int slot)
    {
        if (!weapons.ContainsKey(slot) || weapons[slot] == null) return;

        // Unequip current weapon
        if (currentWeapon != null)
        {
            currentWeapon.OnUnequip();
        }

        // Equip new weapon (visual only for remote players)
        currentSlot = slot;
        currentWeapon = weapons[slot];
        currentWeapon.OnEquip();
    }

    /// <summary>
    /// Called by SpatulaSlapper so remote clients see/hear the melee swing (weapon is child, no PhotonView).
    /// </summary>
    [PunRPC]
    private void RPC_TriggerMeleeSwing()
    {
        if (meleeWeapon is SpatulaSlapper slapper)
        {
            slapper.PlaySwingEffectForRemote();
        }
    }

    /// <summary>
    /// Called by WeaponBase so remote clients play fire effect (weapon is child; RPC must be on player's PhotonView).
    /// </summary>
    [PunRPC]
    private void RPC_FireEffect(int slot)
    {
        if (weapons != null && weapons.ContainsKey(slot) && weapons[slot] != null)
        {
            weapons[slot].PlayFireEffect();
        }
    }

    /// <summary>
    /// Returns the slot index (0-3) for the given weapon, or 0 if not found.
    /// </summary>
    public int GetSlotForWeapon(WeaponBase weapon)
    {
        if (weapons == null || weapon == null) return 0;
        foreach (var kv in weapons)
        {
            if (kv.Value == weapon) return kv.Key;
        }
        return 0;
    }

    /// <summary>
    /// Gets the next valid weapon slot (cycling through available weapons).
    /// </summary>
    private int GetNextValidSlot()
    {
        int slot = currentSlot;

        for (int i = 0; i < 4; i++)
        {
            slot = (slot + 1) % 4;
            if (weapons.ContainsKey(slot) && weapons[slot] != null)
            {
                return slot;
            }
        }

        return currentSlot;
    }

    /// <summary>
    /// Gets the previous valid weapon slot (cycling through available weapons).
    /// </summary>
    private int GetPreviousValidSlot()
    {
        int slot = currentSlot;

        for (int i = 0; i < 4; i++)
        {
            slot = (slot - 1 + 4) % 4;
            if (weapons.ContainsKey(slot) && weapons[slot] != null)
            {
                return slot;
            }
        }

        return currentSlot;
    }

    /// <summary>
    /// Sets weapons (used when setting up the player prefab).
    /// </summary>
    public void SetWeapons(WeaponBase primary, WeaponBase secondary, WeaponBase melee, WeaponBase throwable)
    {
        weapons[0] = primary;
        weapons[1] = secondary;
        weapons[2] = melee;
        weapons[3] = throwable;
    }
}
