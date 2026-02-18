using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum TechNodeState { Locked, Available, Unlocked }

/// <summary>
/// Runtime MonoBehaviour for a single tech tree node.
/// Attach to the TechNode prefab.
/// </summary>
public class TechNode : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image            bgImage;
    [SerializeField] private Image            iconImage;
    [SerializeField] private TextMeshProUGUI  nameLabel;
    [SerializeField] private TextMeshProUGUI  costLabel;
    [SerializeField] private Button           unlockButton;

    [Header("State Colors")]
    [SerializeField] private Color colorLocked    = new Color(0.25f, 0.25f, 0.3f);
    [SerializeField] private Color colorAvailable = new Color(0.4f,  0.7f,  0.4f);
    [SerializeField] private Color colorUnlocked  = new Color(0.2f,  0.9f,  0.5f);

    public TechNodeData Data  { get; private set; }
    public TechNodeState State { get; private set; } = TechNodeState.Locked;

    // All SpringLines that touch this node — excited on unlock
    private System.Collections.Generic.List<SpringLine> _connectedLines
        = new System.Collections.Generic.List<SpringLine>();

    // ── Init ─────────────────────────────────────────────────────────────────

    public void Initialize(TechNodeData data)
    {
        Data = data;
        nameLabel.text = data.displayName;
        costLabel.text = GameUtils.FormatNumber(data.unlockCost);
        if (data.icon != null && iconImage != null)
            iconImage.sprite = data.icon;

        unlockButton.onClick.AddListener(OnUnlockClicked);
        EventBus.OnCellCountChanged += _ => RefreshVisuals();

        RefreshVisuals();
    }

    public void RegisterLine(SpringLine line) => _connectedLines.Add(line);

    // Called by TechTreeManager after loading a save
    public void ForceUnlocked()
    {
        State = TechNodeState.Unlocked;
        RefreshVisuals();
    }

    // ── Click handler ────────────────────────────────────────────────────────

    private void OnUnlockClicked()
    {
        if (State != TechNodeState.Available) return;
        if (!GameManager.Instance.SpendCells(Data.unlockCost)) return;

        State = TechNodeState.Unlocked;
        TechTreeManager.Instance.MarkUnlocked(Data.id);
        ApplyEffect();
        RefreshVisuals();
        ExciteLines();
        EventBus.Emit_TechTreeChanged();
    }

    // ── Effect application ───────────────────────────────────────────────────

    public void ApplyEffect()
    {
        if (Data.cpcMultiplier > 0)
            GameManager.Instance.ApplyCpcMultiplier(Data.cpcMultiplier);
        if (Data.cpcFlatBonus > 0)
            GameManager.Instance.ApplyCpcBonus(Data.cpcFlatBonus);
        if (Data.cpsBonus > 0)
            GameManager.Instance.ApplyCpsBonus(Data.cpsBonus);
    }

    // ── Visuals ──────────────────────────────────────────────────────────────

    public void RefreshVisuals()
    {
        // Recompute state
        if (State != TechNodeState.Unlocked)
            State = TechTreeManager.Instance.CanUnlock(Data.id)
                  ? TechNodeState.Available
                  : TechNodeState.Locked;

        switch (State)
        {
            case TechNodeState.Locked:
                bgImage.color = colorLocked;
                unlockButton.interactable = false;
                costLabel.text = GameUtils.FormatNumber(Data.unlockCost);
                break;
            case TechNodeState.Available:
                bgImage.color = colorAvailable;
                unlockButton.interactable = GameManager.Instance.CanAfford(Data.unlockCost);
                costLabel.text = GameUtils.FormatNumber(Data.unlockCost);
                break;
            case TechNodeState.Unlocked:
                bgImage.color = colorUnlocked;
                unlockButton.interactable = false;
                costLabel.text = "Unlocked";
                break;
        }

        // Propagate state color to all connected lines
        foreach (var line in _connectedLines)
            line.SetState(State);
    }

    private void ExciteLines()
    {
        foreach (var line in _connectedLines)
            line.Excite();
    }

    private void OnDestroy()
    {
        EventBus.OnCellCountChanged -= _ => RefreshVisuals();
    }
}
