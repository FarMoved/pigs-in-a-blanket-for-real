using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Builds the Valorant-style settings overlay UI at runtime under the Canvas.
/// </summary>
public class SettingsOverlayUIBuilder : MonoBehaviour
{
    private static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.84f);
    private static readonly Color ContentPanelColor = new Color(0.06f, 0.06f, 0.07f, 0.92f);
    private static readonly Color PanelColor = new Color(0.12f, 0.12f, 0.12f, 0.95f);
    private static readonly Color AccentTeal = new Color(0.18f, 0.83f, 0.66f, 1f);
    private static readonly Color ModalButtonColor = new Color(0.35f, 0.35f, 0.38f, 1f);
    private static readonly Color CloseButtonColor = new Color(0.92f, 0.92f, 0.92f, 1f);
    private static readonly Color DisabledSettingLabelColor = new Color(0.42f, 0.42f, 0.42f, 0.85f);
    private static readonly Color DisabledSwatchTint = new Color(0.32f, 0.32f, 0.32f, 0.55f);

    private class OutlineColorSwatchRow
    {
        public TextMeshProUGUI Label;
        public Button[] Buttons;
        public Image[] ColorImages;
        public Image[] SelectionFrames;
        public System.Func<bool> IsEnabled;
    }

    private class AudioCategoryRowUI
    {
        public TextMeshProUGUI Label;
        public Toggle Toggle;
        public Slider Slider;
        public TMP_InputField NumericInput;
        public Image SliderFill;
        public Image SliderHandle;
    }

    public void Build(SettingsOverlayController controller)
    {
        Transform canvasTransform = transform;
        RectTransform canvasRect = canvasTransform as RectTransform;
        if (canvasRect == null)
            canvasRect = canvasTransform.GetComponent<RectTransform>();

        GameObject overlayRoot = CreateUIObject("SettingsOverlay", canvasTransform);
        StretchFull(overlayRoot.GetComponent<RectTransform>());
        Image backdrop = overlayRoot.AddComponent<Image>();
        backdrop.color = BackdropColor;
        backdrop.raycastTarget = true;
        overlayRoot.AddComponent<KeybindRebindController>();

        Canvas parentCanvas = canvasTransform.GetComponent<Canvas>();
        if (parentCanvas != null)
        {
            Canvas overlayCanvas = overlayRoot.AddComponent<Canvas>();
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = parentCanvas.sortingOrder + 50;
            overlayRoot.AddComponent<GraphicRaycaster>();
        }

        // Top bar
        GameObject topBar = CreateUIObject("TopBar", overlayRoot.transform);
        RectTransform topBarRect = topBar.GetComponent<RectTransform>();
        topBarRect.anchorMin = new Vector2(0f, 1f);
        topBarRect.anchorMax = new Vector2(1f, 1f);
        topBarRect.pivot = new Vector2(0.5f, 1f);
        topBarRect.sizeDelta = new Vector2(0f, 88f);
        topBarRect.anchoredPosition = Vector2.zero;
        Image topBarBg = topBar.AddComponent<Image>();
        topBarBg.color = new Color(0.06f, 0.06f, 0.06f, 0.94f);
        topBarBg.raycastTarget = false;

        Button backButton = CreateTextButton(topBar.transform, "BackButton", "BACK  //",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(24f, -20f), new Vector2(160f, 36f), 18, TextAlignmentOptions.Center);
        backButton.onClick.AddListener(controller.CloseSettings);

        // Top right leave
        Button leaveButton = CreateTextButton(topBar.transform, "LeaveButton", "LEAVE",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-24f, -20f), new Vector2(100f, 36f), 16, TextAlignmentOptions.Center);
        leaveButton.onClick.AddListener(controller.OpenLeaveModal);

        // Tab bar
        GameObject tabBar = CreateUIObject("TabBar", topBar.transform);
        RectTransform tabBarRect = tabBar.GetComponent<RectTransform>();
        tabBarRect.anchorMin = new Vector2(0.15f, 0f);
        tabBarRect.anchorMax = new Vector2(0.85f, 0.55f);
        tabBarRect.offsetMin = Vector2.zero;
        tabBarRect.offsetMax = Vector2.zero;

        HorizontalLayoutGroup tabLayout = tabBar.AddComponent<HorizontalLayoutGroup>();
        tabLayout.childAlignment = TextAnchor.MiddleCenter;
        tabLayout.spacing = 18f;
        tabLayout.childControlWidth = false;
        tabLayout.childControlHeight = true;
        tabLayout.childForceExpandWidth = false;
        tabLayout.childForceExpandHeight = true;

        string[] tabNames = { "MATCH", "GENERAL", "CONTROLS", "CROSSHAIR", "VIDEO", "AUDIO", "VIEWING" };
        Button[] tabButtons = new Button[tabNames.Length];
        for (int i = 0; i < tabNames.Length; i++)
        {
            int tabIndex = i;
            tabButtons[i] = CreateTextButton(tabBar.transform, "Tab_" + tabNames[i], tabNames[i],
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(90f, 32f), 14, TextAlignmentOptions.Center);
            tabButtons[i].onClick.AddListener(() => controller.SelectTab(tabIndex));
        }

        GameObject underlineGo = CreateUIObject("TabUnderline", topBar.transform);
        Image underline = underlineGo.AddComponent<Image>();
        underline.color = AccentTeal;
        RectTransform underlineRect = underlineGo.GetComponent<RectTransform>();
        underlineRect.anchorMin = new Vector2(0.5f, 0f);
        underlineRect.anchorMax = new Vector2(0.5f, 0f);
        underlineRect.pivot = new Vector2(0.5f, 0.5f);
        underlineRect.sizeDelta = new Vector2(80f, 3f);
        underlineRect.anchoredPosition = new Vector2(0f, -2f);
        underline.raycastTarget = false;

        // Content area — full-width dark panel between top bar and close button
        GameObject contentArea = CreateUIObject("ContentArea", overlayRoot.transform);
        RectTransform contentRect = contentArea.GetComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = new Vector2(0f, -88f);
        Image contentBg = contentArea.AddComponent<Image>();
        contentBg.color = ContentPanelColor;
        contentBg.raycastTarget = false;

        GameObject[] tabPanels = new GameObject[tabNames.Length];
        tabPanels[0] = CreateMatchPanel(contentArea.transform);
        tabPanels[1] = CreateGeneralPanel(contentArea.transform);
        tabPanels[2] = CreateControlsPanel(contentArea.transform);
        tabPanels[3] = CreateCrosshairPanel(contentArea.transform);
        tabPanels[4] = CreateVideoPanel(contentArea.transform);
        tabPanels[5] = CreateAudioPanel(contentArea.transform);
        for (int i = 6; i < tabNames.Length; i++)
            tabPanels[i] = CreateComingSoonPanel(contentArea.transform, tabNames[i]);

        // Close settings button
        Button closeButton = CreateTextButton(overlayRoot.transform, "CloseSettingsButton", "CLOSE SETTINGS",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 48f), new Vector2(420f, 52f), 20, TextAlignmentOptions.Center);
        closeButton.GetComponent<Image>().color = CloseButtonColor;
        TextMeshProUGUI closeLabel = closeButton.GetComponentInChildren<TextMeshProUGUI>();
        if (closeLabel != null)
            closeLabel.color = Color.black;
        closeButton.onClick.AddListener(controller.CloseSettings);

        // Leave modal
        GameObject leaveModal = CreateLeaveModal(overlayRoot.transform, controller);

        controller.RegisterUI(overlayRoot, leaveModal, tabPanels, tabButtons, underline);
        overlayRoot.transform.SetAsLastSibling();
        overlayRoot.SetActive(false);
        leaveModal.SetActive(false);
    }

    private GameObject CreateLeaveModal(Transform parent, SettingsOverlayController controller)
    {
        GameObject modalRoot = CreateUIObject("LeaveModal", parent);
        StretchFull(modalRoot.GetComponent<RectTransform>());
        Image modalScrim = modalRoot.AddComponent<Image>();
        modalScrim.color = new Color(0f, 0f, 0f, 0.45f);
        modalScrim.raycastTarget = true;

        GameObject card = CreateUIObject("ModalCard", modalRoot.transform);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(320f, 160f);
        cardRect.anchoredPosition = Vector2.zero;
        Image cardBg = card.AddComponent<Image>();
        cardBg.color = PanelColor;
        Outline outline = card.AddComponent<Outline>();
        outline.effectColor = new Color(0.75f, 0.75f, 0.75f, 0.6f);
        outline.effectDistance = new Vector2(1f, -1f);

        Button closeX = CreateTextButton(card.transform, "ModalCloseX", "X",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-16f, -16f), new Vector2(32f, 32f), 20, TextAlignmentOptions.Center);
        closeX.onClick.AddListener(controller.CloseLeaveModal);

        Button leaveMatch = CreateTextButton(card.transform, "LeaveMatchButton", "LEAVE MATCH",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -10f), new Vector2(260f, 48f), 18, TextAlignmentOptions.Center);
        leaveMatch.GetComponent<Image>().color = ModalButtonColor;
        leaveMatch.onClick.AddListener(controller.LeaveMatch);

        return modalRoot;
    }

    private GameObject CreateMatchPanel(Transform parent)
    {
        GameObject panel = CreateContentPanel(parent, "TabPanel_Match");
        CreateLabel(panel.transform, "MatchInfoText", "",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -24f), new Vector2(-24f, -120f), 22);
        return panel;
    }

    private GameObject CreateGeneralPanel(Transform parent)
    {
        GameObject panel = CreateContentPanel(parent, "TabPanel_General");
        CreateSettingToggle(panel.transform, "Kill Feed", GameSettings.KillFeedEnabled,
            v => GameSettings.SetKillFeedEnabled(v), new Vector2(0f, -40f));

        CreateLabel(panel.transform, "KeybindsHeader", "KEYBINDS",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -96f), new Vector2(-24f, -28f), 20,
            TextAlignmentOptions.Left, AccentTeal);

        KeybindRebindController rebindController = parent.GetComponentInParent<KeybindRebindController>();
        if (rebindController == null)
            rebindController = Object.FindObjectOfType<KeybindRebindController>();

        TextMeshProUGUI conflictWarning = CreateLabel(panel.transform, "KeybindConflictWarning", "",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -124f), new Vector2(-24f, -44f), 14,
            TextAlignmentOptions.Left, new Color(1f, 0.55f, 0.45f, 1f));
        conflictWarning.gameObject.SetActive(false);
        if (rebindController != null)
            rebindController.RegisterWarningLabel(conflictWarning);

        float rowY = -168f;
        const float rowStep = 48f;
        foreach (GameKeybinds.BindingInfo binding in GameKeybinds.AllBindings)
        {
            CreateKeybindRow(panel.transform, binding, new Vector2(0f, rowY), rebindController);
            rowY -= rowStep;
        }

        rebindController?.RefreshConflictState();
        return panel;
    }

    private void CreateKeybindRow(Transform parent, GameKeybinds.BindingInfo binding, Vector2 anchoredPos,
        KeybindRebindController rebindController)
    {
        GameObject row = CreateUIObject("Row_Keybind_" + binding.Id, parent);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = anchoredPos;
        rowRect.sizeDelta = new Vector2(-48f, 44f);

        CreateLabel(row.transform, "Label", binding.Label.ToUpperInvariant(),
            new Vector2(0f, 0.5f), new Vector2(0.62f, 0.5f), Vector2.zero, Vector2.zero, 16,
            TextAlignmentOptions.Left);

        Button keyButton = CreateTextButton(row.transform, "KeyButton", GameKeybinds.FormatKey(GameKeybinds.Get(binding.Id)),
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-76f, 0f), new Vector2(132f, 36f), 15, TextAlignmentOptions.Center);
        Image keyButtonImage = keyButton.GetComponent<Image>();

        TextMeshProUGUI keyLabel = keyButton.GetComponentInChildren<TextMeshProUGUI>();
        if (rebindController != null && keyLabel != null)
        {
            KeybindId id = binding.Id;
            rebindController.RegisterRow(id, keyLabel, keyButtonImage);
            keyButton.onClick.AddListener(() => rebindController.BeginRebind(id, keyLabel));
        }
    }

    private GameObject CreateControlsPanel(Transform parent)
    {
        GameObject panel = CreateContentPanel(parent, "TabPanel_Controls");
        CreateSettingSlider(panel.transform, "Mouse Sensitivity", 0.5f, 10f, GameSettings.MouseSensitivity,
            v => GameSettings.SetMouseSensitivity(v), new Vector2(0f, -40f), showNumericField: true, decimalPlaces: 2);
        CreateSettingToggle(panel.transform, "Invert Y", GameSettings.InvertY,
            v => GameSettings.SetInvertY(v), new Vector2(0f, -110f));
        return panel;
    }

    private GameObject CreateCrosshairPanel(Transform parent)
    {
        GameObject panel = CreateContentPanel(parent, "TabPanel_Crosshair");
        CrosshairCatalog.EnsureInitialized();

        CreateLabel(panel.transform, "CrosshairTitle", "SELECT CROSSHAIR",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -20f), new Vector2(-24f, -36f), 20);

        GameObject grid = CreateUIObject("CrosshairGrid", panel.transform);
        RectTransform gridRect = grid.GetComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0f, 0f);
        gridRect.anchorMax = new Vector2(1f, 1f);
        gridRect.offsetMin = new Vector2(24f, 24f);
        gridRect.offsetMax = new Vector2(-24f, -108f);

        var gridLayout = grid.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(120f, 96f);
        gridLayout.spacing = new Vector2(12f, 12f);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 4;
        gridLayout.childAlignment = TextAnchor.UpperLeft;

        var selectionFrames = new Image[CrosshairCatalog.Count];
        var previewImages = new Image[CrosshairCatalog.Count];
        Color previewColor = GameSettings.GetCrosshairColor();
        for (int i = 0; i < CrosshairCatalog.Count; i++)
        {
            int index = i;
            CrosshairCatalog.Entry entry = CrosshairCatalog.Get(i);
            selectionFrames[i] = CreateCrosshairCatalogEntry(grid.transform, entry, previewColor, out previewImages[i], () =>
            {
                GameSettings.SetCrosshairIndex(index);
                RefreshCrosshairSelection(selectionFrames, index);
            });
        }

        OutlineColorSwatchRow crosshairColorRow = null;
        crosshairColorRow = CreateOutlineColorSwatches(panel.transform, "CrosshairColors",
            GameSettings.CrosshairColorIndex, i =>
            {
                GameSettings.SetCrosshairColorIndex(i);
                RefreshOutlineColorRow(crosshairColorRow, true, i);
                RefreshCrosshairPreviewColors(previewImages, GameSettings.GetCrosshairColor());
            }, new Vector2(0f, -56f), () => true, "CROSSHAIR COLOR");
        RefreshOutlineColorRow(crosshairColorRow, true, GameSettings.CrosshairColorIndex);

        RefreshCrosshairSelection(selectionFrames, GameSettings.CrosshairIndex);
        return panel;
    }

    private static void RefreshCrosshairPreviewColors(Image[] previews, Color color)
    {
        if (previews == null) return;
        for (int i = 0; i < previews.Length; i++)
        {
            if (previews[i] != null)
                previews[i].color = color;
        }
    }

    private Image CreateCrosshairCatalogEntry(Transform parent, CrosshairCatalog.Entry entry, Color previewColor,
        out Image previewImage, System.Action onSelect)
    {
        GameObject cell = CreateUIObject("CrosshairOption_" + entry.Id, parent);
        Image cellBg = cell.AddComponent<Image>();
        cellBg.color = new Color(0.14f, 0.14f, 0.16f, 0.95f);

        Button button = cell.AddComponent<Button>();
        button.targetGraphic = cellBg;
        button.onClick.AddListener(() => onSelect());

        GameObject frameGo = CreateUIObject("SelectionFrame", cell.transform);
        RectTransform frameRect = frameGo.GetComponent<RectTransform>();
        StretchFull(frameRect);
        Image frame = frameGo.AddComponent<Image>();
        frame.color = new Color(AccentTeal.r, AccentTeal.g, AccentTeal.b, 0f);
        frame.raycastTarget = false;
        Outline outline = frameGo.AddComponent<Outline>();
        outline.effectColor = AccentTeal;
        outline.effectDistance = new Vector2(2f, -2f);

        GameObject previewGo = CreateUIObject("Preview", cell.transform);
        RectTransform previewRect = previewGo.GetComponent<RectTransform>();
        previewRect.anchorMin = new Vector2(0.5f, 0.55f);
        previewRect.anchorMax = new Vector2(0.5f, 0.55f);
        previewRect.sizeDelta = new Vector2(48f, 48f);
        previewRect.anchoredPosition = Vector2.zero;
        previewImage = previewGo.AddComponent<Image>();
        previewImage.sprite = entry.Sprite;
        previewImage.color = previewColor;
        previewImage.preserveAspect = true;
        previewImage.raycastTarget = false;

        GameObject labelGo = CreateUIObject("Label", cell.transform);
        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.sizeDelta = new Vector2(0f, 28f);
        labelRect.anchoredPosition = new Vector2(0f, 6f);
        TextMeshProUGUI label = labelGo.AddComponent<TextMeshProUGUI>();
        label.text = entry.DisplayName.ToUpperInvariant();
        label.fontSize = 12;
        label.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        return frame;
    }

    private static void RefreshCrosshairSelection(Image[] frames, int selectedIndex)
    {
        if (frames == null) return;
        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] == null) continue;
            bool selected = i == selectedIndex;
            frames[i].color = selected
                ? new Color(AccentTeal.r, AccentTeal.g, AccentTeal.b, 0.18f)
                : new Color(AccentTeal.r, AccentTeal.g, AccentTeal.b, 0f);
            Outline outline = frames[i].GetComponent<Outline>();
            if (outline != null)
                outline.enabled = selected;
        }
    }

    private GameObject CreateVideoPanel(Transform parent)
    {
        GameObject panel = CreateContentPanel(parent, "TabPanel_Video");

        CreateSettingOptionButtons(panel.transform, "Graphics Quality", GameSettings.GraphicsQualityNames,
            GameSettings.GraphicsQuality, i => GameSettings.SetGraphicsQuality(i), new Vector2(0f, -28f));

        CreateSettingToggle(panel.transform, "Shadows", GameSettings.ShadowsEnabled,
            v => GameSettings.SetShadowsEnabled(v), new Vector2(0f, -98f));

        CreateSettingSlider(panel.transform, "Field Of View", 70f, 110f, GameSettings.FieldOfView,
            v => GameSettings.SetFieldOfView(v), new Vector2(0f, -158f), showNumericField: true, decimalPlaces: 0);

        OutlineColorSwatchRow enemyColorRow = CreateOutlineColorSwatches(panel.transform, "EnemyOutlineColors",
            GameSettings.EnemyOutlineColorIndex, GameSettings.SetEnemyOutlineColorIndex, new Vector2(0f, -278f),
            () => GameSettings.EnemyOutlinesEnabled);

        CreateSettingToggle(panel.transform, "Enemy Outlines", GameSettings.EnemyOutlinesEnabled,
            v =>
            {
                GameSettings.SetEnemyOutlinesEnabled(v);
                RefreshOutlineColorRow(enemyColorRow, v, GameSettings.EnemyOutlineColorIndex);
            }, new Vector2(0f, -228f));

        OutlineColorSwatchRow teammateColorRow = CreateOutlineColorSwatches(panel.transform, "TeammateOutlineColors",
            GameSettings.TeammateOutlineColorIndex, GameSettings.SetTeammateOutlineColorIndex, new Vector2(0f, -398f),
            () => GameSettings.TeammateOutlinesEnabled);

        CreateSettingToggle(panel.transform, "Teammate Outlines", GameSettings.TeammateOutlinesEnabled,
            v =>
            {
                GameSettings.SetTeammateOutlinesEnabled(v);
                RefreshOutlineColorRow(teammateColorRow, v, GameSettings.TeammateOutlineColorIndex);
            }, new Vector2(0f, -348f));

        RefreshOutlineColorRow(enemyColorRow, GameSettings.EnemyOutlinesEnabled, GameSettings.EnemyOutlineColorIndex);
        RefreshOutlineColorRow(teammateColorRow, GameSettings.TeammateOutlinesEnabled, GameSettings.TeammateOutlineColorIndex);
        return panel;
    }

    private void CreateSettingOptionButtons(Transform parent, string label, string[] options, int selectedIndex,
        System.Action<int> onSelected, Vector2 anchoredPos)
    {
        GameObject row = CreateUIObject("Row_" + label, parent);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = anchoredPos;
        rowRect.sizeDelta = new Vector2(-48f, 56f);

        CreateLabel(row.transform, "Label", label.ToUpperInvariant(),
            new Vector2(0f, 0.5f), new Vector2(0.35f, 0.5f), Vector2.zero, Vector2.zero, 18,
            TextAlignmentOptions.Left);

        GameObject buttonRow = CreateUIObject("Options", row.transform);
        RectTransform buttonRowRect = buttonRow.GetComponent<RectTransform>();
        buttonRowRect.anchorMin = new Vector2(0.35f, 0.5f);
        buttonRowRect.anchorMax = new Vector2(1f, 0.5f);
        buttonRowRect.sizeDelta = new Vector2(-12f, 36f);
        buttonRowRect.anchoredPosition = Vector2.zero;

        HorizontalLayoutGroup layout = buttonRow.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        var buttons = new Button[options.Length];
        for (int i = 0; i < options.Length; i++)
        {
            int index = i;
            buttons[i] = CreateTextButton(buttonRow.transform, "Option_" + i, options[i],
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(132f, 32f), 12, TextAlignmentOptions.Center);
            buttons[i].onClick.AddListener(() =>
            {
                onSelected(index);
                RefreshOptionButtonSelection(buttons, index);
            });
        }

        RefreshOptionButtonSelection(buttons, selectedIndex);
    }

    private static void RefreshOptionButtonSelection(Button[] buttons, int selectedIndex)
    {
        if (buttons == null) return;
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            Image bg = buttons[i].GetComponent<Image>();
            TextMeshProUGUI label = buttons[i].GetComponentInChildren<TextMeshProUGUI>();
            bool selected = i == selectedIndex;
            if (bg != null)
                bg.color = selected
                    ? new Color(AccentTeal.r, AccentTeal.g, AccentTeal.b, 0.35f)
                    : new Color(0.2f, 0.2f, 0.22f, 0.6f);
            if (label != null)
                label.color = selected ? Color.white : new Color(0.75f, 0.75f, 0.75f, 1f);
        }
    }

    private OutlineColorSwatchRow CreateOutlineColorSwatches(Transform parent, string rowName, int selectedIndex,
        System.Action<int> onSelected, Vector2 anchoredPos, System.Func<bool> isEnabled,
        string labelText = "OUTLINE COLOR")
    {
        var swatchRowData = new OutlineColorSwatchRow
        {
            Buttons = new Button[GameSettings.OutlineColors.Length],
            ColorImages = new Image[GameSettings.OutlineColors.Length],
            SelectionFrames = new Image[GameSettings.OutlineColors.Length],
            IsEnabled = isEnabled
        };

        GameObject row = CreateUIObject(rowName, parent);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = anchoredPos;
        rowRect.sizeDelta = new Vector2(-48f, 48f);

        swatchRowData.Label = CreateLabel(row.transform, "Label", labelText,
            new Vector2(0f, 0.5f), new Vector2(0.35f, 0.5f), Vector2.zero, Vector2.zero, 14,
            TextAlignmentOptions.Left);

        GameObject swatchRow = CreateUIObject("Swatches", row.transform);
        RectTransform swatchRowRect = swatchRow.GetComponent<RectTransform>();
        swatchRowRect.anchorMin = new Vector2(0.35f, 0.5f);
        swatchRowRect.anchorMax = new Vector2(1f, 0.5f);
        swatchRowRect.sizeDelta = new Vector2(-12f, 36f);
        swatchRowRect.anchoredPosition = Vector2.zero;

        HorizontalLayoutGroup layout = swatchRow.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        for (int i = 0; i < GameSettings.OutlineColors.Length; i++)
        {
            int index = i;
            GameObject swatchGo = CreateUIObject("Swatch_" + GameSettings.OutlineColorNames[i], swatchRow.transform);
            RectTransform swatchRect = swatchGo.GetComponent<RectTransform>();
            swatchRect.sizeDelta = new Vector2(32f, 32f);

            Image swatchBg = swatchGo.AddComponent<Image>();
            swatchBg.color = new Color(0.14f, 0.14f, 0.16f, 0.95f);

            Button button = swatchGo.AddComponent<Button>();
            button.targetGraphic = swatchBg;
            button.onClick.AddListener(() =>
            {
                onSelected(index);
                bool enabled = swatchRowData.IsEnabled != null && swatchRowData.IsEnabled();
                RefreshOutlineColorRow(swatchRowData, enabled, index);
            });
            swatchRowData.Buttons[i] = button;

            GameObject colorGo = CreateUIObject("Color", swatchGo.transform);
            RectTransform colorRect = colorGo.GetComponent<RectTransform>();
            colorRect.anchorMin = new Vector2(0.5f, 0.5f);
            colorRect.anchorMax = new Vector2(0.5f, 0.5f);
            colorRect.sizeDelta = new Vector2(22f, 22f);
            Image colorImg = colorGo.AddComponent<Image>();
            colorImg.color = GameSettings.OutlineColors[i];
            colorImg.raycastTarget = false;
            swatchRowData.ColorImages[i] = colorImg;

            GameObject frameGo = CreateUIObject("Frame", swatchGo.transform);
            StretchFull(frameGo.GetComponent<RectTransform>());
            Image frame = frameGo.AddComponent<Image>();
            frame.color = new Color(AccentTeal.r, AccentTeal.g, AccentTeal.b, 0f);
            frame.raycastTarget = false;
            Outline outline = frameGo.AddComponent<Outline>();
            outline.effectColor = AccentTeal;
            outline.effectDistance = new Vector2(2f, -2f);
            swatchRowData.SelectionFrames[i] = frame;
        }

        return swatchRowData;
    }

    private static void RefreshOutlineColorRow(OutlineColorSwatchRow row, bool enabled, int selectedIndex)
    {
        if (row == null) return;

        if (row.Label != null)
            row.Label.color = enabled ? Color.white : DisabledSettingLabelColor;

        for (int i = 0; i < GameSettings.OutlineColors.Length; i++)
        {
            if (row.Buttons != null && i < row.Buttons.Length && row.Buttons[i] != null)
                row.Buttons[i].interactable = enabled;

            if (row.ColorImages != null && i < row.ColorImages.Length && row.ColorImages[i] != null)
            {
                Color baseColor = GameSettings.OutlineColors[i];
                row.ColorImages[i].color = enabled
                    ? baseColor
                    : Color.Lerp(baseColor, DisabledSwatchTint, 0.72f);
            }

            if (row.SelectionFrames == null || i >= row.SelectionFrames.Length || row.SelectionFrames[i] == null)
                continue;

            bool selected = enabled && i == selectedIndex;
            row.SelectionFrames[i].color = selected
                ? new Color(AccentTeal.r, AccentTeal.g, AccentTeal.b, 0.18f)
                : new Color(AccentTeal.r, AccentTeal.g, AccentTeal.b, 0f);
            Outline outline = row.SelectionFrames[i].GetComponent<Outline>();
            if (outline != null)
                outline.enabled = selected;
        }
    }

    private GameObject CreateAudioPanel(Transform parent)
    {
        GameObject panel = CreateContentPanel(parent, "TabPanel_Audio");

        CreateSettingPercentSlider(panel.transform, "Master Volume", GameSettings.MasterVolume,
            v => GameSettings.SetMasterVolume(v), new Vector2(0f, -28f));

        AudioCategoryRowUI generalRow = CreateAudioCategoryRow(panel.transform, "General Sounds",
            GameSettings.GeneralSoundsEnabled, GameSettings.GeneralSoundsVolume,
            v => GameSettings.SetGeneralSoundsEnabled(v),
            v => GameSettings.SetGeneralSoundsVolume(v),
            new Vector2(0f, -98f));

        AudioCategoryRowUI enemyRow = CreateAudioCategoryRow(panel.transform, "Enemy Sounds",
            GameSettings.EnemySoundsEnabled, GameSettings.EnemySoundsVolume,
            v => GameSettings.SetEnemySoundsEnabled(v),
            v => GameSettings.SetEnemySoundsVolume(v),
            new Vector2(0f, -168f));

        AudioCategoryRowUI teammateRow = CreateAudioCategoryRow(panel.transform, "Teammate Sounds",
            GameSettings.TeammateSoundsEnabled, GameSettings.TeammateSoundsVolume,
            v => GameSettings.SetTeammateSoundsEnabled(v),
            v => GameSettings.SetTeammateSoundsVolume(v),
            new Vector2(0f, -238f));

        RefreshAudioCategoryRow(generalRow, GameSettings.GeneralSoundsEnabled);
        RefreshAudioCategoryRow(enemyRow, GameSettings.EnemySoundsEnabled);
        RefreshAudioCategoryRow(teammateRow, GameSettings.TeammateSoundsEnabled);
        return panel;
    }

    private void CreateSettingPercentSlider(Transform parent, string label, float normalizedValue,
        System.Action<float> onChanged, Vector2 anchoredPos)
    {
        CreateSettingSlider(parent, label, 0f, 100f, normalizedValue * 100f,
            v => onChanged(v / 100f), anchoredPos, showNumericField: true, decimalPlaces: 0);
    }

    private AudioCategoryRowUI CreateAudioCategoryRow(Transform parent, string label, bool enabled, float normalizedVolume,
        System.Action<bool> onToggleChanged, System.Action<float> onVolumeChanged, Vector2 anchoredPos)
    {
        var rowUi = new AudioCategoryRowUI();

        GameObject row = CreateUIObject("Row_" + label, parent);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = anchoredPos;
        rowRect.sizeDelta = new Vector2(-48f, 56f);

        rowUi.Label = CreateLabel(row.transform, "Label", label.ToUpperInvariant(),
            new Vector2(0f, 0.5f), new Vector2(0.34f, 0.5f), Vector2.zero, Vector2.zero, 16,
            TextAlignmentOptions.Left);

        GameObject toggleGo = CreateUIObject("Toggle", row.transform);
        RectTransform toggleRect = toggleGo.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0.34f, 0.5f);
        toggleRect.anchorMax = new Vector2(0.34f, 0.5f);
        toggleRect.sizeDelta = new Vector2(36f, 36f);
        toggleRect.anchoredPosition = Vector2.zero;

        rowUi.Toggle = toggleGo.AddComponent<Toggle>();
        rowUi.Toggle.isOn = enabled;
        Image toggleBg = toggleGo.AddComponent<Image>();
        toggleBg.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        rowUi.Toggle.targetGraphic = toggleBg;

        GameObject check = CreateUIObject("Checkmark", toggleGo.transform);
        StretchFull(check.GetComponent<RectTransform>());
        Image checkImg = check.AddComponent<Image>();
        checkImg.color = AccentTeal;
        rowUi.Toggle.graphic = checkImg;

        GameObject sliderGo = CreateUIObject("Slider", row.transform);
        RectTransform sliderRect = sliderGo.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.4f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.82f, 0.5f);
        sliderRect.sizeDelta = new Vector2(-8f, 24f);
        sliderRect.anchoredPosition = Vector2.zero;

        rowUi.Slider = sliderGo.AddComponent<Slider>();
        rowUi.Slider.minValue = 0f;
        rowUi.Slider.maxValue = 100f;
        rowUi.Slider.value = normalizedVolume * 100f;

        rowUi.NumericInput = CreateNumericInputField(row.transform, FormatPercentVolume(normalizedVolume));
        bool syncing = false;

        rowUi.Slider.onValueChanged.AddListener(v =>
        {
            if (syncing) return;
            syncing = true;
            rowUi.NumericInput.SetTextWithoutNotify(FormatPercentVolume(v / 100f));
            syncing = false;
            onVolumeChanged(v / 100f);
        });

        rowUi.NumericInput.onEndEdit.AddListener(text =>
        {
            if (!TryParsePercentVolume(text, out float parsed))
            {
                rowUi.NumericInput.SetTextWithoutNotify(FormatPercentVolume(rowUi.Slider.value / 100f));
                return;
            }

            syncing = true;
            rowUi.Slider.SetValueWithoutNotify(parsed * 100f);
            rowUi.NumericInput.SetTextWithoutNotify(FormatPercentVolume(parsed));
            syncing = false;
            onVolumeChanged(parsed);
        });

        rowUi.Toggle.onValueChanged.AddListener(v =>
        {
            onToggleChanged(v);
            RefreshAudioCategoryRow(rowUi, v);
        });

        GameObject bg = CreateUIObject("Background", sliderGo.transform);
        StretchFull(bg.GetComponent<RectTransform>());
        bg.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);

        GameObject fillArea = CreateUIObject("Fill Area", sliderGo.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(6f, 6f);
        fillAreaRect.offsetMax = new Vector2(-6f, -6f);

        GameObject fill = CreateUIObject("Fill", fillArea.transform);
        StretchFull(fill.GetComponent<RectTransform>());
        rowUi.SliderFill = fill.AddComponent<Image>();
        rowUi.SliderFill.color = AccentTeal;
        rowUi.Slider.fillRect = fill.GetComponent<RectTransform>();

        GameObject handleArea = CreateUIObject("Handle Slide Area", sliderGo.transform);
        StretchFull(handleArea.GetComponent<RectTransform>());

        GameObject handle = CreateUIObject("Handle", handleArea.transform);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(16f, 24f);
        rowUi.SliderHandle = handle.AddComponent<Image>();
        rowUi.SliderHandle.color = Color.white;
        rowUi.Slider.handleRect = handleRect;
        rowUi.Slider.targetGraphic = rowUi.SliderHandle;

        return rowUi;
    }

    private static void RefreshAudioCategoryRow(AudioCategoryRowUI row, bool enabled)
    {
        if (row == null) return;

        if (row.Label != null)
            row.Label.color = enabled ? Color.white : DisabledSettingLabelColor;

        if (row.Slider != null)
            row.Slider.interactable = enabled;

        if (row.NumericInput != null)
            row.NumericInput.interactable = enabled;

        float alpha = enabled ? 1f : 0.35f;
        if (row.SliderFill != null)
        {
            Color fill = row.SliderFill.color;
            row.SliderFill.color = new Color(fill.r, fill.g, fill.b, alpha);
        }

        if (row.SliderHandle != null)
        {
            Color handle = enabled ? Color.white : DisabledSettingLabelColor;
            row.SliderHandle.color = handle;
        }
    }

    private static string FormatPercentVolume(float normalizedValue)
    {
        return Mathf.RoundToInt(Mathf.Clamp01(normalizedValue) * 100f).ToString();
    }

    private static bool TryParsePercentVolume(string text, out float normalizedValue)
    {
        normalizedValue = 0f;
        if (!int.TryParse(text, out int percent))
            return false;
        percent = Mathf.Clamp(percent, 0, 100);
        normalizedValue = percent / 100f;
        return true;
    }

    private GameObject CreateComingSoonPanel(Transform parent, string tabName)
    {
        GameObject panel = CreateContentPanel(parent, "TabPanel_" + tabName);
        CreateLabel(panel.transform, "ComingSoon", "COMING SOON",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 60f), 28,
            TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f, 1f));
        return panel;
    }

    private GameObject CreateContentPanel(Transform parent, string name)
    {
        GameObject panel = CreateUIObject(name, parent);
        StretchFull(panel.GetComponent<RectTransform>());
        panel.SetActive(false);
        return panel;
    }

    private void CreateSettingSlider(Transform parent, string label, float min, float max, float value,
        System.Action<float> onChanged, Vector2 anchoredPos, bool showNumericField = false, int decimalPlaces = 2)
    {
        GameObject row = CreateUIObject("Row_" + label, parent);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = anchoredPos;
        rowRect.sizeDelta = new Vector2(-48f, 56f);

        CreateLabel(row.transform, "Label", label.ToUpperInvariant(),
            new Vector2(0f, 0.5f), new Vector2(0.35f, 0.5f), Vector2.zero, Vector2.zero, 18,
            TextAlignmentOptions.Left);

        float sliderAnchorMax = showNumericField ? 0.82f : 1f;
        float sliderRightPad = showNumericField ? 8f : 12f;

        GameObject sliderGo = CreateUIObject("Slider", row.transform);
        RectTransform sliderRect = sliderGo.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.35f, 0.5f);
        sliderRect.anchorMax = new Vector2(sliderAnchorMax, 0.5f);
        sliderRect.sizeDelta = new Vector2(-sliderRightPad, 24f);
        sliderRect.anchoredPosition = Vector2.zero;

        Slider slider = sliderGo.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;

        if (showNumericField)
        {
            TMP_InputField numericInput = CreateNumericInputField(row.transform, FormatSliderValue(value, decimalPlaces));
            bool syncing = false;

            slider.onValueChanged.AddListener(v =>
            {
                if (syncing) return;
                syncing = true;
                numericInput.SetTextWithoutNotify(FormatSliderValue(v, decimalPlaces));
                syncing = false;
                onChanged(v);
            });

            numericInput.onEndEdit.AddListener(text =>
            {
                if (!TryParseSliderValue(text, out float parsed))
                {
                    numericInput.SetTextWithoutNotify(FormatSliderValue(slider.value, decimalPlaces));
                    return;
                }

                parsed = Mathf.Clamp(parsed, min, max);
                syncing = true;
                slider.SetValueWithoutNotify(parsed);
                numericInput.SetTextWithoutNotify(FormatSliderValue(parsed, decimalPlaces));
                syncing = false;
                onChanged(parsed);
            });
        }
        else
        {
            slider.onValueChanged.AddListener(v => onChanged(v));
        }

        GameObject bg = CreateUIObject("Background", sliderGo.transform);
        StretchFull(bg.GetComponent<RectTransform>());
        bg.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);

        GameObject fillArea = CreateUIObject("Fill Area", sliderGo.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(6f, 6f);
        fillAreaRect.offsetMax = new Vector2(-6f, -6f);

        GameObject fill = CreateUIObject("Fill", fillArea.transform);
        StretchFull(fill.GetComponent<RectTransform>());
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = AccentTeal;
        slider.fillRect = fill.GetComponent<RectTransform>();

        GameObject handleArea = CreateUIObject("Handle Slide Area", sliderGo.transform);
        StretchFull(handleArea.GetComponent<RectTransform>());

        GameObject handle = CreateUIObject("Handle", handleArea.transform);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(16f, 24f);
        handle.AddComponent<Image>().color = Color.white;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle.GetComponent<Image>();
    }

    private void CreateSettingToggle(Transform parent, string label, bool value,
        System.Action<bool> onChanged, Vector2 anchoredPos)
    {
        GameObject row = CreateUIObject("Row_" + label, parent);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = anchoredPos;
        rowRect.sizeDelta = new Vector2(-48f, 40f);

        CreateLabel(row.transform, "Label", label.ToUpperInvariant(),
            new Vector2(0f, 0.5f), new Vector2(0.7f, 0.5f), Vector2.zero, Vector2.zero, 18,
            TextAlignmentOptions.Left);

        GameObject toggleGo = CreateUIObject("Toggle", row.transform);
        RectTransform toggleRect = toggleGo.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(1f, 0.5f);
        toggleRect.anchorMax = new Vector2(1f, 0.5f);
        toggleRect.sizeDelta = new Vector2(36f, 36f);
        toggleRect.anchoredPosition = new Vector2(-24f, 0f);

        Toggle toggle = toggleGo.AddComponent<Toggle>();
        toggle.isOn = value;
        Image toggleBg = toggleGo.AddComponent<Image>();
        toggleBg.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        toggle.targetGraphic = toggleBg;

        GameObject check = CreateUIObject("Checkmark", toggleGo.transform);
        StretchFull(check.GetComponent<RectTransform>());
        Image checkImg = check.AddComponent<Image>();
        checkImg.color = AccentTeal;
        toggle.graphic = checkImg;

        toggle.onValueChanged.AddListener(v => onChanged(v));
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta, float fontSize,
        TextAlignmentOptions align = TextAlignmentOptions.Left, Color? color = null)
    {
        GameObject go = CreateUIObject(name, parent);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta == Vector2.zero ? Vector2.zero : sizeDelta;
        if (sizeDelta == Vector2.zero)
        {
            rect.offsetMin = anchoredPos;
            rect.offsetMax = sizeDelta;
        }

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color ?? Color.white;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        return tmp;
    }

    private Button CreateTextButton(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 sizeDelta, float fontSize, TextAlignmentOptions align)
    {
        GameObject go = CreateUIObject(name, parent);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.22f, 0.6f);

        Button button = go.AddComponent<Button>();
        button.targetGraphic = img;

        GameObject labelGo = CreateUIObject("Text", go.transform);
        StretchFull(labelGo.GetComponent<RectTransform>());
        TextMeshProUGUI tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = align;
        tmp.raycastTarget = false;

        return button;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
        return go;
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private TMP_InputField CreateNumericInputField(Transform parent, string initialValue)
    {
        GameObject fieldGo = CreateUIObject("NumericValue", parent);
        RectTransform fieldRect = fieldGo.GetComponent<RectTransform>();
        fieldRect.anchorMin = new Vector2(0.82f, 0.5f);
        fieldRect.anchorMax = new Vector2(1f, 0.5f);
        fieldRect.sizeDelta = new Vector2(-8f, 32f);
        fieldRect.anchoredPosition = Vector2.zero;

        Image bg = fieldGo.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        GameObject textArea = CreateUIObject("Text Area", fieldGo.transform);
        StretchFull(textArea.GetComponent<RectTransform>());
        RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
        textAreaRect.offsetMin = new Vector2(8f, 4f);
        textAreaRect.offsetMax = new Vector2(-8f, -4f);

        GameObject placeholderGo = CreateUIObject("Placeholder", textArea.transform);
        StretchFull(placeholderGo.GetComponent<RectTransform>());
        TextMeshProUGUI placeholder = placeholderGo.AddComponent<TextMeshProUGUI>();
        placeholder.text = "";
        placeholder.fontSize = 16;
        placeholder.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        placeholder.alignment = TextAlignmentOptions.Center;

        GameObject textGo = CreateUIObject("Text", textArea.transform);
        StretchFull(textGo.GetComponent<RectTransform>());
        TextMeshProUGUI text = textGo.AddComponent<TextMeshProUGUI>();
        text.fontSize = 16;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;

        TMP_InputField input = fieldGo.AddComponent<TMP_InputField>();
        input.textViewport = textAreaRect;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.contentType = TMP_InputField.ContentType.DecimalNumber;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.text = initialValue;
        return input;
    }

    private static string FormatSliderValue(float value, int decimalPlaces)
    {
        return value.ToString("F" + Mathf.Clamp(decimalPlaces, 0, 3));
    }

    private static bool TryParseSliderValue(string text, out float value)
    {
        return float.TryParse(text, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out value);
    }
}
