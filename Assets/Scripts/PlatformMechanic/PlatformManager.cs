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

    public Transform GetPlatformInput(int index)
    {
        return m_Platforms[index].PlatformInput.transform;
    }
    
    public Platform GetNextAvailablePlatform()
    {
        return m_Platforms.FirstOrDefault(p => p.CurrentColorType == ColorType.NONE);
    }

    /// <summary>
    /// Finds a platform of a given color that still has free capacity.
    /// </summary>
    public Platform GetFreePlatformOfType(ColorType colorType)
    {
        return m_Platforms.FirstOrDefault(p =>
            p.CurrentColorType == colorType &&
            p.CurrentCounterValue < p.MaxCounterValue);
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
    public List<Platform> GetAllActive()
    {
        return m_Platforms.Where(p => p.CurrentCounterValue > 0).ToList();
    }

    /// <summary>
    /// Checks if all platforms are full.
    /// </summary>
    public bool AreAllFull()
    {
        return m_Platforms.All(p => p.CurrentCounterValue >= p.MaxCounterValue);
    }
}
