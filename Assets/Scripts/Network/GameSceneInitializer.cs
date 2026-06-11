using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

/// <summary>
/// Initializes the game scene when loaded.
/// Spawns the local player after Photon (and NetworkManager) are ready.
/// </summary>
public class GameSceneInitializer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float spawnDelay = 1f;
    [SerializeField] private string gameSceneName = "ParkPicnic";
    [SerializeField] private float networkManagerWaitTimeout = 8f;

    private void Awake()
    {
        if (GetComponent<GameSceneGuard>() == null)
            gameObject.AddComponent<GameSceneGuard>();
    }

    private void Start()
    {
        StartCoroutine(Initialize());
    }

    private IEnumerator Initialize()
    {
        yield return new WaitForSeconds(spawnDelay);

        if (!MatchSessionTracker.CanPlayInGameScene())
        {
#if UNITY_EDITOR
            if (ParrelSyncUtil.IsClone())
            {
                Debug.LogWarning(
                    "ParrelSync clone is on the game map without joining the host's room. " +
                    "Connect in the lobby and join the room code before the host starts.");
                yield break;
            }

            if (SceneManager.GetActiveScene().name == gameSceneName)
            {
                Debug.LogWarning(
                    "ParkPicnic: Not in a Photon room. Starting an offline editor session for solo map testing. " +
                    "For multiplayer, start from the lobby scene instead.");
                yield return StartEditorOfflineSession();
                yield return WaitForNetworkManagerAndSpawn();
            }
#endif
            yield break;
        }

        if (PhotonNetwork.InRoom)
            yield return WaitForNetworkManagerAndSpawn();
        else
            Debug.LogWarning("Not in a Photon room! Cannot spawn player.");
    }

    private IEnumerator WaitForNetworkManagerAndSpawn()
    {
        float elapsed = 0f;
        while (elapsed < networkManagerWaitTimeout)
        {
            NetworkManager manager = EnsureNetworkManager();
            if (manager != null && PhotonNetwork.InRoom && MatchSessionTracker.CanPlayInGameScene())
            {
                manager.TrySpawnLocalPlayerIfNeeded();
                yield break;
            }

            elapsed += 0.25f;
            yield return new WaitForSeconds(0.25f);
        }

        Debug.LogError("Timed out waiting to spawn the local player. Check the console for NetworkManager or Photon errors.");
    }

    private static NetworkManager EnsureNetworkManager()
    {
        if (NetworkManager.Instance != null)
            return NetworkManager.Instance;

        NetworkManager existing = FindObjectOfType<NetworkManager>();
        if (existing != null)
            return existing;

#if UNITY_EDITOR
        GameObject go = new GameObject("NetworkManager");
        return go.AddComponent<NetworkManager>();
#else
        return null;
#endif
    }

#if UNITY_EDITOR
    private static IEnumerator StartEditorOfflineSession()
    {
        if (PhotonNetwork.InRoom)
            yield break;

        MatchSessionTracker.AllowEditorSoloMapTest();

        if (string.IsNullOrEmpty(PhotonNetwork.NickName))
            PhotonNetwork.NickName = "Editor_" + Random.Range(1000, 9999);

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.OfflineMode = true;
            PhotonNetwork.ConnectUsingSettings();
            while (!PhotonNetwork.IsConnectedAndReady)
                yield return null;
        }

        if (!PhotonNetwork.InRoom)
        {
            string roomName = "EditorOffline_" + Application.dataPath.GetHashCode();
            PhotonNetwork.CreateRoom(roomName);
            while (!PhotonNetwork.InRoom)
                yield return null;
        }
    }
#endif
}
