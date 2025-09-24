using System.Collections.Generic;
using Sirenix.OdinInspector.Editor.Drawers;
using UnityEngine;

public class StickmenBoardManager : MonoBehaviour
{
    [SerializeField] Dictionary<Vector2Int, StickmanGroup> board = new();
    [SerializeField] Transform m_GroupHolder;
    [SerializeField] GridPositioner grid;

    void Start()
    {
        RegisterGroupsFromChildren();
        LinkNeighbors();
    }

    private void RegisterGroupsFromChildren()
    {
        if (m_GroupHolder == null || grid == null)
        {
            Debug.LogError("Missing references for GroupHolder or GridPositioner");
            return;
        }

        for (int i = 0; i < m_GroupHolder.childCount; i++)
        {
            Transform child = m_GroupHolder.GetChild(i);
            var group = child.GetComponent<StickmanGroup>();

            if (group == null) continue;

            int row = i / grid.columns;
            int col = i % grid.columns;

            RegisterGroup(group, row, col);
        }
    }

    private void RegisterGroup(StickmanGroup group, int row, int col)
    {
        var key = new Vector2Int(col, row);
        
        if (board.ContainsKey(key))
        {
            Debug.LogWarning($"Overwriting existing group at {row},{col}");
        }
        if (row == 0)
        {
            group.IsTopEmpty = true;
        }
        
        group.GameplayInit(key, grid.GetGridDistance());
        
        board[key] = group;
    }

    private void LinkNeighbors()
    {
        foreach (var kvp in board)
        {
            Vector2Int pos = kvp.Key;
            StickmanGroup group = kvp.Value;
            
            TryLink(group, pos, new Vector2Int(pos.x - 1, pos.y), Direction.Left);
            TryLink(group, pos, new Vector2Int(pos.x + 1, pos.y), Direction.Right);
            TryLink(group, pos, new Vector2Int(pos.x, pos.y + 1), Direction.Top);
            TryLink(group, pos, new Vector2Int(pos.x, pos.y - 1), Direction.Bottom);
        }
    }

    private void TryLink(StickmanGroup source, Vector2Int sourcePos, Vector2Int neighborPos, Direction dir)
    {
        if (board.TryGetValue(neighborPos, out var neighbor))
        {
            if (neighbor.GroupColor == source.GroupColor)
            {
                source.AddNeighbor(dir, neighbor);
            }
        }
    }

    public StickmanGroup GetGroup(Vector2Int val)
    {
        return board.TryGetValue(val, out var group) ? group : null;
    }
    
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return grid.GetWorldPosition(gridPos); 
        // assuming your GridPositioner has such a method
    }

    public bool DoesGridContainsGroup(Vector2Int gridPos)
    {
        return board[gridPos] != null;
    }
    
    public GroupExitData CanGroupGridExit(List<Vector2Int> grids)
    {
        foreach (var gridPos in grids)
        {
            if (gridPos.y == 0)
            {
                return new GroupExitData()
                {
                    CanExit = true,
                    GridExitStartPoint = gridPos
                };
            }
            
            Vector2Int[] neighbors =
            {
                new Vector2Int(gridPos.x - 1, gridPos.y),
                new Vector2Int(gridPos.x + 1, gridPos.y),
                new Vector2Int(gridPos.x, gridPos.y - 1),
                new Vector2Int(gridPos.x, gridPos.y + 1)
            };

            foreach (var neighbor in neighbors)
            {
                if (!board.ContainsKey(neighbor)) 
                    continue;
                
                if (board[neighbor] == null)
                    return new GroupExitData()
                    {
                        CanExit = true,
                        GridExitStartPoint = gridPos
                    };
            }
        }

        return new GroupExitData()
        {
            CanExit = false,
        };
    }
    
    
    public void GetGroupExitPoints(ref GroupExitData exitData)
    {
        exitData.GridExitPointsOrdered = new List<Vector2Int>();

        if (exitData == null || !exitData.CanExit) return;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        Vector2Int start = exitData.GridExitStartPoint;
        queue.Enqueue(start);
        visited.Add(start);

        Vector2Int? exitPoint = null;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            
            if (current.y == 0)
            {
                exitPoint = current;
                break;
            }

            // 4 neighbors
            Vector2Int[] neighbors =
            {
                new Vector2Int(current.x - 1, current.y),
                new Vector2Int(current.x + 1, current.y),
                new Vector2Int(current.x, current.y - 1),
                new Vector2Int(current.x, current.y + 1)
            };

            foreach (var n in neighbors)
            {
                if (visited.Contains(n)) continue;

                if (!board.ContainsKey(n)) continue;
                
                if (board[n] == null)
                {
                    queue.Enqueue(n);
                    visited.Add(n);
                    cameFrom[n] = current;
                }
            }
        }
        
        if (exitPoint.HasValue)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            Vector2Int step = exitPoint.Value;

            while (step != start)
            {
                path.Add(step);
                step = cameFrom[step];
            }
            path.Add(start);
            path.Reverse();

            exitData.GridExitPointsOrdered = path;
        }
    }
    
    public class ExitRecommendation
    {
        public StickmanGroup Group;
        public List<Vector2Int> Path;
    }

    public List<ExitRecommendation> GetRecommendedExitOrder(
        List<StickmanGroup> groups, ColorType sourceColor)
    {
        var results = new List<ExitRecommendation>();

        foreach (var group in groups)
        {
            var startGrid = group.GroupGridLoc; 

            var path = GetSnakeExitPath(startGrid, group, sourceColor);
            if (path.Count > 0)
            {
                results.Add(new ExitRecommendation
                {
                    Group = group,
                    Path = path
                });
            }
        }
        
        results.Sort((a, b) => a.Path.Count.CompareTo(b.Path.Count));

        return results;
    }
    
    private Dictionary<StickmanGroup, List<Vector2Int>> groupExitCache = new();

    /// <summary>
    /// Get the snake-like path for a specific cell in a group.
    /// </summary>
    public List<Vector2Int> GetSnakeExitPath(Vector2Int start, StickmanGroup group, ColorType sourceColor)
    {
        // ✅ If group already has a canonical exit path
        if (groupExitCache.TryGetValue(group, out var exitPath))
        {
            // Merge this start cell into the existing path
            return MergeIntoExitPath(start, exitPath, sourceColor);
        }

        // ✅ Otherwise run BFS normally to find first exit
        var bfsPath = FindPathToExit(start, sourceColor);

        if (bfsPath.Count > 0)
        {
            // store as canonical exit path for this group
            groupExitCache[group] = bfsPath;
        }

        return bfsPath;
    }

    /// <summary>
    /// BFS from a single start cell to the nearest exit (* in row y=0).
    /// </summary>
    private List<Vector2Int> FindPathToExit(Vector2Int start, ColorType sourceColor)
    {
        var queue = new Queue<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var visited = new HashSet<Vector2Int>();

        queue.Enqueue(start);
        visited.Add(start);

        Vector2Int exitFound = new(-1, -1);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            // ✅ exit condition
            if (current.y == 0 && board[current] == null)
            {
                exitFound = current;
                break;
            }

            Vector2Int[] neighbors =
            {
                new Vector2Int(current.x - 1, current.y),
                new Vector2Int(current.x + 1, current.y),
                new Vector2Int(current.x, current.y - 1),
                new Vector2Int(current.x, current.y + 1)
            };

            foreach (var n in neighbors)
            {
                if (visited.Contains(n)) continue;
                if (!board.ContainsKey(n)) continue;

                if (board[n] == null || board[n].GroupColor == sourceColor)
                {
                    queue.Enqueue(n);
                    visited.Add(n);
                    cameFrom[n] = current;
                }
            }
        }

        // ✅ rebuild path
        var path = new List<Vector2Int>();
        if (exitFound == new Vector2Int(-1, -1)) return path;

        var step = exitFound;
        path.Add(step);
        while (cameFrom.ContainsKey(step))
        {
            step = cameFrom[step];
            path.Add(step);
        }

        path.Reverse();
        return path;
    }

    /// <summary>
    /// Merge another cell into the existing canonical exit path.
    /// </summary>
    private List<Vector2Int> MergeIntoExitPath(Vector2Int start, List<Vector2Int> exitPath, ColorType sourceColor)
    {
        // BFS until we hit any node already in the canonical path
        var queue = new Queue<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var visited = new HashSet<Vector2Int>();

        queue.Enqueue(start);
        visited.Add(start);

        Vector2Int mergePoint = new(-1, -1);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (exitPath.Contains(current))
            {
                mergePoint = current;
                break;
            }

            Vector2Int[] neighbors =
            {
                new Vector2Int(current.x - 1, current.y),
                new Vector2Int(current.x + 1, current.y),
                new Vector2Int(current.x, current.y - 1),
                new Vector2Int(current.x, current.y + 1)
            };

            foreach (var n in neighbors)
            {
                if (visited.Contains(n)) continue;
                if (!board.ContainsKey(n)) continue;

                if (board[n] == null || board[n].GroupColor == sourceColor)
                {
                    queue.Enqueue(n);
                    visited.Add(n);
                    cameFrom[n] = current;
                }
            }
        }

        // ✅ rebuild path from start → mergePoint
        var mergePath = new List<Vector2Int>();
        if (mergePoint == new Vector2Int(-1, -1)) return mergePath;

        var step = mergePoint;
        mergePath.Add(step);
        while (cameFrom.ContainsKey(step))
        {
            step = cameFrom[step];
            mergePath.Add(step);
        }
        mergePath.Add(start);

        mergePath.Reverse();

        // ✅ then append canonical exit path starting at mergePoint
        int mergeIndex = exitPath.IndexOf(mergePoint);
        for (int i = mergeIndex + 1; i < exitPath.Count; i++)
        {
            mergePath.Add(exitPath[i]);
        }

        return mergePath;
    }
    
    public List<Vector2Int> GetPathToExitStart(Vector2Int start, Vector2Int target, ColorType sourceColor)
    {
        var queue = new Queue<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var visited = new HashSet<Vector2Int>();

        queue.Enqueue(start);
        visited.Add(start);

        bool reached = false;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current == target)
            {
                reached = true;
                break;
            }

            Vector2Int[] neighbors =
            {
                new Vector2Int(current.x - 1, current.y),
                new Vector2Int(current.x + 1, current.y),
                new Vector2Int(current.x, current.y - 1),
                new Vector2Int(current.x, current.y + 1)
            };

            foreach (var n in neighbors)
            {
                if (visited.Contains(n)) continue;
                if (!board.ContainsKey(n)) continue;

                if (board[n] == null || board[n].GroupColor == sourceColor)
                {
                    queue.Enqueue(n);
                    visited.Add(n);
                    cameFrom[n] = current;

                    // ✅ If we just reached target, stop immediately
                    if (n == target)
                    {
                        reached = true;
                        queue.Clear();
                        break;
                    }
                }
            }
        }

        // ✅ rebuild path
        var path = new List<Vector2Int>();
        if (!reached) return path;

        var step = target;
        path.Add(step);
        while (cameFrom.ContainsKey(step))
        {
            step = cameFrom[step];
            path.Add(step);
        }

        path.Reverse();
        return path;
    }
    
    public void EmptyGrids(List<Vector2Int> grids)
    {
        foreach (var grid in grids)
        {
            board[grid] = null;
        }
    }
    
    public void ClearAllExitCaches()
    {
        groupExitCache.Clear();
    }
}

public class GroupExitData
{
    public bool CanExit;
    public Vector2Int GridExitStartPoint;
    public List<Vector2Int> GridExitPointsOrdered;
}
