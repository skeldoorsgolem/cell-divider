using UnityEngine;

/// <summary>
/// ScriptableObject defining a single tech tree node.
/// Create via: Assets > Create > CellGame > TechNode
/// </summary>
[CreateAssetMenu(fileName = "TechNode", menuName = "CellGame/TechNode")]
public class TechNodeData : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Cost")]
    public double unlockCost;

    [Header("Prerequisites")]
    public string[] prerequisiteIds;   // all must be unlocked first

    [Header("Effect — only fill ONE")]
    public double cpcFlatBonus;        // flat cells-per-click addition
    public double cpcMultiplier;       // e.g. 2.0 = double CPC (applied multiplicatively)
    public double cpsBonus;            // flat cells-per-second addition

    [Header("Layout (set by designer)")]
    // Used by TechTreeManager to position the node in the scroll view.
    // tier = column (0 = leftmost), slot = row within tier
    public int tier;
    public int slot;
}
