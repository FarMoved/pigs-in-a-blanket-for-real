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

    public const float DefaultMouseSensitivity = 2f;
    public const float DefaultMasterVolume = 1f;
    public const bool DefaultKillFeedEnabled = true;
    public const bool DefaultInvertY = false;

    public static float MouseSensitivity { get; private set; } = DefaultMouseSensitivity;
    public static float MasterVolume { get; private set; } = DefaultMasterVolume;
    public static bool KillFeedEnabled { get; private set; } = DefaultKillFeedEnabled;
    public static bool InvertY { get; private set; } = DefaultInvertY;

    public static void Load()
    {
        MouseSensitivity = PlayerPrefs.GetFloat(KeyMouseSensitivity, DefaultMouseSensitivity);
        MasterVolume = PlayerPrefs.GetFloat(KeyMasterVolume, DefaultMasterVolume);
        KillFeedEnabled = PlayerPrefs.GetInt(KeyKillFeedEnabled, DefaultKillFeedEnabled ? 1 : 0) == 1;
        InvertY = PlayerPrefs.GetInt(KeyInvertY, DefaultInvertY ? 1 : 0) == 1;
    }

    public static void Save()
    {
        PlayerPrefs.SetFloat(KeyMouseSensitivity, MouseSensitivity);
        PlayerPrefs.SetFloat(KeyMasterVolume, MasterVolume);
        PlayerPrefs.SetInt(KeyKillFeedEnabled, KillFeedEnabled ? 1 : 0);
        PlayerPrefs.SetInt(KeyInvertY, InvertY ? 1 : 0);
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

    public static void Apply()
    {
        AudioListener.volume = MasterVolume;

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
        }
    }
}
