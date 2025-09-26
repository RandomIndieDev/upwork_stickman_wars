using System.Collections.Generic;
using UnityEngine;

public class GroupFollower : MonoBehaviour
{
    [Header("References")]
    public Transform followPoint;
    public List<Transform> stickmen = new List<Transform>();

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 5f;

    [Header("Flocking Settings")]
    public float separationDistance = 1f;
    public float separationStrength = 2f;
    public float cohesionStrength = 0.5f;
    public float alignmentStrength = 0.5f;

    [Header("Noise Settings")]
    public float noiseStrength = 0.2f;
    public float noiseFrequency = 1.5f;

    [Header("Constraints")]
    public bool lockYMovement = true;      // 🔹 Keep Y fixed
    public bool lockXZRotation = true;     // 🔹 Lock pitch/roll

    private Dictionary<Transform, Vector3> velocities = new Dictionary<Transform, Vector3>();
    private readonly List<Transform> toRemove = new List<Transform>(); // ✅ removal buffer

    public void Setup(List<Stickman> followObjs)
    {
        var trans = new List<Transform>();
        foreach (var obj in followObjs)
            trans.Add(obj.transform);

        Setup(trans);
    }

    public void Setup(List<Transform> followObjs)
    {
        stickmen = followObjs;
        followPoint = transform;
        
        foreach (var sm in stickmen)
        {
            if (!velocities.ContainsKey(sm))
                velocities[sm] = Vector3.zero;
        }

        followPoint = transform; // default follow point = self
    }

    /// <summary>
    /// Safely marks a stickman for removal. Will be applied next frame.
    /// </summary>
    public void RemoveStickman(Transform sm)
    {
        if (sm != null && stickmen.Contains(sm) && !toRemove.Contains(sm))
            toRemove.Add(sm);
    }
    
    public void ClearAll()
    {
        followPoint = null;
        
        stickmen.Clear();
        velocities.Clear();
        toRemove.Clear();
    }


    void Update()
    {
        if (followPoint == null || stickmen.Count == 0) return;

        // ✅ Handle removals at the start of each frame
        if (toRemove.Count > 0)
        {
            foreach (var sm in toRemove)
            {
                stickmen.Remove(sm);
                velocities.Remove(sm);
            }
            toRemove.Clear();
        }

        // ✅ Also auto-cleanup destroyed/null stickmen
        stickmen.RemoveAll(s => s == null);

        foreach (var sm in stickmen)
        {
            if (sm == null) continue; // safety guard
            Vector3 pos = sm.position;

            // --- 1. Follow target ---
            Vector3 toTarget = (followPoint.position - pos).normalized;

            // --- 2. Separation ---
            Vector3 separation = Vector3.zero;
            foreach (var other in stickmen)
            {
                if (other == null || other == sm) continue;
                Vector3 away = pos - other.position;
                float dist = away.magnitude;
                if (dist < separationDistance && dist > 0.001f)
                {
                    float strength = (separationDistance - dist) / separationDistance;
                    separation += away.normalized * strength;
                }
            }

            // --- 3. Cohesion ---
            Vector3 center = Vector3.zero;
            int count = 0;
            foreach (var other in stickmen)
            {
                if (other == null) continue;
                center += other.position;
                count++;
            }
            if (count > 0) center /= count;
            Vector3 cohesion = (center - pos).normalized;

            // --- 4. Alignment ---
            Vector3 avgVel = Vector3.zero;
            int velCount = 0;
            foreach (var other in stickmen)
            {
                if (other == null) continue;
                avgVel += velocities[other];
                velCount++;
            }
            if (velCount > 0) avgVel /= velCount;
            Vector3 alignment = avgVel.normalized;

            // --- 5. Noise (smooth sway) ---
            float time = Time.time * noiseFrequency + sm.GetInstanceID();
            Vector3 noise = new Vector3(Mathf.Sin(time), 0, Mathf.Cos(time)) * noiseStrength;

            // --- Combine forces with weights ---
            Vector3 desiredVelocity =
                toTarget * 1.8f +
                separation * separationStrength +
                cohesion * cohesionStrength +
                alignment * alignmentStrength +
                noise;

            if (desiredVelocity.sqrMagnitude > 0.001f)
                desiredVelocity = desiredVelocity.normalized * moveSpeed;

            // Smooth velocity
            velocities[sm] = Vector3.Lerp(velocities[sm], desiredVelocity, Time.deltaTime * 5f);

            // Apply movement
            Vector3 newPos = sm.position + velocities[sm] * Time.deltaTime;
            if (lockYMovement) newPos.y = sm.position.y;
            sm.position = newPos;

            // --- Rotation ---
            if (velocities[sm].sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(velocities[sm], Vector3.up);
                if (lockXZRotation)
                {
                    // Only rotate around Y
                    Vector3 euler = targetRot.eulerAngles;
                    targetRot = Quaternion.Euler(0, euler.y, 0);
                }
                sm.rotation = Quaternion.Slerp(sm.rotation, targetRot, Time.deltaTime * rotationSpeed);
            }
        }
    }
}
