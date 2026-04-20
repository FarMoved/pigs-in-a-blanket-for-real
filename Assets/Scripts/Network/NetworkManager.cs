using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// Handles Photon networking - connecting, creating/joining rooms, and spawning players.
/// </summary>
public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("Player Settings")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private string playerPrefabName = "PigPlayer"; // Name in Resources folder

    [Header("Room Settings")]
    [SerializeField] private byte maxPlayersPerRoom = 8;
    [SerializeField] private string gameVersion = "1.0";

    [Header("Scenes")]
    [SerializeField] private string lobbySceneName = "lobby"; // Must match scene filename (lobby.unity)
    [SerializeField] private string gameSceneName = "ParkPicnic";

    // Singleton
    public static NetworkManager Instance { get; private set; }

    // State
    private bool isConnecting = false;

    // Events (renamed to avoid conflict with Photon callbacks)
    public System.Action OnConnectedEvent;
    public System.Action OnLobbyJoinedEvent;
    public System.Action OnRoomJoinedEvent;
    public System.Action<string> OnRoomJoinFailedEvent;
    public System.Action<Player> OnPlayerJoinedEvent;
    public System.Action<Player> OnPlayerLeftEvent;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Photon settings
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    /// <summary>
    /// Connects to Photon servers.
    /// </summary>
    public void Connect()
    {
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("Already connected to Photon");
            return;
        }

        isConnecting = true;
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();

        Debug.Log("Connecting to Photon...");
    }

    /// <summary>
    /// Sets the player's display name.
    /// </summary>
    public void SetPlayerName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            name = "Pig_" + Random.Range(1000, 9999);
        }

        PhotonNetwork.NickName = name;
        PlayerPrefs.SetString("PlayerName", name);

        Debug.Log($"Player name set to: {name}");
    }

    /// <summary>
    /// Creates a new room.
    /// </summary>
    public void CreateRoom(string roomName)
    {
        if (string.IsNullOrEmpty(roomName))
        {
            roomName = "Room " + Random.Range(1000, 9999);
        }

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = maxPlayersPerRoom,
            IsVisible = true,
            IsOpen = true
        };

        PhotonNetwork.CreateRoom(roomName, options);

        Debug.Log($"Creating room: {roomName}");
    }

    /// <summary>
    /// Joins an existing room by name.
    /// </summary>
    public void JoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
        Debug.Log($"Joining room: {roomName}");
    }

    /// <summary>
    /// Joins a random available room or creates one if none exist.
    /// </summary>
    public void JoinRandomRoom()
    {
        PhotonNetwork.JoinRandomRoom();
        Debug.Log("Joining random room...");
    }

    /// <summary>
    /// Leaves the current room.
    /// </summary>
    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        Debug.Log("Leaving room...");
    }

    /// <summary>
    /// Spawns the local player in the game.
    /// Should be called after joining a room and loading the game scene.
    /// </summary>
    public void SpawnLocalPlayer()
    {
        if (playerPrefab == null && string.IsNullOrEmpty(playerPrefabName))
        {
            Debug.LogError("Player prefab not set!");
            return;
        }

        // Get team and spawn point
        TeamManager teamManager = FindObjectOfType<TeamManager>();
        if (teamManager != null)
        {
            teamManager.AssignLocalPlayerToTeam();
        }

        SpawnManager spawnManager = FindObjectOfType<SpawnManager>();
        Vector3 spawnPosition = Vector3.zero;
        Quaternion spawnRotation = Quaternion.identity;

        if (spawnManager != null)
        {
            Team team = TeamManager.GetLocalPlayerTeam();
            spawnPosition = spawnManager.GetSpawnPoint(team);

            // Face center of map
            Vector3 lookDirection = -spawnPosition;
            lookDirection.y = 0;
            if (lookDirection != Vector3.zero)
            {
                spawnRotation = Quaternion.LookRotation(lookDirection);
            }
        }

        // Spawn player via Photon
        string prefabName = playerPrefab != null ? playerPrefab.name : playerPrefabName;
        GameObject player = PhotonNetwork.Instantiate(prefabName, spawnPosition, spawnRotation);

        Debug.Log($"Spawned local player at {spawnPosition}");

        // Setup HUD for local player
        HUDController hud = FindObjectOfType<HUDController>();
        if (hud != null)
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            WeaponManager weapons = player.GetComponent<WeaponManager>();
            hud.SetupForLocalPlayer(health, weapons);
        }
    }

    /// <summary>
    /// Loads the game scene (master client only in synced mode).
    /// </summary>
    public void LoadGameScene()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(gameSceneName);
        }
    }

    #region Photon Callbacks

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Master Server");

        if (isConnecting)
        {
            OnConnectedEvent?.Invoke();
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Photon Lobby");
        OnLobbyJoinedEvent?.Invoke();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"Joined room: {PhotonNetwork.CurrentRoom.Name}");
        Debug.Log($"Players in room: {PhotonNetwork.CurrentRoom.PlayerCount}");

        OnRoomJoinedEvent?.Invoke();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Room creation failed: {message}");
        OnRoomJoinFailedEvent?.Invoke(message);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Join room failed: {message}");
        OnRoomJoinFailedEvent?.Invoke(message);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("No random room available, creating one...");
        CreateRoom(null);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"Player joined: {newPlayer.NickName}");
        OnPlayerJoinedEvent?.Invoke(newPlayer);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"Player left: {otherPlayer.NickName}");
        OnPlayerLeftEvent?.Invoke(otherPlayer);
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Left room");
        // Reset time scale and cursor so the lobby works (game may have been paused)
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        // Disconnect so the lobby shows the connection/name screen instead of skipping to lobby panel
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
        SceneManager.LoadScene(lobbySceneName);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"Disconnected from Photon: {cause}");
        isConnecting = false;
    }

    #endregion
}
