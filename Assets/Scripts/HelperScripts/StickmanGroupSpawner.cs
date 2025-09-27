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
    [BoxGroup("Settings"), SerializeField, Min(0)] int m_ProtectedTopRows = 1;

    public static event System.Action<StickmanGroup, int, int> OnGroupSpawned;

    // -------- New: Generation Controls ----------
    public enum GenerationMode { PureRandom, NeighborBias, ClusterGrow, PerlinIslands }

    [BoxGroup("Generation"), SerializeField] GenerationMode m_Mode = GenerationMode.NeighborBias;

    [BoxGroup("Generation"), SerializeField, Min(2)]
    int m_NumColors = 4;

    [BoxGroup("Generation"), ShowIf("@m_Mode == GenerationMode.NeighborBias"), Range(0f, 1f)]
    float m_NeighborBias = 0.7f; // chance to copy a neighbor’s color

    [BoxGroup("Generation"), ShowIf("@m_Mode == GenerationMode.ClusterGrow"), Min(1)]
    int m_ClusterCount = 6;

    [BoxGroup("Generation"), ShowIf("@m_Mode == GenerationMode.ClusterGrow")]
    Vector2Int m_ClusterSize = new Vector2Int(4, 10);

    [BoxGroup("Generation"), ShowIf("@m_Mode == GenerationMode.PerlinIslands"), Min(0.1f)]
    float m_PerlinScale = 1.8f;

    [BoxGroup("Generation"), SerializeField]
    int m_Seed = 12345;

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
        int rows = Mathf.CeilToInt((float)total / cols);

        // Eligible cells for obstacles (exclude protected top rows)
        var eligible = new List<int>(total);
        for (int i = 0; i < total; i++)
        {
            int row = i / cols;
            if (row >= m_ProtectedTopRows) eligible.Add(i);
        }

        int obstacleTarget = Mathf.Clamp(m_ObstacleCount, 0, eligible.Count);
        Shuffle(eligible);
        var obstacleIndices = new HashSet<int>(eligible.Take(obstacleTarget));

        // ---- Generate colors for non-obstacle cells ----
        var rng = new System.Random(m_Seed);
        var palette = BuildPaletteDistinct(rng, m_NumColors);
        var colors = GenerateColorField(rows, cols, obstacleIndices, palette, rng);

        // ---- Instantiate ----
        for (int i = 0; i < total; i++)
        {
            Vector3 localPos = m_GridPositioner.SavedPositions[i];
            int row = i / cols;
            int col = i % cols;

            if (obstacleIndices.Contains(i))
            {
                var obstacle = Instantiate(m_Obstacle, m_GridPositioner.transform);
                m_GridPositioner.SetInPos(obstacle.gameObject, localPos);
                var go = obstacle.GetComponent<GridObject>();
                if (go != null) go.GridObjectType = GridObjectType.Crate;
            }
            else
            {
                var group = Instantiate(m_StickmanGroup, m_GridPositioner.transform);

                // Assign chosen color
                ColorType c = colors[i];
                // Fallback if NONE slipped through
                if (c == ColorType.NONE) c = palette[rng.Next(palette.Count)];

                group.Init(c);
                m_GridPositioner.SetInPos(group.gameObject, localPos);

                var go = group.GetComponent<GridObject>();
                if (go != null) go.GridObjectType = GridObjectType.StickmanGroup;

                OnGroupSpawned?.Invoke(group, row, col);
            }
        }
    }

    // --------------------------------------
    // Color Field Generation
    // --------------------------------------
    private List<ColorType> BuildPaletteDistinct(System.Random rng, int count)
    {
        // Build a set of distinct ColorTypes using your StickmanColors palette.
        var set = new HashSet<ColorType>();
        // Guard in case enum has limited values; avoid infinite loop.
        int safety = 500;
        while (set.Count < count && safety-- > 0)
        {
            set.Add(StickmanColors.Instance.GetRandomColor()); // assumes returns ColorType
        }
        if (set.Count == 0) set.Add(ColorType.NONE);
        return set.ToList();
    }

    private ColorType[] GenerateColorField(
        int rows,
        int cols,
        HashSet<int> obstacles,
        List<ColorType> palette,
        System.Random rng)
    {
        int total = rows * cols;
        var outColors = new ColorType[total];
        for (int i = 0; i < total; i++) outColors[i] = ColorType.NONE;

        switch (m_Mode)
        {
            case GenerationMode.PureRandom:
                for (int i = 0; i < total; i++)
                {
                    if (obstacles.Contains(i)) continue;
                    outColors[i] = palette[rng.Next(palette.Count)];
                }
                break;

            case GenerationMode.NeighborBias:
                for (int i = 0; i < total; i++)
                {
                    if (obstacles.Contains(i)) continue;

                    int r = i / cols;
                    int c = i % cols;
                    int left = (c > 0) ? i - 1 : -1;
                    int up = (r > 0) ? i - cols : -1;

                    var neighborCandidates = new List<ColorType>(2);
                    if (left >= 0 && !obstacles.Contains(left) && outColors[left] != ColorType.NONE)
                        neighborCandidates.Add(outColors[left]);
                    if (up >= 0 && !obstacles.Contains(up) && outColors[up] != ColorType.NONE)
                        neighborCandidates.Add(outColors[up]);

                    bool useNeighbor = neighborCandidates.Count > 0 && rng.NextDouble() < m_NeighborBias;
                    if (useNeighbor)
                    {
                        outColors[i] = neighborCandidates[rng.Next(neighborCandidates.Count)];
                    }
                    else
                    {
                        outColors[i] = palette[rng.Next(palette.Count)];
                    }
                }
                break;

            case GenerationMode.ClusterGrow:
                {
                    // Available cells set (exclude obstacles)
                    var remaining = new HashSet<int>(Enumerable.Range(0, total).Where(idx => !obstacles.Contains(idx)));

                    int minSize = Mathf.Max(1, Mathf.Min(m_ClusterSize.x, m_ClusterSize.y));
                    int maxSize = Mathf.Max(minSize, Mathf.Max(m_ClusterSize.x, m_ClusterSize.y));

                    for (int k = 0; k < m_ClusterCount && remaining.Count > 0; k++)
                    {
                        // Pick a random seed
                        int seedIdx = remaining.ElementAt(rng.Next(remaining.Count));
                        var color = palette[rng.Next(palette.Count)];

                        // Grow cluster via BFS
                        int targetSize = rng.Next(minSize, maxSize + 1);
                        var cluster = GrowCluster(seedIdx, targetSize, rows, cols, remaining, obstacles, rng);

                        foreach (var idx in cluster) outColors[idx] = color;
                    }

                    // Fill any leftover cells by copying a colored neighbor (or random)
                    foreach (var idx in Enumerable.Range(0, total))
                    {
                        if (obstacles.Contains(idx)) continue;
                        if (outColors[idx] != ColorType.NONE) continue;

                        var neigh = GetNeighbors4(idx, rows, cols);
                        var colored = neigh.Where(n => !obstacles.Contains(n) && outColors[n] != ColorType.NONE).ToList();
                        if (colored.Count > 0)
                            outColors[idx] = outColors[colored[rng.Next(colored.Count)]];
                        else
                            outColors[idx] = palette[rng.Next(palette.Count)];
                    }
                }
                break;

            case GenerationMode.PerlinIslands:
                {
                    // Build a stable index order for palette
                    var pal = palette.ToArray();
                    for (int i = 0; i < total; i++)
                    {
                        if (obstacles.Contains(i)) continue;
                        int r = i / cols;
                        int c = i % cols;

                        float nx = (float)rng.NextDouble() * 1000f; // each board gets unique offset
                        float nz = (float)rng.NextDouble() * 1000f;

                        // Sample once with fixed offsets to keep reproducible pattern for the seed
                        float v = Mathf.PerlinNoise((c + nx) / m_PerlinScale, (r + nz) / m_PerlinScale);
                        int band = Mathf.Clamp(Mathf.FloorToInt(v * pal.Length), 0, pal.Length - 1);
                        outColors[i] = pal[band];
                    }

                    // Optional: one smoothing pass to reinforce islands (majority of 4-neighbors)
                    var copy = (ColorType[])outColors.Clone();
                    for (int i = 0; i < total; i++)
                    {
                        if (obstacles.Contains(i)) continue;
                        var neigh = GetNeighbors4(i, rows, cols);
                        var counts = new Dictionary<ColorType, int>();
                        foreach (var n in neigh)
                        {
                            if (obstacles.Contains(n)) continue;
                            var col = copy[n];
                            if (col == ColorType.NONE) continue;
                            counts[col] = counts.TryGetValue(col, out int ct) ? ct + 1 : 1;
                        }
                        if (counts.Count > 0)
                        {
                            var kv = counts.OrderByDescending(p => p.Value).First();
                            // Only overwrite if there’s a clear majority (>=3 of 4)
                            if (kv.Value >= 3) outColors[i] = kv.Key;
                        }
                    }
                }
                break;
        }

        return outColors;
    }

    private List<int> GrowCluster(
        int seedIdx,
        int targetSize,
        int rows,
        int cols,
        HashSet<int> remaining,
        HashSet<int> obstacles,
        System.Random rng)
    {
        var cluster = new List<int>(targetSize);
        var q = new Queue<int>();
        if (!remaining.Contains(seedIdx)) return cluster;

        q.Enqueue(seedIdx);
        remaining.Remove(seedIdx);

        while (q.Count > 0 && cluster.Count < targetSize)
        {
            int cur = q.Dequeue();
            cluster.Add(cur);

            // Randomize neighbor order for organic shapes
            var neigh = GetNeighbors4(cur, rows, cols);
            Shuffle(neigh, rng);

            foreach (var n in neigh)
            {
                if (cluster.Count >= targetSize) break;
                if (obstacles.Contains(n)) continue;
                if (!remaining.Contains(n)) continue;

                remaining.Remove(n);
                q.Enqueue(n);
            }
        }

        return cluster;
    }

    private static List<int> GetNeighbors4(int idx, int rows, int cols)
    {
        int r = idx / cols;
        int c = idx % cols;
        var list = new List<int>(4);
        if (c > 0) list.Add(idx - 1);
        if (c < cols - 1) list.Add(idx + 1);
        if (r > 0) list.Add(idx - cols);
        if (r < rows - 1) list.Add(idx + cols);
        return list;
    }

    [Button("Clear Spawned Groups (Editor)", ButtonSizes.Large)]
    void ClearExisting()
    {
        if (m_GridPositioner == null) return;

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

        if (m_Obstacle != null)
        {
            var obstacleName = m_Obstacle.name;
            var allChildren = m_GridPositioner.GetComponentsInChildren<Transform>(true);
            foreach (var t in allChildren)
            {
                if (t == m_GridPositioner.transform) continue;
                if (t.GetComponent<StickmanGroup>() != null) continue;
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

    // UnityEngine.Random shuffle (Fisher–Yates)
    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // System.Random overload (for deterministic steps)
    private static void Shuffle<T>(IList<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
