using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Photon.Pun;

/// <summary>
/// Valorant-style in-match settings overlay (ESC). Does not pause the game — only blocks local input.
/// </summary>
public class SettingsOverlayController : MonoBehaviour
{
    public static SettingsOverlayController Instance { get; private set; }

    /// <summary>True while settings or leave modal is open — block weapon/fire input.</summary>
    public static bool BlocksGameplayInput =>
        Instance != null && (Instance.isOpen || Instance.isLeaveModalOpen);

    [Header("Built at runtime if null")]
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private GameObject leaveModal;

    private bool isOpen;
    private bool isLeaveModalOpen;

    private GameObject[] tabPanels;
    private Button[] tabButtons;
    private Image tabUnderline;
    private int activeTabIndex;

    private static readonly string[] TabNames =
    {
        "MATCH", "GENERAL", "CONTROLS", "CROSSHAIR", "VIDEO", "AUDIO", "VIEWING"
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        GameSettings.Load();

        if (overlayRoot == null)
        {
            var builder = GetComponent<SettingsOverlayUIBuilder>();
            if (builder == null)
                builder = gameObject.AddComponent<SettingsOverlayUIBuilder>();
            builder.Build(this);
        }

        HideLegacyPauseMenu();
    }

    private void Start()
    {
        if (overlayRoot != null)
            overlayRoot.SetActive(false);
        if (leaveModal != null)
            leaveModal.SetActive(false);

        GameSettings.Apply();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            HandleEscape();

        if (isOpen && activeTabIndex == 0)
            RefreshMatchTab();
    }

    private void HandleEscape()
    {
        if (IsGameOver())
            return;

        if (ScoreboardUI.IsScoreboardOpen)
            return;

        if (KeybindRebindController.Instance != null && KeybindRebindController.Instance.IsListening)
        {
            KeybindRebindController.Instance.CancelRebind();
            return;
        }

        if (isLeaveModalOpen)
        {
            CloseLeaveModal();
            return;
        }

        if (isOpen)
            CloseSettings();
        else
            OpenSettings();
    }

    public void OpenSettings()
    {
        if (IsGameOver()) return;

        isOpen = true;
        if (overlayRoot != null)
        {
            overlayRoot.SetActive(true);
            overlayRoot.transform.SetAsLastSibling();
        }

        CloseLeaveModal();
        SetMenuInputMode(true);
        SelectTab(1); // GENERAL default
    }

    public void CloseSettings()
    {
        isOpen = false;
        CloseLeaveModal();
        if (overlayRoot != null)
            overlayRoot.SetActive(false);
        SetMenuInputMode(false);
    }

    public void OpenLeaveModal()
    {
        if (!isOpen) return;
        isLeaveModalOpen = true;
        if (leaveModal != null)
            leaveModal.SetActive(true);
        ClearSelection();
    }

    public void CloseLeaveModal()
    {
        isLeaveModalOpen = false;
        if (leaveModal != null)
            leaveModal.SetActive(false);
    }

    public void LeaveMatch()
    {
        CloseSettings();
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.LeaveRoom();
    }

    public void SelectTab(int index)
    {
        if (tabPanels == null || tabButtons == null) return;

        activeTabIndex = Mathf.Clamp(index, 0, tabPanels.Length - 1);
        for (int i = 0; i < tabPanels.Length; i++)
        {
            if (tabPanels[i] != null)
                tabPanels[i].SetActive(i == activeTabIndex);
        }

        for (int i = 0; i < tabButtons.Length; i++)
        {
            if (tabButtons[i] == null) continue;
            var colors = tabButtons[i].colors;
            colors.normalColor = i == activeTabIndex
                ? new Color(1f, 1f, 1f, 1f)
                : new Color(0.55f, 0.55f, 0.55f, 1f);
            tabButtons[i].colors = colors;
        }

        if (tabUnderline != null && tabButtons.Length > activeTabIndex && tabButtons[activeTabIndex] != null)
        {
            RectTransform tabRect = tabButtons[activeTabIndex].GetComponent<RectTransform>();
            RectTransform underlineParent = tabUnderline.rectTransform.parent as RectTransform;
            if (tabRect != null && underlineParent != null)
            {
                Vector3[] corners = new Vector3[4];
                tabRect.GetWorldCorners(corners);
                Vector3 center = (corners[0] + corners[3]) * 0.5f;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    underlineParent, center, null, out Vector2 localPoint);
                tabUnderline.rectTransform.anchoredPosition = new Vector2(localPoint.x, tabUnderline.rectTransform.anchoredPosition.y);
                tabUnderline.rectTransform.sizeDelta = new Vector2(tabRect.sizeDelta.x + 8f, 3f);
                tabUnderline.enabled = true;
            }
        }

        if (activeTabIndex == 0)
            RefreshMatchTab();
    }

    public void RegisterUI(
        GameObject root,
        GameObject leaveModalRoot,
        GameObject[] panels,
        Button[] tabs,
        Image underline)
    {
        overlayRoot = root;
        leaveModal = leaveModalRoot;
        tabPanels = panels;
        tabButtons = tabs;
        tabUnderline = underline;
    }

    private void RefreshMatchTab()
    {
        if (tabPanels == null || tabPanels.Length == 0 || tabPanels[0] == null) return;
        Transform info = tabPanels[0].transform.Find("MatchInfoText");
        if (info == null) return;
        var tmp = info.GetComponent<TextMeshProUGUI>();
        if (tmp == null) return;

        string room = PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.Name : "—";
        int players = PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.PlayerCount : 0;
        string name = PhotonNetwork.NickName;
        tmp.text = $"ROOM: {room}\nPLAYERS: {players}\nYOU: {name}";
    }

    private void SetMenuInputMode(bool menuOpen)
    {
        Cursor.lockState = menuOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = menuOpen;

        if (menuOpen)
            ClearSelection();
    }

    private void ClearSelection()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private bool IsGameOver()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        return gm != null && gm.CurrentState == GameState.GameOver;
    }

    private void HideLegacyPauseMenu()
    {
        Transform legacy = transform.Find("PauseMenuPanel");
        if (legacy != null)
            legacy.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
