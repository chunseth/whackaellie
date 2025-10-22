using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Animates animals popping up in the background of the main menu
/// Visual only - no interaction
/// </summary>
public class MenuBackgroundAnimator : MonoBehaviour
{
    [Header("Animal References")]
    [SerializeField] private AnimalDatabase animalDatabase;
    
    [Header("Mole Images (25 total)")]
    [SerializeField] private Image[] moleImages;
    
    [Header("Animation Settings")]
    [SerializeField] private float popUpDistance = 100f;
    [SerializeField] private float animationDuration = 0.4f;
    [SerializeField] private float minVisibleDuration = 1f;
    [SerializeField] private float maxVisibleDuration = 3f;
    [SerializeField] private float minSpawnInterval = 0.5f;
    [SerializeField] private float maxSpawnInterval = 2f;
    [SerializeField] private int maxSimultaneousAnimals = 5;
    
    private List<int> activeAnimals = new List<int>();
    private Dictionary<int, Vector2> originalPositions = new Dictionary<int, Vector2>();
    private bool isAnimating = false;

    private void Start()
    {
        // Store original positions of all mole images
        for (int i = 0; i < moleImages.Length; i++)
        {
            if (moleImages[i] != null)
            {
                RectTransform rt = moleImages[i].GetComponent<RectTransform>();
                if (rt != null)
                {
                    originalPositions[i] = rt.anchoredPosition;
                }
            }
        }
        
        // Start the background animation
        StartBackgroundAnimation();
    }

    /// <summary>
    /// Starts the continuous background animation
    /// </summary>
    public void StartBackgroundAnimation()
    {
        if (!isAnimating)
        {
            isAnimating = true;
            StartCoroutine(AnimationLoop());
        }
    }

    /// <summary>
    /// Stops the background animation
    /// </summary>
    public void StopBackgroundAnimation()
    {
        isAnimating = false;
        StopAllCoroutines();
        
        // Reset all animals to hidden position
        foreach (var kvp in originalPositions)
        {
            if (moleImages[kvp.Key] != null)
            {
                RectTransform rt = moleImages[kvp.Key].GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = kvp.Value;
                }
            }
        }
        
        activeAnimals.Clear();
    }

    /// <summary>
    /// Main animation loop - randomly pops animals up and down
    /// </summary>
    private IEnumerator AnimationLoop()
    {
        while (isAnimating)
        {
            // Wait random interval before next spawn
            float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);

            // Only spawn if we haven't reached max simultaneous animals
            if (activeAnimals.Count < maxSimultaneousAnimals)
            {
                // Find available holes (not currently animating)
                List<int> availableIndices = new List<int>();
                for (int i = 0; i < moleImages.Length; i++)
                {
                    if (moleImages[i] != null && !activeAnimals.Contains(i))
                    {
                        availableIndices.Add(i);
                    }
                }

                // Pick a random available hole
                if (availableIndices.Count > 0)
                {
                    int randomIndex = availableIndices[Random.Range(0, availableIndices.Count)];
                    StartCoroutine(AnimateMole(randomIndex));
                }
            }
        }
    }

    /// <summary>
    /// Animates a single mole: pop up, wait, pop down
    /// </summary>
    private IEnumerator AnimateMole(int index)
    {
        if (index < 0 || index >= moleImages.Length || moleImages[index] == null)
        {
            yield break;
        }

        // Mark as active
        activeAnimals.Add(index);

        Image moleImage = moleImages[index];
        RectTransform rt = moleImage.GetComponent<RectTransform>();
        
        if (rt == null || !originalPositions.ContainsKey(index))
        {
            activeAnimals.Remove(index);
            yield break;
        }

        // Get random animal sprite
        if (animalDatabase != null)
        {
            AnimalData randomAnimal = animalDatabase.GetRandomAnimal();
            if (randomAnimal != null && randomAnimal.normalSprite != null)
            {
                moleImage.sprite = randomAnimal.normalSprite;
            }
        }

        Vector2 hiddenPos = originalPositions[index];
        Vector2 visiblePos = hiddenPos + Vector2.up * popUpDistance;

        // Pop up
        yield return StartCoroutine(SlideMole(rt, hiddenPos, visiblePos));

        // Stay visible for random duration
        float visibleTime = Random.Range(minVisibleDuration, maxVisibleDuration);
        yield return new WaitForSeconds(visibleTime);

        // Pop down
        yield return StartCoroutine(SlideMole(rt, visiblePos, hiddenPos));

        // Mark as inactive
        activeAnimals.Remove(index);
    }

    /// <summary>
    /// Slides a mole image between two positions
    /// </summary>
    private IEnumerator SlideMole(RectTransform rt, Vector2 from, Vector2 to)
    {
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / animationDuration;
            rt.anchoredPosition = Vector2.Lerp(from, to, t);
            yield return null;
        }

        rt.anchoredPosition = to;
    }

    private void OnDestroy()
    {
        StopBackgroundAnimation();
    }
}

