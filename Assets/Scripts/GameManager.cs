using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [BoxGroup("Master Settings"), SerializeField, InlineEditor(InlineEditorObjectFieldModes.Boxed)]
    GameplaySettings m_Settings;

    
    public static GameManager Instance { get; private set; }

    public GameplaySettings GameSettings => m_Settings;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        DOTween.SetTweensCapacity(1000, 10);
    }

}