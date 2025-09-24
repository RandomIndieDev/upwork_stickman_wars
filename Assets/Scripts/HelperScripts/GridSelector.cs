using System;
using UnityEngine;
using Lean.Touch;
using Sirenix.OdinInspector;

public class GridClicker : MonoBehaviour
{
    [BoxGroup("Settings"), SerializeField] LayerMask m_Layer;
    [BoxGroup("Settings"), SerializeField] String m_Tag;
    
    [SerializeField] GridPositioner grid;
    Camera mainCam;

    void Awake()
    {
        mainCam = Camera.main;
        LeanTouch.OnFingerTap += HandleFingerTap;
    }

    void OnDestroy()
    {
        LeanTouch.OnFingerTap -= HandleFingerTap;
    }

    void HandleFingerTap(LeanFinger finger)
    {
        Ray ray = mainCam.ScreenPointToRay(finger.ScreenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, m_Layer))
        {
            if (hit.collider != null && hit.collider.gameObject.CompareTag(m_Tag))
            {
                Vector3 localHit = grid.transform.InverseTransformPoint(hit.point);
                
                int col = Mathf.RoundToInt(localHit.x / grid.spacingX);
                int row = Mathf.RoundToInt(localHit.z / grid.spacingY);
                
                if (grid.xDirection == GridPositioner.AxisDirection.Negative)
                    col = -col;
                if (grid.yDirection == GridPositioner.AxisDirection.Negative)
                    row = -row;
                
                col = Mathf.Clamp(col, 0, grid.columns - 1);
                row = Mathf.Clamp(row, 0, grid.rows - 1);
                
                EventManager.Raise("OnGridClicked", new Vector2Int(row,col));

                Vector3 cellWorld = grid.transform.TransformPoint(
                    new Vector3(col * grid.spacingX * (int)grid.xDirection,
                        0,
                        row * grid.spacingY * (int)grid.yDirection));

                Debug.DrawLine(mainCam.transform.position, cellWorld, Color.green, 1f);
            }
        }
    }
}