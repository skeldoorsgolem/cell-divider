using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Applies a procedurally generated cell sprite to the Image on this GameObject.
/// Attach to the CellButton alongside the Image component.
/// </summary>
[RequireComponent(typeof(Image))]
public class CellButtonVisual : MonoBehaviour
{
    [SerializeField] private int textureSize = 256;
    [SerializeField] private int seed        = 0;

    private void Awake()
    {
        var img = GetComponent<Image>();
        img.sprite = ProceduralCellTexture.GenerateSprite(textureSize, seed);
        img.color  = Color.white; // ensure not transparent
    }
}
