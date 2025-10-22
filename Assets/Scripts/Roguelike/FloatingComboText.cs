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
            
            Debug.Log($"[FloatingText] Parented to {sourceRect.name} at offset: ({randomX}, {randomY})");
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
                
                Debug.Log($"[FloatingText] Spawned at screen: {screenPos}, canvas: {canvasPos}, final: {rectTransform.anchoredPosition}");
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
                
                Debug.Log($"[FloatingText] Spawned (camera mode) at: {rectTransform.anchoredPosition}");
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
    /// Animate scale and fade
    /// </summary>
    private IEnumerator AnimateText()
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / animationDuration;
            float curveValue = scaleCurve.Evaluate(t);
            
            // Scale up
            if (rectTransform != null)
            {
                float scale = Mathf.Lerp(startScale, endScale, curveValue);
                rectTransform.localScale = Vector3.one * scale;
            }
            
            // Fade in then out
            if (canvasGroup != null)
            {
                if (t < 0.3f)
                {
                    // Fade in quickly
                    canvasGroup.alpha = Mathf.Lerp(0, 1, t / 0.3f);
                }
                else
                {
                    // Fade out
                    canvasGroup.alpha = Mathf.Lerp(1, 0, (t - 0.3f) / 0.7f);
                }
            }
            
            yield return null;
        }
        
        // Destroy after animation
        Destroy(gameObject);
    }
}

