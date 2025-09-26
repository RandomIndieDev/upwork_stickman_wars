using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

[ExecuteAlways]
public class StickmanGroupSpawner : MonoBehaviour
{
    [BoxGroup("References"), SerializeField] GridPositioner m_GridPositioner;

    [BoxGroup("Prefabs"), SerializeField] StickmanGroup m_StickmanGroup;
    [BoxGroup("Prefabs"), SerializeField] Transform m_Obstacle;

    [BoxGroup("Settings"), SerializeField, Min(0)] int m_ObstacleCount = 0;
    [BoxGroup("Settings"), SerializeField, Min(0)] int m_ProtectedTopRows = 1; // rows at y=0..N-1 where no obstacles are allowed

    public static event System.Action<StickmanGroup, int, int> OnGroupSpawned;

    [Button("Spawn Groups (Editor)", ButtonSizes.Large)]
    public void SpawnGroups()
    {
        if (m_GridPositioner == null || m_StickmanGroup == null || m_Obstacle == null)
        {
            Debug.LogError("Spawner missing references.");
            return;
        }
        if (m_GridPositioner.SavedPositions == null || m_GridPositioner.SavedPositions.Count == 0)
        {
            Debug.LogError("No saved positions on GridPositioner. Click 'Save Positions' first.");
            return;
        }

        ClearExisting();

        int cols = m_GridPositioner.columns;
        int total = m_GridPositioner.SavedPositions.Count;

        // Build eligible cell indices (exclude protected top rows)
        var eligible = new List<int>(total);
        for (int i = 0; i < total; i++)
        {
            int row = i / cols; // row-major
            if (row >= m_ProtectedTopRows) eligible.Add(i);
        }

        // Clamp obstacle count to eligible cells
        int obstacleTarget = Mathf.Clamp(m_ObstacleCount, 0, eligible.Count);

        // Shuffle eligible indices and take the first N as obstacle positions
        Shuffle(eligible);
        var obstacleIndices = new HashSet<int>(eligible.Take(obstacleTarget));

        // Now instantiate across the whole grid
        for (int i = 0; i < total; i++)
        {
            Vector3 localPos = m_GridPositioner.SavedPositions[i];
            int row = i / cols;
            int col = i % cols;

            if (obstacleIndices.Contains(i))
            {
                var obstacle = Instantiate(m_Obstacle, m_GridPositioner.transform);
                m_GridPositioner.SetInPos(obstacle.gameObject, localPos);

                // Tag with GridObject if your obstacle prefab has it (recommended)
                var go = obstacle.GetComponent<GridObject>();
                if (go != null) go.GridObjectType = GridObjectType.Crate;
            }
            else
            {
                var group = Instantiate(m_StickmanGroup, m_GridPositioner.transform);
                group.Init(StickmanColors.Instance.GetRandomColor());
                m_GridPositioner.SetInPos(group.gameObject, localPos);

                // Tag with GridObject (recommended so your board manager can detect it)
                var go = group.GetComponent<GridObject>();
                if (go != null)
                {
                    go.GridObjectType = GridObjectType.StickmanGroup;
                }

                OnGroupSpawned?.Invoke(group, row, col);
            }
        }
    }

    [Button("Clear Spawned Groups (Editor)", ButtonSizes.Large)]
    void ClearExisting()
    {
        if (m_GridPositioner == null) return;

        // Prefer clearing anything that has a GridObject marker
        var allGridObjects = m_GridPositioner.GetComponentsInChildren<GridObject>(true);
        if (allGridObjects.Length > 0)
        {
            foreach (var obj in allGridObjects)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(obj.gameObject);
                else Destroy(obj.gameObject);
#else
                Destroy(obj.gameObject);
#endif
            }
            return;
        }

        // Fallback: clear StickmanGroup components (older prefabs)
        var groups = m_GridPositioner.GetComponentsInChildren<StickmanGroup>(true);
        foreach (var g in groups)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(g.gameObject);
            else Destroy(g.gameObject);
#else
            Destroy(g.gameObject);
#endif
        }

        // And clear any child with the same name as the obstacle prefab (best-effort fallback)
        if (m_Obstacle != null)
        {
            var obstacleName = m_Obstacle.name;
            var allChildren = m_GridPositioner.GetComponentsInChildren<Transform>(true);
            foreach (var t in allChildren)
            {
                if (t == m_GridPositioner.transform) continue;
                if (t.GetComponent<StickmanGroup>() != null) continue; // already handled
                if (t.name.StartsWith(obstacleName))
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying) DestroyImmediate(t.gameObject);
                    else Destroy(t.gameObject);
#else
                    Destroy(t.gameObject);
#endif
                }
            }
        }
    }

    // Fisher–Yates
    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
