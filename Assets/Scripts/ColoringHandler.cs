using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ColoringHandler : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private PictureHandler pictureHandler;
    [SerializeField] private Color currentColor = Color.red;

    [Tooltip("Pixels of the original line art darker than this (0-1 luminance) are treated as outline walls.")]
    [SerializeField, Range(0f, 1f)] private float lineThreshold = 0.55f;

    [Tooltip("How many pixels the colour bleeds under the outline, so no white halo remains between fill and line.")]
    [SerializeField, Range(0, 4)] private int edgeBleed = 2;

    // Untouched line art per texture: walls and shading are always taken from here,
    // so refilling an already coloured area behaves exactly like the first fill.
    private readonly Dictionary<Texture2D, Color32[]> originals = new();

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
        if (!originals.TryGetValue(tex, out var src))
        {
            src = tex.GetPixels32();
            originals[tex] = src;
        }

        int w = tex.width, h = tex.height;
        int start = y * w + x;
        if (Luminance(src[start]) < lineThreshold) return; // tapped on a line

        var pixels = tex.GetPixels32();
        var region = new bool[src.Length];
        var stack = new Stack<int>();
        stack.Push(start);

        // Scanline fill over the ORIGINAL line art: walls are dark pixels, nothing else.
        while (stack.Count > 0)
        {
            int seed = stack.Pop();
            if (region[seed]) continue;
            int sy = seed / w, row = sy * w;
            int l = seed - row, r = l;
            while (l > 0 && IsOpen(row + l - 1)) l--;
            while (r < w - 1 && IsOpen(row + r + 1)) r++;

            for (int sx = l; sx <= r; sx++)
            {
                region[row + sx] = true;
                if (sy > 0 && IsOpen(row - w + sx)) stack.Push(row - w + sx);
                if (sy < h - 1 && IsOpen(row + w + sx)) stack.Push(row + w + sx);
            }
        }

        // Grow a few pixels into the outline so anti-aliased grey pixels get tinted too.
        var paint = (bool[])region.Clone();
        for (int pass = 0; pass < edgeBleed; pass++)
        {
            var next = (bool[])paint.Clone();
            for (int py = 0; py < h; py++)
            for (int px = 0; px < w; px++)
            {
                int i = py * w + px;
                if (paint[i]) continue;
                if ((px > 0 && paint[i - 1]) || (px < w - 1 && paint[i + 1]) ||
                    (py > 0 && paint[i - w]) || (py < h - 1 && paint[i + w]))
                    next[i] = true;
            }
            paint = next;
        }

        // Multiply the fill by the line art's brightness: white -> pure fill,
        // grey anti-aliasing -> darker fill, black line -> stays black. Smooth edges for free.
        for (int i = 0; i < pixels.Length; i++)
        {
            if (!paint[i]) continue;
            float lum = Luminance(src[i]);
            pixels[i] = new Color32(
                (byte)(fill.r * lum), (byte)(fill.g * lum), (byte)(fill.b * lum), src[i].a);
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        bool IsOpen(int i) => !region[i] && Luminance(src[i]) >= lineThreshold;
    }

    static float Luminance(Color32 c) => (0.299f * c.r + 0.587f * c.g + 0.114f * c.b) / 255f;
}
