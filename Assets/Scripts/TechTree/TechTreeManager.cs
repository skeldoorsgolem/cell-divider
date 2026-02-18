using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Instantiates tech nodes, lays them out using Fruchterman-Reingold force-directed
/// graph placement, and creates SpringLine connections between them.
///
/// The FR algorithm treats nodes as particles:
///   - Repulsive force between ALL pairs of nodes (keeps them spread out)
///   - Attractive force along each edge (pulls connected nodes together)
/// After N iterations the layout settles into a natural, organic-looking graph.
///
/// Prerequisites are the edges. Nodes with no prerequisites float near the centre;
/// heavily-connected nodes cluster; branches spread outward naturally.
/// </summary>
public class TechTreeManager : MonoBehaviour
{
    public static TechTreeManager Instance { get; private set; }

    [Header("Data")]
    [SerializeField] private TechNodeData[]   allNodeData;

    [Header("Prefabs")]
    [SerializeField] private TechNode         nodePrefab;
    [SerializeField] private SpringLine       linePrefab;

    [Header("Layout")]
    [SerializeField] private RectTransform    contentRoot;
    [SerializeField] private float            graphWidth    = 900f;
    [SerializeField] private float            graphHeight   = 700f;
    [SerializeField] private int              frIterations  = 150;
    [SerializeField] private float            frTemperature = 200f;   // initial max displacement
    [SerializeField] private float            frCooling     = 0.95f;  // temp multiplied each iter

    // Runtime
    private Dictionary<string, TechNode>    _nodes    = new Dictionary<string, TechNode>();
    private HashSet<string>                 _unlocked = new HashSet<string>();
    private List<SpringLine>                _lines    = new List<SpringLine>();

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        BuildGraph();
    }

    // ── Graph construction ───────────────────────────────────────────────────

    private void BuildGraph()
    {
        // 1. Compute FR layout positions (in [-graphWidth/2, graphWidth/2] space)
        Dictionary<string, Vector2> positions = ComputeFRLayout();

        // 2. Instantiate nodes at computed positions
        foreach (var data in allNodeData)
        {
            Vector2 pos = positions.ContainsKey(data.id) ? positions[data.id] : Vector2.zero;

            TechNode node = Instantiate(nodePrefab, contentRoot);
            var rt = (RectTransform)node.transform;
            rt.anchoredPosition = pos;
            node.Initialize(data);
            _nodes[data.id] = node;
        }

        // 3. Create SpringLines for each prerequisite edge
        foreach (var data in allNodeData)
        {
            if (data.prerequisiteIds == null) continue;
            foreach (string prereqId in data.prerequisiteIds)
            {
                if (!_nodes.TryGetValue(prereqId,  out TechNode fromNode)) continue;
                if (!_nodes.TryGetValue(data.id,   out TechNode toNode))   continue;

                SpringLine line = Instantiate(linePrefab, contentRoot);
                line.transform.SetAsFirstSibling();   // draw behind nodes
                line.Connect(
                    (RectTransform)fromNode.transform,
                    (RectTransform)toNode.transform);

                fromNode.RegisterLine(line);
                toNode.RegisterLine(line);
                _lines.Add(line);
            }
        }
    }

    // ── Fruchterman-Reingold Layout ──────────────────────────────────────────

    private Dictionary<string, Vector2> ComputeFRLayout()
    {
        var ids  = new List<string>();
        var pos  = new Dictionary<string, Vector2>();
        var disp = new Dictionary<string, Vector2>();

        // Build adjacency (edges = prerequisite relationships)
        var edges = new List<(string from, string to)>();
        foreach (var data in allNodeData)
        {
            ids.Add(data.id);
            // Random initial positions within the graph area
            pos[data.id] = new Vector2(
                Random.Range(-graphWidth  * 0.4f, graphWidth  * 0.4f),
                Random.Range(-graphHeight * 0.4f, graphHeight * 0.4f));
            disp[data.id] = Vector2.zero;

            if (data.prerequisiteIds != null)
                foreach (string prereq in data.prerequisiteIds)
                    edges.Add((prereq, data.id));
        }

        if (ids.Count == 0) return pos;

        // Ideal edge length
        float area = graphWidth * graphHeight;
        float k    = Mathf.Sqrt(area / Mathf.Max(ids.Count, 1));
        float temp = frTemperature;

        for (int iter = 0; iter < frIterations; iter++)
        {
            // Reset displacements
            foreach (string id in ids) disp[id] = Vector2.zero;

            // Repulsive forces between all pairs
            for (int i = 0; i < ids.Count; i++)
            for (int j = i + 1; j < ids.Count; j++)
            {
                string a = ids[i], b = ids[j];
                Vector2 delta = pos[a] - pos[b];
                float   dist  = Mathf.Max(delta.magnitude, 0.01f);
                Vector2 repF  = delta.normalized * (k * k / dist);
                disp[a] += repF;
                disp[b] -= repF;
            }

            // Attractive forces along edges
            foreach (var (from, to) in edges)
            {
                if (!pos.ContainsKey(from) || !pos.ContainsKey(to)) continue;
                Vector2 delta = pos[to] - pos[from];
                float   dist  = Mathf.Max(delta.magnitude, 0.01f);
                Vector2 attF  = delta.normalized * (dist * dist / k);
                disp[to]   -= attF;
                disp[from] += attF;
            }

            // Apply displacements, capped by temperature
            foreach (string id in ids)
            {
                Vector2 d  = disp[id];
                float   dm = Mathf.Min(d.magnitude, temp);
                pos[id] += d.normalized * dm;

                // Clamp to graph bounds
                pos[id] = new Vector2(
                    Mathf.Clamp(pos[id].x, -graphWidth  * 0.5f, graphWidth  * 0.5f),
                    Mathf.Clamp(pos[id].y, -graphHeight * 0.5f, graphHeight * 0.5f));
            }

            // Cool down
            temp *= frCooling;
        }

        return pos;
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public void MarkUnlocked(string id) => _unlocked.Add(id);
    public bool IsUnlocked(string id)   => _unlocked.Contains(id);

    /// True if the node's prerequisites are all unlocked AND it's not already unlocked.
    public bool CanUnlock(string id)
    {
        if (_unlocked.Contains(id)) return false;

        TechNodeData data = System.Array.Find(allNodeData, n => n.id == id);
        if (data == null) return false;
        if (data.prerequisiteIds == null || data.prerequisiteIds.Length == 0) return true;

        foreach (string prereq in data.prerequisiteIds)
            if (!_unlocked.Contains(prereq)) return false;

        return true;
    }

    public List<string> GetAllUnlockedIds() => new List<string>(_unlocked);

    /// Called by GameManager after loading a save.
    public void RestoreUnlocked(List<string> ids)
    {
        if (ids == null) return;
        foreach (string id in ids)
        {
            _unlocked.Add(id);
            if (_nodes.TryGetValue(id, out TechNode node))
            {
                node.ApplyEffect();
                node.ForceUnlocked();
            }
        }
        // Refresh all node visuals now that unlock state is known
        foreach (var node in _nodes.Values)
            node.RefreshVisuals();
    }
}
