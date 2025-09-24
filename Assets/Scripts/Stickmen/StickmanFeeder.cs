using System;
using System.Collections;
using Dreamteck.Splines;
using Sirenix.OdinInspector;
using UnityEngine;

public class StickmanFeeder : MonoBehaviour
{
    [BoxGroup("References"), SerializeField] PlatformManager m_PlatformManager;
    [BoxGroup("References"), SerializeField] SplineComputer m_SplineComputer;

    [BoxGroup("References"), SerializeField] Transform m_NodeIn1;   // End node
    [BoxGroup("References"), SerializeField] Transform m_NodeIn2;   // Control near end
    [BoxGroup("References"), SerializeField] Transform m_NodeOut1;  // Start node
    [BoxGroup("References"), SerializeField] Transform m_NodeOut2;  // Control near start

    [BoxGroup("Settings"), SerializeField] float m_ZInwardOffset = 2f; // inward offset (flat X/Z)
    [BoxGroup("Settings"), SerializeField] public float m_MoveSpeed = 5f; // units per second

    public void BuildSplineFor(int platformIndex, Vector3 exitGridPos)
    {
        if (platformIndex < 0)
        {
            Debug.LogError("Invalid platform index for spline build");
            return;
        }

        Vector3 start = exitGridPos;
        Vector3 end = m_PlatformManager.GetPlatformInput(platformIndex).position;

        m_NodeOut1.position = start;                             
        m_NodeOut2.position = start + new Vector3(0, 0, -m_ZInwardOffset);
        m_NodeIn1.position = end;                     
        m_NodeIn2.position = end + new Vector3(0, 0, m_ZInwardOffset);

        m_SplineComputer.Rebuild();
    }
    
    public void MoveAlongSpline(Transform follower, Action onComplete = null)
    {
        if (follower == null) return;

        var splineFollower = follower.GetComponent<SplineFollower>();
        if (splineFollower == null)
            splineFollower = follower.gameObject.AddComponent<SplineFollower>();


        splineFollower.followMode = SplineFollower.FollowMode.Uniform;
        splineFollower.direction = Spline.Direction.Forward;
        splineFollower.followSpeed = m_MoveSpeed;
        splineFollower.wrapMode = SplineFollower.Wrap.Default;
        splineFollower.spline = m_SplineComputer;

        bool triggeredEarly = false;
        splineFollower.onMotionApplied += () =>
        {
            if (triggeredEarly || !(splineFollower.result.percent >= 0.95f))
            {
                return;
            }

            triggeredEarly = true;
            onComplete?.Invoke();
        };

        splineFollower.onEndReached += d =>
        {
            splineFollower.enabled = false;
            splineFollower.gameObject.SetActive(true);
        };
    }
}
