using System;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Collections.Generic;
using CW.Common;
using DG.Tweening;
using UnityEditor.UIElements;

public class EnemyGridManager : MonoBehaviour
{
    [BoxGroup("References"), SerializeField] Transform m_GroupHolder;
    [BoxGroup("References"), SerializeField] GridPositioner m_GridPositioner;

    Dictionary<Vector2Int, StickmanGroup> board = new();

    public Action OnGridReady;
    public Action OnGridUpdated;
    

    void Start()
    {
        RegisterGroupsFromChildren();
        LinkNeighbors();
    }

    void RegisterGroupsFromChildren()
    {
        if (m_GroupHolder == null || m_GridPositioner == null)
        {
            Debug.LogError("Missing references for GroupHolder or GridPositioner");
            return;
        }

        for (int i = 0; i < m_GroupHolder.childCount; i++)
        {
            Transform child = m_GroupHolder.GetChild(i);
            var group = child.GetComponent<StickmanGroup>();

            if (group == null) continue;

            int row = i / m_GridPositioner.columns;
            int col = i % m_GridPositioner.columns;

            RegisterGroup(group, row, col);
        }
        
        OnGridReady?.Invoke();
    }

    void RegisterGroup(StickmanGroup group, int row, int col)
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

        group.GameplayInit(key);
        group.SetTags("Enemy", "Player");

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

    public List<Vector3> GetAllFirstRowPositions()
    {
        int yVal = 0;

        var positions = new List<Vector3>();
        
        for (int xVal = 0; xVal < m_GridPositioner.columns; xVal++)
        {
            positions.Add(GridToWorld(new Vector2Int(xVal, yVal)));
        }

        return positions;
    }

    public StickmanGroup GetGroupInGrid(Vector2Int gridPos)
    {
        return board[gridPos];
    }
    
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return m_GridPositioner.GetWorldPosition(gridPos); 
    }

public void ClearGridLocations(List<Vector2Int> clearPositions)
{
    if (clearPositions == null || clearPositions.Count == 0)
        return;


    int clearedCount = 0;
    foreach (var pos in clearPositions)
    {
        if (board.TryGetValue(pos, out var group))
        {
            board.Remove(pos);
            if (group != null) Destroy(group.gameObject);
            clearedCount++;
        }
    }

    // Nothing cleared -> nothing to do
    if (clearedCount == 0)
        return;

    // 2) Compact each column so all pieces fall at the same speed
    int movesInProgress = 0;
    float timePerRow = 0.2f; // time to fall exactly 1 row (adjust as needed)

    for (int col = 0; col < m_GridPositioner.columns; col++)
    {
        int writeRow = 0; // next lowest filled slot

        for (int row = 0; row < m_GridPositioner.rows; row++)
        {
            var fromKey = new Vector2Int(col, row);
            if (!board.TryGetValue(fromKey, out var group))
                continue;

            if (row != writeRow)
            {
                var toKey = new Vector2Int(col, writeRow);

                // Update logical board immediately
                board.Remove(fromKey);
                board[toKey] = group;

                group.GameplayInit(toKey);
                Vector3 targetPos = m_GridPositioner.GetWorldPosition(toKey);

                int rowsDropped = row - writeRow;
                float duration = Mathf.Max(0.05f, rowsDropped * timePerRow);

                movesInProgress++;
                group.MoveGroup(targetPos, duration, () =>
                {
                    group.SetStateForAll(new IdleState());
                    if (--movesInProgress <= 0)
                    {
                        OnGridUpdated?.Invoke();
                    }
                });
            }

            // This row is now occupied (either moved or stayed)
            writeRow++;
        }
    }

    // If nothing needed to move (e.g., only top cells cleared), fire update now
    if (movesInProgress == 0)
    {
        OnGridUpdated?.Invoke();
    }
}


    
    public List<Vector2Int> GetGridsWithColor(ColorType type)
    {
        var matches = new List<Vector2Int>();
        int yVal = 0;

        for (int xVal = 0; xVal < m_GridPositioner.columns; xVal++)
        {
            var key = new Vector2Int(xVal, yVal);
            if (board.TryGetValue(key, out var group) && group != null)
            {
                if (group.GroupColor == type)
                    matches.Add(key);
            }
        }

        return matches;
    }



    public int DoesFirstRowContainGroupColor(ColorType type)
    {
        int yVal = 0;
        

        for (int xVal = 0; xVal < m_GridPositioner.rows; xVal++)
        {
            var key = new Vector2Int(xVal, yVal);
            if (board.TryGetValue(key, out var group))
            {
                if (group.GroupColor == type)
                    return xVal;
            }
        }

        return -1;
    }
}
