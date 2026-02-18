using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual feedback on the main cell button: squish scale animation + colour flash.
/// Attach to the same GameObject as the cell button Image.
/// </summary>
public class ClickFeedback : MonoBehaviour
{
    [SerializeField] private Image cellImage;
    [SerializeField] private Color flashColor = new Color(0.8f, 1f, 0.8f);

    private Vector3  _baseScale;
    private Color    _baseColor;
    private Coroutine _squishRoutine;

    private void Awake()
    {
        _baseScale = transform.localScale;
        _baseColor = cellImage != null ? cellImage.color : Color.white;
    }

    public void PlaySquish()
    {
        if (_squishRoutine != null) StopCoroutine(_squishRoutine);
        _squishRoutine = StartCoroutine(SquishRoutine());
    }

    private IEnumerator SquishRoutine()
    {
        float duration = 0.12f;
        float half     = duration * 0.5f;

        // Press down
        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float p = t / half;
            transform.localScale = Vector3.Lerp(_baseScale, _baseScale * 0.88f, p);
            if (cellImage != null)
                cellImage.color = Color.Lerp(_baseColor, flashColor, p);
            yield return null;
        }

        // Spring back
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float p = t / half;
            transform.localScale = Vector3.Lerp(_baseScale * 0.88f, _baseScale, p);
            if (cellImage != null)
                cellImage.color = Color.Lerp(flashColor, _baseColor, p);
            yield return null;
        }

        transform.localScale = _baseScale;
        if (cellImage != null) cellImage.color = _baseColor;
    }
}
