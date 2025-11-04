using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the shopkeeper dialogue system
/// </summary>
public class ShopKeeper : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button shopkeeperPanelButton;
    [SerializeField] private TextMeshProUGUI shopkeeperText;
    [SerializeField] private Image shopkeeperTextImage;  // The background image for the text
    
    [Header("Dialogue")]
    [SerializeField] [TextArea(2, 4)] private string[] dialogueLines = new string[]
    {
        "Welcome to my shop!",
        "Find what you need?",
        "Come back anytime!",
        "Good luck out there!"
    };
    
    [Header("Settings")]
    [SerializeField] private bool cycleInOrder = true;  // If true, cycles in order. If false, picks random.
    [SerializeField] private bool showOnShopOpen = true;  // Show first dialogue when shop opens
    
    private int currentDialogueIndex = 0;
    
    private void Start()
    {
        // Setup button listener
        if (shopkeeperPanelButton != null)
        {
            shopkeeperPanelButton.onClick.AddListener(OnShopkeeperPanelClicked);
        }
        else
        {
            Debug.LogWarning("[ShopKeeper] ShopkeeperPanelButton is not assigned!");
        }
    }
    
    private void OnEnable()
    {
        // Show first dialogue when shop opens
        if (showOnShopOpen)
        {
            currentDialogueIndex = 0;
            DisplayCurrentDialogue();
        }
    }
    
    /// <summary>
    /// Called when the shopkeeper panel button is clicked
    /// </summary>
    private void OnShopkeeperPanelClicked()
    {
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            Debug.LogWarning("[ShopKeeper] No dialogue lines assigned!");
            return;
        }
        
        // Advance to next dialogue
        if (cycleInOrder)
        {
            currentDialogueIndex = (currentDialogueIndex + 1) % dialogueLines.Length;
        }
        else
        {
            // Pick a random dialogue (but not the same as current)
            int newIndex = currentDialogueIndex;
            if (dialogueLines.Length > 1)
            {
                while (newIndex == currentDialogueIndex)
                {
                    newIndex = Random.Range(0, dialogueLines.Length);
                }
            }
            currentDialogueIndex = newIndex;
        }
        
        DisplayCurrentDialogue();
    }
    
    /// <summary>
    /// Display the current dialogue line
    /// </summary>
    private void DisplayCurrentDialogue()
    {
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            return;
        }
        
        string dialogue = dialogueLines[currentDialogueIndex];
        
        if (shopkeeperText != null)
        {
            shopkeeperText.text = dialogue;
            Debug.Log($"[ShopKeeper] Displaying dialogue {currentDialogueIndex + 1}/{dialogueLines.Length}: \"{dialogue}\"");
        }
        else
        {
            Debug.LogWarning("[ShopKeeper] ShopkeeperText is not assigned!");
        }
    }
    
    /// <summary>
    /// Set a specific dialogue by index
    /// </summary>
    public void SetDialogue(int index)
    {
        if (index >= 0 && index < dialogueLines.Length)
        {
            currentDialogueIndex = index;
            DisplayCurrentDialogue();
        }
        else
        {
            Debug.LogWarning($"[ShopKeeper] Invalid dialogue index: {index}. Valid range: 0-{dialogueLines.Length - 1}");
        }
    }
    
    /// <summary>
    /// Get the current dialogue index
    /// </summary>
    public int GetCurrentDialogueIndex()
    {
        return currentDialogueIndex;
    }
    
    /// <summary>
    /// Get total number of dialogue lines
    /// </summary>
    public int GetDialogueCount()
    {
        return dialogueLines != null ? dialogueLines.Length : 0;
    }
}

