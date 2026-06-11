using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Valorant-style ability slots centered along the bottom: icon on top, key underneath, cooldown in icon corner.
/// </summary>
public class AbilityHudUI : MonoBehaviour
{
    private class SlotUI
    {
        public GameObject Root;
        public Image Icon;
        public Image CooldownOverlay;
        public TextMeshProUGUI CooldownText;
        public TextMeshProUGUI KeyLabel;
        public PlayerController.AbilityId AbilityId;
    }

    private static readonly Color ReadyIconColor = Color.white;
    private static readonly Color CooldownIconColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    private static readonly Color SlotBorderColor = new Color(0.82f, 0.82f, 0.82f, 0.9f);
    private static readonly Color CooldownOverlayColor = new Color(0f, 0f, 0f, 0.62f);
    private static readonly Color KeyLabelColor = new Color(0.92f, 0.92f, 0.92f, 1f);

    [SerializeField] private float slotSize = 56f;
    [SerializeField] private float slotSpacing = 16f;
    [SerializeField] private float keyLabelHeight = 20f;
    [SerializeField] private float bottomOffset = 52f;

    private PlayerController playerController;
    private readonly System.Collections.Generic.List<SlotUI> slots = new System.Collections.Generic.List<SlotUI>();
    private RectTransform containerRect;
    private TMP_FontAsset fontAsset;
    private bool built;
    private static Sprite uiSprite;

    public void Setup(PlayerController controller, TextMeshProUGUI fontSource = null)
    {
        playerController = controller;
        if (fontSource != null)
            fontAsset = fontSource.font;

        if (!built)
            Build();

        RefreshVisibleSlots();
    }

    private void Build()
    {
        built = true;
        AbilityIconCatalog.EnsureInitialized();

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
            return;

        GameObject containerGo = new GameObject("AbilityHud");
        containerGo.transform.SetParent(canvas.transform, false);
        containerGo.layer = canvas.gameObject.layer;
        containerRect = containerGo.AddComponent<RectTransform>();

        containerRect.anchorMin = new Vector2(0.5f, 0f);
        containerRect.anchorMax = new Vector2(0.5f, 0f);
        containerRect.pivot = new Vector2(0.5f, 0f);
        containerRect.anchoredPosition = new Vector2(0f, bottomOffset);

        HorizontalLayoutGroup layout = containerGo.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.LowerCenter;
        layout.spacing = slotSpacing;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = containerGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        foreach (PlayerController.AbilityId abilityId in System.Enum.GetValues(typeof(PlayerController.AbilityId)))
            slots.Add(CreateSlot(containerRect, abilityId));
    }

    private SlotUI CreateSlot(Transform parent, PlayerController.AbilityId abilityId)
    {
        var slot = new SlotUI { AbilityId = abilityId };

        GameObject root = new GameObject("AbilitySlot_" + abilityId);
        root.transform.SetParent(parent, false);
        root.layer = parent.gameObject.layer;
        RectTransform rootRect = root.AddComponent<RectTransform>();
        slot.Root = root;

        VerticalLayoutGroup slotLayout = root.AddComponent<VerticalLayoutGroup>();
        slotLayout.childAlignment = TextAnchor.MiddleCenter;
        slotLayout.spacing = 4f;
        slotLayout.childControlWidth = true;
        slotLayout.childControlHeight = true;
        slotLayout.childForceExpandWidth = false;
        slotLayout.childForceExpandHeight = false;

        LayoutElement rootLayout = root.AddComponent<LayoutElement>();
        rootLayout.preferredWidth = slotSize;
        rootLayout.preferredHeight = slotSize + keyLabelHeight + 4f;

        GameObject iconArea = new GameObject("IconArea");
        iconArea.transform.SetParent(root.transform, false);
        iconArea.layer = parent.gameObject.layer;
        RectTransform iconAreaRect = iconArea.AddComponent<RectTransform>();
        iconAreaRect.sizeDelta = new Vector2(slotSize, slotSize);

        LayoutElement iconLayout = iconArea.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = slotSize;
        iconLayout.preferredHeight = slotSize;
        iconLayout.minWidth = slotSize;
        iconLayout.minHeight = slotSize;

        Image border = iconArea.AddComponent<Image>();
        border.sprite = GetUiSprite();
        border.color = SlotBorderColor;
        border.raycastTarget = false;

        GameObject iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(iconArea.transform, false);
        iconGo.layer = parent.gameObject.layer;
        RectTransform iconRect = iconGo.AddComponent<RectTransform>();
        StretchRect(iconRect, 3f);
        slot.Icon = iconGo.AddComponent<Image>();
        slot.Icon.sprite = GetUiSprite();
        slot.Icon.preserveAspect = true;
        slot.Icon.raycastTarget = false;

        GameObject overlayGo = new GameObject("CooldownOverlay");
        overlayGo.transform.SetParent(iconArea.transform, false);
        overlayGo.layer = parent.gameObject.layer;
        RectTransform overlayRect = overlayGo.AddComponent<RectTransform>();
        StretchRect(overlayRect, 3f);
        slot.CooldownOverlay = overlayGo.AddComponent<Image>();
        slot.CooldownOverlay.sprite = GetUiSprite();
        slot.CooldownOverlay.color = CooldownOverlayColor;
        slot.CooldownOverlay.type = Image.Type.Filled;
        slot.CooldownOverlay.fillMethod = Image.FillMethod.Vertical;
        slot.CooldownOverlay.fillOrigin = (int)Image.OriginVertical.Bottom;
        slot.CooldownOverlay.fillAmount = 0f;
        slot.CooldownOverlay.raycastTarget = false;

        GameObject timerGo = new GameObject("CooldownTimer");
        timerGo.transform.SetParent(iconArea.transform, false);
        timerGo.layer = parent.gameObject.layer;
        RectTransform timerRect = timerGo.AddComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(1f, 1f);
        timerRect.anchorMax = new Vector2(1f, 1f);
        timerRect.pivot = new Vector2(1f, 1f);
        timerRect.anchoredPosition = new Vector2(-4f, -2f);
        timerRect.sizeDelta = new Vector2(34f, 18f);
        slot.CooldownText = timerGo.AddComponent<TextMeshProUGUI>();
        ApplyFont(slot.CooldownText);
        slot.CooldownText.fontSize = 14;
        slot.CooldownText.fontStyle = FontStyles.Bold;
        slot.CooldownText.alignment = TextAlignmentOptions.TopRight;
        slot.CooldownText.color = Color.white;
        slot.CooldownText.raycastTarget = false;
        slot.CooldownText.outlineWidth = 0.2f;
        slot.CooldownText.outlineColor = new Color32(0, 0, 0, 200);

        GameObject keyGo = new GameObject("KeyLabel");
        keyGo.transform.SetParent(root.transform, false);
        keyGo.layer = parent.gameObject.layer;
        RectTransform keyRect = keyGo.AddComponent<RectTransform>();
        keyRect.sizeDelta = new Vector2(slotSize, keyLabelHeight);

        LayoutElement keyLayout = keyGo.AddComponent<LayoutElement>();
        keyLayout.preferredWidth = slotSize;
        keyLayout.preferredHeight = keyLabelHeight;
        keyLayout.minHeight = keyLabelHeight;

        slot.KeyLabel = keyGo.AddComponent<TextMeshProUGUI>();
        ApplyFont(slot.KeyLabel);
        slot.KeyLabel.fontSize = 14;
        slot.KeyLabel.fontStyle = FontStyles.Bold;
        slot.KeyLabel.alignment = TextAlignmentOptions.Center;
        slot.KeyLabel.color = KeyLabelColor;
        slot.KeyLabel.raycastTarget = false;
        slot.KeyLabel.outlineWidth = 0.15f;
        slot.KeyLabel.outlineColor = new Color32(0, 0, 0, 180);

        return slot;
    }

    private void ApplyFont(TextMeshProUGUI text)
    {
        if (text == null || fontAsset == null)
            return;
        text.font = fontAsset;
    }

    private void Update()
    {
        if (playerController == null)
            return;

        RefreshVisibleSlots();
        UpdateSlots();
    }

    private void RefreshVisibleSlots()
    {
        if (playerController == null)
            return;

        foreach (SlotUI slot in slots)
        {
            bool enabled = playerController.IsAbilityEnabled(slot.AbilityId);
            if (slot.Root != null)
                slot.Root.SetActive(enabled);
        }
    }

    private void UpdateSlots()
    {
        foreach (SlotUI slot in slots)
        {
            if (slot.Root == null || !slot.Root.activeSelf)
                continue;

            PlayerController.AbilityHudInfo info = playerController.GetAbilityHudInfo(slot.AbilityId);
            if (slot.Icon != null)
            {
                slot.Icon.sprite = info.Icon != null ? info.Icon : GetUiSprite();
                slot.Icon.color = info.CooldownRemaining > 0f ? CooldownIconColor : ReadyIconColor;
            }

            if (slot.KeyLabel != null)
                slot.KeyLabel.text = GameKeybinds.FormatKey(info.Keybind);

            bool onCooldown = info.CooldownRemaining > 0f && info.CooldownDuration > 0f;
            if (slot.CooldownOverlay != null)
            {
                slot.CooldownOverlay.enabled = onCooldown;
                if (onCooldown)
                    slot.CooldownOverlay.fillAmount = Mathf.Clamp01(info.CooldownRemaining / info.CooldownDuration);
            }

            if (slot.CooldownText != null)
            {
                slot.CooldownText.enabled = onCooldown;
                if (onCooldown)
                {
                    float display = info.CooldownRemaining >= 10f
                        ? Mathf.Ceil(info.CooldownRemaining)
                        : info.CooldownRemaining;
                    slot.CooldownText.text = display >= 10f ? display.ToString("0") : display.ToString("0.0");
                }
            }
        }
    }

    private static Sprite GetUiSprite()
    {
        if (uiSprite != null)
            return uiSprite;

        uiSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        if (uiSprite == null)
        {
            Texture2D tex = Texture2D.whiteTexture;
            uiSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }

        return uiSprite;
    }

    private static void StretchRect(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }
}
