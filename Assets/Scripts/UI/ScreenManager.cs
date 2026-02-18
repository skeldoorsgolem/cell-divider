using UnityEngine;

/// <summary>
/// Manages transitions between the three main screens: Title, Settings, Game.
/// Each screen is a child Panel of the root Canvas — only one is active at a time.
///
/// Usage: ScreenManager.Instance.ShowGame() / ShowTitle() / ShowSettings()
/// </summary>
public class ScreenManager : MonoBehaviour
{
    public static ScreenManager Instance { get; private set; }

    [SerializeField] private GameObject titleScreen;
    [SerializeField] private GameObject settingsScreen;
    [SerializeField] private GameObject gameScreen;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start() => ShowTitle();

    public void ShowTitle()
    {
        SetActive(titleScreen);
        AudioSynth.Instance?.PlayUnlock();   // ambient jingle on title
    }

    public void ShowGame()
    {
        SetActive(gameScreen);
    }

    public void ShowSettings()
    {
        SetActive(settingsScreen);
    }

    private void SetActive(GameObject target)
    {
        if (titleScreen    != null) titleScreen.SetActive(titleScreen       == target);
        if (settingsScreen != null) settingsScreen.SetActive(settingsScreen == target);
        if (gameScreen     != null) gameScreen.SetActive(gameScreen         == target);
    }
}
