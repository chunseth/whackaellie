using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Animates floating combo text above hit animals
/// </summary>
public class FloatingComboText : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.4f;
    [SerializeField] private float startScale = 0.5f;
    [SerializeField] private float endScale = 1.5f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Tooltip("How far the text drifts upward over its lifetime")]
    [SerializeField] private float riseDistance = 40f;
    [Tooltip("Pop in with a springy overshoot instead of a plain scale")]
    [SerializeField] private bool punch = true;
    
    private TextMeshProUGUI textComponent;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Color originalColor;
    
    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();
        
        // Add CanvasGroup for fade
        canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        if (textComponent != null)
        {
            originalColor = textComponent.color;
        }
    }
    
    /// <summary>
    /// Customize the look before calling InitializeFromUI/Initialize.
    /// sizeMultiplier scales the whole animation (start and end scale).
    /// </summary>
    public void SetStyle(Color color, float sizeMultiplier, float duration, float rise)
    {
        if (textComponent != null)
        {
            textComponent.color = color;
            textComponent.outlineWidth = 0.22f;
            textComponent.outlineColor = new Color32(0, 0, 0, 220);
        }
        startScale *= sizeMultiplier;
        endScale *= sizeMultiplier;
        animationDuration = duration;
        riseDistance = rise;
    }
    
    /// <summary>
    /// Initialize from UI element position (for RectTransforms)
    /// </summary>
    public void InitializeFromUI(string text, RectTransform sourceRect, Transform parentCanvas)
    {
        if (textComponent != null)
        {
            textComponent.text = text;
        }
        
        // Parent directly to the mole image
        if (sourceRect != null)
        {
            transform.SetParent(sourceRect, false);
        }
        
        if (rectTransform != null)
        {
            // Random offset: -80 to 80 on X, 80-100 on Y
            float randomX = Random.Range(-40f, 40f);
            float randomY = Random.Range(80f, 100f);
            
            // Random rotation: -15 to 15 degrees
            float randomRotation = Random.Range(-15f, 15f);
            
            // Position relative to parent (mole image)
            rectTransform.anchoredPosition = new Vector2(randomX, randomY);
            rectTransform.rotation = Quaternion.Euler(0, 0, randomRotation);
            rectTransform.localScale = Vector3.one * startScale;

        }
        
        // Start animation
        StartCoroutine(AnimateText());
    }
    
    /// <summary>
    /// Initialize and start animation (legacy world position method)
    /// </summary>
    public void Initialize(string text, Vector3 worldPosition, Transform parentCanvas)
    {
        if (textComponent != null)
        {
            textComponent.text = text;
        }
        
        // Set as child of canvas
        if (parentCanvas != null)
        {
            transform.SetParent(parentCanvas, false);
        }
        
        // Random offset: -80 to 80 on X, 80-100 on Y
        float randomX = Random.Range(-40f, 40f);
        float randomY = Random.Range(80f, 100f);
        
        // Random rotation: -15 to 15 degrees
        float randomRotation = Random.Range(-15f, 15f);
        
        // Position in screen space
        if (rectTransform != null)
        {
            Canvas canvas = parentCanvas.GetComponentInParent<Canvas>();
            
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // For Screen Space - Overlay canvas
                Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
                
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvas as RectTransform,
                    screenPos,
                    null,
                    out Vector2 canvasPos
                );
                
                rectTransform.anchoredPosition = canvasPos + new Vector2(randomX, randomY);

            }
            else if (canvas != null)
            {
                // For Screen Space - Camera or World Space canvas
                Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera ?? Camera.main, worldPosition);
                
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvas as RectTransform,
                    screenPos,
                    canvas.worldCamera,
                    out Vector2 canvasPos
                );
                
                rectTransform.anchoredPosition = canvasPos + new Vector2(randomX, randomY);

            }
            
            rectTransform.rotation = Quaternion.Euler(0, 0, randomRotation);
            rectTransform.localScale = Vector3.one * startScale;
            
            // Ensure it's in front
            rectTransform.SetAsLastSibling();
        }
        
        // Start animation
        StartCoroutine(AnimateText());
    }
    
    /// <summary>
    /// Animate: pop in (with overshoot), drift upward, then fade out
    /// </summary>
    private IEnumerator AnimateText()
    {
        float elapsedTime = 0f;
        Vector2 startPosition = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
        
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / animationDuration);
            
            if (rectTransform != null)
            {
                float scale;
                if (punch)
                {
                    // Springy pop over the first 35% of the animation, then hold
                    float popT = Mathf.Clamp01(t / 0.35f);
                    scale = Mathf.LerpUnclamped(startScale, endScale, GameFeel.EaseOutBack(popT));
                }
                else
                {
                    scale = Mathf.Lerp(startScale, endScale, scaleCurve.Evaluate(t));
                }
                rectTransform.localScale = Vector3.one * scale;
                rectTransform.anchoredPosition = startPosition + Vector2.up * (riseDistance * GameFeel.EaseOutQuad(t));
            }
            
            // Fade in quickly, hold, then fade out over the last 40%
            if (canvasGroup != null)
            {
                if (t < 0.15f)
                {
                    canvasGroup.alpha = t / 0.15f;
                }
                else if (t < 0.6f)
                {
                    canvasGroup.alpha = 1f;
                }
                else
                {
                    canvasGroup.alpha = 1f - (t - 0.6f) / 0.4f;
                }
            }
            
            yield return null;
        }
        
        // Destroy after animation
        Destroy(gameObject);
    }
}
