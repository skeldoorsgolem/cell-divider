using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Object pool for FloatingText instances. Attach to a persistent GO in the canvas.
/// </summary>
public class FloatingTextSpawner : MonoBehaviour
{
    [SerializeField] private FloatingText prefab;
    [SerializeField] private RectTransform spawnAnchor;  // near the click button

    private readonly Queue<FloatingText> _pool = new Queue<FloatingText>();

    public void Spawn(double amount)
    {
        string text = $"+{GameUtils.FormatNumber(amount)}";

        // Slight random offset so multiple rapid clicks don't stack exactly
        var pos = spawnAnchor.anchoredPosition + new Vector2(
            Random.Range(-30f, 30f),
            Random.Range(-10f, 10f));

        var ft = _pool.Count > 0 ? _pool.Dequeue() : Instantiate(prefab, transform);
        ft.gameObject.SetActive(true);
        ft.Initialize(text, pos, this);
    }

    public void Return(FloatingText ft)
    {
        ft.gameObject.SetActive(false);
        _pool.Enqueue(ft);
    }
}
