using UnityEngine;
using Dreamteck.Splines;   // Make sure this is included

public class SplineReverser : MonoBehaviour
{
    public SplineComputer SplineComputer;

    [Sirenix.OdinInspector.Button] // Optional since you used [Button]
    public void Reverse()
    {
        if (SplineComputer == null)
        {
            Debug.LogError("SplineComputer reference is missing!");
            return;
        }
        
        SplinePoint[] points = SplineComputer.GetPoints();
        
        System.Array.Reverse(points);

        // Assign them back
        SplineComputer.SetPoints(points);
        SplineComputer.Rebuild();

        Debug.Log("Spline reversed successfully!");
    }
}