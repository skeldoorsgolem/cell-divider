using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages all shop items: instantiation, purchase logic, cost scaling.
/// Attach to a persistent GO in the scene.
/// </summary>
public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [SerializeField] private ShopItemData[]  allItems;
    [SerializeField] private ShopItemUI      shopItemPrefab;
    [SerializeField] private Transform       shopContentRoot;

    private int[] _purchaseCounts;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _purchaseCounts = new int[allItems.Length];
        BuildShopUI();
    }

    // ── Build UI ─────────────────────────────────────────────────────────────

    private void BuildShopUI()
    {
        for (int i = 0; i < allItems.Length; i++)
        {
            ShopItemUI row = Instantiate(shopItemPrefab, shopContentRoot);
            row.Initialize(allItems[i], i);
        }
    }

    // ── Purchase ─────────────────────────────────────────────────────────────

    public void Purchase(int index)
    {
        if (index < 0 || index >= allItems.Length) return;

        ShopItemData item = allItems[index];
        double cost = GetCurrentCost(index);

        // Guards
        bool maxed  = item.maxPurchases > 0 && _purchaseCounts[index] >= item.maxPurchases;
        bool gated  = !string.IsNullOrEmpty(item.requiredNodeId)
                      && (TechTreeManager.Instance == null
                          || !TechTreeManager.Instance.IsUnlocked(item.requiredNodeId));

        if (maxed || gated) return;
        if (!GameManager.Instance.SpendCells(cost)) return;

        _purchaseCounts[index]++;

        // Apply effect
        if (item.cpcFlatBonus > 0) GameManager.Instance.ApplyCpcBonus(item.cpcFlatBonus);
        if (item.cpsBonus      > 0) GameManager.Instance.ApplyCpsBonus(item.cpsBonus);

        EventBus.Emit_ShopChanged();
    }

    // ── Queries ──────────────────────────────────────────────────────────────

    public double GetCurrentCost(int index)
    {
        if (index < 0 || index >= allItems.Length) return double.MaxValue;
        ShopItemData item = allItems[index];
        return item.baseCost * System.Math.Pow(item.costScaling, _purchaseCounts[index]);
    }

    public int GetPurchaseCount(int index)
    {
        if (index < 0 || index >= allItems.Length) return 0;
        return _purchaseCounts[index];
    }

    // ── Save / Load ──────────────────────────────────────────────────────────

    public List<int> GetPurchaseCounts() => new List<int>(_purchaseCounts);

    /// Restores purchase counts and re-applies all effects (called on load).
    public void RestoreCounts(List<int> saved)
    {
        if (saved == null) return;

        for (int i = 0; i < Mathf.Min(saved.Count, allItems.Length); i++)
        {
            int count = saved[i];
            for (int p = 0; p < count; p++)
            {
                // Apply each purchase's effect individually to accumulate correctly
                ShopItemData item = allItems[i];
                if (item.cpcFlatBonus > 0) GameManager.Instance.ApplyCpcBonus(item.cpcFlatBonus);
                if (item.cpsBonus      > 0) GameManager.Instance.ApplyCpsBonus(item.cpsBonus);
            }
            _purchaseCounts[i] = count;
        }

        EventBus.Emit_ShopChanged();
    }
}
