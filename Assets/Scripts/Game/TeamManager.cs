using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages team assignment and team-related functionality.
/// </summary>
public class TeamManager : MonoBehaviourPunCallbacks
{
    [Header("Team Settings")]
    [SerializeField] private Color redTeamColor = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private Color blueTeamColor = new Color(0.2f, 0.4f, 0.9f);

    [Header("Team Materials (Optional)")]
    [SerializeField] private Material redTeamMaterial;
    [SerializeField] private Material blueTeamMaterial;

    // Track team counts
    private int redTeamCount = 0;
    private int blueTeamCount = 0;

    // Events
    public System.Action<Team> OnLocalPlayerTeamAssigned;

    public Color RedTeamColor => redTeamColor;
    public Color BlueTeamColor => blueTeamColor;

    private void Start()
    {
        // Count existing players on each team
        CountTeams();
    }

    /// <summary>
    /// Assigns the local player to a team. Alternates by join order (ActorNumber) so
    /// first joiner = Red, second = Blue, third = Red, etc. Deterministic so no race when
    /// multiple clients load the game scene at once.
    /// </summary>
    public void AssignLocalPlayerToTeam()
    {
        // Assign by position in room: even index = Red, odd = Blue (consistent for all clients)
        Player[] sorted = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();
        int myIndex = -1;
        for (int i = 0; i < sorted.Length; i++)
        {
            if (sorted[i].ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                myIndex = i;
                break;
            }
        }
        Team assignedTeam = (myIndex >= 0 && myIndex % 2 == 0) ? Team.Red : Team.Blue;

        Hashtable props = new Hashtable
        {
            { "Team", assignedTeam }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        OnLocalPlayerTeamAssigned?.Invoke(assignedTeam);

        Debug.Log($"Assigned to {assignedTeam} team (join order {myIndex + 1})");
    }

    /// <summary>
    /// Gets a player's team.
    /// </summary>
    public static Team GetPlayerTeam(Player player)
    {
        if (player == null) return Team.None;

        object teamObj;
        if (player.CustomProperties.TryGetValue("Team", out teamObj))
        {
            return (Team)teamObj;
        }

        return Team.None;
    }

    /// <summary>
    /// Gets the local player's team.
    /// </summary>
    public static Team GetLocalPlayerTeam()
    {
        return GetPlayerTeam(PhotonNetwork.LocalPlayer);
    }

    /// <summary>
    /// Counts players on each team.
    /// </summary>
    private void CountTeams()
    {
        redTeamCount = 0;
        blueTeamCount = 0;

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            Team team = GetPlayerTeam(player);
            if (team == Team.Red)
            {
                redTeamCount++;
            }
            else if (team == Team.Blue)
            {
                blueTeamCount++;
            }
        }
    }

    /// <summary>
    /// Gets all players on a specific team.
    /// </summary>
    public List<Player> GetTeamPlayers(Team team)
    {
        List<Player> teamPlayers = new List<Player>();

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (GetPlayerTeam(player) == team)
            {
                teamPlayers.Add(player);
            }
        }

        return teamPlayers;
    }

    /// <summary>
    /// Gets the team color for a player.
    /// </summary>
    public Color GetTeamColor(Team team)
    {
        switch (team)
        {
            case Team.Red:
                return redTeamColor;
            case Team.Blue:
                return blueTeamColor;
            default:
                return Color.white;
        }
    }

    /// <summary>
    /// Gets the team material for a player.
    /// </summary>
    public Material GetTeamMaterial(Team team)
    {
        switch (team)
        {
            case Team.Red:
                return redTeamMaterial;
            case Team.Blue:
                return blueTeamMaterial;
            default:
                return null;
        }
    }

    /// <summary>
    /// Checks if two players are on the same team.
    /// </summary>
    public static bool AreOnSameTeam(Player player1, Player player2)
    {
        return GetPlayerTeam(player1) == GetPlayerTeam(player2);
    }

    /// <summary>
    /// Checks if two players are enemies.
    /// </summary>
    public static bool AreEnemies(Player player1, Player player2)
    {
        Team team1 = GetPlayerTeam(player1);
        Team team2 = GetPlayerTeam(player2);

        // Neither can be unassigned
        if (team1 == Team.None || team2 == Team.None)
        {
            return false;
        }

        return team1 != team2;
    }

    #region Photon Callbacks

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // Recount teams when player properties change
        if (changedProps.ContainsKey("Team"))
        {
            CountTeams();
            Debug.Log($"Player {targetPlayer.NickName} team updated. Red: {redTeamCount}, Blue: {blueTeamCount}");
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        CountTeams();
    }

    #endregion
}

/// <summary>
/// Team identifiers.
/// </summary>
public enum Team
{
    None = 0,
    Red = 1,
    Blue = 2
}
