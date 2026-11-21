using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class PictureHandler : MonoBehaviour
{
    [SerializeField] private PictureDownloader pictureDownloader;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private BoxCollider2D box;
    private readonly List<Sprite> sprites = new();
    private int currentPicture;
    public Sprite currentSprite => sprites[currentPicture];
    public IReadOnlyList<Sprite> Sprites => sprites;
    /// <summary>Raised whenever a picture is loaded from cache or downloaded.</summary>
    public event Action SpritesChanged;
    private readonly string[] imageExtensions = { ".png", ".jpg", ".jpeg" };

    private async void Awake()
    {
        Directory.CreateDirectory(pictureDownloader.CachedDir);
        var files = Directory.GetFiles(pictureDownloader.CachedDir);
        foreach (var item in files)
        {
            if (imageExtensions.Contains(Path.GetExtension(item)))
            {
                var data = await File.ReadAllBytesAsync(item);
                var sprite = data.ToSprite();
                if (sprite) Add(sprite);
            }
        }
        StartCoroutine(pictureDownloader.Sync(Add));
        if (sprites.Count > 0) Show();
    }

    private void Add(Sprite s)
    {
        sprites.Add(s);
        SpritesChanged?.Invoke();
    }

    public void Select(int index)
    {
        if (index < 0 || index >= sprites.Count) return;
        currentPicture = index;
        Show();
    }

    private void Show()
    {
        var s = sprites[currentPicture];
        spriteRenderer.sprite = s;
        box.size = s.bounds.size;
        box.offset = s.bounds.center;
    }
}
