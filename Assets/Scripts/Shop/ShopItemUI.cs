using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI row for a single shop item. Attach to the ShopItem prefab.
/// Initialized by ShopManager.
/// </summary>
public class ShopItemUI : MonoBehaviour
{
    [SerializeField] private Image            iconImage;
    [SerializeField] private TextMeshProUGUI  nameLabel;
    [SerializeField] private TextMeshProUGUI  descLabel;
    [SerializeField] private TextMeshProUGUI  costLabel;
    [SerializeField] private TextMeshProUGUI  countLabel;
    [SerializeField] private Button           buyButton;
    [SerializeField] private CanvasGroup      canvasGroup;

    private ShopItemData _data;
    private int          _itemIndex;

    // ── Init ─────────────────────────────────────────────────────────────────

    public void Initialize(ShopItemData data, int index)
    {
        _data      = data;
        _itemIndex = index;

        nameLabel.text = data.displayName;
        descLabel.text = data.description;
        if (data.icon != null && iconImage != null)
            iconImage.sprite = data.icon;

        buyButton.onClick.AddListener(() => ShopManager.Instance.Purchase(_itemIndex));

        EventBus.OnCellCountChanged += _ => Refresh();
        EventBus.OnShopChanged      += Refresh;
        EventBus.OnTechTreeChanged  += Refresh;

        Refresh();
    }

    // ── Refresh ──────────────────────────────────────────────────────────────

    public void Refresh()
    {
        bool gated = !string.IsNullOrEmpty(_data.requiredNodeId)
                     && (TechTreeManager.Instance == null
                         || !TechTreeManager.Instance.IsUnlocked(_data.requiredNodeId));

        // Hide entirely if tech gate not met
        if (canvasGroup != null)
        {
            canvasGroup.alpha          = gated ? 0.3f : 1f;
            canvasGroup.interactable   = !gated;
            canvasGroup.blocksRaycasts = !gated;
        }

        int    count       = ShopManager.Instance.GetPurchaseCount(_itemIndex);
        double currentCost = ShopManager.Instance.GetCurrentCost(_itemIndex);
        bool   maxed       = _data.maxPurchases > 0 && count >= _data.maxPurchases;
        bool   canAfford   = GameManager.Instance.CanAfford(currentCost);

        countLabel.text = count > 0 ? $"x{count}" : "";
        costLabel.text  = maxed ? "MAX" : GameUtils.FormatNumber(currentCost);
        buyButton.interactable = !gated && !maxed && canAfford;
    }

    private void OnDestroy()
    {
        EventBus.OnCellCountChanged -= _ => Refresh();
        EventBus.OnShopChanged      -= Refresh;
        EventBus.OnTechTreeChanged  -= Refresh;
    }
}
