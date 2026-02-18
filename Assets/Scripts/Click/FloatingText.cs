using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// A single pooled "+N cells" text that rises and fades, then returns to pool.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class FloatingText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;

    private CanvasGroup _canvasGroup;
    private FloatingTextSpawner _pool;

    private void Awake() => _canvasGroup = GetComponent<CanvasGroup>();

    public void Initialize(string text, Vector3 anchoredPos, FloatingTextSpawner pool)
    {
        _pool = pool;
        label.text = text;
        ((RectTransform)transform).anchoredPosition = anchoredPos;
        _canvasGroup.alpha = 1f;
        StopAllCoroutines();
        StartCoroutine(FloatAndFade());
    }

    private IEnumerator FloatAndFade()
    {
        var rt       = (RectTransform)transform;
        var startPos = rt.anchoredPosition;
        var endPos   = startPos + new Vector2(0, 80f);
        float duration = 0.85f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, p);
            _canvasGroup.alpha  = Mathf.Lerp(1f, 0f, p * p);  // ease out
            yield return null;
        }

        _pool.Return(this);
    }
}
