using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SoundType
{
    DeathSound,
    AddToPlatform,
    CrateClicked,
    GridClickValid,
    GridClickInvalid
}

[System.Serializable]
public class Sound
{
    public SoundType soundType;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
    public bool loop;

    [Header("Advanced")]
    public bool allowMultiple = false;     
    public float pitchVariance = 0.05f;    // +/- random variance per play
    public float pitchStackStep = 0.02f;   // step increase when stacking multiple

    [HideInInspector] public AudioSource source;
    [HideInInspector] public int playCount = 0; // active overlapping plays
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Settings")]
    [SerializeField] private List<Sound> sounds = new List<Sound>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (var s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
        }
    }

    public void Play(SoundType type)
    {
        var s = sounds.Find(sound => sound.soundType == type);
        if (s == null)
        {
            Debug.LogWarning($"⚠️ Sound '{type}' not found!");
            return;
        }

        if (!s.allowMultiple)
        {
            // normal single AudioSource playback
            s.source.volume = s.volume;
            s.source.pitch = s.pitch + Random.Range(-s.pitchVariance, s.pitchVariance);
            s.source.loop = s.loop;
            s.source.Play();
        }
        else
        {
            // multiple overlapping instances → spawn a temp AudioSource
            var tempGO = new GameObject($"TempAudio_{type}");
            var tempSource = tempGO.AddComponent<AudioSource>();
            tempSource.clip = s.clip;
            tempSource.volume = s.volume;

            // base pitch + variance + stack step
            float pitchOffset = Random.Range(-s.pitchVariance, s.pitchVariance);
            float stackedPitch = s.pitch + pitchOffset + (s.playCount * s.pitchStackStep);
            tempSource.pitch = stackedPitch;

            tempSource.loop = false;
            tempSource.Play();

            s.playCount++;
            Destroy(tempGO, s.clip.length / Mathf.Abs(tempSource.pitch));

            // reset counter a little after playback so pitch doesn’t keep increasing forever
            StartCoroutine(ResetCountAfterDelay(s, 0.2f));
        }
    }

    public void Stop(SoundType type)
    {
        var s = sounds.Find(sound => sound.soundType == type);
        if (s != null && s.source.isPlaying)
        {
            s.source.Stop();
            s.playCount = 0;
        }
    }

    public void PlayOneShot(SoundType type, float? volume = null, float? pitch = null)
    {
        var s = sounds.Find(sound => sound.soundType == type);
        if (s == null || s.clip == null)
        {
            Debug.LogWarning($"⚠️ Sound '{type}' not found or missing clip!");
            return;
        }

        float finalVolume = volume ?? s.volume;

        // If allowMultiple is false → behave as normal one-shot
        if (!s.allowMultiple)
        {
            float finalPitch = pitch ?? s.pitch;
            var tempGO = new GameObject($"TempOneShot_{type}");
            var tempSource = tempGO.AddComponent<AudioSource>();
            tempSource.clip = s.clip;
            tempSource.volume = finalVolume;
            tempSource.pitch = finalPitch;
            tempSource.Play();
            Destroy(tempGO, s.clip.length / Mathf.Abs(finalPitch));
        }
        else
        {
            // allowMultiple logic → add variance + stack step
            float basePitch = pitch ?? s.pitch;
            float pitchOffset = Random.Range(-s.pitchVariance, s.pitchVariance);
            float stackedPitch = basePitch + pitchOffset + (s.playCount * s.pitchStackStep);

            var tempGO = new GameObject($"TempOneShot_{type}");
            var tempSource = tempGO.AddComponent<AudioSource>();
            tempSource.clip = s.clip;
            tempSource.volume = finalVolume;
            tempSource.pitch = stackedPitch;
            tempSource.loop = false;
            tempSource.Play();

            s.playCount++;
            Destroy(tempGO, s.clip.length / Mathf.Abs(stackedPitch));
            StartCoroutine(ResetCountAfterDelay(s, 0.2f));
        }
    }


    private IEnumerator ResetCountAfterDelay(Sound s, float delay)
    {
        yield return new WaitForSeconds(delay);
        s.playCount = Mathf.Max(0, s.playCount - 1);
    }
}
