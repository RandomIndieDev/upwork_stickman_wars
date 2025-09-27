using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dreamteck.Splines;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;


public class GameplayLoopManager : MonoBehaviour
{
    [BoxGroup("References"), SerializeField] StickmanFeeder m_Feeder;
    [BoxGroup("References"), SerializeField] PlatformManager m_PlatformManager;
    

    [BoxGroup("References"), SerializeField] StickmenBoardManager m_PlayerBoardManager;
    [BoxGroup("References"), SerializeField] StickmanFeeder m_StickmanFeeder;
    
    //Enemy
    [BoxGroup("References"), SerializeField] EnemyGridManager m_EnemyGridManager;
    [BoxGroup("References"), SerializeField] SplinePathBuilder m_SplinePathBuilder;
    [BoxGroup("References"), SerializeField] StickmanPointFeeder m_StickmanPointFeeder;
    
    

    int m_SelectedTotalSpotCount;
    ColorType m_SelectedColor;
    Vector2Int m_SelectedGrid;
    
    List<Vector2Int> m_SelectedGridList;
    GroupExitData m_CurrentGroupExitData;
    
    bool m_ProcessingTurn;
    
    private Coroutine m_AttackRoutine;
    private bool m_AttackRunning;
    private bool m_AttackPending;

    void Start()
    {
        EventManager.Subscribe<Vector2Int>("OnGridClicked", OnGridClicked);
        
        m_SplinePathBuilder.BuildAndSaveAllPaths();
    }
    
    void OnEnable()
    {
        m_EnemyGridManager.OnGridUpdated += RunPlatformCheck;
    }
    
    void OnDisable()
    {
        m_EnemyGridManager.OnGridUpdated -= RunPlatformCheck;
    }
    
    void OnGridClicked(Vector2Int value)
    {
        if (m_ProcessingTurn) return;
        StartCoroutine(RunTurn(value));
    }

    public void TryStartAttackCheck()
    {
        if (m_AttackRunning)
        {
            m_AttackPending = true;
            return;
        }

        m_AttackRoutine = StartCoroutine(RunAttackCheckWrapper());
    }

    private IEnumerator RunAttackCheckWrapper()
    {
        do
        {
            m_AttackRunning = true;
            m_AttackPending = false;
            
            yield return HandleAttackCheck();

            m_AttackRunning = false;
            
        } while (m_AttackPending);

        m_AttackRoutine = null;
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
        
        yield return HandleValidGridSelected();
        
        m_ProcessingTurn = false;
        TryStartAttackCheck();
    }
    

    void HandlePlayerGridSelect()
    {
        m_SelectedGridList = new List<Vector2Int>();

        var gridType = m_PlayerBoardManager.GetGridType(m_SelectedGrid);

        if (gridType == GridObjectType.Crate)
        {
            m_PlayerBoardManager.GetGridObj(m_SelectedGrid).GetCrate().OnClick();
        }

        if (gridType == GridObjectType.NONE || gridType == GridObjectType.Crate)
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
        var transferCount = 0;
        var selectedGroup = m_PlayerBoardManager.GetGroup(m_SelectedGrid);
        var connectedGroup = selectedGroup.GetAllConnected();
        
        m_SelectedColor = selectedGroup.GroupColor;
        m_PlayerBoardManager.GetGroupExitPoints(ref m_CurrentGroupExitData, m_SelectedColor);

        var allGroups = new HashSet<StickmanGroup>();
        allGroups.Add(selectedGroup);
        allGroups.AddRange(connectedGroup);
        m_SelectedTotalSpotCount = allGroups.Sum(i => i.GetSpotCount());

        m_PlayerBoardManager.EmptyGrids(m_SelectedGridList);
        m_SelectedGridList.Clear();

        var sharedExitWorldPath = new List<Vector3>();
        foreach (var gridPos in m_CurrentGroupExitData.GridExitPointsOrdered)
        {
            sharedExitWorldPath.Add(m_PlayerBoardManager.GridToWorld(gridPos));
        }
        
        var splineBuilt = false;


        var splineFeederList = new List<SplineFeederData>();
        
        var allocations = m_PlatformManager.GetFreePlatformsOfType(m_SelectedColor, m_SelectedTotalSpotCount);
        var recommended = m_PlayerBoardManager.GetRecommendedExitOrder(allGroups.ToList(), m_SelectedColor);
        var groupExitLoc = recommended[0].Path[^1];

        foreach (var (platform, count) in allocations)
        {
            //TODO: Fix hardcoded value
            var groupCount = count / 4;
            var platformPos = m_PlatformManager.GetPlatformInput(platform.Index).position;
            var gridPos = m_PlayerBoardManager.GridToWorld(groupExitLoc);
            var feederIndex = m_Feeder.BuildSplineFor(gridPos, platformPos);

            

            
            var exitRec = recommended.GetRange(0, groupCount);
            recommended.RemoveRange(0, groupCount);

            /*foreach (var exitrec in exitRec)
            {
                Debug.LogError("==========");
                foreach (var exit in exitrec.Path)
                {
                    Debug.LogError(exit);
                }
                Debug.LogError("==========");
            }*/
            
            splineFeederList.Add(new SplineFeederData()
            {
                Platform = platform,
                TransferCount = count,
                ExitRecommendations = exitRec,
                SplineFeederIndex = feederIndex,
            });
        }
        

        yield return new WaitForSeconds(0.02f);

        foreach (var splineFeederData in splineFeederList)
        {
            var exitRecommendations = splineFeederData.ExitRecommendations;
            var feederIndex = splineFeederData.SplineFeederIndex;
            var currentPlatform = splineFeederData.Platform;
            
            currentPlatform.PrebookSpots(m_SelectedColor, splineFeederData.TransferCount);
            
            foreach (var exitRecom in exitRecommendations)
            {
                var worldPath = new List<Vector3>();
                foreach (var gridPos in exitRecom.Path)
                    worldPath.Add(m_PlayerBoardManager.GridToWorld(gridPos));


                transferCount++;
                exitRecom.Group.ActivateFollowPointState();
                exitRecom.Group.TraverseThroughPoints(worldPath, () =>
                {
                    m_Feeder.MoveAlongSpline(feederIndex, exitRecom.Group.FollowSphere, () =>
                    {
                        currentPlatform.AddStickmanGroup(exitRecom.Group, () =>
                        {
                            transferCount--;
                        });
                    }, () =>
                    {
                        exitRecom.Group.ResetFollowSphere();
                        Destroy(exitRecom.Group.FollowSphere.GetComponent<SplineFollower>());
                    });
                });
            }
        }

        yield return new WaitUntil(() => transferCount <= 0);
    }

    void RunPlatformCheck()
    {
        TryStartAttackCheck();
    }

    IEnumerator HandleAttackCheck()
    {
        var clearEnemyGrids = new List<Vector2Int>();
        var platforms = m_PlatformManager.GetAll();
        int movingCount = 0;

        for (int i = 0; i < platforms.Count; i++)
        {
            var platColor = platforms[i].CurrentColorType;
            if (platColor == ColorType.NONE || platforms[i].CurrentCounterValue <= 0)  
                continue;
            
            var currentPlatform = m_PlatformManager.GetPlatformWithIndex(i);
            var gridMatches = m_EnemyGridManager.GetGridsWithColor(platColor);
            var removedCount = 0;

            foreach (var grid in gridMatches)
            {
                if (currentPlatform.CurrentCounterValue <= 0) continue;
                
                var firstRowGroup = m_EnemyGridManager.GetGroupInGrid(grid);
                var connectedVert = firstRowGroup.GetAllConnectedVertical();
                int neededGroups = Mathf.Min(connectedVert.Count, currentPlatform.GetAvailableSets());
                
                var chained = connectedVert.Count > 1
                    ? new List<StickmanGroup>(connectedVert) 
                    : new List<StickmanGroup> { firstRowGroup };
                
                var splinePoints = m_SplinePathBuilder.GetPath(i, grid.x);
                
                removedCount += neededGroups;
                
                var playerStickmen = currentPlatform.RemoveGroups(neededGroups);
                var enemyStickmenGroup = chained.GetRange(0, neededGroups);
                
                
                foreach (var group in playerStickmen)
                    group.ActivateFollowPointState();
                
                var enemyGridList = enemyStickmenGroup.Select(i => i.GroupGridLoc).ToList();
                clearEnemyGrids.AddRange(enemyGridList);
                movingCount++;
                for (int j = 0; j < playerStickmen.Count; j++)
                {
                    if (j >= 1)
                    {
                        splinePoints.Add(m_EnemyGridManager.GridToWorld(enemyGridList[j]));
                    }
                    
                    int localVal = j;


                    m_StickmanPointFeeder.MoveTargetThroughPoints(splinePoints, playerStickmen[j].FollowSphere.transform, () =>
                    {
                        movingCount--;
                    }, () =>
                    {
                        StartCoroutine(HandleAttack(playerStickmen[localVal], enemyStickmenGroup[localVal]));
                    }, 0.8f);
                }
            }
            currentPlatform.UpdateRemaining(removedCount);
        }
        
        yield return new WaitUntil(() => movingCount <= 0);
        
        StartCoroutine(ClearEnemyGrid(clearEnemyGrids, 0.3f));
    }   


    IEnumerator ClearEnemyGrid(List<Vector2Int> list, float delay)
    {
        yield return new WaitForSeconds(delay);
        m_EnemyGridManager.ClearGridLocations(list);
    }

    IEnumerator HandleAttack(StickmanGroup playerGroup, StickmanGroup enemyGroup)
    {
        var players = playerGroup.GetStickmen();
        var enemies = enemyGroup.GetStickmen();
        enemies.Reverse();
        var remappedEnemies = new List<Stickman>();
        players.Reverse();
        remappedEnemies.Add(enemies[2]);
        remappedEnemies.Add(enemies[3]);
        remappedEnemies.Add(enemies[0]);
        remappedEnemies.Add(enemies[1]);
        

        for (int i = 0; i < players.Count; i++)
        {
            players[i].SetTargetAndAttack(remappedEnemies[i]);
            yield return new WaitForSeconds(Random.Range(0.06f, 0.1f));
        }
    }
}
