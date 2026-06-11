using UnityEngine;
using Photon.Pun;

/// <summary>
/// Player preferences persisted via PlayerPrefs and applied to gameplay systems.
/// </summary>
public static class GameSettings
{
    private const string KeyMouseSensitivity = "MouseSensitivity";
    private const string KeyMasterVolume = "MasterVolume";
    private const string KeyKillFeedEnabled = "KillFeedEnabled";
    private const string KeyInvertY = "InvertY";
    private const string KeyCrosshairIndex = "CrosshairIndex";
    private const string KeyCrosshairColorIndex = "CrosshairColorIndex";
    private const string KeyGraphicsQuality = "GraphicsQuality";
    private const string KeyShadowsEnabled = "ShadowsEnabled";
    private const string KeyFieldOfView = "FieldOfView";
    private const string KeyEnemyOutlinesEnabled = "EnemyOutlinesEnabled";
    private const string KeyEnemyOutlineColorIndex = "EnemyOutlineColorIndex";
    private const string KeyTeammateOutlinesEnabled = "TeammateOutlinesEnabled";
    private const string KeyTeammateOutlineColorIndex = "TeammateOutlineColorIndex";
    private const string KeyGeneralSoundsEnabled = "GeneralSoundsEnabled";
    private const string KeyGeneralSoundsVolume = "GeneralSoundsVolume";
    private const string KeyEnemySoundsEnabled = "EnemySoundsEnabled";
    private const string KeyEnemySoundsVolume = "EnemySoundsVolume";
    private const string KeyTeammateSoundsEnabled = "TeammateSoundsEnabled";
    private const string KeyTeammateSoundsVolume = "TeammateSoundsVolume";

    public const float DefaultMouseSensitivity = 2f;
    public const float DefaultMasterVolume = 1f;
    public const bool DefaultKillFeedEnabled = true;
    public const bool DefaultInvertY = false;
    public const int DefaultCrosshairIndex = 0;
    public const int DefaultCrosshairColorIndex = 2;
    public const int DefaultGraphicsQuality = 1;
    public const bool DefaultShadowsEnabled = true;
    public const float DefaultFieldOfView = 60f;
    public const bool DefaultEnemyOutlinesEnabled = true;
    public const int DefaultEnemyOutlineColorIndex = 0;
    public const bool DefaultTeammateOutlinesEnabled = false;
    public const int DefaultTeammateOutlineColorIndex = 5;
    public const bool DefaultGeneralSoundsEnabled = true;
    public const float DefaultGeneralSoundsVolume = 1f;
    public const bool DefaultEnemySoundsEnabled = true;
    public const float DefaultEnemySoundsVolume = 1f;
    public const bool DefaultTeammateSoundsEnabled = true;
    public const float DefaultTeammateSoundsVolume = 1f;

    public static readonly string[] GraphicsQualityNames = { "PERFORMANCE", "BALANCED", "QUALITY" };
    public static readonly string[] OutlineColorNames = { "Red", "Yellow", "Cyan", "Magenta", "White", "Green" };
    public static readonly Color[] OutlineColors =
    {
        new Color(1f, 0.28f, 0.28f, 1f),
        new Color(1f, 0.9f, 0.2f, 1f),
        new Color(0.2f, 0.85f, 1f, 1f),
        new Color(1f, 0.35f, 0.95f, 1f),
        Color.white,
        new Color(0.35f, 1f, 0.55f, 1f)
    };

    private static readonly int[] QualityLevelIndices = { 1, 2, 3 };

    public static float MouseSensitivity { get; private set; } = DefaultMouseSensitivity;
    public static float MasterVolume { get; private set; } = DefaultMasterVolume;
    public static bool KillFeedEnabled { get; private set; } = DefaultKillFeedEnabled;
    public static bool InvertY { get; private set; } = DefaultInvertY;
    public static int CrosshairIndex { get; private set; } = DefaultCrosshairIndex;
    public static int CrosshairColorIndex { get; private set; } = DefaultCrosshairColorIndex;
    public static int GraphicsQuality { get; private set; } = DefaultGraphicsQuality;
    public static bool ShadowsEnabled { get; private set; } = DefaultShadowsEnabled;
    public static float FieldOfView { get; private set; } = DefaultFieldOfView;
    public static bool EnemyOutlinesEnabled { get; private set; } = DefaultEnemyOutlinesEnabled;
    public static int EnemyOutlineColorIndex { get; private set; } = DefaultEnemyOutlineColorIndex;
    public static bool TeammateOutlinesEnabled { get; private set; } = DefaultTeammateOutlinesEnabled;
    public static int TeammateOutlineColorIndex { get; private set; } = DefaultTeammateOutlineColorIndex;
    public static bool GeneralSoundsEnabled { get; private set; } = DefaultGeneralSoundsEnabled;
    public static float GeneralSoundsVolume { get; private set; } = DefaultGeneralSoundsVolume;
    public static bool EnemySoundsEnabled { get; private set; } = DefaultEnemySoundsEnabled;
    public static float EnemySoundsVolume { get; private set; } = DefaultEnemySoundsVolume;
    public static bool TeammateSoundsEnabled { get; private set; } = DefaultTeammateSoundsEnabled;
    public static float TeammateSoundsVolume { get; private set; } = DefaultTeammateSoundsVolume;

    public static void Load()
    {
        GameKeybinds.Load();
        MouseSensitivity = PlayerPrefs.GetFloat(KeyMouseSensitivity, DefaultMouseSensitivity);
        MasterVolume = PlayerPrefs.GetFloat(KeyMasterVolume, DefaultMasterVolume);
        KillFeedEnabled = PlayerPrefs.GetInt(KeyKillFeedEnabled, DefaultKillFeedEnabled ? 1 : 0) == 1;
        InvertY = PlayerPrefs.GetInt(KeyInvertY, DefaultInvertY ? 1 : 0) == 1;
        CrosshairIndex = PlayerPrefs.GetInt(KeyCrosshairIndex, DefaultCrosshairIndex);
        CrosshairCatalog.EnsureInitialized();
        CrosshairIndex = Mathf.Clamp(CrosshairIndex, 0, Mathf.Max(0, CrosshairCatalog.Count - 1));
        CrosshairColorIndex = PlayerPrefs.GetInt(KeyCrosshairColorIndex, DefaultCrosshairColorIndex);
        CrosshairColorIndex = ClampColorIndex(CrosshairColorIndex);
        GraphicsQuality = PlayerPrefs.GetInt(KeyGraphicsQuality, DefaultGraphicsQuality);
        ShadowsEnabled = PlayerPrefs.GetInt(KeyShadowsEnabled, DefaultShadowsEnabled ? 1 : 0) == 1;
        FieldOfView = PlayerPrefs.GetFloat(KeyFieldOfView, DefaultFieldOfView);
        EnemyOutlinesEnabled = PlayerPrefs.GetInt(KeyEnemyOutlinesEnabled, DefaultEnemyOutlinesEnabled ? 1 : 0) == 1;
        EnemyOutlineColorIndex = PlayerPrefs.GetInt(KeyEnemyOutlineColorIndex, DefaultEnemyOutlineColorIndex);
        TeammateOutlinesEnabled = PlayerPrefs.GetInt(KeyTeammateOutlinesEnabled, DefaultTeammateOutlinesEnabled ? 1 : 0) == 1;
        TeammateOutlineColorIndex = PlayerPrefs.GetInt(KeyTeammateOutlineColorIndex, DefaultTeammateOutlineColorIndex);
        GeneralSoundsEnabled = PlayerPrefs.GetInt(KeyGeneralSoundsEnabled, DefaultGeneralSoundsEnabled ? 1 : 0) == 1;
        GeneralSoundsVolume = PlayerPrefs.GetFloat(KeyGeneralSoundsVolume, DefaultGeneralSoundsVolume);
        EnemySoundsEnabled = PlayerPrefs.GetInt(KeyEnemySoundsEnabled, DefaultEnemySoundsEnabled ? 1 : 0) == 1;
        EnemySoundsVolume = PlayerPrefs.GetFloat(KeyEnemySoundsVolume, DefaultEnemySoundsVolume);
        TeammateSoundsEnabled = PlayerPrefs.GetInt(KeyTeammateSoundsEnabled, DefaultTeammateSoundsEnabled ? 1 : 0) == 1;
        TeammateSoundsVolume = PlayerPrefs.GetFloat(KeyTeammateSoundsVolume, DefaultTeammateSoundsVolume);
        GraphicsQuality = Mathf.Clamp(GraphicsQuality, 0, GraphicsQualityNames.Length - 1);
        FieldOfView = Mathf.Clamp(FieldOfView, 70f, 110f);
        EnemyOutlineColorIndex = ClampOutlineColorIndex(EnemyOutlineColorIndex);
        TeammateOutlineColorIndex = ClampOutlineColorIndex(TeammateOutlineColorIndex);
        GeneralSoundsVolume = ClampVolume(GeneralSoundsVolume);
        EnemySoundsVolume = ClampVolume(EnemySoundsVolume);
        TeammateSoundsVolume = ClampVolume(TeammateSoundsVolume);
    }

    public static void Save()
    {
        PlayerPrefs.SetFloat(KeyMouseSensitivity, MouseSensitivity);
        PlayerPrefs.SetFloat(KeyMasterVolume, MasterVolume);
        PlayerPrefs.SetInt(KeyKillFeedEnabled, KillFeedEnabled ? 1 : 0);
        PlayerPrefs.SetInt(KeyInvertY, InvertY ? 1 : 0);
        PlayerPrefs.SetInt(KeyCrosshairIndex, CrosshairIndex);
        PlayerPrefs.SetInt(KeyCrosshairColorIndex, CrosshairColorIndex);
        PlayerPrefs.SetInt(KeyGraphicsQuality, GraphicsQuality);
        PlayerPrefs.SetInt(KeyShadowsEnabled, ShadowsEnabled ? 1 : 0);
        PlayerPrefs.SetFloat(KeyFieldOfView, FieldOfView);
        PlayerPrefs.SetInt(KeyEnemyOutlinesEnabled, EnemyOutlinesEnabled ? 1 : 0);
        PlayerPrefs.SetInt(KeyEnemyOutlineColorIndex, EnemyOutlineColorIndex);
        PlayerPrefs.SetInt(KeyTeammateOutlinesEnabled, TeammateOutlinesEnabled ? 1 : 0);
        PlayerPrefs.SetInt(KeyTeammateOutlineColorIndex, TeammateOutlineColorIndex);
        PlayerPrefs.SetInt(KeyGeneralSoundsEnabled, GeneralSoundsEnabled ? 1 : 0);
        PlayerPrefs.SetFloat(KeyGeneralSoundsVolume, GeneralSoundsVolume);
        PlayerPrefs.SetInt(KeyEnemySoundsEnabled, EnemySoundsEnabled ? 1 : 0);
        PlayerPrefs.SetFloat(KeyEnemySoundsVolume, EnemySoundsVolume);
        PlayerPrefs.SetInt(KeyTeammateSoundsEnabled, TeammateSoundsEnabled ? 1 : 0);
        PlayerPrefs.SetFloat(KeyTeammateSoundsVolume, TeammateSoundsVolume);
        PlayerPrefs.Save();
    }

    public static void SetMouseSensitivity(float value)
    {
        MouseSensitivity = Mathf.Clamp(value, 0.5f, 10f);
        Save();
        Apply();
    }

    public static void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        Save();
        Apply();
    }

    public static void SetKillFeedEnabled(bool enabled)
    {
        KillFeedEnabled = enabled;
        Save();
        Apply();
    }

    public static void SetInvertY(bool invert)
    {
        InvertY = invert;
        Save();
        Apply();
    }

    public static void SetCrosshairIndex(int index)
    {
        CrosshairCatalog.EnsureInitialized();
        CrosshairIndex = Mathf.Clamp(index, 0, Mathf.Max(0, CrosshairCatalog.Count - 1));
        Save();
        Apply();
    }

    public static void SetCrosshairColorIndex(int index)
    {
        CrosshairColorIndex = ClampColorIndex(index);
        Save();
        Apply();
    }

    public static Color GetCrosshairColor() => OutlineColors[CrosshairColorIndex];

    public static void SetGraphicsQuality(int index)
    {
        GraphicsQuality = Mathf.Clamp(index, 0, GraphicsQualityNames.Length - 1);
        Save();
        Apply();
    }

    public static void SetShadowsEnabled(bool enabled)
    {
        ShadowsEnabled = enabled;
        Save();
        Apply();
    }

    public static void SetFieldOfView(float value)
    {
        FieldOfView = Mathf.Clamp(value, 70f, 110f);
        Save();
        Apply();
    }

    public static void SetEnemyOutlinesEnabled(bool enabled)
    {
        EnemyOutlinesEnabled = enabled;
        Save();
        Apply();
    }

    public static void SetEnemyOutlineColorIndex(int index)
    {
        EnemyOutlineColorIndex = ClampOutlineColorIndex(index);
        Save();
        Apply();
    }

    public static void SetTeammateOutlinesEnabled(bool enabled)
    {
        TeammateOutlinesEnabled = enabled;
        Save();
        Apply();
    }

    public static void SetTeammateOutlineColorIndex(int index)
    {
        TeammateOutlineColorIndex = ClampOutlineColorIndex(index);
        Save();
        Apply();
    }

    public static void SetGeneralSoundsEnabled(bool enabled)
    {
        GeneralSoundsEnabled = enabled;
        Save();
        Apply();
    }

    public static void SetGeneralSoundsVolume(float value)
    {
        GeneralSoundsVolume = ClampVolume(value);
        Save();
        Apply();
    }

    public static void SetEnemySoundsEnabled(bool enabled)
    {
        EnemySoundsEnabled = enabled;
        Save();
        Apply();
    }

    public static void SetEnemySoundsVolume(float value)
    {
        EnemySoundsVolume = ClampVolume(value);
        Save();
        Apply();
    }

    public static void SetTeammateSoundsEnabled(bool enabled)
    {
        TeammateSoundsEnabled = enabled;
        Save();
        Apply();
    }

    public static void SetTeammateSoundsVolume(float value)
    {
        TeammateSoundsVolume = ClampVolume(value);
        Save();
        Apply();
    }

    public static Color GetEnemyOutlineColor() => OutlineColors[EnemyOutlineColorIndex];
    public static Color GetTeammateOutlineColor() => OutlineColors[TeammateOutlineColorIndex];

    private static int ClampOutlineColorIndex(int index) => ClampColorIndex(index);

    private static int ClampColorIndex(int index)
    {
        return Mathf.Clamp(index, 0, OutlineColors.Length - 1);
    }

    private static float ClampVolume(float value)
    {
        return Mathf.Clamp01(value);
    }

    public static void Apply()
    {
        AudioListener.volume = MasterVolume;
        ApplyGraphicsSettings();

        foreach (PlayerController controller in Object.FindObjectsOfType<PlayerController>())
        {
            PhotonView pv = controller.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
                controller.ApplyGameSettings();
        }

        HUDController hud = Object.FindObjectOfType<HUDController>();
        if (hud != null)
        {
            hud.ApplyKillFeedSetting();
            hud.ApplyCrosshairSetting();
        }

        PlayerOutlineHighlighter.RefreshAll();
    }

    private static void ApplyGraphicsSettings()
    {
        int qualityLevel = QualityLevelIndices[Mathf.Clamp(GraphicsQuality, 0, QualityLevelIndices.Length - 1)];
        QualitySettings.SetQualityLevel(qualityLevel, true);
        QualitySettings.shadows = ShadowsEnabled ? ShadowQuality.All : ShadowQuality.Disable;
    }
}
