using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

public class GameplayLoopManager : MonoBehaviour
{

    
    [BoxGroup("References"), SerializeField] StickmanFeeder m_Feeder;
    [BoxGroup("References"), SerializeField] PlatformFeeder m_PlatformFeeder;
    [BoxGroup("References"), SerializeField] PlatformManager m_PlatformManager;
    
    [BoxGroup("References"), SerializeField] StickmenBoardManager m_PlayerBoardManager;
    [BoxGroup("References"), SerializeField] StickmanFeeder m_StickmanFeeder;


    ColorType m_SelectedColor;
    Vector2Int m_SelectedGrid;
    List<Vector2Int> m_SelectedGridList;
    GroupExitData m_CurrentGroupExitData;
    bool m_ProcessingTurn;
    
    void Start()
    {
        EventManager.Subscribe<Vector2Int>("OnGridClicked", OnGridClicked);
    }
    
    void OnGridClicked(Vector2Int value)
    {
        if (m_ProcessingTurn) return;
        StartCoroutine(RunTurn(value));
    }

    IEnumerator RunTurn(Vector2Int clickLocation)
    {
        m_SelectedGrid = new Vector2Int(clickLocation.y, clickLocation.x);
        m_ProcessingTurn = true;
        m_CurrentGroupExitData = new GroupExitData();
        
        HandlePlayerGridSelect();

        if (!m_CurrentGroupExitData.CanExit)
        {
            m_ProcessingTurn = false;
            yield break; 
        }

        
        yield return StartCoroutine(HandleValidGridSelected());

        m_ProcessingTurn = false;
    }
    

    void HandlePlayerGridSelect()
    {
        m_SelectedGridList = new List<Vector2Int>();
        
        if (!m_PlayerBoardManager.DoesGridContainsGroup(m_SelectedGrid))
        {
            m_CurrentGroupExitData.CanExit = false;
            return;
        }
        
        var selectedGroup = m_PlayerBoardManager.GetGroup(m_SelectedGrid);
        var connectedGroup = selectedGroup.GetAllConnected();
        
        m_SelectedGridList.AddRange(connectedGroup.Select(group => group.GroupGridLoc));
        m_CurrentGroupExitData = m_PlayerBoardManager.CanGroupGridExit(m_SelectedGridList);

        if (!m_CurrentGroupExitData.CanExit)
        {
            selectedGroup.ShakeGroups();
        }
    }

    IEnumerator HandleValidGridSelected()
    {
        var selectedGroup = m_PlayerBoardManager.GetGroup(m_SelectedGrid);
        var connectedGroup = selectedGroup.GetAllConnected();
        
        m_SelectedColor = selectedGroup.GroupColor;
        m_PlayerBoardManager.GetGroupExitPoints(ref m_CurrentGroupExitData);

        var allGroups = new HashSet<StickmanGroup>();
        allGroups.Add(selectedGroup);
        allGroups.AddRange(connectedGroup);

        m_PlayerBoardManager.EmptyGrids(m_SelectedGridList);
        m_SelectedGridList.Clear();

        var sharedExitWorldPath = new List<Vector3>();
        foreach (var gridPos in m_CurrentGroupExitData.GridExitPointsOrdered)
        {
            sharedExitWorldPath.Add(m_PlayerBoardManager.GridToWorld(gridPos));
        }
        
        var freePlatformOfType = m_PlatformManager.GetFreePlatformOfType(m_SelectedColor);
        if (freePlatformOfType == null)
        {
            freePlatformOfType = m_PlatformManager.GetNextAvailablePlatform();
        }

        //freePlatformOfType.PrebookSpots(allGroups.Count() * 4);
        
        var splineBuilt = false;

        var firstRowGroups = allGroups.Where(g => g.CanExitGrid());
        
        foreach (var group in firstRowGroups)
        {
            if (group.CanExitGrid())
            {
                if (!splineBuilt)
                {
                    m_Feeder.BuildSplineFor(freePlatformOfType.Index, m_PlayerBoardManager.GridToWorld(group.GroupGridLoc));
                    splineBuilt = true;
                }
                
                group.ActivateFollowPointState();
                m_Feeder.MoveAlongSpline(group.FollowSphere, () =>
                {
                    group.
                    StartCoroutine(TransferStickmenToPlatform(group, freePlatformOfType));
                });
            }
        }
        
        allGroups.RemoveWhere(g => g.CanExitGrid());
        
        if (allGroups.Count <= 0) yield break;
        
        var recomended = m_PlayerBoardManager.GetRecommendedExitOrder(allGroups.ToList(), m_SelectedColor);
            
        if (!splineBuilt)
        {
            m_Feeder.BuildSplineFor(freePlatformOfType.Index, m_PlayerBoardManager.GridToWorld(recomended[0].Path[^1]));
        }

        foreach (var recommendation in recomended)
        {
            var worldPath = new List<Vector3>();
            foreach (var gridPos in recommendation.Path)
                worldPath.Add(m_PlayerBoardManager.GridToWorld(gridPos));
                
            recommendation.Group.ActivateFollowPointState();
            recommendation.Group.TraverseThroughPoints(worldPath, () =>
            {
                m_Feeder.MoveAlongSpline(recommendation.Group.FollowSphere, () =>
                {
                    StartCoroutine(TransferStickmenToPlatform(recommendation.Group, freePlatformOfType));
                });
            });
        }
        
        yield return new WaitForSeconds(0.02f);
    }
    
    IEnumerator TransferStickmenToPlatform(StickmanGroup group, Platform platform)
    {
        var stickmen = group.GetStickmen();

        foreach (var stickman in stickmen)
        {
            stickman.SetState(new MovingState());
            m_PlatformFeeder.TransferInto(stickman, platform);

            yield return new WaitForSeconds(0.1f);
        }
    }
}
