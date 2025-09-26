using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Crate : MonoBehaviour
{
    public Transform m_Visual;
    
    public void OnClick()
    {
        m_Visual.transform.DOKill();
        m_Visual.transform.localPosition = Vector3.zero;
        m_Visual.transform.localScale = Vector3.one;

        Sequence seq = DOTween.Sequence();
        seq.Append(m_Visual.transform.DOScaleY(1.7f, 0.1f))   // stretch up
            .Append(m_Visual.transform.DOScaleY(0.8f, 0.08f))  // squash down
            .Append(m_Visual.transform.DOScaleY(1f, 0.12f));   // settle back
    }


}
