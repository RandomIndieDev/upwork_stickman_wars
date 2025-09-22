using UnityEngine;

[CreateAssetMenu(fileName = "StickmanShape", menuName = "Game/StickmanShape")]
public class StickmanShape : ScriptableObject
{
    [Header("2x2 Shape Definition")]
    public PositionFillType[,] grid = new PositionFillType[2, 2]
    {
        { PositionFillType.Empty , PositionFillType.Empty },
        { PositionFillType.Empty, PositionFillType.Empty }
    };
}