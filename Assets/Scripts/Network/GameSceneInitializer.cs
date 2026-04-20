using UnityEngine;
using Photon.Pun;

/// <summary>
/// Initializes the game scene when loaded.
/// Spawns the local player and sets up necessary components.
/// </summary>
public class GameSceneInitializer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float spawnDelay = 1f;

    private void Start()
    {
        // Only spawn if we're in a Photon room
        if (PhotonNetwork.InRoom)
        {
            Invoke(nameof(SpawnPlayer), spawnDelay);
        }
        else
        {
            Debug.LogWarning("Not in a Photon room! Cannot spawn player.");
        }
    }

    private void SpawnPlayer()
    {
        NetworkManager networkManager = NetworkManager.Instance;
        if (networkManager != null)
        {
            networkManager.SpawnLocalPlayer();
        }
        else
        {
            Debug.LogError("NetworkManager not found!");
        }
    }
}
