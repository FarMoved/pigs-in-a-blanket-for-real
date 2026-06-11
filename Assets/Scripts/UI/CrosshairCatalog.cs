using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedural crosshair shapes for the settings catalog and in-game HUD.
/// </summary>
public static class CrosshairCatalog
{
    public readonly struct Entry
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly Sprite Sprite;
        public readonly Vector2 HudSize;

        public Entry(string id, string displayName, Sprite sprite, Vector2 hudSize)
        {
            Id = id;
            DisplayName = displayName;
            Sprite = sprite;
            HudSize = hudSize;
        }
    }

    private static readonly List<Entry> Entries = new List<Entry>();
    private static bool initialized;

    public static IReadOnlyList<Entry> All
    {
        get
        {
            EnsureInitialized();
            return Entries;
        }
    }

    public static int Count
    {
        get
        {
            EnsureInitialized();
            return Entries.Count;
        }
    }

    public static void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;

        const int texSize = 64;
        Color white = Color.white;
        Color clear = new Color(0, 0, 0, 0);

        Add("dot", "Dot", DrawDot(white), new Vector2(10f, 10f));
        Add("plus", "Plus", DrawPlus(texSize, white, clear, gap: 0, thickness: 2), new Vector2(18f, 18f));
        Add("plus_gap", "Gap Plus", DrawPlus(texSize, white, clear, gap: 4, thickness: 2), new Vector2(20f, 20f));
        Add("cross", "X Cross", DrawX(texSize, white, clear, thickness: 2), new Vector2(18f, 18f));
        Add("circle", "Circle", DrawCircle(texSize, white, clear, thickness: 2, radius: 22), new Vector2(24f, 24f));
        Add("circle_dot", "Circle Dot", DrawCircleDot(texSize, white, clear), new Vector2(24f, 24f));
        Add("brackets", "Brackets", DrawBrackets(texSize, white, clear, arm: 10, thickness: 2), new Vector2(22f, 22f));
        Add("t_shape", "T-Shape", DrawTShape(texSize, white, clear, thickness: 2), new Vector2(18f, 18f));
    }

    public static Entry Get(int index)
    {
        EnsureInitialized();
        if (Entries.Count == 0)
            return default;
        index = Mathf.Clamp(index, 0, Entries.Count - 1);
        return Entries[index];
    }

    public static int IndexOfId(string id)
    {
        EnsureInitialized();
        for (int i = 0; i < Entries.Count; i++)
        {
            if (Entries[i].Id == id)
                return i;
        }
        return 0;
    }

    private static void Add(string id, string displayName, Texture2D tex, Vector2 hudSize)
    {
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        Entries.Add(new Entry(id, displayName, sprite, hudSize));
    }

    private static Texture2D NewTexture(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        return tex;
    }

    private static void Fill(Texture2D tex, Color color)
    {
        for (int y = 0; y < tex.height; y++)
            for (int x = 0; x < tex.width; x++)
                tex.SetPixel(x, y, color);
    }

    private static void Plot(Texture2D tex, int x, int y, Color color)
    {
        if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
            tex.SetPixel(x, y, color);
    }

    private static void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, int thickness, Color color)
    {
        int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy, e2;
        while (true)
        {
            for (int oy = -thickness; oy <= thickness; oy++)
                for (int ox = -thickness; ox <= thickness; ox++)
                    Plot(tex, x0 + ox, y0 + oy, color);
            if (x0 == x1 && y0 == y1) break;
            e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    private static Texture2D DrawDot(Color fg)
    {
        // Fully opaque square texture so the HUD size maps 1:1 to visible pixels.
        const int squareSize = 16;
        var tex = new Texture2D(squareSize, squareSize, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        Fill(tex, fg);
        tex.Apply();
        return tex;
    }

    private static Texture2D DrawPlus(int size, Color fg, Color bg, int gap, int thickness)
    {
        var tex = NewTexture(size);
        Fill(tex, bg);
        int c = size / 2;
        DrawLine(tex, c, gap, c, c - gap - 1, thickness, fg);
        DrawLine(tex, c, c + gap + 1, c, size - gap - 1, thickness, fg);
        DrawLine(tex, gap, c, c - gap - 1, c, thickness, fg);
        DrawLine(tex, c + gap + 1, c, size - gap - 1, c, thickness, fg);
        tex.Apply();
        return tex;
    }

    private static Texture2D DrawX(int size, Color fg, Color bg, int thickness)
    {
        var tex = NewTexture(size);
        Fill(tex, bg);
        int pad = 10;
        DrawLine(tex, pad, pad, size - pad, size - pad, thickness, fg);
        DrawLine(tex, size - pad, pad, pad, size - pad, thickness, fg);
        tex.Apply();
        return tex;
    }

    private static Texture2D DrawCircle(int size, Color fg, Color bg, int thickness, int radius)
    {
        var tex = NewTexture(size);
        Fill(tex, bg);
        int c = size / 2;
        const int segments = 96;
        for (int i = 0; i < segments; i++)
        {
            float a0 = (i / (float)segments) * Mathf.PI * 2f;
            float a1 = ((i + 1) / (float)segments) * Mathf.PI * 2f;
            int x0 = c + Mathf.RoundToInt(Mathf.Cos(a0) * radius);
            int y0 = c + Mathf.RoundToInt(Mathf.Sin(a0) * radius);
            int x1 = c + Mathf.RoundToInt(Mathf.Cos(a1) * radius);
            int y1 = c + Mathf.RoundToInt(Mathf.Sin(a1) * radius);
            DrawLine(tex, x0, y0, x1, y1, thickness, fg);
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D DrawCircleDot(int size, Color fg, Color bg)
    {
        var tex = DrawCircle(size, fg, bg, 2, 22);
        int c = size / 2;
        for (int dy = -2; dy <= 2; dy++)
            for (int dx = -2; dx <= 2; dx++)
                Plot(tex, c + dx, c + dy, fg);
        tex.Apply();
        return tex;
    }

    private static Texture2D DrawBrackets(int size, Color fg, Color bg, int arm, int thickness)
    {
        var tex = NewTexture(size);
        Fill(tex, bg);
        int inset = 12;
        int outX = inset + arm;
        int outY = inset + arm;
        // Top-left
        DrawLine(tex, inset, inset, outX, inset, thickness, fg);
        DrawLine(tex, inset, inset, inset, outY, thickness, fg);
        // Top-right
        DrawLine(tex, size - inset, inset, size - outX, inset, thickness, fg);
        DrawLine(tex, size - inset, inset, size - inset, outY, thickness, fg);
        // Bottom-left
        DrawLine(tex, inset, size - inset, outX, size - inset, thickness, fg);
        DrawLine(tex, inset, size - inset, inset, size - outY, thickness, fg);
        // Bottom-right
        DrawLine(tex, size - inset, size - inset, size - outX, size - inset, thickness, fg);
        DrawLine(tex, size - inset, size - inset, size - inset, size - outY, thickness, fg);
        tex.Apply();
        return tex;
    }

    private static Texture2D DrawTShape(int size, Color fg, Color bg, int thickness)
    {
        var tex = NewTexture(size);
        Fill(tex, bg);
        int c = size / 2;
        DrawLine(tex, c - 10, c + 2, c + 10, c + 2, thickness, fg);
        DrawLine(tex, c, c + 2, c, size - 10, thickness, fg);
        tex.Apply();
        return tex;
    }
}
