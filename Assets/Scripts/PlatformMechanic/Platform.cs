using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

[Serializable]
public class StickmanSlot
{
    public Stickman stickman;
    public TwoDimensionalPos position;

    public StickmanSlot(Stickman stickman, TwoDimensionalPos position)
    {
        this.stickman = stickman;
        this.position = position;
    }

    public TwoDimensionalPos GetOneRowBackPos()
    {
        return new TwoDimensionalPos()
        {
            xPos = position.xPos,
            yPos = position.yPos + 1
        };
    }

    public override string ToString()
    {
        return $"{position.xPos} - {position.yPos + 1}";
    }
}

public class Platform : MonoBehaviour
{
    [BoxGroup("References"), SerializeField] TMP_Text m_PlatformCounterText;
    [BoxGroup("References"), SerializeField] GridPositioner m_GridPositioner;
    [BoxGroup("References"), SerializeField] GameObject m_PlatformInput;
    
    [SerializeField, ReadOnly] ColorType m_CurrentColorType = ColorType.NONE;
    [SerializeField, ReadOnly] int m_CurrentCounterValue;
    [SerializeField, ReadOnly] int m_InTransitionCounterValue;
    
    [SerializeField, ReadOnly] int m_MaxCounterValue;
    
    [SerializeField, ReadOnly] List<StickmanSlot> m_StickmanSlots = new();
    List<StickmanSlot> m_FreedUpSpots = new();

    int m_Index;
    
    public GameObject PlatformInput => m_PlatformInput;
    public int Index => m_Index;

    public void PrebookSpots(int count)
    {
        m_InTransitionCounterValue += count;
    }

    public void Init(int index)
    {
        m_Index = index;
        
        m_CurrentColorType = ColorType.NONE;
        m_StickmanSlots = new List<StickmanSlot>();
        m_FreedUpSpots = new List<StickmanSlot>();
    }

    // ✅ Exposed for PlatformManager
    public int MaxCounterValue
    {
        get => m_MaxCounterValue;
        set => m_MaxCounterValue = value;
    }

    public ColorType CurrentColorType
    {
        get => m_CurrentColorType;
        set => m_CurrentColorType = value;
    }

    public int CurrentCounterValue
    {
        get => m_CurrentCounterValue;
        private set => m_CurrentCounterValue = value;
    }

    void IncrementCounter() => CurrentCounterValue++;
    void DecrementCounter(int val = 1) => CurrentCounterValue -= val;

    public int GetRealCurrentCount()
    {
        return m_CurrentCounterValue + m_InTransitionCounterValue;
    }

    void UpdateCountText()
    {
        m_PlatformCounterText.text = $"{m_MaxCounterValue - m_CurrentCounterValue}";
    }

    public void AddStickman(Stickman stickman)
    {

        var gridPos = Get2DPosition(m_CurrentCounterValue);
        var worldPos = m_GridPositioner.GetWorldPosition(new Vector2Int(gridPos.xPos, gridPos.yPos));

        stickman.RotateModelTo(worldPos, 0.05f);

        stickman.SetState(new MovingState());
        stickman.transform.SetParent(m_GridPositioner.transform);
        stickman.transform.DOMove(worldPos, 1f).OnComplete(() =>
        {
            stickman.SetState(new IdleState());
            UpdateCountText();
        });

        var newSlot = new StickmanSlot(stickman, gridPos);
        m_StickmanSlots.Add(newSlot);
        
        IncrementCounter();
        m_InTransitionCounterValue--;

        if (m_CurrentColorType == ColorType.NONE)
            m_CurrentColorType = stickman.ColorType;
    }

    public void UpdateRemaining()
    {
        foreach (var freedUpSpot in m_FreedUpSpots)
        {
            var fromPos = freedUpSpot.GetOneRowBackPos();
            var toPos = freedUpSpot.position;

            var stick = m_StickmanSlots.Find(s =>
                s.position.xPos == fromPos.xPos &&
                s.position.yPos == fromPos.yPos);

            if (stick == null) continue;

            stick.position = toPos;
            var targetWorldPos = m_GridPositioner.GetWorldPosition(new Vector2Int(toPos.xPos, toPos.yPos));

            stick.stickman.SetState(new WalkingState());
            stick.stickman.transform.DOMove(targetWorldPos, 0.6f)
                .SetEase(Ease.Linear)
                .OnComplete(() => stick.stickman.SetState(new IdleState()));
        }

        m_FreedUpSpots.Clear();
    }

    public List<Stickman> RemoveStickmen(int count)
    {
        var removedStickmen = new List<Stickman>();

        if (count > m_CurrentCounterValue)
        {
            Debug.LogError("Cant Remove More Stickmen");
            return removedStickmen;
        }

        var orderedSlots = m_StickmanSlots
            .OrderBy(slot => slot.position.yPos)
            .ThenBy(slot => slot.position.xPos)
            .ToList();

        var toRemove = orderedSlots.Take(count).ToList();

        foreach (var slot in toRemove)
        {
            m_FreedUpSpots.Add(slot);
            removedStickmen.Add(slot.stickman);
            m_StickmanSlots.Remove(slot);
        }

        DecrementCounter(count);
        UpdateCountText();

        return removedStickmen;
    }

    TwoDimensionalPos Get2DPosition(int index)
    {
        int row = index / m_GridPositioner.columns;
        int col = index % m_GridPositioner.columns;

        return new TwoDimensionalPos
        {
            xPos = col,
            yPos = row
        };
    }
}
