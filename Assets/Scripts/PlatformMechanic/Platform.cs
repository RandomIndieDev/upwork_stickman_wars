using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class Platform : MonoBehaviour
{
    [BoxGroup("References"), SerializeField] TMP_Text m_PlatformCounterText;
    [BoxGroup("References"), SerializeField] GridPositioner m_GridPositioner;

    [BoxGroup("Canvas Elements"), SerializeField] Transform m_PlatformImg;
    [BoxGroup("Canvas Elements"), SerializeField] Transform m_CounterTxt;

    [BoxGroup("References"), SerializeField] Transform m_PlatformInput;
    [BoxGroup("References"), SerializeField] Transform m_PlatformOutput;

    [SerializeField, ReadOnly] ColorType m_CurrentColorType = ColorType.NONE;
    
    [SerializeField, ReadOnly] int m_AvailableCounterValue;
    [SerializeField, ReadOnly] int m_InTransitionCounterValue;
    [SerializeField, ReadOnly] int m_MaxCounterValue;

    AudioManager m_AudioManager;

    int m_Index;

    public Transform PlatformOutput => m_PlatformOutput;
    public Transform PlatformInput => m_PlatformInput;

    public List<StickmanGroup> m_GroupList;

    public int Index => m_Index;

    GameplaySettings m_Settings;

    void Start()
    {
        m_Settings = GameManager.Instance.GameSettings;
        
        m_AudioManager = AudioManager.Instance;
    }

    public void PrebookSpots(ColorType colorType, int count)
    {
        if (count <= 0)
        {
            Debug.LogWarning("⚠️ PrebookSpots called with non-positive count.");
            return;
        }

        if (m_CurrentColorType == ColorType.NONE)
        {
            m_CurrentColorType = colorType;
        }
        else if (m_CurrentColorType != colorType)
        {
            Debug.LogError($"❌ Cannot prebook spots with {colorType}, " +
                           $"platform already reserved for {m_CurrentColorType}.");
            return;
        }

        m_InTransitionCounterValue += count;
    }

    public void Init(int index)
    {
        m_Index = index;

        m_CurrentColorType = ColorType.NONE;
        m_GroupList = new List<StickmanGroup>();
    }

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
        get => m_AvailableCounterValue;
        private set => m_AvailableCounterValue = value;
    }

    void IncrementCounter() => CurrentCounterValue++;

    void DecrementCounter(int val)
    {
        m_AvailableCounterValue -= val;

        if (m_AvailableCounterValue > 0)
        {
            return;
        }

        m_CurrentColorType = ColorType.NONE;
    }

    public int GetAvailableCurrentCount()
    {
        return m_AvailableCounterValue;
    }

    public int GetAvailableSets()
    {
        // ✅ Work in sets of groups directly
        return m_AvailableCounterValue / 4;
    }

    void UpdateCountText()
    {
        m_PlatformCounterText.text = $"{m_MaxCounterValue - m_AvailableCounterValue}";
    }


    public void AddStickmanGroup(StickmanGroup stickmanGroup, Action OnCompleted)
    {
        var stickmen = stickmanGroup.GetStickmen();
        float duration = m_Settings.MoveToPlatformSpeed;

        var group = stickmanGroup.GetStickmen();

        for (int i = 0; i < group.Count; i++)
        {
            var stickman = group[i];
            var gridPos = Get2DPosition(m_AvailableCounterValue);
            var worldPos = m_GridPositioner.GetWorldPosition(new Vector2Int(gridPos.xPos, gridPos.yPos));

            stickman.RotateModelTo(worldPos, 0.05f);
            stickman.SetState(new MovingState());

            int counter1 = i;
            
            IncrementCounter();
            
            
            PlayCanvasAnimation(3);
            stickman.transform.DOMove(worldPos, duration).SetDelay(m_Settings.DelayBeforeMovingToPlatform * i).OnStart(() =>
                {
                    m_AudioManager.PlayOneShot(SoundType.AddToPlatform);
                }).SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    stickman.SetState(new IdleState());
                    UpdateCountText();

                    if (counter1 == 3)
                    {
                        PlayTextAnimation();
                        stickmanGroup.ResetFollowSphere();
                        OnCompleted?.Invoke();
                    }
                });
        }
        
        m_GroupList.Add(stickmanGroup);
        
        m_InTransitionCounterValue -= stickmen.Count;
        if (m_CurrentColorType == ColorType.NONE)
        {
            m_CurrentColorType = stickmanGroup.GroupColor;
        }
    }


    public void UpdateRemaining(int removedCount)
    {
        if (removedCount <= 0 || m_GroupList.Count == 0)
            return;

        float duration = 0.7f;
        int groupIndex = 0;

        foreach (var group in m_GroupList)
        {
            var stickmen = group.GetStickmen();
            stickmen.Reverse();
            for (int i = 0; i < stickmen.Count; i++)
            {
                var stickman = stickmen[i];
                
                int flatIndex = groupIndex * 4 + i;
                var gridPos = Get2DPosition(flatIndex);
                var worldPos = m_GridPositioner.GetWorldPosition(new Vector2Int(gridPos.xPos, gridPos.yPos));

                // Re-animate to the new world position
                stickman.RotateModelTo(worldPos, 0.05f);
                stickman.SetState(new MovingState());

                stickman.transform.DOMove(worldPos, duration)
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        stickman.SetState(new IdleState());
                    });
            }

            groupIndex++;
        }
    }
    
    public List<StickmanGroup> RemoveGroups(int count)
    {
        if (count > m_GroupList.Count)
        {
            return null;
        }

        var removedGroups = m_GroupList.GetRange(0, count);
        m_GroupList.RemoveRange(0, count);
        
        DecrementCounter(count * 4);
        UpdateCountText();
        PlayTextAnimation();

        return removedGroups;
    }


    void PlayTextAnimation()
    {
        m_CounterTxt.transform.DOKill();

        m_CounterTxt.transform.localPosition = Vector3.zero;
        m_CounterTxt.transform.localScale = Vector3.one;

        m_CounterTxt.transform
            .DOScale(1.2f, 0.2f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                m_CounterTxt.transform.DOScale(1f, 0.2f).SetEase(Ease.OutQuad);
            });
    }
    
    void PlayCanvasAnimation(int loopCount)
    {
        m_PlatformImg.transform.DOKill();
        m_PlatformImg.transform.localScale = Vector3.one;

        m_PlatformImg.transform
            .DOPunchScale(Vector3.one * 0.1f, 0.25f, 1, 0.6f)
            .SetEase(Ease.OutQuad)
            .SetLoops(loopCount, LoopType.Restart);
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
