using UnityEngine;
using System.Collections.Generic;

public class ShapeArrayPositioner : MonoBehaviour
{
    [SerializeField] GameObject stickmanPrefab;
    [SerializeField] float spacing = 1f;
    [SerializeField] StickmanShape shape;

    private List<GameObject> spawnedStickmen = new List<GameObject>();

    [ContextMenu("Generate Shape")]
    public void GenerateShape()
    {
        ClearShape();

        for (int row = 0; row < 2; row++)
        {
            for (int col = 0; col < 2; col++)
            {
                PositionFillType type = shape.grid[row, col];
                if (type == PositionFillType.Empty) continue;

                Vector3 localPos = new Vector3(col * spacing, 0, -row * spacing);

                GameObject stickman = Instantiate(stickmanPrefab, transform);
                stickman.transform.localPosition = localPos;
                stickman.name = $"Stickman_{row}_{col}";
                
                var s = stickman.GetComponent<Stickman>();

                spawnedStickmen.Add(stickman);
            }
        }
    }

    public void ClearShape()
    {
        foreach (var go in spawnedStickmen)
        {
            if (go != null) DestroyImmediate(go);
        }
        spawnedStickmen.Clear();
    }
}

