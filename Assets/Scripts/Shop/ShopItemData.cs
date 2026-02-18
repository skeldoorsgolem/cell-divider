using UnityEngine;

/// <summary>
/// ScriptableObject defining a single purchasable shop item.
/// Create via: Assets > Create > CellGame > ShopItem
/// </summary>
[CreateAssetMenu(fileName = "ShopItem", menuName = "CellGame/ShopItem")]
public class ShopItemData : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Cost")]
    public double baseCost;
    [Tooltip("Cost multiplied by this per purchase. 1.15 = +15% per buy.")]
    public float costScaling = 1.15f;
    [Tooltip("0 = unlimited")]
    public int maxPurchases = 0;

    [Header("Effect per purchase — fill ONE")]
    public double cpcFlatBonus;    // flat cells-per-click
    public double cpsBonus;        // flat cells-per-second (auto-divider)

    [Header("Unlock gate (optional)")]
    [Tooltip("Leave empty for always visible. Set to a TechNodeData.id to gate visibility.")]
    public string requiredNodeId;
}
