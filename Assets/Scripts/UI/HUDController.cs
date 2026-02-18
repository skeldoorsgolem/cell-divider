using TMPro;
using UnityEngine;

/// <summary>
/// Drives the persistent top HUD: cell count, CPS, CPC.
/// Attach to the HUD root GameObject.
/// </summary>
public class HUDController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI cellCountLabel;
    [SerializeField] private TextMeshProUGUI cpsLabel;
    [SerializeField] private TextMeshProUGUI cpcLabel;

    private void OnEnable()  => EventBus.OnCellCountChanged += Refresh;
    private void OnDisable() => EventBus.OnCellCountChanged -= Refresh;

    private void Start() => Refresh(GameManager.Instance?.CellCount ?? 0);

    private void Refresh(double cellCount)
    {
        if (GameManager.Instance == null) return;

        cellCountLabel.text = $"{GameUtils.FormatNumber(cellCount)} cells";
        cpsLabel.text       = $"{GameUtils.FormatNumber(GameManager.Instance.CellsPerSecond)}/sec";
        cpcLabel.text       = $"{GameUtils.FormatNumber(GameManager.Instance.EffectiveCpc)}/click";
    }
}
