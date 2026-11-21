using UnityEngine;

/// <summary>
/// Loops the background track (Resources/Music/base of music) with smooth volume and pitch changes.
/// Quieter in menus/shop, ducks briefly under big sound effects, and speeds up slightly near the end of a wave.
/// </summary>
public class MusicPlayer : MonoBehaviour
{
    public static MusicPlayer Instance { get; private set; }

    private const string ClipPath = "Music/base of music";

    [SerializeField] private float gameplayVolume = 0.35f;
    [SerializeField] private float quietVolume = 0.15f;
    [SerializeField] private float fadeSpeed = 1.5f;      // volume units per second
    [SerializeField] private float pitchSpeed = 0.5f;     // pitch units per second

    private AudioSource source;
    private float targetVolume;
    private float targetPitch = 1f;
    private float duckAmount = 0f;   // 0 = no duck, 1 = silent
    private float duckTimer = 0f;

    public static MusicPlayer Ensure()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("MusicPlayer");
            Instance = go.AddComponent<MusicPlayer>();
        }
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        source = gameObject.AddComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0f;
        source.clip = Resources.Load<AudioClip>(ClipPath);
        targetVolume = quietVolume;

        if (source.clip != null)
        {
            source.Play();
        }
        else
        {
            Debug.LogWarning($"[Music] Could not load Resources/{ClipPath}");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Full volume during waves.</summary>
    public void SetGameplay() { targetVolume = gameplayVolume; targetPitch = 1f; }

    /// <summary>Lower volume for menus, shop, pause and results.</summary>
    public void SetQuiet() { targetVolume = quietVolume; targetPitch = 1f; }

    /// <summary>Slightly faster music for the final seconds of a wave.</summary>
    public void SetUrgent(bool urgent) { targetPitch = urgent ? 1.06f : 1f; }

    /// <summary>Briefly lower the music so an important sound effect cuts through.</summary>
    public void Duck(float amount = 0.6f, float seconds = 0.35f)
    {
        duckAmount = Mathf.Max(duckAmount, Mathf.Clamp01(amount));
        duckTimer = Mathf.Max(duckTimer, seconds);
    }

    private void Update()
    {
        if (source == null) return;
        float dt = Time.unscaledDeltaTime;

        if (duckTimer > 0f)
        {
            duckTimer -= dt;
        }
        else
        {
            duckAmount = Mathf.MoveTowards(duckAmount, 0f, dt * 3f);
        }

        float wanted = targetVolume * (1f - duckAmount);
        float speed = duckTimer > 0f ? fadeSpeed * 4f : fadeSpeed;
        source.volume = Mathf.MoveTowards(source.volume, wanted, dt * speed);
        source.pitch = Mathf.MoveTowards(source.pitch, targetPitch, dt * pitchSpeed);
    }
}
