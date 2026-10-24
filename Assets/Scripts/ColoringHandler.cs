using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ColoringHandler : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private PictureHandler pictureHandler;
    [SerializeField] private Color currentColor = Color.red;
    public void OnPointerClick(PointerEventData e)
    {
        var sprite = pictureHandler.currentSprite;
        var tex = sprite.texture;
        Vector3 local = transform.InverseTransformPoint(e.pointerPressRaycast.worldPosition);
        Bounds b = sprite.bounds;
        int px = Mathf.Clamp((int)((local.x - b.min.x) / b.size.x * tex.width), 0, tex.width - 1);
        int py = Mathf.Clamp((int)((local.y - b.min.y) / b.size.y * tex.height), 0, tex.height - 1);

        FloodFill(tex, px, py, currentColor);
    }
    void FloodFill(Texture2D tex, int x, int y, Color32 fill)
    {
        var pixels = tex.GetPixels32();
        int w = tex.width, h = tex.height;
        Color32 target = pixels[y * w + x];
        if (Same(target, fill)) return;

        var q = new Queue<Vector2Int>();
        q.Enqueue(new Vector2Int(x, y));

        while (q.Count > 0)
        {
            var p = q.Dequeue();
            if (p.x < 0 || p.y < 0 || p.x >= w || p.y >= h) continue;
            int i = p.y * w + p.x;
            if (!Same(pixels[i], target)) continue;

            pixels[i] = fill;
            q.Enqueue(new Vector2Int(p.x + 1, p.y));
            q.Enqueue(new Vector2Int(p.x - 1, p.y));
            q.Enqueue(new Vector2Int(p.x, p.y + 1));
            q.Enqueue(new Vector2Int(p.x, p.y - 1));
        }
        tex.SetPixels32(pixels);
        tex.Apply();
    }
    bool Same(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;
}
