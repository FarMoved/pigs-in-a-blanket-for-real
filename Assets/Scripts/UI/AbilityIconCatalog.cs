using UnityEngine;

/// <summary>
/// Procedural placeholder icons for ability HUD slots.
/// </summary>
public static class AbilityIconCatalog
{
    public enum AbilityIcon
    {
        SizzleStep,
        WallSkim,
        LaunchPatty
    }

    private static Sprite[] sprites;
    private static bool initialized;

    public static void EnsureInitialized()
    {
        if (initialized)
            return;

        sprites = new Sprite[3];
        sprites[(int)AbilityIcon.SizzleStep] = CreateIcon(new Color(1f, 0.45f, 0.12f, 1f), new Color(1f, 0.78f, 0.2f, 1f));
        sprites[(int)AbilityIcon.WallSkim] = CreateIcon(new Color(0.1f, 0.55f, 0.95f, 1f), new Color(0.35f, 0.85f, 1f, 1f));
        sprites[(int)AbilityIcon.LaunchPatty] = CreateIcon(new Color(0.55f, 0.85f, 0.2f, 1f), new Color(0.75f, 1f, 0.35f, 1f));
        initialized = true;
    }

    public static Sprite Get(AbilityIcon icon)
    {
        EnsureInitialized();
        int index = (int)icon;
        return index >= 0 && index < sprites.Length ? sprites[index] : null;
    }

    private static Sprite CreateIcon(Color baseColor, Color accentColor)
    {
        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color clear = new Color(0f, 0f, 0f, 0f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f) / size;
                float ny = (y + 0.5f) / size;
                float dist = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.5f, 0.5f));
                if (dist > 0.46f)
                {
                    tex.SetPixel(x, y, clear);
                    continue;
                }

                float ring = Mathf.SmoothStep(0.34f, 0.42f, dist);
                Color pixel = Color.Lerp(accentColor, baseColor, ring);
                if (dist < 0.16f)
                    pixel = Color.Lerp(pixel, Color.white, 0.35f);
                tex.SetPixel(x, y, pixel);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
