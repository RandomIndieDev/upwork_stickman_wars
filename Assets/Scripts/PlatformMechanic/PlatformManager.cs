using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

public class PlatformManager : MonoBehaviour
{
    [BoxGroup("References"), SerializeField] List<Platform> m_Platforms;
    [SerializeField] int m_MaxCounterValue;

    void Start()
    {
        for (int i = 0; i < m_Platforms.Count; i++)
        {
            m_Platforms[i].Init(i); 
            m_Platforms[i].MaxCounterValue = m_MaxCounterValue;
        }
    }

    public List<ColorType> GetPlatformColors()
    {
        var colorList = m_Platforms.Select(platform => platform.CurrentColorType).ToList();

        return colorList;
    }

    public Transform GetPlatformInput(int index)
    {
        return m_Platforms[index].PlatformInput.transform;
    }

    public Platform GetPlatformWithIndex(int index)
    {
        return m_Platforms[index];
    }
    
    public List<(Platform platform, int assignedCount)> GetFreePlatformsOfType(ColorType colorType, int count)
    {
        var result = new List<(Platform, int)>();
        int remaining = count;
        
        foreach (var platform in m_Platforms)
        {
            if (platform.CurrentColorType != colorType) continue;

            int freeSpace = platform.MaxCounterValue - platform.CurrentCounterValue;
            if (freeSpace <= 0) continue;

            int toAssign = Mathf.Min(remaining, freeSpace);
            result.Add((platform, toAssign));
            remaining -= toAssign;

            if (remaining <= 0) return result;
        }
        
        foreach (var platform in m_Platforms)
        {
            if (platform.CurrentColorType != ColorType.NONE) continue;

            int freeSpace = platform.MaxCounterValue - platform.CurrentCounterValue;
            if (freeSpace <= 0) continue;

            int toAssign = Mathf.Min(remaining, freeSpace);
            result.Add((platform, toAssign));
            remaining -= toAssign;

            if (remaining <= 0) return result;
        }

        foreach (var (platform, value) in result)
        {
            platform.PrebookSpots(colorType, value);
        }

        return result;
    }


    /// <summary>
    /// Finds a non-empty platform of a given color.
    /// </summary>
    public Platform GetNonEmptyPlatformOfType(ColorType colorType)
    {
        return m_Platforms.FirstOrDefault(p =>
            p.CurrentColorType == colorType &&
            p.CurrentCounterValue > 0);
    }

    /// <summary>
    /// Finds any platform of a given color, regardless of fullness.
    /// </summary>
    public Platform GetPlatformOfType(ColorType colorType)
    {
        return m_Platforms.FirstOrDefault(p => p.CurrentColorType == colorType);
    }

    /// <summary>
    /// Returns all platforms that are not full.
    /// </summary>
    public List<Platform> GetAllNotFull()
    {
        return m_Platforms.Where(p => p.CurrentCounterValue < p.MaxCounterValue).ToList();
    }

    /// <summary>
    /// Returns all platforms that currently contain stickmen.
    /// </summary>
    public List<Platform> GetAll()
    {
        return m_Platforms;
    }

    /// <summary>
    /// Checks if all platforms are full.
    /// </summary>
    public bool AreAllFull()
    {
        return m_Platforms.All(p => p.CurrentCounterValue >= p.MaxCounterValue);
    }
}
