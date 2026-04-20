using System;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using ExitGames.Client.Photon;

/// <summary>
/// Manages game state, scoring, and match flow.
/// The master client controls the game timer and win conditions.
/// </summary>
public class GameManager : MonoBehaviourPunCallbacks
{
    [Header("Match Settings")]
    [SerializeField] private float matchDuration = 240f; // 4 minutes
    [SerializeField] private float preMatchCountdown = 5f;

    [Header("References")]
    [SerializeField] private SpawnManager spawnManager;
    [SerializeField] private HUDController hudController;

    // Game state
    private GameState currentState = GameState.WaitingForPlayers;
    private float matchTimer;
    private int redTeamScore = 0;
    private int blueTeamScore = 0;

    private int lastKillAttacker = -1, lastKillVictim = -1;
    private float lastKillTime = -999f;

    private System.Collections.Generic.Dictionary<int, int> playerKills = new System.Collections.Generic.Dictionary<int, int>();
    private System.Collections.Generic.Dictionary<int, int> playerDeaths = new System.Collections.Generic.Dictionary<int, int>();

    // Properties
    public GameState CurrentState => currentState;
    public float MatchTimer => matchTimer;
    public int RedTeamScore => redTeamScore;
    public int BlueTeamScore => blueTeamScore;

    // Events
    public System.Action<GameState> OnGameStateChanged;
    public System.Action<float> OnTimerUpdated;
    public System.Action<int, int> OnScoreUpdated; // red, blue
    public System.Action<Team> OnMatchEnded;
    public System.Action OnPlayerStatsUpdated;

    private void Start()
    {
        matchTimer = matchDuration;

        if (photonView == null)
        {
            Debug.LogError("GameManager: This GameObject must have a PhotonView component. Add one in the ParkPicnic scene so RPCs (timer, score, match end) work.");
        }

        // Find references if not set
        if (spawnManager == null)
        {
            spawnManager = FindObjectOfType<SpawnManager>();
        }

        if (hudController == null)
        {
            hudController = FindObjectOfType<HUDController>();
        }
    }

    private void Update()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            switch (currentState)
            {
                case GameState.WaitingForPlayers:
                    CheckReadyToStart();
                    break;
                case GameState.Countdown:
                    break;
                case GameState.Playing:
                    UpdateMatchTimer();
                    break;
                case GameState.GameOver:
                    break;
            }
        }

        // All clients: local countdown when playing so timer display always goes down
        if (currentState == GameState.Playing && matchTimer > 0f)
        {
            matchTimer -= Time.deltaTime;
            matchTimer = Mathf.Max(0f, matchTimer);
            OnTimerUpdated?.Invoke(matchTimer);
        }
    }

    /// <summary>
    /// Checks if enough players are ready to start the match.
    /// </summary>
    private void CheckReadyToStart()
    {
        // Start when we have at least 2 players (or 1 for testing)
        if (PhotonNetwork.CurrentRoom.PlayerCount >= 1)
        {
            StartCoroutine(StartCountdown());
        }
    }

    /// <summary>
    /// Starts the pre-match countdown.
    /// </summary>
    private IEnumerator StartCountdown()
    {
        SetGameState(GameState.Countdown);

        float countdown = preMatchCountdown;
        while (countdown > 0)
        {
            photonView.RPC("RPC_UpdateCountdown", RpcTarget.All, countdown);
            yield return new WaitForSeconds(1f);
            countdown--;
        }

        StartMatch();
    }

    /// <summary>
    /// Starts the match.
    /// </summary>
    private void StartMatch()
    {
        matchTimer = matchDuration;
        _lastSyncedTimerSecond = -1;
        redTeamScore = 0;
        blueTeamScore = 0;

        SetGameState(GameState.Playing);

        var startProps = new ExitGames.Client.Photon.Hashtable
        {
            { "MatchTimer", matchTimer },
            { "GameState", (int)GameState.Playing }
        };
        PhotonNetwork.CurrentRoom?.SetCustomProperties(startProps);
        if (photonView != null)
            photonView.RPC("RPC_StartMatch", RpcTarget.All, matchTimer);
    }

    /// <summary>
    /// Updates the match timer.
    /// </summary>
    private int _lastSyncedTimerSecond = -1;

    private void UpdateMatchTimer()
    {
        int currentSecond = Mathf.FloorToInt(matchTimer);
        if (currentSecond != _lastSyncedTimerSecond)
        {
            _lastSyncedTimerSecond = currentSecond;
            if (photonView != null)
                photonView.RPC("RPC_SyncTimer", RpcTarget.All, matchTimer);
            if (PhotonNetwork.CurrentRoom != null)
                PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "MatchTimer", matchTimer } });
        }

        if (matchTimer <= 0)
        {
            EndMatch();
        }
    }

    /// <summary>
    /// Ends the match and determines the winner.
    /// </summary>
    private void EndMatch()
    {
        matchTimer = 0;
        SetGameState(GameState.GameOver);

        Team winner = Team.None;
        if (redTeamScore > blueTeamScore)
        {
            winner = Team.Red;
        }
        else if (blueTeamScore > redTeamScore)
        {
            winner = Team.Blue;
        }
        // If equal, it's a draw (Team.None)

        photonView.RPC("RPC_EndMatch", RpcTarget.All, (int)winner);
    }

    /// <summary>
    /// Applies a kill (team score, Kills/Deaths) and broadcasts to all. Only call this on the master client.
    /// Uses attackerActorNumber for which team gets the point so scoring is correct (avoids view-ID mixups).
    /// </summary>
    public void ReportKill(int attackerViewID, int victimViewID, int attackerActorNumber, int victimActorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        ApplyKill(attackerViewID, victimViewID, attackerActorNumber, victimActorNumber);
    }

    private void ApplyKill(int attackerViewID, int victimViewID, int attackerActorNumber, int victimActorNumber)
    {
        if (PhotonNetwork.CurrentRoom == null) return;
        if (attackerActorNumber == lastKillAttacker && victimActorNumber == lastKillVictim && Time.time - lastKillTime < 3f)
            return;
        lastKillAttacker = attackerActorNumber;
        lastKillVictim = victimActorNumber;
        lastKillTime = Time.time;

        PhotonView attackerView = PhotonView.Find(attackerViewID);
        PhotonView victimView = PhotonView.Find(victimViewID);

        // Use ActorNumber to get attacker's team so the correct team gets the point (no view-ID mixup)
        Player attackerPlayer = null;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.ActorNumber == attackerActorNumber) { attackerPlayer = p; break; }
        }
        if (attackerPlayer == null) return;

        object teamObj;
        if (!attackerPlayer.CustomProperties.TryGetValue("Team", out teamObj)) return;
        Team attackerTeam = (Team)teamObj;

        if (attackerTeam == Team.Red)
            redTeamScore++;
        else if (attackerTeam == Team.Blue)
            blueTeamScore++;

        if (!playerKills.ContainsKey(attackerActorNumber)) playerKills[attackerActorNumber] = 0;
        if (!playerDeaths.ContainsKey(victimActorNumber)) playerDeaths[victimActorNumber] = 0;
        playerKills[attackerActorNumber] = playerKills[attackerActorNumber] + 1;
        playerDeaths[victimActorNumber] = playerDeaths[victimActorNumber] + 1;

        string serializedStats = SerializePlayerStats();
        PlayerStatsCache.ApplySerialized(serializedStats);

        // Use room properties so all clients get stats and kill notification without needing GameManager's PhotonView
        var props = new ExitGames.Client.Photon.Hashtable
        {
            { "RedScore", redTeamScore },
            { "BlueScore", blueTeamScore },
            { "PlayerStats", serializedStats },
            { "LastKillAttackerViewID", attackerViewID },
            { "LastKillVictimViewID", victimViewID }
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        OnScoreUpdated?.Invoke(redTeamScore, blueTeamScore);
        if (OnPlayerStatsUpdated != null) OnPlayerStatsUpdated.Invoke();

        // Also run kill feed/confirmation on master (room props are applied locally when we set them)
        NotifyKillFromIds(attackerViewID, victimViewID);
    }

    public int GetPlayerKills(int actorNumber)
    {
        int v;
        return playerKills != null && playerKills.TryGetValue(actorNumber, out v) ? v : 0;
    }

    public int GetPlayerDeaths(int actorNumber)
    {
        int v;
        return playerDeaths != null && playerDeaths.TryGetValue(actorNumber, out v) ? v : 0;
    }

    private string SerializePlayerStats()
    {
        var sb = new System.Text.StringBuilder();
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            int k = GetPlayerKills(p.ActorNumber);
            int d = GetPlayerDeaths(p.ActorNumber);
            if (sb.Length > 0) sb.Append('|');
            sb.Append(p.ActorNumber).Append(',').Append(k).Append(',').Append(d);
        }
        return sb.ToString();
    }

    private static int GetPlayerStat(Player player, string statName)
    {
        if (player == null) return 0;
        object value;
        return player.CustomProperties.TryGetValue(statName, out value) ? (int)value : 0;
    }

    /// <summary>
    /// Sets the game state and syncs it.
    /// </summary>
    private void SetGameState(GameState newState)
    {
        currentState = newState;
        photonView.RPC("RPC_SetGameState", RpcTarget.All, (int)newState);
    }

    #region RPCs

    [PunRPC]
    private void RPC_SetGameState(int state)
    {
        currentState = (GameState)state;
        OnGameStateChanged?.Invoke(currentState);
        Debug.Log($"Game state changed to: {currentState}");
    }

    [PunRPC]
    private void RPC_UpdateCountdown(float countdown)
    {
        Debug.Log($"Match starting in {countdown}...");
        // Update UI countdown
        if (hudController != null)
        {
            hudController.ShowCountdown(countdown);
        }
    }

    [PunRPC]
    private void RPC_StartMatch(float duration)
    {
        matchTimer = duration;
        redTeamScore = 0;
        blueTeamScore = 0;
        playerKills.Clear();
        playerDeaths.Clear();
        var parts = new List<string>();
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            playerKills[p.ActorNumber] = 0;
            playerDeaths[p.ActorNumber] = 0;
            parts.Add(p.ActorNumber + ",0,0");
        }
        PlayerStatsCache.ApplySerialized(string.Join("|", parts));
        var startProps = new ExitGames.Client.Photon.Hashtable
        {
            { "RedScore", 0 },
            { "BlueScore", 0 },
            { "PlayerStats", string.Join("|", parts) },
            { "LastKillAttackerViewID", -1 },
            { "LastKillVictimViewID", -1 },
            { "MatchTimer", matchTimer }
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(startProps);
        OnTimerUpdated?.Invoke(matchTimer);
        OnScoreUpdated?.Invoke(redTeamScore, blueTeamScore);
        if (OnPlayerStatsUpdated != null) OnPlayerStatsUpdated.Invoke();
        Debug.Log("Match started!");
    }

    [PunRPC]
    private void RPC_SyncPlayerStats(string serialized)
    {
        if (string.IsNullOrEmpty(serialized)) return;
        playerKills.Clear();
        playerDeaths.Clear();
        foreach (string part in serialized.Split('|'))
        {
            string[] t = part.Split(',');
            if (t.Length >= 3 && int.TryParse(t[0], out int actor) && int.TryParse(t[1], out int k) && int.TryParse(t[2], out int d))
            {
                playerKills[actor] = k;
                playerDeaths[actor] = d;
            }
        }
        PlayerStatsCache.ApplySerialized(serialized);
        if (OnPlayerStatsUpdated != null) OnPlayerStatsUpdated.Invoke();
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey("RedScore") || propertiesThatChanged.ContainsKey("BlueScore"))
        {
            object r, b;
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("RedScore", out r))
                redTeamScore = (int)r;
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("BlueScore", out b))
                blueTeamScore = (int)b;
            OnScoreUpdated?.Invoke(redTeamScore, blueTeamScore);
        }

        if (propertiesThatChanged.ContainsKey("PlayerStats"))
        {
            object raw;
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("PlayerStats", out raw) && raw != null)
            {
                string serialized = raw as string ?? raw.ToString();
                if (!string.IsNullOrEmpty(serialized))
                {
                    PlayerStatsCache.ApplySerialized(serialized);
                    if (OnPlayerStatsUpdated != null) OnPlayerStatsUpdated.Invoke();
                }
            }
        }

        if (propertiesThatChanged.ContainsKey("LastKillAttackerViewID") && propertiesThatChanged.ContainsKey("LastKillVictimViewID"))
        {
            object aId, vId;
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("LastKillAttackerViewID", out aId) &&
                PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("LastKillVictimViewID", out vId) &&
                aId is int attackerViewID && vId is int victimViewID)
            {
                NotifyKillFromIds(attackerViewID, victimViewID);
            }
        }

        if (propertiesThatChanged.ContainsKey("MatchTimer"))
        {
            object t;
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("MatchTimer", out t) && t != null)
            {
                float time = t is int ? (int)t : (t is float ? (float)t : Convert.ToSingle(t));
                matchTimer = Mathf.Max(0f, time);
                OnTimerUpdated?.Invoke(matchTimer);
            }
        }

        if (propertiesThatChanged.ContainsKey("GameState"))
        {
            object g;
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("GameState", out g) && g is int state)
                currentState = (GameState)state;
        }
    }

    /// <summary>Updates kill feed and shows "Eliminated X" for the killer. Call on all clients (from room props or master after ApplyKill).</summary>
    private void NotifyKillFromIds(int attackerViewID, int victimViewID)
    {
        if (attackerViewID < 0 || victimViewID < 0) return;
        PhotonView attackerView = PhotonView.Find(attackerViewID);
        PhotonView victimView = PhotonView.Find(victimViewID);
        string attackerName = attackerView?.Owner?.NickName ?? "Unknown";
        string victimName = victimView?.Owner?.NickName ?? "Unknown";

        if (hudController == null) hudController = FindObjectOfType<HUDController>();
        if (hudController != null)
        {
            hudController.AddKillFeedEntry(attackerName, victimName);
            if (attackerView != null && attackerView.IsMine)
                hudController.ShowKillConfirmation(victimName);
        }
    }

    [PunRPC]
    private void RPC_SyncTimer(float time)
    {
        matchTimer = time;
        OnTimerUpdated?.Invoke(matchTimer);
    }

    [PunRPC]
    private void RPC_UpdateScore(int red, int blue)
    {
        redTeamScore = red;
        blueTeamScore = blue;
        OnScoreUpdated?.Invoke(redTeamScore, blueTeamScore);
        Debug.Log($"Score - Red: {red} | Blue: {blue}");
    }

    [PunRPC]
    private void RPC_EndMatch(int winner)
    {
        Team winningTeam = (Team)winner;
        OnMatchEnded?.Invoke(winningTeam);

        string winnerText = winningTeam == Team.None ? "It's a draw!" :
                           winningTeam == Team.Red ? "Red Team Wins!" : "Blue Team Wins!";

        Debug.Log($"Match ended! {winnerText}");

        if (hudController != null)
        {
            hudController.ShowGameOver(winningTeam);
        }
    }

    [PunRPC]
    private void RPC_NotifyKill(int attackerViewID, int victimViewID)
    {
        // Get player names for kill feed
        PhotonView attackerView = PhotonView.Find(attackerViewID);
        PhotonView victimView = PhotonView.Find(victimViewID);

        string attackerName = attackerView?.Owner?.NickName ?? "Unknown";
        string victimName = victimView?.Owner?.NickName ?? "Unknown";

        Debug.Log($"KILL: {attackerName} eliminated {victimName}");

        if (hudController == null) hudController = FindObjectOfType<HUDController>();
        if (hudController != null)
        {
            hudController.AddKillFeedEntry(attackerName, victimName);
            if (attackerView != null && attackerView.IsMine)
                hudController.ShowKillConfirmation(victimName);
        }
    }

    #endregion
}

/// <summary>
/// Represents the current state of the game.
/// </summary>
public enum GameState
{
    WaitingForPlayers,
    Countdown,
    Playing,
    GameOver
}
