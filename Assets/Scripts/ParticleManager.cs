using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : MonoBehaviour
{
    public static ParticleManager Instance;

    [System.Serializable]
    public class ParticleEntry
    {
        public string key;
        public ParticleSystem prefab;
    }

    [Header("Registered Particles")]
    public List<ParticleEntry> particlePrefabs = new List<ParticleEntry>();

    private Dictionary<string, ParticleSystem> m_ParticleDict;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        m_ParticleDict = new Dictionary<string, ParticleSystem>();
        foreach (var entry in particlePrefabs)
        {
            if (entry != null && entry.prefab != null && !m_ParticleDict.ContainsKey(entry.key))
            {
                m_ParticleDict.Add(entry.key, entry.prefab);
            }
        }
    }

    // -------------------- Basic Play --------------------

    public void Play(string key, Vector3 position)
    {
        if (!m_ParticleDict.TryGetValue(key, out var prefab))
        {
            Debug.LogWarning($"Particle with key '{key}' not found!");
            return;
        }

        var go = Instantiate(prefab, position, Quaternion.identity).gameObject;
        var rootPs = go.GetComponent<ParticleSystem>();
        if (rootPs != null) rootPs.Play();
        Destroy(go, GetLongestLifetime(go));
    }

    public void Play(string key, Vector3 position, Quaternion rotation)
    {
        if (!m_ParticleDict.TryGetValue(key, out var prefab))
        {
            Debug.LogWarning($"Particle with key '{key}' not found!");
            return;
        }

        var go = Instantiate(prefab, position, rotation).gameObject;
        var rootPs = go.GetComponent<ParticleSystem>();
        if (rootPs != null) rootPs.Play();
        Destroy(go, GetLongestLifetime(go));
    }

    // -------------------- Color-Tinted Play --------------------

    /// <summary>
    /// Plays a particle tinted to 'color'. If preserveAlpha is true, keeps the prefab's original alpha.
    /// </summary>
    public void Play(string key, Vector3 position, Color color, bool preserveAlpha = true)
    {
        if (!m_ParticleDict.TryGetValue(key, out var prefab))
        {
            Debug.LogWarning($"Particle with key '{key}' not found!");
            return;
        }

        var go = Instantiate(prefab, position, Quaternion.identity).gameObject;
        ApplyStartColorToHierarchy(go, color, preserveAlpha);

        var rootPs = go.GetComponent<ParticleSystem>();
        if (rootPs != null) rootPs.Play();
        Destroy(go, GetLongestLifetime(go));
    }

    /// <summary>
    /// Plays a particle tinted to 'color' with rotation. If preserveAlpha is true, keeps the prefab's original alpha.
    /// </summary>
    public void Play(string key, Vector3 position, Quaternion rotation, Color color, bool preserveAlpha = true)
    {
        if (!m_ParticleDict.TryGetValue(key, out var prefab))
        {
            Debug.LogWarning($"Particle with key '{key}' not found!");
            return;
        }

        var go = Instantiate(prefab, position, rotation).gameObject;
        ApplyStartColorToHierarchy(go, color, preserveAlpha);

        var rootPs = go.GetComponent<ParticleSystem>();
        if (rootPs != null) rootPs.Play();
        Destroy(go, GetLongestLifetime(go));
    }

    // -------------------- Helpers --------------------

    private void ApplyStartColorToHierarchy(GameObject root, Color tint, bool preserveAlpha)
    {
        foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;

            float alpha = preserveAlpha ? ExtractAlpha(main.startColor) : tint.a;
            var tinted = new Color(tint.r, tint.g, tint.b, alpha);

            // Force a solid color (overrides gradients/random-between)
            main.startColor = new ParticleSystem.MinMaxGradient(tinted);
        }

        // If your shader ignores vertex colors and expects a material _Color/_TintColor,
        // you can optionally also set a MaterialPropertyBlock here.
        // Keep it off by default to avoid double-tinting.
    }

    private float ExtractAlpha(ParticleSystem.MinMaxGradient g)
    {
        switch (g.mode)
        {
            case ParticleSystemGradientMode.Color:
                return g.color.a;
            case ParticleSystemGradientMode.TwoColors:
                return Mathf.Max(g.colorMin.a, g.colorMax.a);
            case ParticleSystemGradientMode.Gradient:
                return g.gradient.Evaluate(1f).a;
            case ParticleSystemGradientMode.TwoGradients:
                return Mathf.Max(g.gradientMin.Evaluate(1f).a, g.gradientMax.Evaluate(1f).a);
            default:
                return 1f;
        }
    }

    private float GetLongestLifetime(GameObject root)
    {
        float max = 0f;
        foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            // Add a small buffer for sub-emitters
            float t = main.duration + main.startLifetime.constantMax + 0.1f;
            if (t > max) max = t;
        }
        return Mathf.Max(0.1f, max);
    }
}
