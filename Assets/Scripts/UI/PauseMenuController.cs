using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Photon.Pun;

/// <summary>
/// Controls the pause menu (ESC). Toggles time scale, cursor, and optional player input.
/// Back to Lobby calls NetworkManager.LeaveRoom(); Settings opens a placeholder panel.
/// Does not re-enable input when resuming if the local player is dead (avoids "ghost" state).
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button backToLobbyButton;
    [SerializeField] private Button settingsButton;

    private bool isPaused;
    private PlayerController localPlayerController;

    private void Start()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(Resume);
        }

        if (backToLobbyButton != null)
        {
            backToLobbyButton.onClick.AddListener(BackToLobby);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OpenSettings);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettings();
            }
            else
            {
                TogglePause();
            }
        }
    }

    /// <summary>
    /// Toggles pause state.
    /// </summary>
    public void TogglePause()
    {
        isPaused = !isPaused;
        SetPaused(isPaused);
    }

    /// <summary>
    /// Resumes the game (closes pause panel).
    /// </summary>
    public void Resume()
    {
        isPaused = false;
        SetPaused(false);
    }

    /// <summary>
    /// Leaves the room and returns to lobby (NetworkManager loads lobby scene in OnLeftRoom).
    /// Unpauses first so the leave completes correctly and the first click works.
    /// </summary>
    public void BackToLobby()
    {
        // Close pause menu and restore time scale first so Photon/Unity can process the leave
        SetPaused(false);
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.LeaveRoom();
        }
    }

    /// <summary>
    /// Opens the settings panel (placeholder).
    /// </summary>
    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Closes the settings panel.
    /// </summary>
    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    private void SetPaused(bool paused)
    {
        Time.timeScale = paused ? 0f : 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(paused);
        }

        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = paused;

        // Clear selection when opening pause menu so first click registers on the button
        if (paused && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        // Disable/enable local player input when paused (never re-enable if player is dead)
        if (localPlayerController == null)
        {
            localPlayerController = FindObjectOfType<PlayerController>();
        }

        if (localPlayerController != null)
        {
            bool shouldEnableInput = !paused;
            if (shouldEnableInput)
            {
                // Don't re-enable input when dead (e.g. after clicking Resume on death screen)
                PlayerHealth localHealth = GetLocalPlayerHealth();
                if (localHealth != null && localHealth.IsDead)
                {
                    shouldEnableInput = false;
                }
            }
            localPlayerController.SetInputEnabled(shouldEnableInput);
        }

        // When opening pause menu, disable Resume button if dead so it's clear they must wait to respawn
        if (resumeButton != null && paused)
        {
            PlayerHealth localHealth = GetLocalPlayerHealth();
            resumeButton.interactable = localHealth == null || !localHealth.IsDead;
        }
    }

    private PlayerHealth GetLocalPlayerHealth()
    {
        PhotonView[] views = FindObjectsOfType<PhotonView>();
        foreach (PhotonView pv in views)
        {
            if (pv.IsMine)
            {
                PlayerHealth h = pv.GetComponent<PlayerHealth>();
                if (h != null) return h;
            }
        }
        return null;
    }
}
