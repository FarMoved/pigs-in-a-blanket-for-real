using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

/// <summary>
/// Handles player health, damage, death, and respawning.
/// Synced across network via Photon RPCs.
/// </summary>
public class PlayerHealth : MonoBehaviourPunCallbacks
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float respawnDelay = 2f;

    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private GameObject playerModel;

    // Current health (synced via RPC)
    private int currentHealth;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public float RespawnDelay => respawnDelay;
    public bool IsDead => currentHealth <= 0;

    // Events for UI updates
    public System.Action<int, int> OnHealthChanged; // current, max
    public System.Action OnDeath;
    public System.Action OnRespawn;

    private void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Call this to damage the player. Called by the attacker (any client) when they hit this player.
    /// The RPC runs on all clients so health stays in sync.
    /// </summary>
    /// <param name="damage">Amount of damage to deal</param>
    /// <param name="attackerViewID">PhotonView ID of the attacker for kill credit</param>
    public void TakeDamage(int damage, int attackerViewID, int attackerActorNumber)
    {
        if (IsDead) return;

        photonView.RPC("RPC_TakeDamage", RpcTarget.All, damage, attackerViewID, attackerActorNumber);
    }

    [PunRPC]
    private void RPC_TakeDamage(int damage, int attackerViewID, int attackerActorNumber)
    {
        if (IsDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        Debug.Log($"Player {photonView.Owner.NickName} took {damage} damage. Health: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die(attackerViewID, attackerActorNumber);
            // Attacker also reports the kill so the master always receives it (victim's RPC can fail in some setups)
            if (PhotonNetwork.LocalPlayer.ActorNumber == attackerActorNumber)
            {
                PhotonView attackerView = PhotonView.Find(attackerViewID);
                if (attackerView != null)
                    attackerView.RPC("RPC_AttackerReportsKill", RpcTarget.MasterClient, photonView.ViewID, photonView.Owner.ActorNumber);
            }
        }
    }

    /// <summary>
    /// Handles player death.
    /// </summary>
    private void Die(int attackerViewID, int attackerActorNumber)
    {
        Debug.Log($"Player {photonView.Owner.NickName} has been eliminated!");

        OnDeath?.Invoke();

        if (photonView.IsMine)
        {
            int victimActor = PhotonNetwork.LocalPlayer.ActorNumber;
            photonView.RPC("RPC_ReportKillToMaster", RpcTarget.MasterClient, attackerViewID, photonView.ViewID, attackerActorNumber, victimActor);
        }

        // Disable player controls and hide model
        if (playerController != null)
        {
            playerController.SetInputEnabled(false);
        }

        if (playerModel != null)
        {
            playerModel.SetActive(false);
        }

        // Victim runs respawn locally after delay and sends RPC from their own view (no master dependency)
        if (photonView.IsMine)
        {
            Vector3 spawnPosition = transform.position;
            SpawnManager spawnManager = FindObjectOfType<SpawnManager>();
            if (spawnManager != null)
            {
                object teamObj;
                if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Team", out teamObj))
                {
                    Team team = (Team)teamObj;
                    spawnPosition = spawnManager.GetSpawnPoint(team);
                }
            }
            StartCoroutine(RespawnAfterDelayLocal(spawnPosition));
        }
    }

    private IEnumerator RespawnAfterDelayLocal(Vector3 spawnPosition)
    {
        int myViewID = photonView != null ? photonView.ViewID : -1;
        yield return new WaitForSecondsRealtime(respawnDelay);
        PhotonView pv = myViewID >= 0 ? PhotonView.Find(myViewID) : null;
        if (pv != null && pv.IsMine)
            pv.RPC("RPC_Respawn", RpcTarget.All, spawnPosition);
    }

    /// <summary>
    /// Runs on the master client only. Victim sends this from their own PhotonView so the RPC is delivered; master then applies the kill and broadcasts score.
    /// </summary>
    [PunRPC]
    private void RPC_ReportKillToMaster(int attackerViewID, int victimViewID, int attackerActorNumber, int victimActorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm == null) return;
        gm.ReportKill(attackerViewID, victimViewID, attackerActorNumber, victimActorNumber);
    }

    /// <summary>Runs on master. Attacker sends this from their view so the master always gets the kill (works when victim report fails).</summary>
    [PunRPC]
    private void RPC_AttackerReportsKill(int victimViewID, int victimActorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        int attackerViewID = photonView.ViewID;
        int attackerActorNumber = photonView.Owner != null ? photonView.Owner.ActorNumber : -1;
        if (attackerActorNumber < 0) return;
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm == null) return;
        gm.ReportKill(attackerViewID, victimViewID, attackerActorNumber, victimActorNumber);
    }

    /// <summary>Called by master so this client sets its own Kills (only we can set our CustomProperties).</summary>
    [PunRPC]
    private void RPC_SetKills(int value)
    {
        if (!photonView.IsMine) return;
        PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "Kills", value } });
    }

    /// <summary>Called by master so this client sets its own Deaths.</summary>
    [PunRPC]
    private void RPC_SetDeaths(int value)
    {
        if (!photonView.IsMine) return;
        PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "Deaths", value } });
    }

    [PunRPC]
    private void RPC_Respawn(Vector3 spawnPosition)
    {
        // Reset health
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Teleport to spawn position
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            transform.position = spawnPosition;
            cc.enabled = true;
        }
        else
        {
            transform.position = spawnPosition;
        }

        // Re-enable player
        if (playerController != null)
        {
            playerController.SetInputEnabled(true);
        }

        if (playerModel != null)
        {
            playerModel.SetActive(true);
        }

        OnRespawn?.Invoke();

        // Ensure death panel is hidden on this client (backup in case HUD subscription missed it)
        if (photonView.IsMine)
        {
            HUDController hud = FindObjectOfType<HUDController>();
            if (hud != null)
                hud.EnsureDeathPanelHidden();
        }

        Debug.Log($"Player {photonView.Owner.NickName} has respawned!");
    }

    /// <summary>
    /// Heal the player by a specified amount.
    /// </summary>
    public void Heal(int amount)
    {
        if (!photonView.IsMine || IsDead) return;

        photonView.RPC("RPC_Heal", RpcTarget.All, amount);
    }

    [PunRPC]
    private void RPC_Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
