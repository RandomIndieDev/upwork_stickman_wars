using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StickmanState
{
    Idle,
    Moving,
    Walking,
    Attacking,
    FollowPointState,
    Death
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

    private readonly Transform _offsetSource; // external point we pass in

    private Transform _followSphere;
    private Vector3 _offset;         // base offset from followSphere to keep formation
    private Vector3 _staticOffset;   // small unique bias per stickman
    private float _noiseSeed;        // per-stickman seed
    private float _orbitPhase;       // per-stickman orbital phase
    private Vector3 _vel;            // for SmoothDamp

    // ---- Tunables (tweak to taste) ----
    private readonly float _smoothTime;       // SmoothDamp time (smaller = snappier)
    private readonly float _maxSpeed;         // SmoothDamp max speed cap
    private readonly float _orbitRadius;      // radius of slow circular drift
    private readonly float _orbitSpeed;       // speed of the circular drift (rad/s)
    private readonly float _noiseAmp;         // amplitude of Perlin noise jitter
    private readonly float _noiseFreq;        // frequency of Perlin noise
    private readonly float _leashDistance;    // when far, reduce noise/orbit to re-group

    public FollowPointState(
        Transform offsetSource,
        float smoothTime   = 0.12f,
        float maxSpeed     = 10f,
        float orbitRadius  = 0.12f,
        float orbitSpeed   = 0.8f,
        float noiseAmp     = 0.06f,
        float noiseFreq    = 0.7f,
        float leashDistance = 1.5f)
    {
        _offsetSource  = offsetSource;
        _smoothTime    = smoothTime;
        _maxSpeed      = maxSpeed;
        _orbitRadius   = orbitRadius;
        _orbitSpeed    = orbitSpeed;
        _noiseAmp      = noiseAmp;
        _noiseFreq     = noiseFreq;
        _leashDistance = leashDistance;
    }

    public void Enter(Stickman stickman)
    {
        _followSphere = stickman.CurrentFollowTarget;
        if (_followSphere == null) return;

        // Base formation offset: relative to provided offsetSource (fallback: stickman)
        if (_offsetSource != null)
            _offset = _offsetSource.position - _followSphere.position;
        else
            _offset = stickman.transform.position - _followSphere.position;

        // Small unique bias so multiple stickmen don't stack perfectly
        _staticOffset = new Vector3(
            Random.Range(-0.10f, 0.10f),
            0f,
            Random.Range(-0.10f, 0.10f)
        );

        // Per-stickman seeds for noise & orbit, keeps variety but stable over time
        _noiseSeed  = Random.Range(0f, 100f);
        _orbitPhase = Random.Range(0f, Mathf.PI * 2f);

        _vel = Vector3.zero;

        stickman.Animator.SetBool("Jog", true);
    }

    public void Tick(Stickman stickman)
    {
        if (_followSphere == null) return;

        // Base target = follow target + formation offset
        Vector3 baseTarget = _followSphere.position + _offset;

        // Soft leash: if far from base, damp the decorative offsets (orbit/noise)
        float distToBase = Vector3.Distance(stickman.transform.position, baseTarget);
        float leashT = Mathf.InverseLerp(0f, _leashDistance, distToBase); // 0 near, 1 far
        float decorScale = 1f - 0.65f * leashT; // reduce up to 65% when far

        float t = Time.time;

        // Slow orbital drift around the base point (keeps the group "alive")
        Vector3 orbit =
            new Vector3(Mathf.Cos(t * _orbitSpeed + _orbitPhase), 0f,
                        Mathf.Sin(t * _orbitSpeed + _orbitPhase)) * (_orbitRadius * decorScale);

        // Subtle noise for non-uniform motion (different axes & seeds)
        float nx = (Mathf.PerlinNoise(t * _noiseFreq, _noiseSeed) - 0.5f) * 2f;
        float nz = (Mathf.PerlinNoise(_noiseSeed, t * _noiseFreq) - 0.5f) * 2f;
        Vector3 noise = new Vector3(nx, 0f, nz) * (_noiseAmp * decorScale);

        // Final desired target
        Vector3 desired = baseTarget + _staticOffset + orbit + noise;

        // Smooth-damped movement feels organic and stays together
        stickman.transform.position = Vector3.SmoothDamp(
            stickman.transform.position,
            desired,
            ref _vel,
            _smoothTime,
            _maxSpeed
        );

        // Face movement direction if moving
        Vector3 moveDir = desired - stickman.transform.position;
        moveDir.y = 0f;
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

    private Stickman _target;
    private float _attackRange = 0.5f;   // how close is "close enough"
    private float _attackSpeed = 4f;     // movement speed when attacking

    public AttackingState(Stickman target)
    {
        _target = target;
    }

    public void Enter(Stickman stickman)
    {
        if (_target == null)
        {
            stickman.SetState(null); // fallback to idle
            return;
        }

        stickman.Animator.SetBool("Jog", true); // optional animation trigger
    }

    public void Tick(Stickman stickman)
    {
        if (_target == null)
        {
            stickman.SetState(null);
            return;
        }
        
        Vector3 direction = (_target.transform.position - stickman.transform.position).normalized;
        stickman.transform.position += direction * _attackSpeed * Time.deltaTime;
        
        if (direction.sqrMagnitude > 0.001f)
        {
            stickman.RotateModelTo(stickman.transform.position + direction, 0.2f);
        }

        float dist = Vector3.Distance(stickman.transform.position, _target.transform.position);
        if (dist <= _attackRange)
        {        
            stickman.SetState(new DeathState());
            _target.SetState(new DeathState());
        }
    }

    public void Exit(Stickman stickman)
    {
        stickman.Animator.SetBool("Jog", false);
    }
}

public class DeathState : IState<Stickman>
{
    public string Name => StickmanState.Death.ToString();

    public void Enter(Stickman stickman) 
    { 
        stickman.gameObject.SetActive(false);
        ParticleManager.Instance.Play("Splash", stickman.transform.position, StickmanColors.Instance.GetColor(stickman.ColorType));
    }

    public void Tick(Stickman stickman) { }

    public void Exit(Stickman stickman) { }
}

