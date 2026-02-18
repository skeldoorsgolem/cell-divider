using UnityEngine;

/// <summary>
/// Handles the main "Divide Cell" button click.
/// Attach to the same GameObject as SquishyCell.
/// Call OnCellButtonClicked() from the Button's OnClick event,
/// or let SquishyCell's IPointerDown handle it directly.
/// </summary>
public class ClickManager : MonoBehaviour
{
    [SerializeField] private FloatingTextSpawner floatingTextSpawner;
    [SerializeField] private ClickFeedback       clickFeedback;
    [SerializeField] private ParticleSystem      clickParticles;
    [SerializeField] private SquishyCell         squishyCell;

    /// Set by GameScreen after the screen activates.
    public void SetSquishyCell(SquishyCell cell) => squishyCell = cell;

    public void OnCellButtonClicked()
    {
        if (GameManager.Instance == null) return;

        double gained = GameManager.Instance.EffectiveCpc;
        GameManager.Instance.AddCells(gained);

        floatingTextSpawner?.Spawn(gained);
        clickFeedback?.PlaySquish();
        clickParticles?.Play();
        AudioSynth.Instance?.PlayClick();

        // Poke the squishy cell at a random edge point (if no pointer data available)
        squishyCell?.Poke(UnityEngine.Random.insideUnitCircle.normalized * squishyCell.GetComponent<RectTransform>().rect.width * 0.3f);
    }
}
