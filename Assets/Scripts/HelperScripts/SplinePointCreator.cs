using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class SplinePointCreator : MonoBehaviour
{
    [BoxGroup("Settings"), SerializeField] int m_PointResolution = 10;
    [BoxGroup("Settings"), SerializeField] float m_HandleDistance = 2f;
    [BoxGroup("Settings"), SerializeField] float m_Offset = 2f;
    [BoxGroup("Settings"), SerializeField] Color m_DebugColor = Color.cyan;

    [BoxGroup("References"), SerializeField] Transform m_GroupFollowerParent;

    [BoxGroup("References"), SerializeField] GroupFollower m_GroupFollower;

    private readonly Dictionary<(int, int), List<Vector3>> m_Paths = new();
    private readonly Dictionary<(int, int), List<Vector3>> m_DebugPoints = new();

    public void Init()
    {
        m_Paths.Clear();
        m_DebugPoints.Clear();
    }

    [Button("Build Pathways")]
    public void BuildPathways(List<Vector3> platforms, List<Vector3> gridLocs)
    {
        Init();
        if (platforms == null || platforms.Count == 0 || gridLocs == null || gridLocs.Count == 0)
        {
            Debug.LogError("❌ Missing platforms or grid locations");
            return;
        }

        for (int p = 0; p < platforms.Count; p++)
        {
            for (int g = 0; g < gridLocs.Count; g++)
            {
                var path = BuildCurvedSegment(platforms[p], gridLocs[g]);
                m_Paths[(p, g)] = path;
                m_DebugPoints[(p, g)] = new List<Vector3>(path);
            }
        }
    }

    private List<Vector3> BuildCurvedSegment(Vector3 start, Vector3 end)
    {
        var positions = new List<Vector3>();

        // Direction from start → end
        Vector3 dir = (end - start).normalized;
        float dist = Vector3.Distance(start, end);

        // Offset distance (cannot be bigger than half the distance)
        float offset = Mathf.Min(m_HandleDistance, dist * 0.5f);

        // 🔹 Extra Z offset you can tweak in inspector
        // Positive = push forward, Negative = pull back
        Vector3 zOffsetVec = new Vector3(0, 0, m_Offset);

        // Points
        Vector3 p0 = start;
        Vector3 p1 = start + new Vector3(0, 0, offset) + zOffsetVec; // lead-out with extra offset
        Vector3 p2 = end   - new Vector3(0, 0, offset) + zOffsetVec; // lead-in with extra offset
        Vector3 p3 = end;

        positions.Add(p0);
        positions.Add(p1);
        positions.Add(p2);
        positions.Add(p3);

        return positions;
    }





    private Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        return (uuu * p0) +
               (3 * uu * t * p1) +
               (3 * u * tt * p2) +
               (ttt * p3);
    }


    public List<Vector3> GetPath(int platformIndex, int gridIndex)
    {
        return m_Paths.TryGetValue((platformIndex, gridIndex), out var path) ? path : null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = m_DebugColor;
        foreach (var kvp in m_DebugPoints)
        {
            var path = kvp.Value;
            for (int i = 0; i < path.Count - 1; i++)
            {
                Gizmos.DrawSphere(path[i], 0.05f);
                Gizmos.DrawLine(path[i], path[i + 1]);
            }
        }
    }
}
