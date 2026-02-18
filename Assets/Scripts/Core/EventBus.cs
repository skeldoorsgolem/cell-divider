using System;

/// <summary>
/// Static event bus for decoupled communication between systems.
/// Subscribe in OnEnable, unsubscribe in OnDisable.
/// </summary>
public static class EventBus
{
    /// Fired whenever the cell count changes (includes auto-divide ticks).
    public static event Action<double> OnCellCountChanged;

    /// Fired when a tech node is unlocked.
    public static event Action OnTechTreeChanged;

    /// Fired when a shop purchase is made.
    public static event Action OnShopChanged;

    public static void Emit_CellCountChanged(double newCount) =>
        OnCellCountChanged?.Invoke(newCount);

    public static void Emit_TechTreeChanged() =>
        OnTechTreeChanged?.Invoke();

    public static void Emit_ShopChanged() =>
        OnShopChanged?.Invoke();
}
