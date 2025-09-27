using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
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

    [BoxGroup("References"), SerializeField] EnemyGridManager m_EnemyGridManager;
    [BoxGroup("References"), SerializeField] SplinePathBuilder m_SplinePathBuilder;
    [BoxGroup("References"), SerializeField] StickmanPointFeeder m_StickmanPointFeeder;
    
    int m_SelectedTotalSpotCount;
    ColorType m_SelectedColor;
    Vector2Int m_SelectedGrid;
    
    List<Vector2Int> m_SelectedGridList;
    GroupExitData m_CurrentGroupExitData;

    AudioManager m_AudioManager;
    GameplaySettings m_GameplaySettings;
    
    Coroutine m_AttackRoutine;
    
    bool m_ProcessingTurn;
    bool m_AttackRunning;
    bool m_AttackPending;

    List<Vector2Int> m_ClearedGrids;

    bool m_GridUpdateRunning;
    
    void Start()
    {
        EventManager.Subscribe<Vector2Int>("OnGridClicked", OnGridClicked);
        
        m_GameplaySettings = GameManager.Instance.GameSettings;
        m_AudioManager = AudioManager.Instance;

        m_ClearedGrids = new List<Vector2Int>();
        
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
    
    private IEnumerator RunGridUpdateWrapper()
    {
        do
        {
            m_GridUpdateRunning = true;
            
            yield return ClearEnemyGrid(m_ClearedGrids, 0.4f);

            m_ClearedGrids.Clear();
            m_GridUpdateRunning = false;
            
        } while (m_GridUpdateRunning);

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
        
        EnablePlayerInput();
        TryStartAttackCheck();
    }

    void EnablePlayerInput()
    {
        m_ProcessingTurn = false;
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
            m_AudioManager.PlayOneShot(SoundType.GridClickInvalid);
            selectedGroup.ShakeGroups();
        }
        else
        {
            m_AudioManager.PlayOneShot(SoundType.GridClickValid);
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
            
            splineFeederList.Add(new SplineFeederData()
            {
                Platform = platform,
                TransferCount = count,
                ExitRecommendations = exitRec,
                SplineFeederIndex = feederIndex,
            });
        }
        

        yield return new WaitForSeconds(0.02f);

        var isDirectAttackAvailable = HasDirectAttackPath();

        foreach (var splineFeederData in splineFeederList)
        {
            var exitRecommendations = splineFeederData.ExitRecommendations;
            var groupList = splineFeederData.ExitRecommendations
                .Where(i => i.Group != null)  
                .Select(i => i.Group)          
                .ToList();

            var feederIndex = splineFeederData.SplineFeederIndex;
            var currentPlatform = splineFeederData.Platform;

            if (isDirectAttackAvailable)
            {
                StartCoroutine(HandleDirectAttackTransfer(groupList, currentPlatform));
            }
            else
            {
                currentPlatform.PrebookSpots(m_SelectedColor, splineFeederData.TransferCount);
            }
            
            foreach (var exitRecom in exitRecommendations)
            {
                if (exitRecom.Path.Count <= 1 && exitRecom.Group.GroupGridLoc != m_SelectedGrid)
                {
                    exitRecom.Path[0] = m_SelectedGrid;
                }
                
                var worldPath = new List<Vector3>();
                foreach (var gridPos in exitRecom.Path)
                    worldPath.Add(m_PlayerBoardManager.GridToWorld(gridPos));


                transferCount++;

                exitRecom.Platform = currentPlatform;
                
                exitRecom.Group.ActivateFollowPointState();
                exitRecom.Group.TraverseThroughPoints(m_SelectedGrid,worldPath, () =>
                {
                    m_Feeder.MoveAlongSpline(feederIndex, exitRecom.Group.FollowSphere, () =>
                    {
                        if (isDirectAttackAvailable)
                        {
                            exitRecom.Group.OnReadyForStep?.Invoke();
                            return;
                        };
                        
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


    bool HasDirectAttackPath()
    {
        return m_EnemyGridManager.GetGridsWithColor(m_SelectedColor).Count > 0;
    }
    
    IEnumerator HandleDirectAttackTransfer(List<StickmanGroup> groups, Platform atPlatform)
    {
        var moveToLocation = atPlatform.PlatformOutput.position;
        var attackableGridData = m_EnemyGridManager.GetAttackableGridsData(m_SelectedColor);
        var currentGroupIndex = 0;
        var jobCount = 0;
        
        
        for (int i = 0; i < attackableGridData.Count; i++)
        {
            if (currentGroupIndex >= groups.Count) continue;
            
            var enemyGroupCount = attackableGridData[i].ConnectedGrids.Count; 
            int neededGroups = Mathf.Min(enemyGroupCount, groups.Count);
            
            attackableGridData[i].AssignedPlayerGroups.AddRange(groups.GetRange(currentGroupIndex, neededGroups));
            currentGroupIndex += neededGroups;
        }

        if (currentGroupIndex <= groups.Count)
        {
            for (int i = currentGroupIndex; i < groups.Count; i++)
            {
                groups[i].OnReadyForStep = null;
                int i1 = i;
                groups[i].OnReadyForStep += () =>
                {
                    atPlatform.AddStickmanGroup(groups[i1], () =>
                    {

                    });
                };
            }
        }

        for (int i = 0; i < attackableGridData.Count; i++)
        {
            if (attackableGridData[i].AssignedPlayerGroups.Count <= 0) continue;

            var gridX = attackableGridData[i].ConnectedGrids[0].x;
            var assignedGroups = attackableGridData[i].AssignedPlayerGroups;
            
            for (int j = 0; j < assignedGroups.Count; j++)
            {
                var splinePoints = m_SplinePathBuilder.GetPath(atPlatform.Index, gridX);
                var group = assignedGroups[j];
                var enemyGroup = m_EnemyGridManager.GetGroupInGrid(attackableGridData[i].ConnectedGrids[j]);

                jobCount++;
                group.OnReadyForStep = null;
                group.OnReadyForStep += () =>
                {
                    group.FollowSphere.DOMove(moveToLocation, m_GameplaySettings.MovePastPlatformSpeed).SetEase(Ease.Linear).onComplete += () =>
                    {
                        m_StickmanPointFeeder.MoveTargetThroughPoints(splinePoints, group.FollowSphere.transform, () =>
                        {
                            jobCount--;
                        }, () =>
                        {
                            StartCoroutine(HandleAttack(group, enemyGroup));
                        }, 0.8f);
                    };
                };
            }
        }

        EnablePlayerInput();
        
        yield return new WaitUntil(() => jobCount <= 0);

        m_ClearedGrids.AddRange(GetClearedEnemyGrids(attackableGridData));
        yield return RunGridUpdateWrapper();
    }

    List<Vector2Int> GetClearedEnemyGrids(List<AttackableGridsData> data)
    {
        var clearedList = new List<Vector2Int>();

        foreach (var gridData in data)
        {
            var clearedCount = gridData.AssignedPlayerGroups.Count;
            clearedList.AddRange(gridData.ConnectedGrids.GetRange(0,clearedCount));
        }

        return clearedList;
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
                

                
                removedCount += neededGroups;
                
                var playerStickmen = currentPlatform.RemoveGroups(neededGroups);
                var enemyStickmenGroup = chained.GetRange(0, neededGroups);
                var splinePoints = m_SplinePathBuilder.GetPath(currentPlatform.Index, grid.x);
                
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

        m_ClearedGrids.AddRange(clearEnemyGrids);
        yield return RunGridUpdateWrapper();
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
