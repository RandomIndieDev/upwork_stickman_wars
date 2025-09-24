using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins.Options;
using Sirenix.OdinInspector;
using UnityEngine;

[ExecuteAlways]
public class StickmanGroupSpawner : MonoBehaviour
{
    [BoxGroup("References"), SerializeField] GridPositioner m_GridPositioner;
    [BoxGroup("Prefabs"), SerializeField] StickmanGroup m_StickmanGroup;

    public static event System.Action<StickmanGroup, int, int> OnGroupSpawned;
    
    [Button("Spawn Groups (Editor)", ButtonSizes.Large)]
    public void SpawnGroups()
    {
        ClearExisting();
        
        int index = 0;
        foreach (var savedPos in m_GridPositioner.SavedPositions)
        {
            var stickmanGroup = Instantiate(m_StickmanGroup, m_GridPositioner.transform);
            stickmanGroup.Init(StickmanColors.Instance.GetRandomColor());
            m_GridPositioner.SetInPos(stickmanGroup.gameObject, savedPos);

            int row = index / m_GridPositioner.columns;
            int col = index % m_GridPositioner.columns;
            
            OnGroupSpawned?.Invoke(stickmanGroup, row, col);
            index++;
        }
    }

    [Button("Clear Spawned Groups (Editor)", ButtonSizes.Large)]
    void ClearExisting()
    {
        var groups = GetComponentsInChildren<StickmanGroup>();
        foreach (var g in groups)
        {
            if (Application.isEditor)
                DestroyImmediate(g.gameObject);
            else
                Destroy(g.gameObject);
        }
    }
}


