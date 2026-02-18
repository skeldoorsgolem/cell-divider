using UnityEngine;

/// <summary>
/// Generates a cell texture procedurally at runtime — no art assets required.
///
/// The cell is painted as a `Texture2D` by evaluating each pixel:
///   - Outer membrane: thick wobbly ring with Perlin noise distortion
///   - Cytoplasm: semi-transparent greenish fill
///   - Nucleus: filled circle offset slightly from centre
///   - Organelles: small dots scattered in cytoplasm
///   - Highlight: small white specular spot
///
/// Call `Generate()` to get a Texture2D, then apply to a RawImage or SpriteRenderer.
/// </summary>
public static class ProceduralCellTexture
{
    /// <summary>
    /// Generate a cell texture.
    /// </summary>
    /// <param name="size">Texture resolution (power of 2 recommended, e.g. 256)</param>
    /// <param name="seed">Seed for noise variation — each cell type gets a unique look</param>
    /// <param name="membraneColor">Outer ring colour</param>
    /// <param name="cytoplasmColor">Inner fill colour</param>
    /// <param name="nucleusColor">Nucleus colour</param>
    public static Texture2D Generate(
        int   size          = 256,
        int   seed          = 0,
        Color? membraneColor = null,
        Color? cytoplasmColor= null,
        Color? nucleusColor  = null)
    {
        Color membrane  = membraneColor  ?? new Color(0.15f, 0.55f, 0.25f, 1f);
        Color cytoplasm = cytoplasmColor ?? new Color(0.25f, 0.70f, 0.35f, 0.85f);
        Color nucleus   = nucleusColor   ?? new Color(0.10f, 0.35f, 0.55f, 1f);

        // Use seed to offset Perlin samples — deterministic per cell type
        float noiseOffX = seed * 3.7f;
        float noiseOffY = seed * 5.3f;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        float half    = size * 0.5f;
        float cellR   = half * 0.88f;   // outer membrane radius
        float innerR  = cellR * 0.80f;  // cytoplasm inner edge
        float memW    = cellR - innerR;  // membrane width

        // Nucleus: slightly off-centre, ~30% of cell radius
        float nucR   = cellR * 0.30f;
        float nucOX  = cellR * 0.10f;   // offset x
        float nucOY  = cellR * 0.08f;   // offset y

        var pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float px = x - half;
            float py = y - half;
            float dist = Mathf.Sqrt(px * px + py * py);

            // Normalised angle for Perlin wobble
            float angle = Mathf.Atan2(py, px);

            // Perlin noise distorts the membrane boundary radially
            // Sample at a point on the unit circle so noise is angular
            float noiseX = Mathf.Cos(angle) * 0.5f + 0.5f + noiseOffX;
            float noiseY = Mathf.Sin(angle) * 0.5f + 0.5f + noiseOffY;
            float wobble = Mathf.PerlinNoise(noiseX, noiseY) * memW * 0.7f;

            float outerEdge = cellR  + wobble;
            float innerEdge = innerR + wobble * 0.6f;

            Color pixel = new Color(0, 0, 0, 0);  // transparent by default

            if (dist <= innerEdge)
            {
                // ── Inside cytoplasm ───────────────────────────────────────

                // Nucleus
                float dNuc = Mathf.Sqrt((px - nucOX) * (px - nucOX) +
                                        (py - nucOY) * (py - nucOY));
                if (dNuc <= nucR)
                {
                    // Nucleus gradient: darker at edge
                    float t    = dNuc / nucR;
                    float n    = Mathf.PerlinNoise(px * 0.04f + noiseOffX,
                                                   py * 0.04f + noiseOffY);
                    Color nucEdge = Color.Lerp(nucleus, nucleus * 0.6f, t);
                    pixel = Color.Lerp(nucEdge, nucEdge * (0.9f + n * 0.2f), 0.3f);
                    pixel.a = 1f;
                }
                else
                {
                    // Cytoplasm with subtle Perlin texture
                    float cytoNoise = Mathf.PerlinNoise(px * 0.06f + noiseOffX + 10f,
                                                        py * 0.06f + noiseOffY + 10f);
                    pixel = cytoplasm;
                    pixel.r += (cytoNoise - 0.5f) * 0.06f;
                    pixel.g += (cytoNoise - 0.5f) * 0.06f;
                    pixel.a = cytoplasm.a;

                    // Organelles: small mitochondria-like dots
                    pixel = DrawOrganelles(pixel, px, py, cellR, seed, noiseOffX, noiseOffY);
                }
            }
            else if (dist <= outerEdge)
            {
                // ── Membrane ring ──────────────────────────────────────────
                float t       = (dist - innerEdge) / Mathf.Max(outerEdge - innerEdge, 0.01f);
                float memNoise= Mathf.PerlinNoise(px * 0.08f + noiseOffX + 5f,
                                                  py * 0.08f + noiseOffY + 5f);
                Color inner   = Color.Lerp(cytoplasm, membrane, 0.5f);
                pixel = Color.Lerp(inner, membrane * (0.85f + memNoise * 0.3f), t);
                pixel.a = Mathf.Lerp(0.9f, 1f, t);
            }

            // ── Specular highlight ─────────────────────────────────────────
            // Small white dot in upper-left quadrant
            float hlX  = -cellR * 0.35f;
            float hlY  =  cellR * 0.35f;
            float hlD  = Mathf.Sqrt((px - hlX) * (px - hlX) + (py - hlY) * (py - hlY));
            float hlR  = cellR * 0.12f;
            if (hlD < hlR && dist < innerEdge)
            {
                float hlT = 1f - hlD / hlR;
                hlT = hlT * hlT;  // sharpen
                pixel = Color.Lerp(pixel, Color.white, hlT * 0.45f);
            }

            pixels[y * size + x] = pixel;
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    // Scatter small elliptical organelle dots in the cytoplasm
    private static Color DrawOrganelles(Color baseColor, float px, float py,
                                        float cellR, int seed,
                                        float ox, float oy)
    {
        // Fixed organelle positions derived from seed
        System.Random rng = new System.Random(seed + 42);
        int count = 5 + seed % 4;

        for (int i = 0; i < count; i++)
        {
            float angle  = (float)(rng.NextDouble() * Mathf.PI * 2);
            float radius = (float)(rng.NextDouble() * 0.5f + 0.1f) * cellR * 0.55f;
            float cx     = Mathf.Cos(angle) * radius;
            float cy     = Mathf.Sin(angle) * radius;
            float rA     = cellR * 0.06f;   // semi-axis A
            float rB     = cellR * 0.035f;  // semi-axis B
            float rot    = (float)(rng.NextDouble() * Mathf.PI);

            // Rotated ellipse distance
            float cosR = Mathf.Cos(rot), sinR = Mathf.Sin(rot);
            float lx = cosR * (px - cx) + sinR * (py - cy);
            float ly = -sinR * (px - cx) + cosR * (py - cy);
            float ellipse = (lx * lx) / (rA * rA) + (ly * ly) / (rB * rB);

            if (ellipse < 1f)
            {
                float t = 1f - ellipse;
                Color orgColor = new Color(0.15f, 0.50f, 0.65f, 0.9f);
                baseColor = Color.Lerp(baseColor, orgColor, t * 0.7f);
            }
        }

        return baseColor;
    }

    /// <summary>
    /// Creates a Unity Sprite from the generated texture.
    /// </summary>
    public static Sprite GenerateSprite(int size = 256, int seed = 0,
                                        Color? membraneColor  = null,
                                        Color? cytoplasmColor = null,
                                        Color? nucleusColor   = null)
    {
        Texture2D tex = Generate(size, seed, membraneColor, cytoplasmColor, nucleusColor);
        return Sprite.Create(tex,
                             new Rect(0, 0, size, size),
                             new Vector2(0.5f, 0.5f),
                             size);   // pixels per unit = texture size → 1 unit wide
    }
}
