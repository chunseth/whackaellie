using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shared "game feel" helpers: easing, particle bursts, screen shake, hit-stop and banners.
/// Everything is built at runtime from code so no scene or prefab changes are needed.
/// </summary>
public class GameFeel : MonoBehaviour
{
    private static GameFeel instance;
    private static Sprite circleSprite;

    // Hit-stop uses this exact time scale so we only restore time if nothing else changed it (e.g. pause)
    private const float HitStopTimeScale = 0.05f;
    private Coroutine hitStopCoroutine;
    private float timeScaleBeforeHitStop = 1f;

    private Coroutine shakeCoroutine;
    private RectTransform shakeTarget;
    private Vector2 shakeBasePosition;

    private static GameFeel Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("GameFeel");
                instance = go.AddComponent<GameFeel>();
            }
            return instance;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            // Make sure we never leave the game slowed down or the grid offset
            if (hitStopCoroutine != null && Mathf.Approximately(Time.timeScale, HitStopTimeScale))
            {
                Time.timeScale = timeScaleBeforeHitStop;
            }
            if (shakeTarget != null)
            {
                shakeTarget.anchoredPosition = shakeBasePosition;
            }
            instance = null;
        }
    }

    #region Easing

    public static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float p = t - 1f;
        return 1f + c3 * p * p * p + c1 * p * p;
    }

    public static float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    public static float EaseInQuad(float t)
    {
        return t * t;
    }

    #endregion

    #region Sprites

    /// <summary>
    /// A soft white circle generated once at runtime, used for particles and the score bar.
    /// </summary>
    public static Sprite CircleSprite
    {
        get
        {
            if (circleSprite == null)
            {
                const int size = 64;
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                float radius = size * 0.5f;
                Color32[] pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x + 0.5f - radius;
                        float dy = y + 0.5f - radius;
                        float d = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                        float a = Mathf.Clamp01((1f - d) * 4f); // soft edge
                        pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                }
                tex.SetPixels32(pixels);
                tex.Apply();
                circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            }
            return circleSprite;
        }
    }

    #endregion

    #region Particles

    /// <summary>
    /// Spawn a burst of simple UI particles at a world position, drawn on top of everything in the canvas.
    /// </summary>
    public static void Burst(Transform canvasRoot, Vector3 worldPosition, Color color, int count, float speed, float size, float lifetime)
    {
        if (canvasRoot == null || count <= 0) return;
        Instance.StartCoroutine(Instance.BurstRoutine(canvasRoot, worldPosition, color, count, speed, size, lifetime));
    }

    private IEnumerator BurstRoutine(Transform canvasRoot, Vector3 worldPosition, Color color, int count, float speed, float size, float lifetime)
    {
        GameObject container = new GameObject("FX_Burst", typeof(RectTransform));
        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.SetParent(canvasRoot, false);
        containerRect.position = worldPosition;
        containerRect.SetAsLastSibling();

        RectTransform[] parts = new RectTransform[count];
        Image[] images = new Image[count];
        Vector2[] velocities = new Vector2[count];
        float[] sizes = new float[count];

        for (int i = 0; i < count; i++)
        {
            GameObject p = new GameObject("p", typeof(RectTransform), typeof(Image));
            RectTransform rt = p.GetComponent<RectTransform>();
            rt.SetParent(containerRect, false);
            rt.anchoredPosition = Vector2.zero;
            Image img = p.GetComponent<Image>();
            img.sprite = CircleSprite;
            img.color = color;
            img.raycastTarget = false;

            float angle = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
            float s = speed * Random.Range(0.6f, 1.2f);
            velocities[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * s;
            sizes[i] = size * Random.Range(0.6f, 1.1f);
            rt.sizeDelta = Vector2.one * sizes[i];
            parts[i] = rt;
            images[i] = img;
        }

        float elapsed = 0f;
        while (elapsed < lifetime)
        {
            float dt = Time.unscaledDeltaTime;
            elapsed += dt;
            float t = Mathf.Clamp01(elapsed / lifetime);
            for (int i = 0; i < count; i++)
            {
                if (parts[i] == null) continue;
                velocities[i] *= Mathf.Pow(0.02f, dt); // drag
                velocities[i] += Vector2.down * 600f * dt; // a little gravity
                parts[i].anchoredPosition += velocities[i] * dt;
                parts[i].sizeDelta = Vector2.one * Mathf.Lerp(sizes[i], sizes[i] * 0.3f, t);
                Color c = color;
                c.a = color.a * (1f - t);
                images[i].color = c;
            }
            yield return null;
        }

        if (container != null)
        {
            Destroy(container);
        }
    }

    #endregion

    #region Screen shake

    /// <summary>
    /// Shake a UI element (e.g. the hole grid) by offsetting its anchored position. Always restores the original spot.
    /// </summary>
    public static void Shake(RectTransform target, float magnitude, float duration)
    {
        if (target == null) return;
        GameFeel feel = Instance;
        if (feel.shakeCoroutine != null)
        {
            feel.StopCoroutine(feel.shakeCoroutine);
            if (feel.shakeTarget != null)
            {
                feel.shakeTarget.anchoredPosition = feel.shakeBasePosition;
            }
        }
        feel.shakeTarget = target;
        feel.shakeBasePosition = target.anchoredPosition;
        feel.shakeCoroutine = feel.StartCoroutine(feel.ShakeRoutine(magnitude, duration));
    }

    private IEnumerator ShakeRoutine(float magnitude, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && shakeTarget != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float falloff = 1f - Mathf.Clamp01(elapsed / duration);
            shakeTarget.anchoredPosition = shakeBasePosition + Random.insideUnitCircle * magnitude * falloff;
            yield return null;
        }
        if (shakeTarget != null)
        {
            shakeTarget.anchoredPosition = shakeBasePosition;
        }
        shakeCoroutine = null;
    }

    #endregion

    #region Hit-stop

    /// <summary>
    /// Freeze the game for a split second so big moments land. Safe with pause: if anything else changes
    /// the time scale during the stop, we leave it alone.
    /// </summary>
    public static void HitStop(float duration)
    {
        if (Time.timeScale <= 0f) return; // paused
        GameFeel feel = Instance;
        if (feel.hitStopCoroutine != null)
        {
            feel.StopCoroutine(feel.hitStopCoroutine);
        }
        else
        {
            feel.timeScaleBeforeHitStop = Time.timeScale;
        }
        feel.hitStopCoroutine = feel.StartCoroutine(feel.HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = HitStopTimeScale;
        yield return new WaitForSecondsRealtime(duration);
        if (Mathf.Approximately(Time.timeScale, HitStopTimeScale))
        {
            Time.timeScale = timeScaleBeforeHitStop;
        }
        hitStopCoroutine = null;
    }

    #endregion

    #region Banners

    /// <summary>
    /// Show a big centered message that pops in, holds, and fades out. Doesn't block taps.
    /// </summary>
    public static void Banner(Transform canvasRoot, TMP_FontAsset font, string text, Color color, float fontSize, float holdSeconds, float yOffset = 0f)
    {
        if (canvasRoot == null) return;
        Instance.StartCoroutine(Instance.BannerRoutine(canvasRoot, font, text, color, fontSize, holdSeconds, yOffset));
    }

    private IEnumerator BannerRoutine(Transform canvasRoot, TMP_FontAsset font, string text, Color color, float fontSize, float holdSeconds, float yOffset)
    {
        GameObject go = new GameObject("FX_Banner", typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(canvasRoot, false);
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(-60f, 400f);
        rt.anchoredPosition = new Vector2(0f, yOffset);
        rt.SetAsLastSibling();

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = new Color32(0, 0, 0, 200);

        CanvasGroup group = go.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        const float popIn = 0.25f;
        const float fadeOut = 0.35f;
        float elapsed = 0f;
        while (elapsed < popIn)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / popIn);
            rt.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, EaseOutBack(t));
            group.alpha = t;
            yield return null;
        }
        rt.localScale = Vector3.one;
        group.alpha = 1f;

        yield return new WaitForSecondsRealtime(holdSeconds);

        elapsed = 0f;
        while (elapsed < fadeOut)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOut);
            group.alpha = 1f - t;
            rt.anchoredPosition = new Vector2(0f, yOffset + 40f * t);
            yield return null;
        }

        Destroy(go);
    }

    #endregion
}
