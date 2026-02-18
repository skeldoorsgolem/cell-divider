using UnityEngine;

/// <summary>
/// Draws a springy rope between two RectTransforms using a LineRenderer.
/// Uses manual spring-mass simulation — no Rigidbody required.
///
/// Setup:
///   - Add a GameObject with this component + a LineRenderer inside the TechTree canvas.
///   - Call Connect(fromRect, toRect) after instantiation.
///   - Call Excite() when a connected node is unlocked for the wobbly celebration effect.
///
/// Tuning:
///   stiffness 60-120, damping 4-8 gives a satisfying organic feel.
///   segmentCount 6-10 is plenty for short branches.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class SpringLine : MonoBehaviour
{
    [Header("Spring parameters")]
    [SerializeField] private int   segmentCount = 8;
    [SerializeField] private float stiffness    = 80f;
    [SerializeField] private float damping      = 5f;
    [SerializeField] private float mass         = 1f;

    [Header("Visuals")]
    [SerializeField] private Color colorLocked    = new Color(0.3f, 0.3f, 0.3f, 0.6f);
    [SerializeField] private Color colorAvailable = new Color(0.7f, 0.9f, 0.7f, 0.9f);
    [SerializeField] private Color colorUnlocked  = new Color(0.2f, 1f, 0.4f, 1f);
    [SerializeField] private float lineWidth      = 4f;

    private LineRenderer _lr;
    private RectTransform _fromRT;
    private RectTransform _toRT;

    private Vector3[] _positions;
    private Vector3[] _velocities;

    private bool _connected;

    // ── Public API ───────────────────────────────────────────────────────────

    public void Connect(RectTransform from, RectTransform to)
    {
        _fromRT = from;
        _toRT   = to;

        int total = segmentCount + 2;  // includes two pinned endpoints
        _positions  = new Vector3[total];
        _velocities = new Vector3[total];

        // Initialise masses evenly spaced between endpoints
        Vector3 startWP = WorldPos(_fromRT);
        Vector3 endWP   = WorldPos(_toRT);
        for (int i = 0; i < total; i++)
        {
            float t = (float)i / (total - 1);
            _positions[i]  = Vector3.Lerp(startWP, endWP, t);
            _velocities[i] = Vector3.zero;
        }

        _lr.positionCount = total;
        _lr.SetPositions(_positions);
        _connected = true;

        SetColor(colorLocked);
    }

    /// Plucks the line — adds a perpendicular impulse to each intermediate mass.
    public void Excite(float impulseStrength = 50f)
    {
        if (!_connected) return;

        Vector3 lineDir = (_positions[_positions.Length - 1] - _positions[0]).normalized;
        Vector3 perp    = Vector3.Cross(lineDir, Vector3.forward).normalized;

        for (int i = 1; i < _positions.Length - 1; i++)
        {
            float sign = (Random.value > 0.5f) ? 1f : -1f;
            _velocities[i] += perp * sign * impulseStrength * Random.Range(0.7f, 1.3f);
        }
    }

    public void SetState(TechNodeState state)
    {
        switch (state)
        {
            case TechNodeState.Locked:    SetColor(colorLocked);    break;
            case TechNodeState.Available: SetColor(colorAvailable); break;
            case TechNodeState.Unlocked:  SetColor(colorUnlocked);  break;
        }
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        _lr           = GetComponent<LineRenderer>();
        _lr.useWorldSpace = true;
        _lr.startWidth = lineWidth;
        _lr.endWidth   = lineWidth;
    }

    private void Update()
    {
        if (!_connected) return;

        float dt = Time.deltaTime;

        // Pin endpoints to current world positions (handles scroll/layout changes)
        _positions[0]                        = WorldPos(_fromRT);
        _positions[_positions.Length - 1]    = WorldPos(_toRT);

        int n = _positions.Length;

        // Spring-mass integration for intermediate masses
        for (int i = 1; i < n - 1; i++)
        {
            float  t       = (float)i / (n - 1);
            Vector3 rest   = Vector3.Lerp(_positions[0], _positions[n - 1], t);
            Vector3 disp   = _positions[i] - rest;

            Vector3 force  = (-stiffness * disp) + (-damping * _velocities[i]);
            _velocities[i] += (force / mass) * dt;
            _positions[i]  += _velocities[i] * dt;
        }

        _lr.SetPositions(_positions);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Vector3 WorldPos(RectTransform rt)
    {
        // Convert anchored UI position to world space
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        // Centre of the rect
        return (corners[0] + corners[2]) * 0.5f;
    }

    private void SetColor(Color c)
    {
        _lr.startColor = c;
        _lr.endColor   = c;
    }
}
