using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

/// <summary>
/// Keeps the game scene limited to clients that intentionally joined the current match.
/// </summary>
public class GameSceneGuard : MonoBehaviour
{
    [SerializeField] private string lobbySceneName = "lobby";
    [SerializeField] private float validationDelay = 0.5f;

    private void Start()
    {
        if (SceneManager.GetActiveScene().name != "ParkPicnic")
            return;

        StartCoroutine(ValidateSession());
    }

    private IEnumerator ValidateSession()
    {
        yield return new WaitForSeconds(validationDelay);

        if (SceneManager.GetActiveScene().name != "ParkPicnic")
            yield break;

        if (!PhotonNetwork.InRoom)
        {
            ReturnToLobby("Not in a Photon room.");
            yield break;
        }

        if (!MatchSessionTracker.CanPlayInGameScene())
        {
            Debug.LogWarning(
                "GameSceneGuard: This editor is on the game map but did not join through the lobby. " +
                "Returning to lobby. (ParrelSync clones must join the host room manually.)");
            if (PhotonNetwork.InRoom)
                PhotonNetwork.LeaveRoom();
            else
                ReturnToLobby("Not part of this match session.");
        }
    }

    private void ReturnToLobby(string reason)
    {
        Debug.LogWarning("GameSceneGuard: " + reason);
        SceneManager.LoadScene(lobbySceneName);
    }
}
