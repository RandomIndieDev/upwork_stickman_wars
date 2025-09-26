using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        EventManager.Subscribe<Vector2Int>("OnGridClicked", OnGridClicked);
        DOTween.SetTweensCapacity(1000, 10);
    }

    void OnGridClicked(Vector2Int value)
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
