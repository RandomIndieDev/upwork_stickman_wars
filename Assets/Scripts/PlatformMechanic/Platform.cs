using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class Platform : MonoBehaviour
{
    [BoxGroup("References"),SerializeField] TMP_Text m_PlatformCounterText;
    [BoxGroup("References"),SerializeField] ArrayPositioner m_ArrayPositioner;
    

    [SerializeField, ReadOnly] ColorType m_CurrentColorType;
    [SerializeField, ReadOnly] int m_CurrentCounterValue;
    
    public ColorType CurrentColorType
    {
        get => m_CurrentColorType;
        set => m_CurrentColorType = value;
    }

    public int CurrentCounterValue
    {
        get => m_CurrentCounterValue;
        set => m_CurrentCounterValue = value;
    }
    
    public void SetCurrentColorType(ColorType colorType)
    {
        m_CurrentColorType = colorType;
    }
    
    void IncrementCounter()
    {
        m_CurrentCounterValue += 1;
    }

    void DecrementCounter()
    {
        m_CurrentCounterValue -= 1;
    }

    public void AddStickman(Stickman stickman)
    {
        var nextFreeSpot = m_ArrayPositioner.GetPositionalObject(m_CurrentCounterValue);
        
        IncrementCounter();
    }
}
