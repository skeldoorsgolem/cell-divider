using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Settings screen.
///
/// Scene setup:
///   SettingsScreen (Panel, full-screen)
///     TitleLabel         ← TextMeshProUGUI "Settings"
///     MasterVolumeSlider ← Slider (0-1) → OnMasterVolumeChanged()
///     SFXVolumeSlider    ← Slider (0-1) → OnSFXVolumeChanged()
///     MasterValueLabel   ← TextMeshProUGUI shows current %
///     SFXValueLabel      ← TextMeshProUGUI
///     ResetSaveButton    ← Button → OnResetSaveClicked()
///     ConfirmResetPanel  ← Panel (hidden by default)
///       ConfirmYesButton → OnConfirmReset()
///       ConfirmNoButton  → OnCancelReset()
///     BackButton         ← Button → OnBackClicked()
/// </summary>
public class SettingsScreen : MonoBehaviour
{
    [Header("Volume")]
    [SerializeField] private Slider           masterVolumeSlider;
    [SerializeField] private Slider           sfxVolumeSlider;
    [SerializeField] private TextMeshProUGUI  masterValueLabel;
    [SerializeField] private TextMeshProUGUI  sfxValueLabel;

    [Header("Reset")]
    [SerializeField] private GameObject       confirmResetPanel;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        // Load saved settings
        float master = PlayerPrefs.GetFloat("MasterVolume", 0.7f);
        float sfx    = PlayerPrefs.GetFloat("SFXVolume",    1.0f);

        if (masterVolumeSlider != null) masterVolumeSlider.value = master;
        if (sfxVolumeSlider    != null) sfxVolumeSlider.value    = sfx;

        RefreshLabels(master, sfx);
    }

    // ── Slider callbacks ─────────────────────────────────────────────────────

    public void OnMasterVolumeChanged(float value)
    {
        if (AudioSynth.Instance != null) AudioSynth.Instance.masterVolume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
        if (masterValueLabel != null) masterValueLabel.text = $"{Mathf.RoundToInt(value * 100)}%";
    }

    public void OnSFXVolumeChanged(float value)
    {
        if (AudioSynth.Instance != null) AudioSynth.Instance.sfxVolume = value;
        PlayerPrefs.SetFloat("SFXVolume", value);
        if (sfxValueLabel != null) sfxValueLabel.text = $"{Mathf.RoundToInt(value * 100)}%";
    }

    // ── Reset save ───────────────────────────────────────────────────────────

    public void OnResetSaveClicked()
    {
        AudioSynth.Instance?.PlayClick();
        confirmResetPanel?.SetActive(true);
    }

    public void OnConfirmReset()
    {
        SaveSystem.DeleteAll();
        // Reload the scene to start fresh
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    public void OnCancelReset()
    {
        AudioSynth.Instance?.PlayClick();
        confirmResetPanel?.SetActive(false);
    }

    // ── Navigation ───────────────────────────────────────────────────────────

    public void OnBackClicked()
    {
        AudioSynth.Instance?.PlayClick();
        ScreenManager.Instance.ShowTitle();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void RefreshLabels(float master, float sfx)
    {
        if (masterValueLabel != null) masterValueLabel.text = $"{Mathf.RoundToInt(master * 100)}%";
        if (sfxValueLabel    != null) sfxValueLabel.text    = $"{Mathf.RoundToInt(sfx    * 100)}%";
    }
}
