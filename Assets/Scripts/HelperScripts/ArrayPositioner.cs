using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class TwoDimensionalPos
{
    public int xPos;
    public int yPos;
}


public class ArrayPositioner : MonoBehaviour
{
    
    [BoxGroup("Settings")] public int rows = 4;
    [BoxGroup("Settings")] public int columns = 8;
    [BoxGroup("Settings")] public float spacingX = 1f;
    [BoxGroup("Settings")] public float spacingY = 1f;

    [Header("Point Settings")]
    public GameObject pointPrefab;
    public bool generateOnStart = true;

    [SerializeField] List<GameObject> m_ArrayPositions;
    void Start()
    {
        if (generateOnStart && pointPrefab != null)
        {
            GenerateGrid();
        }
    }

    public GameObject GetPositionalObject(int pos)
    {
        return m_ArrayPositions[pos];
    }
    
    public TwoDimensionalPos Get2DPosition(int index)
    {
        if (index < 0 || index >= m_ArrayPositions.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range!");

        int row = index / columns;  
        int col = index % columns; 

        return new TwoDimensionalPos
        {
            xPos = col,  
            yPos = row  
        };
    }
    
    [Button]
    public void GenerateGrid()
    {
        if (m_ArrayPositions != null)
        {
            foreach (GameObject g in m_ArrayPositions.Where(g => g != null))
            {
                DestroyImmediate(g);
            }
        }

        m_ArrayPositions = new List<GameObject>();

        var rowCount = 0;
        
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Vector3 localPos = new Vector3(col * spacingX, 0, rowCount * spacingY);

                GameObject point = Instantiate(pointPrefab, transform);
                point.transform.localPosition = localPos;
                point.name = $"Point_{row}_{col}";
                
                m_ArrayPositions.Add(point);
            }

            rowCount--;
        }
    }

    [Button]
    public void CleanUp()
    {
        foreach (GameObject g in m_ArrayPositions.Where(g => g != null))
        {
            DestroyImmediate(g.transform.GetChild(0).transform.gameObject);
        }
    }
}