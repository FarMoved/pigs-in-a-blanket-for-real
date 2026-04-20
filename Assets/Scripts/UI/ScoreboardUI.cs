using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

/// <summary>
/// Modern competitive scoreboard: two horizontal team panels (blue top, red bottom),
/// columns Name / Score / Kills / Deaths / Assists, team score display, futuristic styling.
/// </summary>
public class ScoreboardUI : MonoBehaviourPunCallbacks
{
    [Header("Scoreboard Panel")]
    [SerializeField] private GameObject scoreboardPanel;
    [SerializeField] private GameObject scoreboardBackdrop;
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    [Header("Team Panels (blue top, red bottom)")]
    [SerializeField] private Transform blueTeamContainer;
    [SerializeField] private Transform redTeamContainer;

    [Header("Single list fallback")]
    [SerializeField] private Transform playerListContainer;

    [Header("Player Entry")]
    [SerializeField] private GameObject playerEntryPrefab;

    [Header("Score display")]
    [SerializeField] private TextMeshProUGUI blueTeamScoreHeader;
    [SerializeField] private TextMeshProUGUI redTeamScoreHeader;
    [SerializeField] private TextMeshProUGUI matchTimeText;
    [SerializeField] private TextMeshProUGUI gameModeText;

    private Dictionary<int, GameObject> playerEntries = new Dictionary<int, GameObject>();

    // Column layout: Name + KDA only (no Score; Kills/Deaths/Assists replaced by single KDA column)
    private static readonly (float min, float max)[] ColumnAnchors = { (0f, 0.55f), (0.55f, 1f) };
    private static readonly string[] ColumnLabels = { "Name", "KDA" };
    private bool headerColumnsInitialized;
    private float lastScoreboardRefreshTime;
    private const float ScoreboardRefreshInterval = 0.15f;

    private GameManager _cachedGameManager;

    private void Start()
    {
        if (scoreboardPanel != null) scoreboardPanel.SetActive(false);
        if (scoreboardBackdrop != null) scoreboardBackdrop.SetActive(false);
        EnsureHeaderColumnsAligned();
        SubscribeToStats();
    }

    private void OnEnable()
    {
        SubscribeToStats();
    }

    private void OnDestroy()
    {
        if (_cachedGameManager != null)
        {
            _cachedGameManager.OnPlayerStatsUpdated -= OnGameManagerStatsUpdated;
            _cachedGameManager = null;
        }
    }

    private void SubscribeToStats()
    {
        if (_cachedGameManager != null) return;
        _cachedGameManager = FindObjectOfType<GameManager>();
        if (_cachedGameManager != null)
            _cachedGameManager.OnPlayerStatsUpdated += OnGameManagerStatsUpdated;
    }

    private void OnGameManagerStatsUpdated()
    {
        if (scoreboardPanel == null) return;
        RefreshScoreboard();
    }

    /// <summary>
    /// Replaces the single header text with one label per column so headers align with player rows.
    /// </summary>
    private void EnsureHeaderColumnsAligned()
    {
        if (headerColumnsInitialized || scoreboardPanel == null) return;

        Transform leftHeader = scoreboardPanel.transform.Find("ColumnHeader");
        Transform rightHeader = scoreboardPanel.transform.Find("ColumnHeaderRight");
        if (leftHeader == null || rightHeader == null) return;

        SetupHeaderLabels(leftHeader);
        SetupHeaderLabels(rightHeader);
        headerColumnsInitialized = true;
    }

    private void SetupHeaderLabels(Transform headerRoot)
    {
        TextMeshProUGUI existingTMP = headerRoot.GetComponent<TextMeshProUGUI>();
        if (existingTMP != null)
        {
            existingTMP.text = "";
            existingTMP.enabled = false;
        }

        Color color = existingTMP != null ? existingTMP.color : new Color(0.7f, 0.75f, 0.85f, 1f);
        TMP_FontAsset font = existingTMP != null ? existingTMP.font : null;
        float fontSize = existingTMP != null ? existingTMP.fontSize : 16f;

        for (int i = 0; i < ColumnLabels.Length; i++)
        {
            var (min, max) = ColumnAnchors[i];
            GameObject go = new GameObject("Col_" + ColumnLabels[i]);
            go.transform.SetParent(headerRoot, false);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(min, 0f);
            rect.anchorMax = new Vector2(max, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = ColumnLabels[i];
            tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = i == 0 ? TextAlignmentOptions.Left : TextAlignmentOptions.Center;
            // Extra left margin on "Name" so it sits a bit to the right on both sides
            float leftMargin = i == 0 ? 18 : 5;
            tmp.margin = new Vector4(leftMargin, 2, 5, 2);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ShowScoreboard();
        }
        else if (Input.GetKeyUp(toggleKey))
        {
            HideScoreboard();
        }

        if (scoreboardPanel != null && scoreboardPanel.activeSelf)
        {
            GameManager gm = FindObjectOfType<GameManager>();
            if (matchTimeText != null && gm != null && gm.CurrentState == GameState.Playing)
            {
                float remaining = gm.MatchTimer;
                int m = Mathf.FloorToInt(remaining / 60f);
                int s = Mathf.FloorToInt(remaining % 60f);
                matchTimeText.text = $"Match Time - {m}:{s:D2}";
            }
            if (gameModeText != null)
                gameModeText.text = "Kill Confirmed\nIn Progress";
            if (Time.unscaledTime - lastScoreboardRefreshTime >= ScoreboardRefreshInterval)
            {
                lastScoreboardRefreshTime = Time.unscaledTime;
                RefreshScoreboard();
            }
        }
    }

    public void ShowScoreboard()
    {
        if (scoreboardBackdrop != null) scoreboardBackdrop.SetActive(true);
        if (scoreboardPanel != null)
        {
            scoreboardPanel.SetActive(true);
            lastScoreboardRefreshTime = -999f;
            RefreshScoreboard();
            CancelInvoke(nameof(RefreshScoreboard));
            Invoke(nameof(RefreshScoreboard), 0.1f);
            Invoke(nameof(RefreshScoreboard), 0.35f);
        }
    }

    public void HideScoreboard()
    {
        if (scoreboardBackdrop != null) scoreboardBackdrop.SetActive(false);
        if (scoreboardPanel != null) scoreboardPanel.SetActive(false);
    }

    public void RefreshScoreboard()
    {
        ClearEntries();
        GameManager gm = FindObjectOfType<GameManager>();

        List<Player> blue = new List<Player>();
        List<Player> red = new List<Player>();
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (TeamManager.GetPlayerTeam(p) == Team.Blue)
                blue.Add(p);
            else if (TeamManager.GetPlayerTeam(p) == Team.Red)
                red.Add(p);
            else
                blue.Add(p);
        }

        // Build display names so duplicate nicknames (e.g. ParrelSync same name) get " (2)", " (3)" etc.
        Dictionary<string, List<Player>> nameToPlayers = new Dictionary<string, List<Player>>();
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            string name = string.IsNullOrEmpty(p.NickName) ? "Player" : p.NickName;
            if (!nameToPlayers.ContainsKey(name))
                nameToPlayers[name] = new List<Player>();
            nameToPlayers[name].Add(p);
        }
        foreach (var list in nameToPlayers.Values)
            list.Sort((a, b) => a.ActorNumber.CompareTo(b.ActorNumber));

        // Two horizontal panels: blue top, red bottom
        if (blueTeamContainer != null && redTeamContainer != null)
        {
            int idx = 0;
            foreach (Player player in blue)
            {
                AddPlayerEntryTo(player, blueTeamContainer, true, idx % 2 == 1, nameToPlayers, gm);
                idx++;
            }
            idx = 0;
            foreach (Player player in red)
            {
                AddPlayerEntryTo(player, redTeamContainer, false, idx % 2 == 1, nameToPlayers, gm);
                idx++;
            }
        }
        else if (playerListContainer != null)
        {
            int index = 0;
            foreach (Player player in blue)
            {
                AddPlayerEntryTo(player, playerListContainer, true, index % 2 == 1, nameToPlayers, gm);
                index++;
            }
            index = 0;
            foreach (Player player in red)
            {
                AddPlayerEntryTo(player, playerListContainer, false, index % 2 == 1, nameToPlayers, gm);
                index++;
            }
        }
        else
        {
            foreach (Player player in PhotonNetwork.PlayerList)
                AddPlayerEntry(player, nameToPlayers, gm);
        }

        UpdateTeamScores();

        if (scoreboardPanel != null)
        {
            UnityEngine.Canvas.ForceUpdateCanvases();
            var rect = scoreboardPanel.GetComponent<RectTransform>();
            if (rect != null)
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }
    }

    private void AddPlayerEntryTo(Player player, Transform container, bool isBlueTeam, bool alternateRow,
        Dictionary<string, List<Player>> nameToPlayers, GameManager gm)
    {
        if (playerEntryPrefab == null || container == null) return;

        GameObject entry = Instantiate(playerEntryPrefab, container);
        playerEntries[player.ActorNumber] = entry;
        HideOldStatColumns(entry);

        string displayName = GetScoreboardDisplayName(player, nameToPlayers);
        int kills = GetPlayerStat(player, "Kills", gm);
        int deaths = GetPlayerStat(player, "Deaths", gm);
        int assists = GetPlayerStat(player, "Assists", gm);

        PlayerScoreEntry entryScript = entry.GetComponent<PlayerScoreEntry>();
        if (entryScript != null)
        {
            entryScript.SetData(displayName, 0, kills, deaths, assists, player.IsLocal);
            entryScript.SetRowStyle(alternateRow, isBlueTeam, player.IsLocal);
        }
        else
        {
            TextMeshProUGUI text = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = displayName + (player.IsLocal ? " (You)" : "");
        }

        AddRowKdaLabel(entry, kills, deaths, assists);
    }

    private void HideOldStatColumns(GameObject row)
    {
        if (row == null) return;
        foreach (string name in new[] { "Score", "Kills", "Deaths", "Assists" })
        {
            Transform t = row.transform.Find(name);
            if (t != null) t.gameObject.SetActive(false);
        }
        Transform nameCol = row.transform.Find("PlayerName");
        if (nameCol != null)
        {
            var rect = nameCol.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.offsetMax = Vector2.zero;
            }
        }
    }

    private void AddRowKdaLabel(GameObject row, int kills, int deaths, int assists)
    {
        if (row == null) return;
        var go = new GameObject("RowKda");
        go.transform.SetParent(row.transform, false);
        go.transform.SetAsLastSibling();
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(8f, 2f);
        rect.offsetMax = new Vector2(-8f, -2f);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = $"K: {kills}      D: {deaths}      A: {assists}";
        tmp.fontSize = 18;
        tmp.color = new Color(1f, 1f, 1f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;
    }

    /// <summary>
    /// Returns a display name for the scoreboard. If multiple players share the same nickname (e.g. ParrelSync),
    /// appends " (2)", " (3)" etc. so each row is clearly a different player.
    /// </summary>
    private static string GetScoreboardDisplayName(Player player, Dictionary<string, List<Player>> nameToPlayers)
    {
        string name = string.IsNullOrEmpty(player.NickName) ? "Player " + player.ActorNumber : player.NickName;
        if (nameToPlayers == null || !nameToPlayers.ContainsKey(name))
            return name;
        List<Player> list = nameToPlayers[name];
        if (list.Count <= 1)
            return name;
        int index = list.FindIndex(p => p.ActorNumber == player.ActorNumber);
        if (index <= 0)
            return name;
        return name + " (" + (index + 1) + ")";
    }

    private void AddPlayerEntry(Player player, Dictionary<string, List<Player>> nameToPlayers = null, GameManager gm = null)
    {
        if (playerEntryPrefab == null) return;

        Team team = TeamManager.GetPlayerTeam(player);
        Transform container = team == Team.Red ? redTeamContainer : blueTeamContainer;
        if (container == null) container = playerListContainer;
        if (container == null && scoreboardPanel != null) container = scoreboardPanel.transform;
        if (container == null) return;

        GameObject entry = Instantiate(playerEntryPrefab, container);
        playerEntries[player.ActorNumber] = entry;
        HideOldStatColumns(entry);

        string displayName = GetScoreboardDisplayName(player, nameToPlayers);
        int kills = GetPlayerStat(player, "Kills", gm);
        int deaths = GetPlayerStat(player, "Deaths", gm);
        int assists = GetPlayerStat(player, "Assists", gm);
        PlayerScoreEntry entryScript = entry.GetComponent<PlayerScoreEntry>();
        if (entryScript != null)
            entryScript.SetData(displayName, 0, kills, deaths, assists, player.IsLocal);
        else
        {
            TextMeshProUGUI text = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = displayName + (player.IsLocal ? " (You)" : "");
        }
        AddRowKdaLabel(entry, kills, deaths, assists);
    }

    private int GetPlayerStat(Player player, string statName, GameManager gm = null)
    {
        if (player == null) return 0;
        if (statName == "Kills") return PlayerStatsCache.GetKills(player.ActorNumber);
        if (statName == "Deaths") return PlayerStatsCache.GetDeaths(player.ActorNumber);
        if (player.CustomProperties.TryGetValue(statName, out object value) && value != null)
        {
            if (value is int) return (int)value;
            if (value is long) return (int)(long)value;
            if (value is byte) return (int)(byte)value;
            try { return Convert.ToInt32(value); } catch { return 0; }
        }
        return 0;
    }

    private void UpdateTeamScores()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm == null) return;
        if (blueTeamScoreHeader != null) blueTeamScoreHeader.text = gm.BlueTeamScore.ToString();
        if (redTeamScoreHeader != null) redTeamScoreHeader.text = gm.RedTeamScore.ToString();
    }

    private void ClearEntries()
    {
        foreach (var entry in playerEntries.Values)
        {
            if (entry != null)
            {
                Destroy(entry);
            }
        }
        playerEntries.Clear();
    }

    public void UpdatePlayerEntry(Player player)
    {
        if (playerEntries.ContainsKey(player.ActorNumber))
        {
            Destroy(playerEntries[player.ActorNumber]);
            playerEntries.Remove(player.ActorNumber);
            RefreshScoreboard();
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (scoreboardPanel == null || !scoreboardPanel.activeSelf) return;
        if (changedProps == null || changedProps.Count == 0) return;
        if (changedProps.ContainsKey("Kills") || changedProps.ContainsKey("Deaths") || changedProps.ContainsKey("Score") || changedProps.ContainsKey("Assists"))
            RefreshScoreboard();
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        if (scoreboardPanel == null || !scoreboardPanel.activeSelf || propertiesThatChanged == null) return;
        foreach (var key in propertiesThatChanged.Keys)
        {
            string k = key != null ? key.ToString() : "";
            if (k.StartsWith("Kills_") || k.StartsWith("Deaths_"))
            {
                RefreshScoreboard();
                break;
            }
        }
    }
}

/// <summary>
/// Component for individual player score entries.
/// Attach to the player entry prefab.
/// </summary>
public class PlayerScoreEntry : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI killsText;
    [SerializeField] private TextMeshProUGUI deathsText;
    [SerializeField] private TextMeshProUGUI assistsText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color localPlayerColor = new Color(1f, 1f, 1f, 0.25f);

    // Futuristic blue/red team row colors (high contrast, subtle)
    private static readonly Color BlueDark  = new Color(0.08f, 0.18f, 0.32f, 0.92f);
    private static readonly Color BlueLight = new Color(0.12f, 0.28f, 0.48f, 0.92f);
    private static readonly Color RedDark   = new Color(0.32f, 0.10f, 0.12f, 0.92f);
    private static readonly Color RedLight  = new Color(0.48f, 0.18f, 0.20f, 0.92f);

    public void SetData(string playerName, int score, int kills, int deaths, int assists, bool isLocal)
    {
        string name = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
        string nameSuffix = isLocal ? " (You)" : "";
        string nameLine = name + nameSuffix;

        SetTextByChildName("PlayerName", nameLine);
        SetTextByChildName("Score", "");
        SetTextByChildName("Kills", "");
        SetTextByChildName("Deaths", "");
        SetTextByChildName("Assists", "");

        if (playerNameText != null)
        {
            playerNameText.text = nameLine;
            playerNameText.ForceMeshUpdate(true, true);
        }
        if (scoreText != null) { scoreText.text = ""; scoreText.gameObject.SetActive(false); }
        if (killsText != null) { killsText.text = ""; killsText.gameObject.SetActive(false); }
        if (deathsText != null) { deathsText.text = ""; deathsText.gameObject.SetActive(false); }
        if (assistsText != null) { assistsText.text = ""; assistsText.gameObject.SetActive(false); }
    }

    private static bool _loggedScoreboardFindOnce;
    private void SetTextByChildName(string childName, string text)
    {
        Transform t = transform.Find(childName);
        if (t == null)
        {
            if (!_loggedScoreboardFindOnce && (childName == "Kills" || childName == "Deaths"))
            {
                _loggedScoreboardFindOnce = true;
                Debug.LogWarning($"[Scoreboard] PlayerScoreEntry could not Find child \"{childName}\". Direct children: " + string.Join(", ", GetDirectChildNames()));
            }
            return;
        }
        t.gameObject.SetActive(true);
        var tmp = t.GetComponent<TMPro.TextMeshProUGUI>();
        if (tmp == null) return;
        tmp.text = text;
        tmp.ForceMeshUpdate(true, true);
        tmp.color = new Color(1f, 1f, 1f, 1f);
        t.SetAsLastSibling();
    }
    private System.Collections.Generic.List<string> GetDirectChildNames()
    {
        var list = new System.Collections.Generic.List<string>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform c = transform.GetChild(i);
            list.Add(c != null ? c.name : "?");
        }
        return list;
    }

    private void TrySetChildText(int index, string text)
    {
        var texts = GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
        if (index >= 0 && index < texts.Length && texts[index] != null)
            texts[index].text = text;
    }

    private void TrySetCombinedLineIfNeeded(string displayName, int score, int kills, int deaths, int assists)
    {
        bool hasDedicated = (playerNameText != null && scoreText != null && killsText != null && deathsText != null);
        if (hasDedicated) return;
        var texts = GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
        if (texts == null || texts.Length == 0) return;
        string line = $"{displayName}  |  Score: {score}  K: {kills}  D: {deaths}  A: {assists}";
        texts[0].text = line;
    }

    public void SetRowStyle(bool alternateRow, bool isBlueTeam, bool isLocal = false)
    {
        if (backgroundImage == null) return;
        backgroundImage.color = isBlueTeam
            ? (alternateRow ? BlueLight : BlueDark)
            : (alternateRow ? RedLight : RedDark);
        if (isLocal)
            backgroundImage.color = Color.Lerp(backgroundImage.color, localPlayerColor, 0.5f);
    }
}
