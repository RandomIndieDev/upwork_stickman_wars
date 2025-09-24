using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class GridPositioner : MonoBehaviour
{
    public enum AxisDirection { Positive = 1, Negative = -1 }

    [BoxGroup("References"), SerializeField] Transform m_HolderPos;
    
    [BoxGroup("Settings")] public int rows = 5;
    [BoxGroup("Settings")] public int columns = 5;
    [BoxGroup("Settings")] public float spacingX = 1f;
    [BoxGroup("Settings")] public float spacingY = 1f;
    
    [BoxGroup("Point Obj Settings")] public Vector3 m_CustomYRotation;

    [BoxGroup("Settings")] public AxisDirection xDirection = AxisDirection.Positive;
    [BoxGroup("Settings")] public AxisDirection yDirection = AxisDirection.Positive;

    [BoxGroup("Settings"), LabelText("Constant Preview")]
    public bool constantPreview = true;

    [SerializeField, ReadOnly] List<Vector3> savedPositions = new List<Vector3>();

    public List<Vector3> SavedPositions => savedPositions;

    [Button("Save Positions")]
    void SavePositions()
    {
        savedPositions.Clear();

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                float x = col * spacingX * (int)xDirection;
                float z = row * spacingY * (int)yDirection;
                Vector3 localPos = new Vector3(x, 0, z);
                savedPositions.Add(localPos);
            }
        }

        Debug.Log($"Saved {savedPositions.Count} positions.");
    }
    
    public Vector3 GetWorldPosition(Vector2Int gridPos)
    {
        Vector3 localPos = new Vector3(
            gridPos.x * spacingX * (int)xDirection,
            0,
            gridPos.y * spacingY * (int)yDirection
        );

        return transform.TransformPoint(localPos);
    }
    
    public void SetInPos(GameObject obj, Vector3 pos)
    {
        obj.transform.parent = m_HolderPos;
        obj.transform.localPosition = pos;
        obj.transform.localEulerAngles = m_CustomYRotation;
    }

    public Vector2 GetGridDistance()
    {
        return new Vector2(spacingX, spacingY);
    }

    void OnDrawGizmos()
    {
        if (!constantPreview) return;

        Gizmos.color = Color.cyan;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                float x = col * spacingX * (int)xDirection;
                float z = row * spacingY * (int)yDirection;
                Vector3 localPos = new Vector3(x, 0, z);
                
                Vector3 worldPos = transform.TransformPoint(localPos);

                Gizmos.DrawSphere(worldPos, 0.1f);
            }
        }
    }

}