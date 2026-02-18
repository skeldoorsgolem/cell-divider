using UnityEngine;

/// <summary>
/// Generates a unique organic blob sprite for each tech node.
/// Each node ID is hashed to a seed, producing a deterministic but varied shape.
///
/// The blob is a polygon where each vertex radius is modulated by summed sine waves
/// (a standard technique for organic silhouettes), then the interior is filled
/// with a radial gradient and a subtle Perlin texture.
/// </summary>
public static class ProceduralNodeSprite
{
    /// <summary>
    /// Generate a blob sprite for a tech node.
    /// </summary>
    /// <param name="nodeId">The node's string ID — hashed to produce unique shape</param>
    /// <param name="size">Texture resolution (128 is plenty for a small node)</param>
    /// <param name="state">Controls colour scheme</param>
    public static Texture2D Generate(string nodeId, int size = 128, TechNodeState state = TechNodeState.Locked)
    {
        int seed = Mathf.Abs(nodeId.GetHashCode());

        // Colour scheme by state
        Color fill, edge, glow;
        switch (state)
        {
            case TechNodeState.Available:
                fill = new Color(0.30f, 0.65f, 0.30f, 0.95f);
                edge = new Color(0.15f, 0.85f, 0.35f, 1f);
                glow = new Color(0.5f,  1f,    0.5f,  0.4f);
                break;
            case TechNodeState.Unlocked:
                fill = new Color(0.15f, 0.80f, 0.45f, 1f);
                edge = new Color(0.10f, 1f,    0.55f, 1f);
                glow = new Color(0.3f,  1f,    0.6f,  0.6f);
                break;
            default: // Locked
                fill = new Color(0.22f, 0.25f, 0.28f, 0.9f);
                edge = new Color(0.30f, 0.35f, 0.40f, 1f);
                glow = new Color(0f,    0f,    0f,    0f);
                break;
        }

        // Build blob boundary: sum of sine waves, each with random amplitude/frequency
        System.Random rng = new System.Random(seed);
        int waveCount  = 4 + seed % 3;
        float[] waveAmp   = new float[waveCount];
        float[] waveFreq  = new float[waveCount];
        float[] wavePhase = new float[waveCount];
        float totalAmp = 0f;

        for (int w = 0; w < waveCount; w++)
        {
            waveAmp[w]   = (float)(rng.NextDouble() * 0.12f + 0.03f);
            waveFreq[w]  = (float)(rng.NextInt(2, 7));
            wavePhase[w] = (float)(rng.NextDouble() * Mathf.PI * 2f);
            totalAmp    += waveAmp[w];
        }

        float noiseOX = (float)(rng.NextDouble() * 100f);
        float noiseOY = (float)(rng.NextDouble() * 100f);

        float half  = size * 0.5f;
        float baseR = half * 0.78f;

        var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float px    = x - half;
            float py    = y - half;
            float dist  = Mathf.Sqrt(px * px + py * py);
            float angle = Mathf.Atan2(py, px);

            // Blob radius at this angle
            float blobR = baseR;
            for (int w = 0; w < waveCount; w++)
                blobR += waveAmp[w] * baseR * Mathf.Sin(waveFreq[w] * angle + wavePhase[w]);

            float edgeWidth = baseR * 0.10f;
            Color pixel = new Color(0, 0, 0, 0);

            if (dist < blobR)
            {
                float t = dist / blobR;  // 0 = centre, 1 = edge

                // Interior Perlin texture
                float n = Mathf.PerlinNoise(px * 0.06f + noiseOX, py * 0.06f + noiseOY);

                // Radial gradient: brighter at centre
                Color centre = Color.Lerp(fill * 1.3f, fill, t * t);
                pixel   = centre;
                pixel.r += (n - 0.5f) * 0.05f;
                pixel.g += (n - 0.5f) * 0.05f;
                pixel.a  = fill.a;

                // Edge glow band
                if (dist > blobR - edgeWidth)
                {
                    float et = (dist - (blobR - edgeWidth)) / edgeWidth;
                    pixel = Color.Lerp(pixel, edge, et);
                    pixel.a = Mathf.Lerp(fill.a, 1f, et);
                }

                // Outer soft glow (alpha blend outside the edge)
                if (glow.a > 0 && dist > blobR - edgeWidth * 0.5f)
                {
                    float gt = (dist - (blobR - edgeWidth * 0.5f)) / (edgeWidth * 0.5f);
                    pixel = Color.Lerp(pixel, glow, gt * glow.a);
                }

                // Small specular highlight
                float hlX = -baseR * 0.25f, hlY = baseR * 0.30f;
                float hlD = Mathf.Sqrt((px - hlX) * (px - hlX) + (py - hlY) * (py - hlY));
                if (hlD < baseR * 0.15f)
                {
                    float ht = 1f - hlD / (baseR * 0.15f);
                    pixel = Color.Lerp(pixel, Color.white, ht * ht * 0.35f);
                }
            }

            pixels[y * size + x] = pixel;
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    public static Sprite GenerateSprite(string nodeId, int size = 128,
                                        TechNodeState state = TechNodeState.Locked)
    {
        var tex = Generate(nodeId, size, state);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}

// Extension to make System.Random more usable
internal static class RandomExtensions
{
    public static int NextInt(this System.Random rng, int min, int max)
        => rng.Next(min, max);
}
