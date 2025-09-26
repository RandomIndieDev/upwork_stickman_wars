using System;
using System.Collections;
using System.Collections.Generic;
using Dreamteck.Splines;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

[Serializable]
public class SplineFeeder
{
    int m_TransferCount;

    [BoxGroup("Spline")] public SplineComputer m_Spline;
    
    [BoxGroup("Node References")] public Transform m_NodeIn1;
    [BoxGroup("Node References")] public Transform m_NodeIn2; 
    [BoxGroup("Node References")] public Transform m_NodeOut1;  
    [BoxGroup("Node References")] public Transform m_NodeOut2;

    public void UpdateNodePositions(Vector3 startPos,Vector3 endPos, float zOffset)
    {
        m_NodeOut1.position = endPos;                             
        m_NodeOut2.position = endPos + new Vector3(0, 0, -zOffset);
        m_NodeIn1.position = startPos;                     
        m_NodeIn2.position = startPos + new Vector3(0, 0, zOffset);
        
        m_Spline.RebuildImmediate();
    }

    public void IncrementTransferCount() 
    {
        m_TransferCount++;
    }

    public void DecrementTransferCount() 
    {
        m_TransferCount--;
    }

    public bool IsAvailable() => m_TransferCount <= 0;
}

public class StickmanFeeder : MonoBehaviour
{
    [BoxGroup("Spline References"), SerializeField] List<SplineFeeder> m_SplineFeeders;

    [BoxGroup("Settings"), SerializeField] float m_ZInwardOffset = 2f;
    [BoxGroup("Settings"), SerializeField] public float m_MoveSpeed = 5f;

    int GetFreeSplineFeeder()
    {
        for (int i = 0; i < m_SplineFeeders.Count; i++)
        {
            if (m_SplineFeeders[i].IsAvailable()) return i;
        }

        return -1;
    }

    public SplineFeeder GetSplineFeeder(int index)
    {
        return m_SplineFeeders[index];
    }

    public int BuildSplineFor(Vector3 startGridPos, Vector3 exitGridPos)
    {
        var feederIndex = GetFreeSplineFeeder();

        if (feederIndex < 0)
        {
            Debug.LogError($"Error: No free splines available : Script StickmanFeeder");
            return -1;
        }
        
        m_SplineFeeders[feederIndex].UpdateNodePositions(startGridPos, exitGridPos, m_ZInwardOffset);

        return feederIndex;
    }
    
    
    public void MoveAlongSpline(int splineIndex, Transform follower, Action onComplete = null, Action OnEndReached = null)
    {
        if (follower == null) return;
        
        var spline = m_SplineFeeders[splineIndex].m_Spline;
        
        var splineFollower = follower.GetComponent<SplineFollower>();
        if (splineFollower == null)
            splineFollower = follower.gameObject.AddComponent<SplineFollower>();

        splineFollower.followMode = SplineFollower.FollowMode.Uniform;
        splineFollower.direction = Spline.Direction.Forward;
        splineFollower.followSpeed = m_MoveSpeed;
        splineFollower.wrapMode = SplineFollower.Wrap.Default;
        
        splineFollower.spline = spline;

        bool triggeredEarly = false;
        splineFollower.onMotionApplied += () =>
        {
            if (triggeredEarly || !(splineFollower.result.percent >= 0.93f))
            {
                return;
            }

            triggeredEarly = true;
            onComplete?.Invoke();
        };

        splineFollower.onEndReached += d =>
        {
            splineFollower.enabled = false;
            OnEndReached?.Invoke();
        };
    }
}

public class SplineFeederData
{
    public Platform Platform;
    public List<StickmenBoardManager.ExitRecommendation> ExitRecommendations;
    public int TransferCount;
    public int SplineFeederIndex;
}
