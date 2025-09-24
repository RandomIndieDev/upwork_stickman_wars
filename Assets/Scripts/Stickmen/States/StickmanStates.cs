using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StickmanState
{
    Idle,
    Moving,
    Walking,
    Attacking,
    FollowPointState
}

public class IdleState : IState<Stickman>
{
    public string Name => StickmanState.Idle.ToString();

    public void Enter(Stickman stickman) 
    { 
        stickman.ResetRotation();
        stickman.Animator.SetTrigger("Idle");
    }

    public void Tick(Stickman stickman) { }

    public void Exit(Stickman stickman) { }
}

public class MovingState : IState<Stickman>
{
    public string Name => StickmanState.Moving.ToString();

    public void Enter(Stickman stickman) 
    { 
        stickman.Animator.SetBool("Jog", true);
    }

    public void Tick(Stickman stickman) { }

    public void Exit(Stickman stickman)
    {
        stickman.Animator.SetBool("Jog", false);
    }
}

public class FollowPointState : IState<Stickman>
{
    public string Name => StickmanState.FollowPointState.ToString();

    private Vector3 _offset;
    private Transform _followSphere;

    // Random factors
    private Vector3 _staticOffset;   // constant per-stickman drift
    private float _noiseSeed;        // for Perlin noise

    public void Enter(Stickman stickman)
    {
        StickmanGroup group = stickman.transform.GetComponentInParent<StickmanGroup>();
        if (group == null) return;

        _followSphere = group.FollowSphere;
        _offset = stickman.transform.position - _followSphere.position;

        // 🔹 Subtle static random drift so stickmen don't overlap perfectly
        _staticOffset = new Vector3(
            Random.Range(-0.1f, 0.1f),
            0,
            Random.Range(-0.1f, 0.1f)
        );

        // Seed for Perlin noise randomness
        _noiseSeed = Random.Range(0f, 100f);

        stickman.Animator.SetBool("Jog", true);
    }

    public void Tick(Stickman stickman)
    {
        if (_followSphere == null) return;

        // Base target = follow sphere + formation offset
        Vector3 baseTarget = _followSphere.position + _offset;

        // 🔹 Add smooth oscillating noise offset (tiny wobble)
        float noiseX = (Mathf.PerlinNoise(Time.time * 0.8f, _noiseSeed) - 0.5f) * 0.1f;
        float noiseZ = (Mathf.PerlinNoise(_noiseSeed, Time.time * 0.8f) - 0.5f) * 0.1f;
        Vector3 noiseOffset = new Vector3(noiseX, 0, noiseZ);

        // Blended target = base + static drift + dynamic noise
        Vector3 targetPos = baseTarget + _staticOffset + noiseOffset;

        // Smooth move toward target
        stickman.transform.position = Vector3.Lerp(
            stickman.transform.position,
            targetPos,
            Time.deltaTime * 10f);

        // ✅ Face the direction of movement (target - current)
        Vector3 moveDir = targetPos - stickman.transform.position;
        moveDir.y = 0; // keep upright

        if (moveDir.sqrMagnitude > 0.0001f)
        {
            stickman.RotateModelTo(stickman.transform.position + moveDir.normalized, 0.2f);
        }
    }

    public void Exit(Stickman stickman)
    {
        stickman.ResetRotation();
    }
}



public class WalkingState : IState<Stickman>
{
    public string Name => StickmanState.Moving.ToString();

    public void Enter(Stickman stickman) 
    { 
        stickman.Animator.SetTrigger("Walk");
    }

    public void Tick(Stickman stickman) { }

    public void Exit(Stickman stickman) { }
}

public class AttackingState : IState<Stickman>
{
    public string Name => StickmanState.Attacking.ToString();

    public void Enter(Stickman stickman) 
    { 

    }

    public void Tick(Stickman stickman) { }

    public void Exit(Stickman stickman) { }
}
