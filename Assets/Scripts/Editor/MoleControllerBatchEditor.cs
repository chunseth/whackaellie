using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// Editor tool to batch-update all MoleController components in the scene
/// Access via: Tools > WhackAEllie > Configure All Mole Controllers
/// </summary>
public class MoleControllerBatchEditor : EditorWindow
{
    [Header("References to Assign")]
    private AnimalDatabase animalDatabase;
    private bool autoAssignMoleImages = true;
    
    [Header("Settings")]
    private float popUpDistance = 100f;
    private float animationDuration = 0.4f;
    private float visibleDuration = 2f;
    private float hitSpriteDuration = 0.2f;
    private float soundVolume = 0.5f;
    
    private Vector2 scrollPosition;

    [MenuItem("Tools/WhackAEllie/Configure All Mole Controllers")]
    public static void ShowWindow()
    {
        MoleControllerBatchEditor window = GetWindow<MoleControllerBatchEditor>("Mole Controller Setup");
        window.minSize = new Vector2(400, 500);
        window.Show();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        GUILayout.Label("Batch Configure Mole Controllers", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox(
            "This tool will update ALL MoleController components in the current scene with the settings below.",
            MessageType.Info
        );
        
        EditorGUILayout.Space();
        
        // References Section
        GUILayout.Label("References", EditorStyles.boldLabel);
        animalDatabase = (AnimalDatabase)EditorGUILayout.ObjectField(
            "Animal Database",
            animalDatabase,
            typeof(AnimalDatabase),
            false
        );
        
        autoAssignMoleImages = EditorGUILayout.Toggle(
            new GUIContent("Auto-Assign Mole Images", "Automatically find and assign MoleImage child objects"),
            autoAssignMoleImages
        );
        
        EditorGUILayout.Space();
        
        // Settings Section
        GUILayout.Label("Animation Settings", EditorStyles.boldLabel);
        popUpDistance = EditorGUILayout.FloatField("Pop Up Distance", popUpDistance);
        animationDuration = EditorGUILayout.FloatField("Animation Duration", animationDuration);
        visibleDuration = EditorGUILayout.FloatField("Visible Duration", visibleDuration);
        hitSpriteDuration = EditorGUILayout.FloatField("Hit Sprite Duration", hitSpriteDuration);
        soundVolume = EditorGUILayout.Slider("Sound Volume", soundVolume, 0f, 1f);
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        // Find Controllers
        MoleController[] controllers = FindObjectsOfType<MoleController>();
        
        EditorGUILayout.HelpBox(
            $"Found {controllers.Length} MoleController(s) in the scene.",
            controllers.Length > 0 ? MessageType.Info : MessageType.Warning
        );
        
        EditorGUILayout.Space();
        
        // Apply Buttons
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Apply Settings to All Controllers", GUILayout.Height(40)))
        {
            ApplyToAllControllers(controllers);
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Apply Only Animal Database", GUILayout.Height(30)))
        {
            ApplyDatabaseOnly(controllers);
        }
        
        if (GUILayout.Button("Apply Only Animation Settings", GUILayout.Height(30)))
        {
            ApplySettingsOnly(controllers);
        }
        
        if (GUILayout.Button("Auto-Assign Mole Images Only", GUILayout.Height(30)))
        {
            AssignMoleImagesOnly(controllers);
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        // List of found controllers
        if (controllers.Length > 0)
        {
            GUILayout.Label("Found Controllers:", EditorStyles.boldLabel);
            foreach (MoleController controller in controllers)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(controller, typeof(MoleController), true);
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeGameObject = controller.gameObject;
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        
        EditorGUILayout.EndScrollView();
    }

    private void ApplyToAllControllers(MoleController[] controllers)
    {
        if (controllers.Length == 0)
        {
            EditorUtility.DisplayDialog("No Controllers Found", "No MoleController components found in the scene.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
            "Confirm Batch Update",
            $"This will update {controllers.Length} MoleController component(s). Continue?",
            "Yes",
            "Cancel"))
        {
            return;
        }

        int updatedCount = 0;
        
        foreach (MoleController controller in controllers)
        {
            Undo.RecordObject(controller, "Batch Update MoleController");
            
            // Get the serialized object to access private fields
            SerializedObject so = new SerializedObject(controller);
            
            // Assign references
            if (animalDatabase != null)
            {
                SerializedProperty dbProp = so.FindProperty("animalDatabase");
                dbProp.objectReferenceValue = animalDatabase;
            }
            
            // Auto-assign mole images
            if (autoAssignMoleImages)
            {
                AssignMoleImage(controller, so);
            }
            
            // Apply settings
            so.FindProperty("popUpDistance").floatValue = popUpDistance;
            so.FindProperty("animationDuration").floatValue = animationDuration;
            so.FindProperty("visibleDuration").floatValue = visibleDuration;
            so.FindProperty("hitSpriteDuration").floatValue = hitSpriteDuration;
            so.FindProperty("soundVolume").floatValue = soundVolume;
            
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);
            updatedCount++;
        }

        Debug.Log($"Successfully updated {updatedCount} MoleController(s)!");
        EditorUtility.DisplayDialog("Success", $"Updated {updatedCount} MoleController(s)!", "OK");
    }

    private void ApplyDatabaseOnly(MoleController[] controllers)
    {
        if (animalDatabase == null)
        {
            EditorUtility.DisplayDialog("No Database", "Please assign an Animal Database first.", "OK");
            return;
        }

        int updatedCount = 0;
        foreach (MoleController controller in controllers)
        {
            Undo.RecordObject(controller, "Assign Animal Database");
            SerializedObject so = new SerializedObject(controller);
            SerializedProperty dbProp = so.FindProperty("animalDatabase");
            dbProp.objectReferenceValue = animalDatabase;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);
            updatedCount++;
        }

        Debug.Log($"Assigned Animal Database to {updatedCount} MoleController(s)!");
        EditorUtility.DisplayDialog("Success", $"Assigned database to {updatedCount} controller(s)!", "OK");
    }

    private void ApplySettingsOnly(MoleController[] controllers)
    {
        int updatedCount = 0;
        foreach (MoleController controller in controllers)
        {
            Undo.RecordObject(controller, "Update Animation Settings");
            SerializedObject so = new SerializedObject(controller);
            
            so.FindProperty("popUpDistance").floatValue = popUpDistance;
            so.FindProperty("animationDuration").floatValue = animationDuration;
            so.FindProperty("visibleDuration").floatValue = visibleDuration;
            so.FindProperty("hitSpriteDuration").floatValue = hitSpriteDuration;
            so.FindProperty("soundVolume").floatValue = soundVolume;
            
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);
            updatedCount++;
        }

        Debug.Log($"Updated settings on {updatedCount} MoleController(s)!");
        EditorUtility.DisplayDialog("Success", $"Updated settings on {updatedCount} controller(s)!", "OK");
    }

    private void AssignMoleImagesOnly(MoleController[] controllers)
    {
        int updatedCount = 0;
        foreach (MoleController controller in controllers)
        {
            Undo.RecordObject(controller, "Assign Mole Images");
            SerializedObject so = new SerializedObject(controller);
            
            if (AssignMoleImage(controller, so))
            {
                updatedCount++;
            }
            
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);
        }

        Debug.Log($"Auto-assigned mole images to {updatedCount} MoleController(s)!");
        EditorUtility.DisplayDialog("Success", $"Auto-assigned images to {updatedCount} controller(s)!", "OK");
    }

    private bool AssignMoleImage(MoleController controller, SerializedObject so)
    {
        // Try to find a child named "MoleImage"
        Transform moleImageTransform = controller.transform.Find("MoleImage");
        
        if (moleImageTransform != null)
        {
            RectTransform moleImageRect = moleImageTransform.GetComponent<RectTransform>();
            Image moleImageComponent = moleImageTransform.GetComponent<Image>();
            
            if (moleImageRect != null)
            {
                SerializedProperty imageProp = so.FindProperty("moleImage");
                imageProp.objectReferenceValue = moleImageRect;
            }
            
            if (moleImageComponent != null)
            {
                SerializedProperty imageCompProp = so.FindProperty("moleImageComponent");
                imageCompProp.objectReferenceValue = moleImageComponent;
            }
            
            return true;
        }
        else
        {
            Debug.LogWarning($"Could not find 'MoleImage' child on {controller.gameObject.name}", controller);
            return false;
        }
    }
}

