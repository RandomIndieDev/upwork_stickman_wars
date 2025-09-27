using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class StickmenBoardManager : MonoBehaviour
{
    [SerializeField] private Dictionary<Vector2Int, GridObject> board = new();
    [SerializeField] private Transform m_GroupHolder;
    [SerializeField] private GridPositioner grid;

    private Dictionary<StickmanGroup, List<Vector2Int>> groupExitCache = new();

    void Start()
    {
        RegisterObjectsFromChildren();
        LinkNeighbors();
    }

    private void RegisterObjectsFromChildren()
    {
        if (m_GroupHolder == null || grid == null)
        {
            Debug.LogError("❌ Missing references for GroupHolder or GridPositioner");
            return;
        }

        for (int i = 0; i < m_GroupHolder.childCount; i++)
        {
            Transform child = m_GroupHolder.GetChild(i);
            var gridObj = child.GetComponent<GridObject>();
            if (gridObj == null) continue;

            int row = i / grid.columns;
            int col = i % grid.columns;

            RegisterObject(gridObj, row, col);
        }
    }

    private void RegisterObject(GridObject gridObj, int row, int col)
    {
        var key = new Vector2Int(col, row);

        if (board.ContainsKey(key))
        {
            Debug.LogWarning($"⚠️ Overwriting existing object at {row},{col}");
        }

        if (gridObj.GridObjectType == GridObjectType.StickmanGroup && gridObj.GetStickManGroup() != null)
        {
            var group = gridObj.GetStickManGroup();
            if (row == 0) group.IsTopEmpty = true;

            group.GameplayInit(key);
        }

        board[key] = gridObj;
    }

    private void LinkNeighbors()
    {
        foreach (var kvp in board)
        {
            Vector2Int pos = kvp.Key;
            GridObject obj = kvp.Value;

            if (obj == null || obj.GridObjectType != GridObjectType.StickmanGroup)
                continue;

            var group = obj.GetStickManGroup();
            if (group == null) continue;

            TryLink(group, pos, new Vector2Int(pos.x - 1, pos.y), Direction.Left);
            TryLink(group, pos, new Vector2Int(pos.x + 1, pos.y), Direction.Right);
            TryLink(group, pos, new Vector2Int(pos.x, pos.y + 1), Direction.Top);
            TryLink(group, pos, new Vector2Int(pos.x, pos.y - 1), Direction.Bottom);
        }
    }

    private void TryLink(StickmanGroup source, Vector2Int sourcePos, Vector2Int neighborPos, Direction dir)
    {
        if (!board.TryGetValue(neighborPos, out var neighborObj)) return;

        if (neighborObj == null || neighborObj.GridObjectType != GridObjectType.StickmanGroup) return;

        var neighborGroup = neighborObj.GetStickManGroup();
        if (neighborGroup != null && neighborGroup.GroupColor == source.GroupColor)
        {
            source.AddNeighbor(dir, neighborGroup);
        }
    }

    public StickmanGroup GetGroup(Vector2Int pos)
    {
        if (board.TryGetValue(pos, out var gridObj) &&
            gridObj != null &&
            gridObj.GridObjectType == GridObjectType.StickmanGroup)
        {
            return gridObj.GetStickManGroup();
        }

        return null;
    }

    public bool IsObstacle(Vector2Int pos)
    {
        return board.TryGetValue(pos, out var gridObj) &&
               gridObj != null &&
               gridObj.GridObjectType == GridObjectType.Crate;
    }

    public bool IsEmpty(Vector2Int pos)
    {
        return !board.ContainsKey(pos) || board[pos] == null;
    }

    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return grid.GetWorldPosition(gridPos);
    }

    // 🔹 Traversal helper
    private bool CanTraverse(Vector2Int pos, ColorType sourceColor)
    {
        if (!board.ContainsKey(pos)) return false;

        var gridObj = board[pos];
        if (gridObj == null) return true; // empty cell is traversable

        if (gridObj.GridObjectType == GridObjectType.Crate) return false; // obstacle blocks

        var group = gridObj.GetStickManGroup();
        if (group == null) return false;

        return group.GroupColor == sourceColor;
    }

    // ------------------ EXIT / PATHFINDING ------------------

    public GroupExitData CanGroupGridExit(List<Vector2Int> grids)
    {
        foreach (var gridPos in grids)
        {
            // ✅ If any part of the group is already on the first row, it can exit
            if (gridPos.y == 0)
            {
                return new GroupExitData
                {
                    CanExit = true,
                    GridExitStartPoint = gridPos
                };
            }

            // Otherwise, can exit if any neighbor is empty
            Vector2Int[] neighbors =
            {
                new Vector2Int(gridPos.x - 1, gridPos.y),
                new Vector2Int(gridPos.x + 1, gridPos.y),
                new Vector2Int(gridPos.x, gridPos.y - 1),
                new Vector2Int(gridPos.x, gridPos.y + 1)
            };

            foreach (var neighbor in neighbors)
            {
                if (!board.ContainsKey(neighbor)) continue;

                if (IsEmpty(neighbor)) // empty = no GridObject there
                {
                    return new GroupExitData
                    {
                        CanExit = true,
                        GridExitStartPoint = gridPos
                    };
                }
            }
        }

        return new GroupExitData { CanExit = false };
    }


    public void GetGroupExitPoints(ref GroupExitData exitData, ColorType sourceColor)
    {
        exitData.GridExitPointsOrdered = new List<Vector2Int>();

        if (exitData == null || !exitData.CanExit) return;

        Queue<Vector2Int> queue = new();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new();
        HashSet<Vector2Int> visited = new();

        Vector2Int start = exitData.GridExitStartPoint;
        queue.Enqueue(start);
        visited.Add(start);

        Vector2Int? exitPoint = null;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current.y == 0 && IsEmpty(current))
            {
                exitPoint = current;
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
                if (CanTraverse(n, sourceColor))
                {
                    queue.Enqueue(n);
                    visited.Add(n);
                    cameFrom[n] = current;
                }
            }
        }

        if (exitPoint.HasValue)
        {
            List<Vector2Int> path = new();
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

    public List<Vector2Int> GetSnakeExitPath(Vector2Int start, StickmanGroup group, ColorType sourceColor)
    {
        if (groupExitCache.TryGetValue(group, out var exitPath))
        {
            return MergeIntoExitPath(start, exitPath, sourceColor);
        }
        
        var bfsPath = FindPathToExit(start, sourceColor);
        if (bfsPath.Count > 0)
        {
            groupExitCache[group] = bfsPath;
        }

        return bfsPath;
    }

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

            if (current.y == 0 && IsEmpty(current))
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
                if (CanTraverse(n, sourceColor))
                {
                    queue.Enqueue(n);
                    visited.Add(n);
                    cameFrom[n] = current;
                }
            }
        }

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

    private List<Vector2Int> MergeIntoExitPath(Vector2Int start, List<Vector2Int> exitPath, ColorType sourceColor)
    {
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
                if (CanTraverse(n, sourceColor))
                {
                    queue.Enqueue(n);
                    visited.Add(n);
                    cameFrom[n] = current;
                }
            }
        }

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

        int mergeIndex = exitPath.IndexOf(mergePoint);
        for (int i = mergeIndex + 1; i < exitPath.Count; i++)
        {
            mergePath.Add(exitPath[i]);
        }

        return mergePath;
    }
    
    public class ExitRecommendation
    {
        public Platform Platform;
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

        // ✅ shortest path first = most reachable group exits first
        results.Sort((a, b) => a.Path.Count.CompareTo(b.Path.Count));

        return results;
    }
    
    public GridObject GetGridObj(Vector2Int gridPos)
    {
        if (!board.TryGetValue(gridPos, out var obj) || obj == null)
            return null;

        return obj;
    }
    
    public GridObjectType GetGridType(Vector2Int gridPos)
    {
        if (!board.TryGetValue(gridPos, out var obj) || obj == null)
            return GridObjectType.NONE;

        return obj.GridObjectType;
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
