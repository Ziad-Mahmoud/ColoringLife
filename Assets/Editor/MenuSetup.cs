using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Creates the Panel Settings asset and a "Menu UI" UIDocument wired to MenuController in the open scene.
/// Runs once automatically when the open scene has no MenuController; also available from Tools menu.
/// </summary>
public static class MenuSetup
{
    private const string PanelPath = "Assets/UI/MenuPanelSettings.asset";
    private const string ThemePath = "Assets/UI/DefaultRuntimeTheme.tss";
    private const string UxmlPath = "Assets/UI/MenuUI.uxml";

    [InitializeOnLoadMethod]
    private static void AutoRun()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (Object.FindFirstObjectByType<PictureHandler>() == null) return; // not the game scene
            if (Object.FindFirstObjectByType<MenuController>() != null) return; // already set up
            Setup();
        };
    }

    [MenuItem("Tools/Coloring Life/Setup Demo Menu")]
    public static void Setup()
    {
        var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
        var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
        if (uxml == null || theme == null)
        {
            Debug.LogError($"MenuSetup: missing {UxmlPath} or {ThemePath}");
            return;
        }

        var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
        if (panel == null)
        {
            panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.themeStyleSheet = theme;
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1080, 1920);
            panel.match = 0.5f;
            AssetDatabase.CreateAsset(panel, PanelPath);
            AssetDatabase.SaveAssets();
        }

        var controller = Object.FindFirstObjectByType<MenuController>();
        var go = controller ? controller.gameObject : new GameObject("Menu UI");
        if (!go.TryGetComponent(out UIDocument doc)) doc = go.AddComponent<UIDocument>();
        doc.panelSettings = panel;
        doc.visualTreeAsset = uxml;
        if (!controller) controller = go.AddComponent<MenuController>();

        var so = new SerializedObject(controller);
        so.FindProperty("pictureHandler").objectReferenceValue = Object.FindFirstObjectByType<PictureHandler>();
        so.FindProperty("coloringHandler").objectReferenceValue = Object.FindFirstObjectByType<ColoringHandler>();
        so.FindProperty("zoomHandler").objectReferenceValue = Object.FindFirstObjectByType<ZoomHandler>();
        so.ApplyModifiedPropertiesWithoutUndo();

        var scene = go.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = go;
        Debug.Log("MenuSetup: demo menu added and scene saved.", go);
    }
}
