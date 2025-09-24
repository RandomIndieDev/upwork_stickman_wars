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

    NONE = 999
}

[ExecuteAlways]
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

    [Title("Stickman Materials")]
    [SerializeField, DictionaryDrawerSettings(KeyLabel = "Color Type", ValueLabel = "Material")]
    private Dictionary<ColorType, Material> materials = new Dictionary<ColorType, Material>();

    private static readonly int ColorProp = Shader.PropertyToID("_Color");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        if (Application.isPlaying)
            DontDestroyOnLoad(this);
    }

    public ColorType GetRandomColor()
    {
        if (colors.Count == 0)
            return ColorType.NONE;

        var keys = new List<ColorType>(colors.Keys);
        int index = Random.Range(0, keys.Count);
        return keys[index];
    }

    public Color GetColor(ColorType type) => colors[type];

    public void SetColor(ColorType type, Color newColor) => colors[type] = newColor;

    public IEnumerable<Color> GetAllColors() => colors.Values;

    public Material GetMaterial(ColorType type)
    {
        return materials.ContainsKey(type) ? materials[type] : null;
    }

    [Button("Update All Materials From Colors")]
    private void UpdateAllMaterials()
    {
        foreach (var kvp in materials)
        {
            ColorType type = kvp.Key;
            Material mat = kvp.Value;

            if (mat != null && colors.ContainsKey(type))
            {
                mat.SetColor(ColorProp, colors[type]);
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(mat);
#endif
                Debug.Log($"Updated {mat.name} with {type} color {colors[type]}");
            }
        }
    }
}
