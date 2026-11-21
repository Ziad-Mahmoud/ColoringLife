using UnityEngine;

// Plays the main background music loop for the whole app, surviving scene loads.
// Created automatically on startup; no scene setup needed.
public class MusicPlayer : MonoBehaviour
{
    private const string ClipPath = "Audio/BackgroundMusic";
    private const float Volume = 0.5f;

    public static MusicPlayer Instance { get; private set; }
    public AudioSource Source { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var clip = Resources.Load<AudioClip>(ClipPath);
        if (clip == null)
        {
            Debug.LogWarning($"MusicPlayer: no clip at Resources/{ClipPath}");
            return;
        }

        var go = new GameObject(nameof(MusicPlayer));
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<MusicPlayer>();
        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.loop = true;
        src.volume = Volume;
        src.playOnAwake = false;
        src.Play();
        Instance.Source = src;
    }
}
