using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class Stickman : MonoBehaviour
{
    [BoxGroup("Info"), SerializeField] ColorType m_ColorType;

    public ColorType ColorType
    {
        get => m_ColorType;
        set => m_ColorType = value;
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
