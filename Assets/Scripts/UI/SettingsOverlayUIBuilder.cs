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
        for (int i = 3; i < tabNames.Length; i++)
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
        CreateSettingSlider(panel.transform, "Master Volume", 0f, 1f, GameSettings.MasterVolume,
            v => GameSettings.SetMasterVolume(v), new Vector2(0f, -40f));
        CreateSettingToggle(panel.transform, "Kill Feed", GameSettings.KillFeedEnabled,
            v => GameSettings.SetKillFeedEnabled(v), new Vector2(0f, -110f));
        return panel;
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
