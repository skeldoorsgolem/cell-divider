using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Title screen controller.
///
/// Scene setup:
///   TitleScreen (Panel, full-screen)
///     Background         ← RawImage — ProceduralBackground fills this at Start()
///     TitleLabel         ← TextMeshProUGUI "CELL DIVIDER"
///     SubtitleLabel      ← TextMeshProUGUI "divide. evolve. dominate."
///     AnimatedCell       ← GameObject with SquishyCell (auto-pulses gently)
///     PlayButton         ← Button → calls TitleScreen.OnPlayClicked()
///     SettingsButton     ← Button → calls TitleScreen.OnSettingsClicked()
///     VersionLabel       ← TextMeshProUGUI "v0.1"
/// </summary>
public class TitleScreen : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RawImage         backgroundImage;
    [SerializeField] private SquishyCell      titleCell;
    [SerializeField] private TextMeshProUGUI  titleLabel;
    [SerializeField] private CanvasGroup      canvasGroup;

    [Header("Animation")]
    [SerializeField] private float pulseSpeed  = 0.8f;   // idle cell pulse rate
    [SerializeField] private float pulseAmount = 0.04f;  // gentle scale breathe

    private Vector3 _cellBaseScale;
    private float   _pulseTime;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Start()
    {
        // Generate background: dark organic texture
        if (backgroundImage != null)
            backgroundImage.texture = GenerateBackground(512);

        // Store base scale for pulse
        if (titleCell != null)
            _cellBaseScale = titleCell.transform.localScale;

        // Fade in
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            StartCoroutine(FadeIn(0.6f));
        }
    }

    private void Update()
    {
        // Gentle idle breathe on the title cell
        if (titleCell != null)
        {
            _pulseTime += Time.deltaTime * pulseSpeed;
            float s = 1f + Mathf.Sin(_pulseTime * Mathf.PI * 2f) * pulseAmount;
            titleCell.transform.localScale = _cellBaseScale * s;
        }
    }

    // ── Buttons ──────────────────────────────────────────────────────────────

    public void OnPlayClicked()
    {
        AudioSynth.Instance?.PlayClick();
        StartCoroutine(FadeOut(0.3f, () => ScreenManager.Instance.ShowGame()));
    }

    public void OnSettingsClicked()
    {
        AudioSynth.Instance?.PlayClick();
        ScreenManager.Instance.ShowSettings();
    }

    // ── Background generation ────────────────────────────────────────────────

    /// Generates a dark biology-themed background texture using Perlin noise.
    private static Texture2D GenerateBackground(int size)
    {
        var tex    = new Texture2D(size, size, TextureFormat.RGB24, false);
        var pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float nx = x / (float)size * 3f;
            float ny = y / (float)size * 3f;

            // Layer two octaves of Perlin noise for organic feel
            float n = Mathf.PerlinNoise(nx,       ny)       * 0.6f
                    + Mathf.PerlinNoise(nx * 2.1f, ny * 2.1f) * 0.4f;

            // Dark green palette
            float r = n * 0.04f;
            float g = n * 0.12f + 0.02f;
            float b = n * 0.06f;

            pixels[y * size + x] = new Color(r, g, b);
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    // ── Transitions ──────────────────────────────────────────────────────────

    private IEnumerator FadeIn(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = t / duration;
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOut(float duration, System.Action onComplete)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = 1f - (t / duration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        onComplete?.Invoke();
    }
}
