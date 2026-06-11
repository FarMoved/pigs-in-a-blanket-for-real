using UnityEngine;

/// <summary>
/// Resolves effective playback volume from player audio preferences.
/// Call from weapon/UI audio code once sounds are wired up.
/// </summary>
public static class GameAudio
{
    public enum Category
    {
        General,
        Enemy,
        Teammate
    }

    /// <summary>Final linear volume in 0–1 after master and category settings.</summary>
    public static float GetEffectiveVolume(Category category)
    {
        float master = GameSettings.MasterVolume;
        switch (category)
        {
            case Category.Enemy:
                return master * (GameSettings.EnemySoundsEnabled ? GameSettings.EnemySoundsVolume : 0f);
            case Category.Teammate:
                return master * (GameSettings.TeammateSoundsEnabled ? GameSettings.TeammateSoundsVolume : 0f);
            default:
                return master * (GameSettings.GeneralSoundsEnabled ? GameSettings.GeneralSoundsVolume : 0f);
        }
    }

    /// <summary>Volume for a sound tied to another player (enemy vs teammate).</summary>
    public static float GetEffectiveVolumeForPlayer(bool isTeammate, bool isEnemy)
    {
        if (isTeammate)
            return GetEffectiveVolume(Category.Teammate);
        if (isEnemy)
            return GetEffectiveVolume(Category.Enemy);
        return GetEffectiveVolume(Category.General);
    }

    public static void PlayOneShot(AudioSource source, AudioClip clip, Category category)
    {
        if (source == null || clip == null) return;
        float volume = GetEffectiveVolume(category);
        if (volume <= 0f) return;
        source.PlayOneShot(clip, volume);
    }

    public static void PlayClipAtPoint(AudioClip clip, Vector3 position, Category category)
    {
        if (clip == null) return;
        float volume = GetEffectiveVolume(category);
        if (volume <= 0f) return;
        AudioSource.PlayClipAtPoint(clip, position, volume);
    }
}
