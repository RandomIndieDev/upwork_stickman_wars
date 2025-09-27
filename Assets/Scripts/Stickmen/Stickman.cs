using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;

public class Stickman : MonoBehaviour
{
    static readonly int m_MainColor = Shader.PropertyToID("_Color");
    
    public string CurrentState => stateMachine.CurrentStateName;
    
    [BoxGroup("References"), SerializeField] GameObject m_Model;
    [BoxGroup("References"), SerializeField] SkinnedMeshRenderer m_MeshRenderer;
    [BoxGroup("References"), SerializeField] Animator m_Animator;
    
    [BoxGroup("Info"), SerializeField] ColorType m_ColorType;
    
    [ShowInInspector, ReadOnly, BoxGroup("Debug")]
    private string DebugState => stateMachine != null ? stateMachine.CurrentStateName : "None";
    StateMachine stateMachine;

    StickmanGroup m_CurrentGroup;
    
    public Animator Animator => m_Animator;
    public GameObject Model => m_Model;
    public StickmanGroup Group => m_CurrentGroup;
    public AudioManager AudioManager => m_AudioManager;
    public ParticleManager ParticleManager => m_ParticleManager;
    
    Transform m_CurrentFollowTarget;
    
    AudioManager m_AudioManager;
    ParticleManager m_ParticleManager;

    public Transform CurrentFollowTarget
    {
        get => m_CurrentFollowTarget;
        set => m_CurrentFollowTarget = value;
    }

    public ColorType ColorType
    {
        get => m_ColorType;
        set => m_ColorType = value;
    }
    
    public void Init(StickmanGroup currentGroup)
    {
        m_CurrentGroup = currentGroup;
        
        m_AudioManager = AudioManager.Instance;
        m_ParticleManager = ParticleManager.Instance;

        stateMachine = gameObject.GetComponent<StateMachine>();
        if (stateMachine == null)
        {
            stateMachine = gameObject.AddComponent<StateMachine>();
        }
        stateMachine.Init(this);
        SetMaterial(StickmanColors.Instance.GetMaterial(m_ColorType));
    }

    public void SetTag(string tag)
    {
        gameObject.tag = tag;
    }
    
    public void SetState(IState<Stickman> newState)
    {
        stateMachine.SetState(newState);
    }

    public void SetTargetAndAttack(Stickman target)
    {
        SetState(new AttackingState(target));
    }

    public void SetMaterial(Material mat)
    {
        var mats = m_MeshRenderer.sharedMaterials;
        mats[0] = mat;
        m_MeshRenderer.materials = mats;
    }
    
    public void RotateModelTo(Vector3 target, float time)
    {
        Vector3 localTarget = m_Model.transform.parent.InverseTransformPoint(target);
        Quaternion localRot = Quaternion.LookRotation(localTarget, Vector3.up);
        m_Model.transform.DOLocalRotateQuaternion(localRot, time);
    }

    public void ResetRotation()
    {
        m_Model.transform.DOKill();
        m_Model.transform.DOLocalRotate(Vector3.zero, 0.08f);
    }
}
