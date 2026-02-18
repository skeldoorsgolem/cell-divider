using UnityEngine;

/// <summary>
/// Handles the main "Divide Cell" button click.
/// Attach to the Button GameObject. Wire OnClick to OnCellButtonClicked().
/// </summary>
public class ClickManager : MonoBehaviour
{
    [SerializeField] private FloatingTextSpawner floatingTextSpawner;
    [SerializeField] private ClickFeedback       clickFeedback;
    [SerializeField] private ParticleSystem      clickParticles;

    public void OnCellButtonClicked()
    {
        if (GameManager.Instance == null) return;

        double gained = GameManager.Instance.EffectiveCpc;
        GameManager.Instance.AddCells(gained);

        floatingTextSpawner?.Spawn(gained);
        clickFeedback?.PlaySquish();
        clickParticles?.Play();
    }
}
