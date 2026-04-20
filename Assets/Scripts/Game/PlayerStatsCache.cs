using System.Collections.Generic;

/// <summary>
/// Single source of truth for Kills/Deaths. Updated by GameManager when RPC_SyncPlayerStats is received.
/// Scoreboard reads from here so it never depends on GameManager instance or room/player properties.
/// </summary>
public static class PlayerStatsCache
{
    private static readonly Dictionary<int, int> Kills = new Dictionary<int, int>();
    private static readonly Dictionary<int, int> Deaths = new Dictionary<int, int>();

    public static void ApplySerialized(string serialized)
    {
        if (string.IsNullOrEmpty(serialized)) return;
        Kills.Clear();
        Deaths.Clear();
        if (serialized.IndexOf('|') < 0 && serialized.IndexOf(' ') >= 0)
            serialized = serialized.Replace(" ", "|");
        foreach (string part in serialized.Split('|'))
        {
            string[] t = part.Split(',');
            if (t.Length >= 3 && int.TryParse(t[0], out int actor) && int.TryParse(t[1], out int k) && int.TryParse(t[2], out int d))
            {
                Kills[actor] = k;
                Deaths[actor] = d;
            }
        }
    }

    public static int GetKills(int actorNumber)
    {
        int v;
        return Kills.TryGetValue(actorNumber, out v) ? v : 0;
    }

    public static int GetDeaths(int actorNumber)
    {
        int v;
        return Deaths.TryGetValue(actorNumber, out v) ? v : 0;
    }
}
