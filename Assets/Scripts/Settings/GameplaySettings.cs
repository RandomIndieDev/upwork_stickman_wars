using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Settings/GameSettings", order = 1)]
public class GameplaySettings : ScriptableObject
{
    [BoxGroup("Platform Settings"), SerializeField] public float DelayBeforeMovingToPlatform;
    [BoxGroup("Platform Settings"), SerializeField] public float MoveToPlatformSpeed;
    [BoxGroup("Platform Settings"), SerializeField] public float MovePastPlatformSpeed = 1f;
}
