/// <summary>
/// Tracks whether this editor joined the current Photon room through the lobby this Play session.
/// Prevents ParrelSync clones (or stale clients) from spawning in matches they did not join.
/// </summary>
public static class MatchSessionTracker
{
    private static bool joinedRoomThroughLobbyThisSession;
    private static bool allowEditorSoloMapTest;

    public static bool JoinedRoomThroughLobbyThisSession => joinedRoomThroughLobbyThisSession;

    public static void MarkJoinedRoomThroughLobby()
    {
        joinedRoomThroughLobbyThisSession = true;
        allowEditorSoloMapTest = false;
    }

    public static void Clear()
    {
        joinedRoomThroughLobbyThisSession = false;
        allowEditorSoloMapTest = false;
    }

    public static void AllowEditorSoloMapTest()
    {
#if UNITY_EDITOR
        allowEditorSoloMapTest = true;
#endif
    }

    public static bool CanPlayInGameScene()
    {
        if (joinedRoomThroughLobbyThisSession)
            return true;

#if UNITY_EDITOR
        return allowEditorSoloMapTest && !ParrelSyncUtil.IsClone();
#else
        return false;
#endif
    }
}
