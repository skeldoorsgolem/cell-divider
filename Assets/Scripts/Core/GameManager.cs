using System.Collections;
using UnityEngine;

/// <summary>
/// Singleton. Owns all numeric game state: cell count, CPC, CPS.
/// All other systems call into this to read or mutate values.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ── Numeric state ────────────────────────────────────────────────────────

    public double CellCount        { get; private set; }
    public double CellsPerClick    { get; private set; } = 1.0;
    public double CellsPerSecond   { get; private set; }

    // Flat CPS added by auto-dividers and shop items
    private double _cpsFlat;

    // Multiplicative bonuses
    private double _cpcMultiplier  = 1.0;

    // Effective CPC taking multiplier into account
    public double EffectiveCpc => CellsPerClick * _cpcMultiplier;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        LoadGame();
        StartCoroutine(AutoDivideLoop());
        StartCoroutine(AutoSaveLoop());
    }

    private void OnApplicationQuit() => SaveGame();

    // ── Public API ───────────────────────────────────────────────────────────

    public void AddCells(double amount)
    {
        CellCount += amount;
        EventBus.Emit_CellCountChanged(CellCount);
    }

    /// Returns false and does nothing if player can't afford it.
    public bool SpendCells(double amount)
    {
        if (CellCount < amount) return false;
        CellCount -= amount;
        EventBus.Emit_CellCountChanged(CellCount);
        return true;
    }

    public bool CanAfford(double cost) => CellCount >= cost;

    /// Add a flat CPC bonus (e.g. from shop purchase).
    public void ApplyCpcBonus(double flatBonus)
    {
        CellsPerClick += flatBonus;
    }

    /// Multiply CPC (e.g. from tech node unlock).
    public void ApplyCpcMultiplier(double multiplier)
    {
        _cpcMultiplier *= multiplier;
    }

    /// Add flat CPS (from auto-dividers or shop items).
    public void ApplyCpsBonus(double flatCps)
    {
        _cpsFlat       += flatCps;
        CellsPerSecond  = _cpsFlat;
    }

    // ── Coroutines ───────────────────────────────────────────────────────────

    private IEnumerator AutoDivideLoop()
    {
        const float tick = 0.1f;
        while (true)
        {
            yield return new WaitForSeconds(tick);
            if (CellsPerSecond > 0)
                AddCells(CellsPerSecond * tick);
        }
    }

    private IEnumerator AutoSaveLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(30f);
            SaveGame();
        }
    }

    // ── Save / Load ──────────────────────────────────────────────────────────

    private void SaveGame()
    {
        var data = new SaveSystem.SaveData
        {
            cellCountStr     = CellCount.ToString("R"),
            cellsPerClickStr = CellsPerClick.ToString("R"),
            cellsPerSecondStr= CellsPerSecond.ToString("R"),
            cpcMultiplierStr = _cpcMultiplier.ToString("R"),
        };

        // Tech tree and shop save their own slices and merge into data
        if (TechTreeManager.Instance != null)
            data.unlockedNodeIds = TechTreeManager.Instance.GetAllUnlockedIds();
        if (ShopManager.Instance != null)
            data.shopPurchaseCounts = ShopManager.Instance.GetPurchaseCounts();

        SaveSystem.Save(data);
    }

    private void LoadGame()
    {
        var data = SaveSystem.Load();
        if (data == null) return;

        if (double.TryParse(data.cellCountStr, out double cells))
            CellCount = cells;
        if (double.TryParse(data.cellsPerClickStr, out double cpc))
            CellsPerClick = cpc;
        // CPS and multiplier are recomputed by tech tree / shop restore,
        // so we don't need to restore them directly here.

        EventBus.Emit_CellCountChanged(CellCount);
    }

    // Called by TechTreeManager and ShopManager after they've restored state
    public void ResetDerivedStats()
    {
        _cpcMultiplier = 1.0;
        CellsPerClick  = 1.0;
        _cpsFlat       = 0;
        CellsPerSecond = 0;
    }
}
