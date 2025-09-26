using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StickmanPointFeeder : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;

    private readonly Dictionary<Transform, Coroutine> activeRoutines = new();

    /// <param name="onNearEnd">Called once when overall path progress crosses threshold (e.g., 0.9f).</param>
    /// <param name="overallThreshold">0..1 fraction of total path length.</param>
    public void MoveTargetThroughPoints(
        List<Vector3> points,
        Transform target,
        Action onCompleted = null,
        Action onNearEnd = null,
        float overallThreshold = 0.9f
    )
    {
        if (points == null || points.Count == 0 || target == null) return;

        if (activeRoutines.TryGetValue(target, out var existing))
        {
            if (existing != null) StopCoroutine(existing);
            activeRoutines.Remove(target);
        }

        var routine = StartCoroutine(MoveRoutine(points, target, onCompleted, onNearEnd, overallThreshold));
        activeRoutines[target] = routine;
    }

    private IEnumerator MoveRoutine(
        List<Vector3> points,
        Transform target,
        Action onCompleted,
        Action onNearEnd,
        float overallThreshold
    )
    {
        // --- precompute total path length ---
        float totalLen = 0f;
        Vector3 prev = target.position;
        for (int i = 0; i < points.Count; i++)
        {
            totalLen += Vector3.Distance(i == 0 ? prev : points[i - 1], points[i]);
        }

        if (totalLen <= 0.0001f)
        {
            onNearEnd?.Invoke();
            onCompleted?.Invoke();
            yield break;
        }

        float traveledOverall = 0f;
        bool overallTriggered = false;

        Vector3 start = target.position;

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 end = points[i];
            float segLen = Vector3.Distance(start, end);

            // Move along this segment
            while (Vector3.Distance(target.position, end) > 0.01f)
            {
                target.position = Vector3.MoveTowards(target.position, end, moveSpeed * Time.deltaTime);

                if (!overallTriggered && segLen > 0f)
                {
                    float segTraveled = segLen - Vector3.Distance(target.position, end);
                    float overallProgress = (traveledOverall + Mathf.Max(0f, segTraveled)) / totalLen;

                    if (overallProgress >= overallThreshold)
                    {
                        overallTriggered = true;  // lock to fire only once total
                        onNearEnd?.Invoke();
                    }
                }

                yield return null;
            }

            // finalize segment
            traveledOverall += segLen;
            start = end;
        }

        onCompleted?.Invoke();
    }
}
