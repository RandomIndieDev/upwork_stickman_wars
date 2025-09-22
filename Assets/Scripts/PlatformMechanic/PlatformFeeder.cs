using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class PlatformFeeder : MonoBehaviour
{
    [BoxGroup("References"), SerializeField] PlatformManager m_PlatformManager;

    public void TransferInto(List<Stickman> stickmen)
    {
        var color = stickmen[0].ColorType;

        var freePlatformOfType = m_PlatformManager.GetFreePlatformOfType(color);
        if (freePlatformOfType == null) 
            freePlatformOfType = m_PlatformManager.GetNextAvailablePlatform();


        foreach (var stickman in stickmen)
        {
            
        }
    }
}
