using System;
using System.Collections;
using System.Collections.Generic;
using Dreamteck.Splines;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class SplinePathData
{
    public int PlatformIndex;
    public int GridIndex;
    public List<Vector3> ControlPoints = new();
}

public class SplinePathBuilder : MonoBehaviour
{
    [BoxGroup("References"), SerializeField] SplineComputer m_Spline;
    [BoxGroup("References"), SerializeField] List<Transform> m_Platforms;
    [BoxGroup("References"), SerializeField] GridPositioner m_Grid;

    [BoxGroup("Settings"), SerializeField] float m_ZOffset = 2f;
    [BoxGroup("Settings"), SerializeField] Color m_DebugColor = Color.yellow;
    [BoxGroup("Settings"), SerializeField] float m_MiddleOffset = 1f;
    [BoxGroup("Settings"), SerializeField] int m_Resolution = 20;

    [BoxGroup("Node References")] public Transform m_NodeIn1;
    [BoxGroup("Node References")] public Transform m_NodeIn2;
    [BoxGroup("Node References")] public Transform m_NodeOut1;
    [BoxGroup("Node References")] public Transform m_NodeOut2;

    private readonly Dictionary<(int, int), SplinePathData> m_BuiltPaths = new();

    public void UpdateNodePositions(Vector3 startPos, Vector3 endPos, float zOffset)
    {
        Vector3 zOffsetVec = new Vector3(0, 0, m_MiddleOffset);
        // base positions
        m_NodeOut1.position = endPos;
        m_NodeOut2.position = endPos + new Vector3(0, 0, -zOffset) + zOffsetVec;
        m_NodeIn1.position = startPos;
        m_NodeIn2.position = startPos + new Vector3(0, 0, zOffset) + zOffsetVec;

        // apply middle offset sideways (x-axis example)
        Vector3 mid = (startPos + endPos) / 2f;
        Vector3 dir = (endPos - startPos).normalized;
        Vector3 side = Vector3.Cross(dir, Vector3.up).normalized; // perpendicular sideways vector
        

        m_Spline.RebuildImmediate();
    }

    [Button("Build & Save Single Path")]
    public void BuildAndSavePath(int platformIndex, int gridX)
    {
        if (platformIndex < 0 || platformIndex >= m_Platforms.Count) return;
        if (m_Grid == null || m_Spline == null) return;

        Vector3 platformPos = m_Platforms[platformIndex].position;
        Vector3 gridWorldPos = m_Grid.GetWorldPosition(new Vector2Int(gridX, 0));

        StartCoroutine(BuildPathRoutine(platformIndex, gridX, platformPos, gridWorldPos));
    }

    [Button("Build & Save All Paths")]
    public void BuildAndSaveAllPaths()
    {
        if (m_Grid == null || m_Spline == null || m_Platforms == null) return;

        StartCoroutine(BuildAllPathsRoutine());
    }

    private IEnumerator BuildPathRoutine(int platformIndex, int gridX, Vector3 startPos, Vector3 endPos)
    {
        UpdateNodePositions(startPos, endPos, m_ZOffset);

        // allow spline to rebuild internally
        yield return null;

        var sampledPoints = new List<Vector3>();
        for (int i = 0; i <= m_Resolution; i++)
        {
            double percent = i / (double)m_Resolution;
            var result = m_Spline.EvaluatePosition(percent);
            sampledPoints.Add(result);
        }

        m_BuiltPaths[(platformIndex, gridX)] = new SplinePathData
        {
            PlatformIndex = platformIndex,
            GridIndex = gridX,
            ControlPoints = sampledPoints
        };

        //Debug.Log($"✅ Saved path for Platform {platformIndex}, Grid {gridX} with {sampledPoints.Count} points");
    }

    private IEnumerator BuildAllPathsRoutine()
    {
        m_BuiltPaths.Clear();

        for (int p = 0; p < m_Platforms.Count; p++)
        {
            for (int g = 0; g < m_Grid.columns; g++)
            {
                Vector3 platformPos = m_Platforms[p].position;
                Vector3 gridWorldPos = m_Grid.GetWorldPosition(new Vector2Int(g, 0));

                yield return BuildPathRoutine(p, g, platformPos, gridWorldPos);

                // tiny delay for safety
                yield return new WaitForSeconds(0.02f);
            }
        }

        Debug.Log($"✅ Built all paths. Total saved: {m_BuiltPaths.Count}");
    }

    public List<Vector3> GetPath(int platformIndex, int gridIndex)
    {
        return m_BuiltPaths.TryGetValue((platformIndex, gridIndex), out var data)
            ? data.ControlPoints
            : null;
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = m_DebugColor;
        foreach (var kvp in m_BuiltPaths)
        {
            var path = kvp.Value.ControlPoints;
            for (int i = 0; i < path.Count - 1; i++)
            {
                Gizmos.DrawSphere(path[i], 0.05f);
                Gizmos.DrawLine(path[i], path[i + 1]);
            }
        }
    }
}
