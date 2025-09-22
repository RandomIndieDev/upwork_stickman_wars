using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class PlatformManager : MonoBehaviour
{
    [BoxGroup("References"), SerializeField] List<Platform> m_Platforms;

    [SerializeField] int m_MaxCounterValue;
    
    public Platform GetNextAvailablePlatform()
    {
        foreach (var platform in m_Platforms)
        {
            if (platform.CurrentColorType != ColorType.NONE)
            {
                return platform;
            }
        }
        return null;
    }
    
    public bool ContainsPlatformOfType(ColorType colorType)
    {
        foreach (var platform in m_Platforms)
        {
            if (platform.CurrentColorType == colorType)
            {
                return true;
            }
        }
        return false;
    }
    
    public Platform GetFreePlatformOfType(ColorType colorType)
    {
        foreach (var platform in m_Platforms)
        {
            if (platform.CurrentColorType == colorType &&  platform.CurrentCounterValue <  m_MaxCounterValue)
            {
                return platform;
            }
        }
        return null;
    }
    
    public Platform GetPlatformOfType(ColorType colorType)
    {
        foreach (var platform in m_Platforms)
        {
            if (platform.CurrentColorType == colorType)
            {
                return platform;
            }
        }
        return null;
    }
}