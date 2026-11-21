using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class ColoringHandler : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
{
    private float pressTime;

    public void OnPointerDown(PointerEventData e) => pressTime = Time.unscaledTime;

    [SerializeField] private PictureHandler pictureHandler;
    [SerializeField] private Color currentColor = Color.red;

    [Tooltip("Pixels of the original line art darker than this (0-1 luminance) are treated as outline walls.")]
    [SerializeField, Range(0f, 1f)] private float lineThreshold = 0.55f;

    [Tooltip("How many pixels the colour bleeds under the outline, so no white halo remains between fill and line.")]
    [SerializeField, Range(0, 4)] private int edgeBleed = 2;

    [Header("Animation")]
    [Tooltip("Seconds for the colour to spread across the whole area.")]
    [SerializeField, Min(0f)] private float fillDuration = 0.6f;
    [SerializeField] private Ease fillEase = Ease.OutQuad;

    // Untouched line art per texture: walls and shading are always taken from here,
    // so refilling an already coloured area behaves exactly like the first fill.
    private readonly Dictionary<Texture2D, Color32[]> originals = new();
    // Live pixel buffer per texture, shared by every running fill so they never overwrite each other.
    private readonly Dictionary<Texture2D, Color32[]> working = new();

    public void OnPointerClick(PointerEventData e)
    {
        // A finger lifted after a pinch or pan is not a colouring tap.
        if (ZoomHandler.LastGestureTime >= pressTime) return;

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
            working[tex] = (Color32[])src.Clone();
        }
        var pixels = working[tex];

        int w = tex.width, h = tex.height;
        int start = y * w + x;
        if (Luminance(src[start]) < lineThreshold) return; // tapped on a line

        var paint = BuildPaintMask(src, w, h, start);
        var order = OrderFromTap(paint, w, h, start);

        // Pre-compute final colours: fill multiplied by line-art brightness gives smooth edges.
        var colors = new Color32[order.Count];
        for (int k = 0; k < order.Count; k++)
        {
            var s = src[order[k]];
            float lum = Luminance(s);
            colors[k] = new Color32((byte)(fill.r * lum), (byte)(fill.g * lum), (byte)(fill.b * lum), s.a);
        }

        // Tween progress 0->1; pixels are revealed ring by ring outward from the tap.
        int applied = 0;
        float progress = 0f;
        DOTween.To(() => progress, v => progress = v, 1f, fillDuration)
            .SetEase(fillEase)
            .SetTarget(this)
            .OnUpdate(() =>
            {
                int upTo = Mathf.Min(order.Count, Mathf.CeilToInt(progress * order.Count));
                if (upTo <= applied) return;
                for (int k = applied; k < upTo; k++) pixels[order[k]] = colors[k];
                applied = upTo;
                tex.SetPixels32(pixels);
                tex.Apply();
            })
            .OnComplete(() =>
            {
                for (int k = applied; k < order.Count; k++) pixels[order[k]] = colors[k];
                tex.SetPixels32(pixels);
                tex.Apply();
            });
    }

    // Region bounded by dark lines in the original art, grown a few pixels under the outline.
    bool[] BuildPaintMask(Color32[] src, int w, int h, int start)
    {
        var region = new bool[src.Length];
        var stack = new Stack<int>();
        stack.Push(start);

        bool IsOpen(int i) => !region[i] && Luminance(src[i]) >= lineThreshold;

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

        var paint = region;
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
        return paint;
    }

    // Breadth-first order from the tap point, so the colour spreads like a wave.
    static List<int> OrderFromTap(bool[] paint, int w, int h, int start)
    {
        var order = new List<int>();
        var seen = new bool[paint.Length];
        var q = new Queue<int>();
        q.Enqueue(start);
        seen[start] = true;
        while (q.Count > 0)
        {
            int i = q.Dequeue();
            order.Add(i);
            int x = i % w, y = i / w;
            if (x > 0) Visit(i - 1);
            if (x < w - 1) Visit(i + 1);
            if (y > 0) Visit(i - w);
            if (y < h - 1) Visit(i + w);
        }
        return order;

        void Visit(int n)
        {
            if (seen[n] || !paint[n]) return;
            seen[n] = true;
            q.Enqueue(n);
        }
    }

    void OnDestroy() => DOTween.Kill(this);

    static float Luminance(Color32 c) => (0.299f * c.r + 0.587f * c.g + 0.114f * c.b) / 255f;
}
