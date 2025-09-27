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

    // ---- Tunables ----
    private readonly float _smoothTime;
    private readonly float _maxSpeed;
    private readonly float _orbitRadius;
    private readonly float _orbitSpeed;
    private readonly float _noiseAmp;
    private readonly float _noiseFreq;
    private readonly float _leashDistance;

    // ---- Separation ----
    private readonly float _separationRadius = 0.4f;   // how close before repelling
    private readonly float _separationStrength = 0.5f; // multiplier for push

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

        // Base offset relative to provided offsetSource (fallback: self)
        _offset = _offsetSource != null
            ? _offsetSource.position - _followSphere.position
            : stickman.transform.position - _followSphere.position;

        // Small unique bias so multiple stickmen don’t stack perfectly
        _staticOffset = new Vector3(
            Random.Range(-0.10f, 0.10f),
            0f,
            Random.Range(-0.10f, 0.10f)
        );

        // Seeds for noise & orbit
        _noiseSeed  = Random.Range(0f, 100f);
        _orbitPhase = Random.Range(0f, Mathf.PI * 2f);
        _vel = Vector3.zero;

        stickman.Animator.SetBool("Jog", true);
    }

    public void Tick(Stickman stickman)
    {
        if (_followSphere == null) return;

        // --- Base target formation ---
        Vector3 baseTarget = _followSphere.position + _offset;

        // Leash scaling → reduce orbit/noise when far away
        float distToBase = Vector3.Distance(stickman.transform.position, baseTarget);
        float leashT = Mathf.InverseLerp(0f, _leashDistance, distToBase);
        float decorScale = 1f - 0.65f * leashT;

        float t = Time.time;

        // Orbit around the follow point
        Vector3 orbit =
            new Vector3(Mathf.Cos(t * _orbitSpeed + _orbitPhase), 0f,
                        Mathf.Sin(t * _orbitSpeed + _orbitPhase)) * (_orbitRadius * decorScale);

        // Perlin noise jitter
        float nx = (Mathf.PerlinNoise(t * _noiseFreq, _noiseSeed) - 0.5f) * 2f;
        float nz = (Mathf.PerlinNoise(_noiseSeed, t * _noiseFreq) - 0.5f) * 2f;
        Vector3 noise = new Vector3(nx, 0f, nz) * (_noiseAmp * decorScale);

        // Final target
        Vector3 desired = baseTarget + _staticOffset + orbit + noise;

        // --- NEW: Separation from neighbors ---
        Vector3 separation = Vector3.zero;
        if (stickman.Group != null) // assumes Stickman has a ref to its group
        {
            foreach (var other in stickman.Group.Members)
            {
                if (other == stickman) continue;

                float dist = Vector3.Distance(stickman.transform.position, other.transform.position);
                if (dist < _separationRadius)
                {
                    Vector3 away = (stickman.transform.position - other.transform.position).normalized;
                    separation += away * (_separationRadius - dist);
                }
            }
        }

        desired += separation * _separationStrength;

        // SmoothDamp toward the final desired position
        stickman.transform.position = Vector3.SmoothDamp(
            stickman.transform.position,
            desired,
            ref _vel,
            _smoothTime,
            _maxSpeed
        );

        // Rotate toward movement direction
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
        stickman.transform.position += direction * (_attackSpeed * Time.deltaTime);
        
        if (direction.sqrMagnitude > 0.001f)
        {
            stickman.RotateModelTo(stickman.transform.position + direction, 0.2f);
        }

        float dist = Vector3.Distance(stickman.transform.position, _target.transform.position);
        if (dist <= _attackRange)
        {        
            
            stickman.AudioManager.PlayOneShot(SoundType.DeathSound);
            var centerPoint = GetCenterPoint(stickman.transform, _target.transform);
            stickman.ParticleManager.Play("Splash", centerPoint + Vector3.up * 0.28f, StickmanColors.Instance.GetColor(stickman.ColorType));
            stickman.SetState(new DeathState());
            _target.SetState(new DeathState());
        }
    }

    public void Exit(Stickman stickman)
    {
        stickman.Animator.SetBool("Jog", false);
    }
    
    Vector3 GetCenterPoint(Transform a, Transform b)
    {
        return (a.position + b.position) / 2f;
    }

}

public class DeathState : IState<Stickman>
{
    public string Name => StickmanState.Death.ToString();

    public void Enter(Stickman stickman)
    {
        ;
        stickman.gameObject.SetActive(false);
    }

    public void Tick(Stickman stickman) { }

    public void Exit(Stickman stickman) { }
}

