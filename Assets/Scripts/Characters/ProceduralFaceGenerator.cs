using UnityEngine;

/// <summary>
/// Generates bold, readable 64x64 face textures used on the character head
/// quads. Features are deliberately high-contrast so a face is recognisable on
/// a low-poly head at gameplay distance and at night.
/// </summary>
public static class ProceduralFaceGenerator
{
    public static Texture2D GenerateSurvivorFace(string survivorName)
    {
        int size = 64;
        var p = NewCanvas(size, SurvivorSkin(survivorName), out Texture2D tex);

        // eyes: whites + dark pupils (contrast)
        DrawCircle(p, size, size / 2 - 9, size / 2 + 6, 6, new Color(0.95f, 0.95f, 0.93f));
        DrawCircle(p, size, size / 2 + 9, size / 2 + 6, 6, new Color(0.95f, 0.95f, 0.93f));
        DrawCircle(p, size, size / 2 - 9, size / 2 + 6, 3, new Color(0.08f, 0.06f, 0.06f));
        DrawCircle(p, size, size / 2 + 9, size / 2 + 6, 3, new Color(0.08f, 0.06f, 0.06f));

        // eyebrows
        FillRect(p, size, size / 2 - 14, size / 2 + 13, 6, 2, new Color(0.16f, 0.10f, 0.06f));
        FillRect(p, size, size / 2 + 8, size / 2 + 13, 6, 2, new Color(0.16f, 0.10f, 0.06f));

        // nose
        FillRect(p, size, size / 2 - 1, size / 2 + 1, 2, 5, Shade(SurvivorSkin(survivorName), 0.82f));

        // mouth
        FillRect(p, size, size / 2 - 6, size / 2 - 9, 12, 2, new Color(0.45f, 0.20f, 0.20f));

        Commit(tex, p);
        return tex;
    }

    public static Texture2D GenerateWitchFace(WitchType witchType)
    {
        int size = 64;
        Color skin = new Color(0.78f, 0.74f, 0.72f);
        Color iris = new Color(0.85f, 0.9f, 0.95f);
        Color pupil = new Color(0.05f, 0.05f, 0.08f);

        switch (witchType)
        {
            case WitchType.Humanoid: skin = new Color(0.80f, 0.79f, 0.77f); iris = new Color(0.85f, 0.95f, 1f); break;
            case WitchType.Winged:   skin = new Color(0.85f, 0.80f, 0.74f); iris = new Color(1f, 0.85f, 0.5f); break;
            case WitchType.Hound:    skin = new Color(0.42f, 0.32f, 0.24f); iris = new Color(1f, 0.85f, 0.25f); break;
            case WitchType.Crawler:  skin = new Color(0.62f, 0.52f, 0.46f); iris = new Color(1f, 0.3f, 0.2f); break;
            case WitchType.Tall:     skin = new Color(0.90f, 0.86f, 0.82f); iris = new Color(0.85f, 1f, 0.85f); break;
            case WitchType.Final:    skin = new Color(0.72f, 0.66f, 0.70f); iris = new Color(1f, 0.25f, 0.3f); break;
        }

        var p = NewCanvas(size, skin, out Texture2D tex);

        // sunken dark eye sockets
        DrawCircle(p, size, size / 2 - 11, size / 2 + 6, 8, Shade(skin, 0.35f));
        DrawCircle(p, size, size / 2 + 11, size / 2 + 6, 8, Shade(skin, 0.35f));
        // glowing irises + black pupils
        DrawCircle(p, size, size / 2 - 11, size / 2 + 6, 5, iris);
        DrawCircle(p, size, size / 2 + 11, size / 2 + 6, 5, iris);
        DrawCircle(p, size, size / 2 - 11, size / 2 + 6, 2, pupil);
        DrawCircle(p, size, size / 2 + 11, size / 2 + 6, 2, pupil);

        // heavy brows
        FillRect(p, size, size / 2 - 16, size / 2 + 14, 8, 3, Shade(skin, 0.18f));
        FillRect(p, size, size / 2 + 8, size / 2 + 14, 8, 3, Shade(skin, 0.18f));

        // nose shadow
        FillRect(p, size, size / 2 - 1, size / 2 + 1, 2, 6, Shade(skin, 0.55f));

        // mouth
        if (witchType == WitchType.Tall) // Sandy Vill - wide grin
        {
            FillRect(p, size, size / 2 - 14, size / 2 - 11, 28, 3, new Color(0.30f, 0.05f, 0.07f));
            FillRect(p, size, size / 2 - 12, size / 2 - 8, 24, 1, new Color(0.9f, 0.85f, 0.8f)); // teeth
        }
        else
        {
            FillRect(p, size, size / 2 - 7, size / 2 - 10, 14, 3, new Color(0.35f, 0.06f, 0.09f));
        }

        Commit(tex, p);
        return tex;
    }

    // --------------------------------------------------------------- helpers

    private static Color SurvivorSkin(string name)
    {
        switch (CharacterLore.OfficialName(name))
        {
            case "Luis Padilla": return new Color(0.90f, 0.76f, 0.64f);
            case "Mick Richards": return new Color(0.64f, 0.47f, 0.35f);
            case "Josh Garcia":  return new Color(0.94f, 0.80f, 0.68f);
            case "Vincent Reid": return new Color(0.76f, 0.58f, 0.46f);
            default:             return new Color(0.88f, 0.74f, 0.62f);
        }
    }

    private static Color Shade(Color c, float f) { return new Color(c.r * f, c.g * f, c.b * f); }

    private static Color[] NewCanvas(int size, Color fill, out Texture2D tex)
    {
        tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        var p = new Color[size * size];
        for (int i = 0; i < p.Length; i++) p[i] = fill;
        return p;
    }

    private static void Commit(Texture2D tex, Color[] p)
    {
        tex.SetPixels(p);
        tex.Apply();
    }

    private static void DrawCircle(Color[] pixels, int size, int cx, int cy, int radius, Color color)
    {
        for (int x = -radius; x <= radius; x++)
            for (int y = -radius; y <= radius; y++)
                if (x * x + y * y <= radius * radius)
                    SetPixel(pixels, size, cx + x, cy + y, color);
    }

    private static void FillRect(Color[] pixels, int size, int x0, int y0, int w, int h, Color color)
    {
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                SetPixel(pixels, size, x0 + x, y0 + y, color);
    }

    private static void SetPixel(Color[] pixels, int size, int x, int y, Color color)
    {
        if (x >= 0 && x < size && y >= 0 && y < size)
            pixels[y * size + x] = color;
    }
}
