using System;using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DG.Tweening;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;
using Sequence = DG.Tweening.Sequence;

public enum Direction
{
    Left,
    Right,
    Top,
    Bottom
}

[Serializable]
public class StickmanGroup : MonoBehaviour
{
    [SerializeField] ColorType m_GroupColorType;
    [SerializeField] List<Stickman> m_Stickmen;
    
    [SerializeField] Vector2Int m_GroupGridLoc;


    [BoxGroup("References"), SerializeField]
    List<Transform> m_localOriginalPos;
    

    [BoxGroup("Settings"), SerializeField] Transform m_FollowSphere;

    bool m_IsTopEmpty = false;
    
    Dictionary<Direction, StickmanGroup> m_ConnectedGroups = new();

    public Vector2Int GroupGridLoc => m_GroupGridLoc;
    public ColorType GroupColor => m_GroupColorType;
    public Transform FollowSphere => m_FollowSphere;
    public List<Stickman> Members => m_Stickmen;

    public Action OnReadyForStep;
    
    public bool IsTopEmpty
    {
        get => m_IsTopEmpty;
        set => m_IsTopEmpty = value;
    }

    public void GameplayInit(Vector2Int gridLoc)
    {
        m_GroupGridLoc = gridLoc;
        
        foreach (var stickman in m_Stickmen)
        {
            stickman.Init(this);
        }
        
        if (m_localOriginalPos == null) return;
        if (m_localOriginalPos.Count <= 0)
        {
            return;
        }

        for (int i = 0; i < m_Stickmen.Count; i++)
        {
            m_localOriginalPos[i].localPosition = m_Stickmen[i].transform.localPosition;
        }
    }

    public void ResetFollowSphere()
    {
        if (m_Stickmen == null || m_Stickmen.Count == 0)
            return;

        Vector3 sum = Vector3.zero;
        int count = 0;

        foreach (var stickman in m_Stickmen)
        {
            if (stickman == null) continue;
            sum += stickman.transform.position;
            count++;
        }

        if (count > 0)
        {
            m_FollowSphere.position = sum / count;
        }
    }

    public void Hide()
    {
        foreach (var stickman in m_Stickmen)
        {
            stickman.gameObject.SetActive(false);
        }
    }

    public void SetStateForAll(IState<Stickman> state)
    {
        foreach (var stickman in m_Stickmen)
        {
            stickman.SetState(state);   
        }
    }

    public void Init(ColorType selectedColor)
    {
        m_GroupColorType = selectedColor;
        
        foreach (var stickman in m_Stickmen)
        {
            stickman.ColorType = selectedColor;
            stickman.Init(this);
        }
    }

    public void SetTags(string self, string enemy)
    {
        foreach (var stickman in m_Stickmen)
        {
            stickman.SetTag(self);
        }
    }

    public int GetSpotCount()
    {
        return m_Stickmen.Count;
    }
    
    public List<Stickman> GetStickmen()
    {
        //TODO: Remove
        m_Stickmen.Reverse();
        
        return m_Stickmen;
    }

    public void AddNeighbor(Direction dir, StickmanGroup neighbor)
    {
        if (!m_ConnectedGroups.ContainsKey(dir))
        {
            m_ConnectedGroups[dir] = neighbor;
        }
    }

    public void ShakeGroups()
    {
        var visited = new HashSet<StickmanGroup>();
        CollectConnectedGroups(this, visited);

        if (visited.Count == 0) return;
        
        GameObject tempParent = new GameObject("ShakeContainer");
        tempParent.transform.position = GetCenterPoint(visited);
        
        Dictionary<StickmanGroup, Transform> originalParents = new Dictionary<StickmanGroup, Transform>();

        foreach (var group in visited)
        {
            originalParents[group] = group.transform.parent;
            group.transform.SetParent(tempParent.transform, true);
        }
        
        tempParent.transform
            .DOShakePosition(
                duration: 0.5f,
                strength: new Vector3(0.2f, 0, 0),
                vibrato: 10,
                randomness: 0,
                snapping: false,
                fadeOut: true,
                randomnessMode: ShakeRandomnessMode.Harmonic
            )
            .OnComplete(() =>
            {
                foreach (var kvp in originalParents)
                {
                    kvp.Key.transform.SetParent(kvp.Value, true);
                }
                GameObject.Destroy(tempParent);
            });
    }
    
    public List<StickmanGroup> GetConnectedChain(int maxCount)
    {
        var result = new List<StickmanGroup>();
        var visited = new HashSet<StickmanGroup>();
        CollectChain(this, maxCount, result, visited);
        return result;
    }

    private void CollectChain(StickmanGroup group, int maxCount, List<StickmanGroup> result, HashSet<StickmanGroup> visited)
    {
        if (group == null || visited.Contains(group) || result.Count >= maxCount)
            return;

        visited.Add(group);
        result.Add(group);

        if (result.Count >= maxCount) return;

        // Priority order: Left, Right, Diagonal, Up
        Direction[] priorities = { Direction.Left, Direction.Right, Direction.Top, Direction.Bottom };

        foreach (var dir in priorities)
        {
            var neighbor = group.GetNeighbor(dir);
            if (neighbor != null && neighbor.GroupColor == group.GroupColor)
            {
                CollectChain(neighbor, maxCount, result, visited);
                if (result.Count >= maxCount) return;
            }
        }
    }



    void CollectConnectedGroups(StickmanGroup group, HashSet<StickmanGroup> visited)
    {
        if (group == null || visited.Contains(group)) return;

        visited.Add(group);

        foreach (var kvp in group.m_ConnectedGroups)
        {
            StickmanGroup neighbor = kvp.Value;
            if (neighbor != null && neighbor.GroupColor == group.GroupColor)
            {
                CollectConnectedGroups(neighbor, visited);
            }
        }
    }

    Vector3 GetCenterPoint(HashSet<StickmanGroup> groups)
    {
        Vector3 sum = Vector3.zero;
        foreach (var g in groups) sum += g.transform.position;
        return sum / groups.Count;
    }

    public void MoveGroup(Vector3 pos, float time, Action OnCompleted)
    {
        SetStateForAll(new WalkingState());
        transform.DOMove(pos, time).onComplete += () =>
        {
            OnCompleted?.Invoke();
        };
    }
    
    public StickmanGroup GetNeighbor(Direction dir) =>
        m_ConnectedGroups.TryGetValue(dir, out var neighbor) ? neighbor : null;

    public IEnumerable<KeyValuePair<Direction, StickmanGroup>> GetAllNeighbors() =>
        m_ConnectedGroups;
    
    public HashSet<StickmanGroup> GetAllChained()
    {
        var result = new HashSet<StickmanGroup>();
        CollectChained(this, result);
        return result;
    }

    public bool IsEscapeable()
    {
        var visited = new HashSet<StickmanGroup>();
        return CollectEscapeable(this, visited);
    }

    bool CollectEscapeable(StickmanGroup group, HashSet<StickmanGroup> visited)
    {
        if (group == null || visited.Contains(group))
            return false;

        visited.Add(group);

        if (group.IsTopEmpty)
            return true;

        foreach (var kvp in group.m_ConnectedGroups)
        {
            StickmanGroup neighbor = kvp.Value;
            if (neighbor != null && neighbor.GroupColor == group.GroupColor)
            {
                if (CollectEscapeable(neighbor, visited))
                    return true;
            }
        }

        return false;
    }
    
    void CollectChained(StickmanGroup group, HashSet<StickmanGroup> visited)
    {
        if (group == null || visited.Contains(group))
            return;

        visited.Add(group);

        foreach (var kvp in group.m_ConnectedGroups)
        {
            StickmanGroup neighbor = kvp.Value;
            if (neighbor != null && neighbor.GroupColor == group.GroupColor)
            {
                CollectChained(neighbor, visited);
            }
        }
    }
    
    public HashSet<StickmanGroup> GetAllConnected()
    {
        var visited = new HashSet<StickmanGroup>();
        var stack = new Stack<StickmanGroup>();
        stack.Push(this);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if (!visited.Add(current)) continue; // already processed

            foreach (var kvp in current.m_ConnectedGroups)
            {
                var neighbor = kvp.Value;
                if (neighbor != null && neighbor.GroupColor == GroupColor && !visited.Contains(neighbor))
                {
                    stack.Push(neighbor);
                }
            }
        }

        return visited;
    }
    
    public HashSet<StickmanGroup> GetAllConnectedVertical()
    {
        var visited = new HashSet<StickmanGroup>();
        var stack = new Stack<StickmanGroup>();
        stack.Push(this);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if (!visited.Add(current)) continue; // already processed

            // Only check vertical neighbors (Top, Bottom)
            foreach (var kvp in current.m_ConnectedGroups)
            {
                if (kvp.Key != Direction.Top && kvp.Key != Direction.Bottom)
                    continue;

                var neighbor = kvp.Value;
                if (neighbor != null && neighbor.GroupColor == GroupColor && !visited.Contains(neighbor))
                {
                    stack.Push(neighbor);
                }
            }
        }

        return visited;
    }


    [Button]
    public void ActivateFollowPointState()
    {
        for (int i = 0; i < m_Stickmen.Count; i++)
        {
            m_Stickmen[i].CurrentFollowTarget = FollowSphere;
            m_Stickmen[i].SetState(new FollowPointState(m_localOriginalPos[i]));
        }
    }
    
    public void TraverseThroughPoints(Vector2Int selectedGrid, List<Vector3> positions, Action OnCompleted)
    {
        if (positions == null || positions.Count == 0) return;

        // ✅ Case 1: Player clicked group (same grid) → instant
        if (positions.Count == 1 && selectedGrid == GroupGridLoc)
        {
            OnCompleted?.Invoke();
            return;
        }

        // ✅ Case 2: Single step but NOT clicked group → add small delay
        if (positions.Count == 1 && selectedGrid != GroupGridLoc)
        {
            DOVirtual.DelayedCall(0.15f, () => OnCompleted?.Invoke());
            return;
        }
        
        m_FollowSphere.DOKill();

        float totalDistance = 0f;
        for (int i = 1; i < positions.Count; i++)
            totalDistance += Vector3.Distance(positions[i - 1], positions[i]);

        float baseSpeed = .8f;
        float speed = baseSpeed + Mathf.Sqrt(positions.Count) * 1.2f;
        float duration = totalDistance / speed;

        m_FollowSphere
            .DOPath(positions.ToArray(), duration, PathType.CatmullRom)
            .SetEase(Ease.Linear)
            .SetLookAt(0.01f, Vector3.up)
            .OnComplete(() => OnCompleted?.Invoke());
    }




    public bool CanExitGrid()
    {
        return m_GroupGridLoc.y == 0;
    }
}

