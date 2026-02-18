using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Game screen controller — wires up the main gameplay UI.
///
/// Scene hierarchy under GameScreen:
///
///   GameScreen (Panel)
///     HUD (top bar)
///       CellCountLabel     ← TextMeshProUGUI  — HUDController.cs
///       CPSLabel
///       CPCLabel
///       SettingsButton     ← Button → OnSettingsClicked()
///
///     CellArea (left/centre)
///       SquishyCellGO      ← GameObject with SquishyCell.cs (the main clickable cell)
///                            ClickManager.cs also on this GO
///       FloatingTextPool   ← FloatingTextSpawner.cs
///
///     SidePanel (right)
///       TabBar
///         ShopTab          ← Button → PanelToggle
///         TechTreeTab      ← Button → PanelToggle
///
///       ShopPanel          ← ScrollView content — ShopManager populates this
///       TechTreePanel      ← ScrollView content — TechTreeManager populates this
///
///     MilestoneToast       ← TextMeshProUGUI, hidden by default
///                            MilestoneTracker shows this on cell-count milestones
/// </summary>
public class GameScreen : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SquishyCell  squishyCell;
    [SerializeField] private ClickManager clickManager;

    [Header("Milestone toast")]
    [SerializeField] private TextMeshProUGUI milestoneToast;
    [SerializeField] private CanvasGroup     toastGroup;

    private MilestoneTracker _milestones;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        // Wire the squishy cell into ClickManager so clicks trigger deformation
        if (clickManager != null && squishyCell != null)
            clickManager.SetSquishyCell(squishyCell);

        _milestones ??= new MilestoneTracker(OnMilestoneReached);
        EventBus.OnCellCountChanged += _milestones.Check;
    }

    private void OnDisable()
    {
        EventBus.OnCellCountChanged -= _milestones.Check;
    }

    // ── Navigation ───────────────────────────────────────────────────────────

    public void OnSettingsClicked()
    {
        AudioSynth.Instance?.PlayClick();
        ScreenManager.Instance.ShowSettings();
    }

    // ── Milestones ───────────────────────────────────────────────────────────

    private void OnMilestoneReached(double count)
    {
        if (milestoneToast == null) return;
        milestoneToast.text = $"✦ {GameUtils.FormatNumber(count)} cells! ✦";
        AudioSynth.Instance?.PlayMilestone();
        StopAllCoroutines();
        StartCoroutine(ShowToast());
    }

    private System.Collections.IEnumerator ShowToast()
    {
        if (toastGroup == null) yield break;
        toastGroup.alpha = 1f;
        yield return new WaitForSeconds(2.0f);
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            toastGroup.alpha = 1f - (t / 0.5f);
            yield return null;
        }
        toastGroup.alpha = 0f;
    }
}

// ── Milestone tracker ────────────────────────────────────────────────────────

/// Fires a callback each time a milestone is crossed (powers of 10 × 1, 2, 5).
public class MilestoneTracker
{
    private readonly System.Action<double> _onReached;
    private double _nextMilestone = 100;

    private static readonly double[] _steps = { 1, 2, 5 };

    public MilestoneTracker(System.Action<double> onReached) => _onReached = onReached;

    public void Check(double cellCount)
    {
        if (cellCount >= _nextMilestone)
        {
            _onReached?.Invoke(_nextMilestone);
            AdvanceMilestone();
        }
    }

    private void AdvanceMilestone()
    {
        // Walk through 100, 200, 500, 1000, 2000, 5000, 10000 ...
        double magnitude = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10((float)_nextMilestone)));
        double step      = _nextMilestone / magnitude;  // 1, 2, or 5

        int idx = System.Array.IndexOf(_steps, step);
        if (idx < 0) idx = 0;

        if (idx < _steps.Length - 1)
            _nextMilestone = magnitude * _steps[idx + 1];
        else
            _nextMilestone = magnitude * 10 * _steps[0];
    }
}
