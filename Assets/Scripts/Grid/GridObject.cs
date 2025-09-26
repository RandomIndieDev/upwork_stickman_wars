using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public enum GridObjectType
{
    StickmanGroup,
    Crate,
    
    NONE = 100
}

public class GridObject : MonoBehaviour
{
    public GridObjectType GridObjectType;

    [BoxGroup("References"), SerializeField] StickmanGroup m_Group;
    [BoxGroup("References"), SerializeField] Crate m_Crate;

    public StickmanGroup GetStickManGroup()
    {
        return m_Group;
    }

    public Crate GetCrate()
    {
        return m_Crate;
    }
}
