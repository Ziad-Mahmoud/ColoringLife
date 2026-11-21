using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Drives the UI Toolkit demo menu: main menu -> picture selection -> in-game HUD.
/// Put it on the same GameObject as the UIDocument that uses MenuUI.uxml.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class MenuController : MonoBehaviour
{
    [SerializeField] private PictureHandler pictureHandler;
    [SerializeField] private ColoringHandler coloringHandler;
    [SerializeField] private ZoomHandler zoomHandler;
    [SerializeField] private List<Color> palette = new()
    {
        new Color(0.94f, 0.35f, 0.31f), new Color(1f, 0.6f, 0.2f), new Color(1f, 0.85f, 0.3f),
        new Color(0.55f, 0.8f, 0.35f), new Color(0.3f, 0.7f, 0.6f), new Color(0.35f, 0.6f, 0.95f),
        new Color(0.55f, 0.45f, 0.85f), new Color(0.95f, 0.55f, 0.75f), new Color(0.6f, 0.4f, 0.3f),
        new Color(0.6f, 0.6f, 0.6f), Color.white,
    };

    private VisualElement mainScreen, selectScreen, gameScreen;
    private VisualElement pictureGrid, paletteRow;
    private Label emptyLabel;
    private VisualElement selectedSwatch;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        mainScreen = root.Q("main-screen");
        selectScreen = root.Q("select-screen");
        gameScreen = root.Q("game-screen");
        pictureGrid = root.Q("picture-grid");
        paletteRow = root.Q("palette");
        emptyLabel = root.Q<Label>("empty-label");

        root.Q<Button>("play-button").clicked += () => ShowScreen(selectScreen);
        root.Q<Button>("quit-button").clicked += Quit;
        root.Q<Button>("select-back-button").clicked += () => ShowScreen(mainScreen);
        root.Q<Button>("game-back-button").clicked += () => ShowScreen(selectScreen);
        root.Q<Button>("reset-view-button").clicked += () => zoomHandler.ResetView();

        BuildPalette();
        pictureHandler.SpritesChanged += BuildPictureGrid;
        ShowScreen(mainScreen);
    }

    private void OnDisable()
    {
        if (pictureHandler) pictureHandler.SpritesChanged -= BuildPictureGrid;
    }

    private void ShowScreen(VisualElement screen)
    {
        foreach (var s in new[] { mainScreen, selectScreen, gameScreen })
            s.EnableInClassList("screen--active", s == screen);

        bool inGame = screen == gameScreen;
        coloringHandler.enabled = inGame;
        zoomHandler.enabled = inGame;
        if (screen == selectScreen) BuildPictureGrid(); // refresh thumbnails with latest colouring
    }

    private void BuildPictureGrid()
    {
        pictureGrid.Clear();
        var sprites = pictureHandler.Sprites;
        emptyLabel.style.display = sprites.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

        for (int i = 0; i < sprites.Count; i++)
        {
            int index = i;
            var card = new Button(() => OpenPicture(index)) { text = "" };
            card.AddToClassList("picture-card");
            card.style.backgroundImage = new StyleBackground(sprites[i]);
            pictureGrid.Add(card);
        }
    }

    private void OpenPicture(int index)
    {
        pictureHandler.Select(index);
        zoomHandler.ResetView();
        ShowScreen(gameScreen);
    }

    private void BuildPalette()
    {
        paletteRow.Clear();
        foreach (var color in palette)
        {
            var swatch = new VisualElement();
            swatch.AddToClassList("swatch");
            swatch.style.backgroundColor = color;
            swatch.RegisterCallback<ClickEvent>(_ => SelectSwatch(swatch, color));
            paletteRow.Add(swatch);
            if (selectedSwatch == null) SelectSwatch(swatch, color);
        }
    }

    private void SelectSwatch(VisualElement swatch, Color color)
    {
        selectedSwatch?.RemoveFromClassList("swatch--selected");
        selectedSwatch = swatch;
        swatch.AddToClassList("swatch--selected");
        coloringHandler.CurrentColor = color;
    }

    private static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
