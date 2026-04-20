using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

/// <summary>
/// Manages the lobby UI for creating/joining rooms.
/// </summary>
public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("Connection Panel")]
    [SerializeField] private GameObject connectionPanel;
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private Button connectButton;
    [SerializeField] private TextMeshProUGUI connectionStatusText;

    [Header("Lobby Panel")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button joinRandomButton;
    [SerializeField] private Button joinRoomButton;
    [SerializeField] private Button backToConnectionButton;
    [SerializeField] private Transform roomListContainer;
    [SerializeField] private GameObject roomEntryPrefab;

    [Header("Join By Code Panel")]
    [SerializeField] private GameObject joinByCodePanel;
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private Button joinByCodeSubmitButton;
    [SerializeField] private Button joinByCodeBackButton;
    [SerializeField] private TextMeshProUGUI joinByCodeStatusText;

    [Header("Room Panel")]
    [SerializeField] private GameObject roomPanel;
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private TextMeshProUGUI roomCodeDisplayText;
    [SerializeField] private TextMeshProUGUI roomPlayerCountText;
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject playerEntryPrefab;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveRoomButton;

    // Room list tracking
    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();
    private Dictionary<string, GameObject> roomEntries = new Dictionary<string, GameObject>();
    private Dictionary<int, GameObject> playerEntries = new Dictionary<int, GameObject>();

    private void Start()
    {
        // Ensure clean state when lobby loads (e.g. returning from game)
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Show connection panel initially
        ShowPanel(connectionPanel);

        // Load saved player name
        string savedName = PlayerPrefs.GetString("PlayerName", "");
        if (playerNameInput != null && !string.IsNullOrEmpty(savedName))
        {
            playerNameInput.text = savedName;
        }

        // Setup button listeners
        if (connectButton != null)
        {
            connectButton.onClick.AddListener(OnConnectClicked);
        }
        if (createRoomButton != null)
        {
            createRoomButton.onClick.AddListener(OnCreateRoomClicked);
        }
        if (joinRandomButton != null)
        {
            joinRandomButton.onClick.AddListener(OnJoinRandomClicked);
        }
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }
        if (leaveRoomButton != null)
        {
            leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);
        }
        if (joinRoomButton != null)
        {
            joinRoomButton.onClick.AddListener(OnJoinRoomButtonClicked);
        }
        if (backToConnectionButton != null)
        {
            backToConnectionButton.onClick.AddListener(OnBackToConnectionClicked);
        }
        if (joinByCodeSubmitButton != null)
        {
            joinByCodeSubmitButton.onClick.AddListener(OnJoinByCodeSubmitClicked);
        }
        if (joinByCodeBackButton != null)
        {
            joinByCodeBackButton.onClick.AddListener(OnJoinByCodeBackClicked);
        }

        UpdateConnectionStatus("Not connected");
    }

    private void ShowPanel(GameObject panel)
    {
        if (connectionPanel != null) connectionPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (joinByCodePanel != null) joinByCodePanel.SetActive(false);
        if (roomPanel != null) roomPanel.SetActive(false);

        if (panel != null) panel.SetActive(true);
    }

    private void UpdateConnectionStatus(string status)
    {
        if (connectionStatusText != null)
        {
            connectionStatusText.text = status;
        }
    }

    #region Button Handlers

    private void OnConnectClicked()
    {
        string playerName = playerNameInput != null ? playerNameInput.text : "";

        if (string.IsNullOrEmpty(playerName))
        {
            playerName = "Pig_" + Random.Range(1000, 9999);
        }

        NetworkManager.Instance?.SetPlayerName(playerName);
        NetworkManager.Instance?.Connect();

        UpdateConnectionStatus("Connecting...");
        if (connectButton != null) connectButton.interactable = false;
    }

    private void OnCreateRoomClicked()
    {
        // Create room with a random 5-digit code (friends join using this code)
        int code = Random.Range(10000, 100000);
        string roomName = code.ToString();
        NetworkManager.Instance?.CreateRoom(roomName);
    }

    private void OnJoinRandomClicked()
    {
        NetworkManager.Instance?.JoinRandomRoom();
    }

    private void OnJoinRoomButtonClicked()
    {
        if (joinByCodePanel == null) return;
        if (roomCodeInput != null) roomCodeInput.text = "";
        if (joinByCodeStatusText != null) joinByCodeStatusText.text = "";
        ShowPanel(joinByCodePanel);
    }

    private void OnBackToConnectionClicked()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
        ShowPanel(connectionPanel);
        UpdateConnectionStatus("Disconnected. Enter name and click Connect to play.");
        if (connectButton != null) connectButton.interactable = true;
    }

    private void OnJoinByCodeSubmitClicked()
    {
        string raw = roomCodeInput != null ? roomCodeInput.text.Trim() : "";
        // Allow only digits (strip any letters or other characters)
        string code = string.IsNullOrEmpty(raw) ? "" : new string(System.Array.FindAll(raw.ToCharArray(), char.IsDigit));
        if (roomCodeInput != null && code != raw)
            roomCodeInput.text = code;
        if (string.IsNullOrEmpty(code))
        {
            if (joinByCodeStatusText != null) joinByCodeStatusText.text = "Enter a 5-digit room code.";
            return;
        }
        if (code.Length != 5 || !int.TryParse(code, out _))
        {
            if (joinByCodeStatusText != null) joinByCodeStatusText.text = "Room code must be 5 numbers.";
            return;
        }
        if (joinByCodeStatusText != null) joinByCodeStatusText.text = "Joining...";
        NetworkManager.Instance?.JoinRoom(code);
    }

    private void OnJoinByCodeBackClicked()
    {
        ShowPanel(lobbyPanel);
    }

    private void OnStartGameClicked()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            NetworkManager.Instance?.LoadGameScene();
        }
    }

    private void OnLeaveRoomClicked()
    {
        NetworkManager.Instance?.LeaveRoom();
    }

    private void OnJoinRoomClicked(string roomName)
    {
        NetworkManager.Instance?.JoinRoom(roomName);
    }

    #endregion

    #region Room List Management

    private void UpdateRoomList()
    {
        // Clear old entries
        foreach (var entry in roomEntries.Values)
        {
            Destroy(entry);
        }
        roomEntries.Clear();

        // Create new entries
        foreach (var roomInfo in cachedRoomList.Values)
        {
            if (!roomInfo.IsOpen || !roomInfo.IsVisible || roomInfo.RemovedFromList)
            {
                continue;
            }

            CreateRoomEntry(roomInfo);
        }
    }

    private void CreateRoomEntry(RoomInfo roomInfo)
    {
        if (roomEntryPrefab == null || roomListContainer == null) return;

        GameObject entry = Instantiate(roomEntryPrefab, roomListContainer);
        roomEntries[roomInfo.Name] = entry;

        // Set room info
        TextMeshProUGUI nameText = entry.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.text = $"{roomInfo.Name} ({roomInfo.PlayerCount}/{roomInfo.MaxPlayers})";
        }

        // Add join button handler
        Button joinButton = entry.GetComponentInChildren<Button>();
        if (joinButton != null)
        {
            string roomName = roomInfo.Name;
            joinButton.onClick.AddListener(() => OnJoinRoomClicked(roomName));
        }
    }

    #endregion

    #region Player List Management

    private void UpdatePlayerList()
    {
        // Clear old entries
        foreach (var entry in playerEntries.Values)
        {
            Destroy(entry);
        }
        playerEntries.Clear();

        // Create new entries
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            CreatePlayerEntry(player);
        }

        UpdateRoomPlayerCount();

        // Update start button (only master client can start)
        if (startGameButton != null)
        {
            startGameButton.interactable = PhotonNetwork.IsMasterClient;
        }
    }

    private void UpdateRoomPlayerCount()
    {
        if (roomPlayerCountText == null || !PhotonNetwork.InRoom) return;

        int current = PhotonNetwork.CurrentRoom.PlayerCount;
        int max = PhotonNetwork.CurrentRoom.MaxPlayers;
        roomPlayerCountText.text = $"Players: {current} / {max}";
    }

    private void CreatePlayerEntry(Player player)
    {
        if (playerEntryPrefab == null || playerListContainer == null) return;

        GameObject entry = Instantiate(playerEntryPrefab, playerListContainer);
        playerEntries[player.ActorNumber] = entry;

        TextMeshProUGUI nameText = entry.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText != null)
        {
            string suffix = "";
            if (player.IsLocal) suffix += " (You)";
            if (player.IsMasterClient) suffix += " [Host]";

            nameText.text = player.NickName + suffix;
        }
    }

    #endregion

    #region Photon Callbacks

    public override void OnConnectedToMaster()
    {
        UpdateConnectionStatus("Connected to server");
    }

    public override void OnJoinedLobby()
    {
        UpdateConnectionStatus("In lobby");
        ShowPanel(lobbyPanel);

        if (connectButton != null) connectButton.interactable = true;
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomInfo info in roomList)
        {
            if (info.RemovedFromList)
            {
                cachedRoomList.Remove(info.Name);
            }
            else
            {
                cachedRoomList[info.Name] = info;
            }
        }

        UpdateRoomList();
    }

    public override void OnJoinedRoom()
    {
        ShowPanel(roomPanel);

        if (roomNameText != null)
        {
            roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        }
        if (roomCodeDisplayText != null)
        {
            roomCodeDisplayText.text = "Room Code: " + PhotonNetwork.CurrentRoom.Name;
        }

        UpdateRoomPlayerCount();
        UpdatePlayerList();
    }

    public override void OnLeftRoom()
    {
        ShowPanel(lobbyPanel);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerList();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        UpdatePlayerList();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        UpdateConnectionStatus($"Failed: {message}");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        UpdateConnectionStatus($"Failed: {message}");
        if (joinByCodeStatusText != null && joinByCodePanel != null && joinByCodePanel.activeSelf)
        {
            joinByCodeStatusText.text = "Room not found. Check the code and try again.";
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        ShowPanel(connectionPanel);
        UpdateConnectionStatus("Disconnected. Click Connect to play again.");

        if (connectButton != null) connectButton.interactable = true;
    }

    #endregion
}
