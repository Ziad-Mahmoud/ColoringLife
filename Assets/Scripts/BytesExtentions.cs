using UnityEngine;

public static class BytesExtentions
{
    private static Vector2 centerPivot = new (0.5f, 0.5f);
    private static Rect lastRect = Rect.zero;
   public static Sprite ToSprite(this byte[] bytes)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(bytes))
        {
            Object.Destroy(tex);
            return null;
        }
        if (lastRect.width != tex.width || lastRect.height != tex.height)
        {
            lastRect = new Rect(0, 0, tex.width, tex.height);
        }
        return Sprite.Create(tex, lastRect, centerPivot);
    }
}
