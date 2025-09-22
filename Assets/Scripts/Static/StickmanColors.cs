using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public enum PositionFillType
{
    Occupied,
    Empty
}

public enum ColorType
{
    Color_1,
    Color_2,
    Color_3,
    Color_4,
    Color_5,
    Color_6,
    
    NONE = 0
}

public class StickmanColors : SerializedMonoBehaviour
{
    public static StickmanColors Instance { get; private set; }

    [Title("Stickman Colors")]
    [SerializeField, DictionaryDrawerSettings(KeyLabel = "Color Type", ValueLabel = "Color Value")]
    private Dictionary<ColorType, Color> colors = new Dictionary<ColorType, Color>()
    {
        { ColorType.Color_1, new Color(0.9f, 0.2f, 0.2f) },   // Red-ish
        { ColorType.Color_2, new Color(0.2f, 0.5f, 0.9f) },   // Blue-ish
        { ColorType.Color_3, new Color(0.2f, 0.8f, 0.4f) },   // Green-ish
        { ColorType.Color_4, new Color(0.95f, 0.85f, 0.2f) }, // Yellow-ish
        { ColorType.Color_5, new Color(0.6f, 0.3f, 0.8f) },   // Purple-ish
        { ColorType.Color_6, new Color(1.0f, 0.55f, 0.1f) }   // Orange-ish
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this);
    }

    // Get color by enum
    public Color GetColor(ColorType type)
    {
        return colors[type];
    }

    // Set color by enum
    public void SetColor(ColorType type, Color newColor)
    {
        colors[type] = newColor;
    }

    // Get all colors (useful for randomization)
    public IEnumerable<Color> GetAllColors()
    {
        return colors.Values;
    }
}