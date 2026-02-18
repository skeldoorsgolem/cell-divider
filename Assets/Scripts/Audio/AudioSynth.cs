using System;
using UnityEngine;

/// <summary>
/// Generates AudioClips procedurally at runtime — no sound files required.
///
/// All sounds are built by filling a float[] buffer with synthesised waveforms,
/// then calling AudioClip.Create(). The clips are generated once and cached.
///
/// Sounds:
///   Click     — soft squelchy "bloop" (sine + slight pitch drop, short envelope)
///   Unlock    — ascending chime (two sine partials, quick attack, long release)
///   Purchase  — satisfying low "thud" (sine at 80Hz, short decay)
///   AutoTick  — subtle wet "pip" (triangle wave, very short)
///   Milestone — bright ascending arpeggio (three notes in sequence)
///
/// Usage:
///   AudioSynth.Instance.PlayClick();
///   AudioSynth.Instance.PlayUnlock();
///   AudioSynth.Instance.PlayPurchase();
/// </summary>
public class AudioSynth : MonoBehaviour
{
    public static AudioSynth Instance { get; private set; }

    [Range(0f, 1f)] public float masterVolume = 0.7f;
    [Range(0f, 1f)] public float sfxVolume    = 1.0f;

    private const int SampleRate = 44100;

    private AudioClip _clipClick;
    private AudioClip _clipUnlock;
    private AudioClip _clipPurchase;
    private AudioClip _clipAutoTick;
    private AudioClip _clipMilestone;

    private AudioSource _source;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;

        GenerateAll();
    }

    // ── Public play methods ──────────────────────────────────────────────────

    public void PlayClick()    => PlayClip(_clipClick,     0.55f);
    public void PlayUnlock()   => PlayClip(_clipUnlock,    0.80f);
    public void PlayPurchase() => PlayClip(_clipPurchase,  0.65f);
    public void PlayAutoTick() => PlayClip(_clipAutoTick,  0.25f);
    public void PlayMilestone()=> PlayClip(_clipMilestone, 0.90f);

    private void PlayClip(AudioClip clip, float volume)
    {
        if (clip == null) return;
        _source.PlayOneShot(clip, volume * sfxVolume * masterVolume);
    }

    // ── Generation ───────────────────────────────────────────────────────────

    private void GenerateAll()
    {
        _clipClick     = GenerateClick();
        _clipUnlock    = GenerateUnlock();
        _clipPurchase  = GeneratePurchase();
        _clipAutoTick  = GenerateAutoTick();
        _clipMilestone = GenerateMilestone();
    }

    // ─── Click: soft bloop with pitch drop ───────────────────────────────────
    // Sine wave starting at 320Hz, drops to 180Hz over 0.12s.
    // Envelope: instant attack, exponential decay.
    private AudioClip GenerateClick()
    {
        int len    = (int)(SampleRate * 0.18f);
        var data   = new float[len];
        float dur  = len / (float)SampleRate;

        for (int i = 0; i < len; i++)
        {
            float t     = i / (float)SampleRate;
            float tNorm = t / dur;
            float freq  = Mathf.Lerp(320f, 180f, tNorm * tNorm);  // pitch drops
            float env   = Mathf.Exp(-tNorm * 14f);                 // fast decay
            float phase = 2f * Mathf.PI * freq * t;
            // Slight waveshaping: soft clip for a slightly squelchy tone
            float s     = Mathf.Sin(phase) * 0.8f + Mathf.Sin(phase * 1.5f) * 0.2f;
            data[i]     = SoftClip(s * env);
        }

        return MakeClip("Click", data);
    }

    // ─── Unlock: ascending chime ─────────────────────────────────────────────
    // Two sine waves: fundamental + octave. Quick attack, gentle release.
    private AudioClip GenerateUnlock()
    {
        int len   = (int)(SampleRate * 0.55f);
        var data  = new float[len];
        float dur = len / (float)SampleRate;

        float[] freqs   = { 523f, 659f, 784f };  // C5, E5, G5 — a major chord arpeggio
        float[] offsets = { 0f,   0.06f, 0.12f }; // staggered start

        for (int fi = 0; fi < freqs.Length; fi++)
        {
            int startSample = (int)(offsets[fi] * SampleRate);
            for (int i = startSample; i < len; i++)
            {
                float t     = (i - startSample) / (float)SampleRate;
                float tNorm = t / (dur - offsets[fi]);
                // Attack + decay envelope
                float env   = Mathf.Exp(-tNorm * 5f) * Mathf.Min(1f, t * 40f);
                float s     = Mathf.Sin(2f * Mathf.PI * freqs[fi] * t);
                float s2    = Mathf.Sin(2f * Mathf.PI * freqs[fi] * 2f * t) * 0.2f; // octave
                data[i]    += (s + s2) * env * 0.28f;
            }
        }

        Normalise(data);
        return MakeClip("Unlock", data);
    }

    // ─── Purchase: satisfying low thud ───────────────────────────────────────
    private AudioClip GeneratePurchase()
    {
        int len   = (int)(SampleRate * 0.20f);
        var data  = new float[len];
        float dur = len / (float)SampleRate;

        for (int i = 0; i < len; i++)
        {
            float t    = i / (float)SampleRate;
            float tN   = t / dur;
            float freq = Mathf.Lerp(200f, 90f, tN);  // pitch drops quickly
            float env  = Mathf.Exp(-tN * 18f);
            float s    = Mathf.Sin(2f * Mathf.PI * freq * t);
            // Add a bit of noise for "weight"
            float noise = (UnityEngine.Random.value * 2f - 1f) * 0.15f;
            data[i]    = SoftClip((s + noise) * env);
        }

        return MakeClip("Purchase", data);
    }

    // ─── AutoTick: tiny wet pip ───────────────────────────────────────────────
    // Very short, very quiet — plays when an auto-divider fires.
    private AudioClip GenerateAutoTick()
    {
        int len   = (int)(SampleRate * 0.06f);
        var data  = new float[len];
        float dur = len / (float)SampleRate;

        for (int i = 0; i < len; i++)
        {
            float t   = i / (float)SampleRate;
            float tN  = t / dur;
            float env = Mathf.Exp(-tN * 30f);
            // Triangle wave: more muffled than sine
            float phase = (2f * Mathf.PI * 480f * t) % (2f * Mathf.PI);
            float tri = 2f * Mathf.Abs(phase / Mathf.PI - 1f) - 1f;
            data[i] = tri * env * 0.5f;
        }

        return MakeClip("AutoTick", data);
    }

    // ─── Milestone: bright 4-note ascending arpeggio ─────────────────────────
    private AudioClip GenerateMilestone()
    {
        float noteDur = 0.12f;
        float[] semitones = { 0f, 4f, 7f, 12f };  // C, E, G, C (major chord)
        float baseFreq    = 523f;  // C5

        int len  = (int)(SampleRate * (noteDur * semitones.Length + 0.3f));
        var data = new float[len];

        for (int ni = 0; ni < semitones.Length; ni++)
        {
            float freq     = baseFreq * Mathf.Pow(2f, semitones[ni] / 12f);
            int   start    = (int)(ni * noteDur * SampleRate);
            int   noteLen  = (int)(noteDur * 1.8f * SampleRate);

            for (int i = start; i < Mathf.Min(start + noteLen, len); i++)
            {
                float t    = (i - start) / (float)SampleRate;
                float tN   = t / (noteDur * 1.8f);
                float env  = Mathf.Exp(-tN * 7f) * Mathf.Min(1f, t * 80f);
                float s    = Mathf.Sin(2f * Mathf.PI * freq * t)
                           + Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.3f;
                data[i]   += s * env * 0.22f;
            }
        }

        Normalise(data);
        return MakeClip("Milestone", data);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static AudioClip MakeClip(string name, float[] data)
    {
        var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    /// Soft clip (tanh approximation): prevents harsh digital clipping
    private static float SoftClip(float x)
    {
        float ax = Mathf.Abs(x);
        return (ax <= 1f) ? x : (x / ax) * (1f - 1f / (1f + ax));
    }

    /// Normalise buffer so peak = 0.95
    private static void Normalise(float[] data)
    {
        float peak = 0f;
        foreach (float s in data) peak = Mathf.Max(peak, Mathf.Abs(s));
        if (peak < 0.001f) return;
        float scale = 0.95f / peak;
        for (int i = 0; i < data.Length; i++) data[i] *= scale;
    }
}
