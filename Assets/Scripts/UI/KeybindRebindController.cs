using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Listens for the next key press while rebinding a control in the settings overlay.
/// </summary>
public class KeybindRebindController : MonoBehaviour
{
    public static KeybindRebindController Instance { get; private set; }

    private class RowUI
    {
        public KeybindId Id;
        public TextMeshProUGUI KeyLabel;
        public Image ButtonBackground;
    }

    private static readonly Color NormalButtonColor = new Color(0.22f, 0.22f, 0.24f, 1f);
    private static readonly Color ConflictButtonColor = new Color(0.42f, 0.14f, 0.14f, 1f);
    private static readonly Color ListeningButtonColor = new Color(0.18f, 0.35f, 0.32f, 1f);

    public bool IsListening => listening;

    private bool listening;
    private KeybindId activeId;
    private TextMeshProUGUI activeLabel;
    private Image activeButtonBackground;
    private readonly Dictionary<KeybindId, RowUI> rows = new Dictionary<KeybindId, RowUI>();
    private TextMeshProUGUI warningLabel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RegisterWarningLabel(TextMeshProUGUI label)
    {
        warningLabel = label;
    }

    public void RegisterRow(KeybindId id, TextMeshProUGUI keyLabel, Image buttonBackground)
    {
        rows[id] = new RowUI
        {
            Id = id,
            KeyLabel = keyLabel,
            ButtonBackground = buttonBackground
        };
    }

    public void RefreshConflictState()
    {
        HashSet<KeybindId> conflicts = GameKeybinds.GetConflictingBindings();

        foreach (KeyValuePair<KeybindId, RowUI> pair in rows)
        {
            RowUI row = pair.Value;
            if (row.KeyLabel != null && !(listening && row.Id == activeId))
                row.KeyLabel.text = GameKeybinds.FormatKey(GameKeybinds.Get(row.Id));

            if (row.ButtonBackground == null)
                continue;

            bool isConflict = conflicts.Contains(row.Id);
            bool isListeningRow = listening && row.Id == activeId;
            row.ButtonBackground.color = isListeningRow
                ? ListeningButtonColor
                : isConflict
                    ? ConflictButtonColor
                    : NormalButtonColor;
        }

        if (warningLabel == null)
            return;

        if (conflicts.Count == 0)
        {
            warningLabel.gameObject.SetActive(false);
            return;
        }

        warningLabel.gameObject.SetActive(true);
        warningLabel.text = "Warning: Duplicate keybinds detected. Highlighted keys are shared by multiple actions.";
    }

    public void BeginRebind(KeybindId id, TextMeshProUGUI label)
    {
        if (label == null)
            return;

        if (listening)
            CancelRebind();

        activeId = id;
        activeLabel = label;
        if (rows.TryGetValue(id, out RowUI row))
            activeButtonBackground = row.ButtonBackground;

        listening = true;
        activeLabel.text = "";
        RefreshConflictState();
    }

    private void Update()
    {
        if (!listening)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelRebind();
            return;
        }

        foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (key == KeyCode.None || key == KeyCode.Escape)
                continue;

            if (!Input.GetKeyDown(key))
                continue;

            if (GameKeybinds.TryGetOtherBindingForKey(key, activeId, out KeybindId otherId))
            {
                ShowTransientWarning(
                    $"{GameKeybinds.FormatKey(key)} is already bound to {GameKeybinds.GetBindingLabel(otherId)}.");
                return;
            }

            GameKeybinds.Set(activeId, key);
            if (activeLabel != null)
                activeLabel.text = GameKeybinds.FormatKey(key);

            listening = false;
            activeLabel = null;
            activeButtonBackground = null;
            RefreshConflictState();
            return;
        }
    }

    public void CancelRebind()
    {
        if (!listening)
            return;

        if (activeLabel != null)
            activeLabel.text = GameKeybinds.FormatKey(GameKeybinds.Get(activeId));

        listening = false;
        activeLabel = null;
        activeButtonBackground = null;
        RefreshConflictState();
    }

    private void ShowTransientWarning(string message)
    {
        if (warningLabel == null)
            return;

        warningLabel.gameObject.SetActive(true);
        warningLabel.text = message;
    }
}
