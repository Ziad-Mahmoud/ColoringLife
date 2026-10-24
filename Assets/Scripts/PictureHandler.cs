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
    private readonly string[] imageExtensions = { ".png", ".jpg", ".jpeg" };
    private async void Awake()
    {
        var files = Directory.GetFiles(pictureDownloader.CachedDir);
        foreach (var item in files)
        {
            if (imageExtensions.Contains(Path.GetExtension(item))) 
            { 
                var data = await File.ReadAllBytesAsync(item);
                sprites.Add(data.ToSprite());
            }
        }
        StartCoroutine(pictureDownloader.Sync(s => sprites.Add(s)));
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
