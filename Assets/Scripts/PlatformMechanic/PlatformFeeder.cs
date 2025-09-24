using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class PlatformFeeder : MonoBehaviour
{
    [BoxGroup("References"), SerializeField] PlatformManager m_PlatformManager;

    public void TransferInto(Stickman stickman, Platform platform)
    {
        var color = stickman.ColorType;

        var freePlatformOfType = m_PlatformManager.GetFreePlatformOfType(color);
        if (freePlatformOfType == null)
        {
            freePlatformOfType = m_PlatformManager.GetNextAvailablePlatform();
        }
        
        freePlatformOfType.AddStickman(stickman);
    }

    public void RemoveFrom(int count, ColorType colorType)
    {
        var platform = m_PlatformManager.GetNonEmptyPlatformOfType(colorType);
        var stickmen = platform.RemoveStickmen(count);

        foreach (var stickman in stickmen)
        {
            stickman.gameObject.SetActive(false);
        }

        platform.UpdateRemaining();
    }
}
