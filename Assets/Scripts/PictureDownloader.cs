using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
public class PictureDownloader : MonoBehaviour
{
    [SerializeField] string url;
    public string CachedDir => Path.Combine(Application.persistentDataPath, "images");
    private const string imageName = "pic";
    private const string imageExtension = ".jpeg";
    public IEnumerator Sync(Action<Sprite> onSprite)
    {
        Directory.CreateDirectory(CachedDir);

        var counterPath = Path.Combine(CachedDir, "image-counter.txt");
        var imageCounter = ReadCounter(counterPath);

        while (true)
        {
            var imageUrl = $"{url}/{imageName}{imageCounter}{imageExtension}";
            var imageSave = $"{CachedDir}/{imageName}{imageCounter}{imageExtension}";
            using var req = UnityWebRequest.Get(imageUrl);
            req.timeout = 15;
            req.downloadHandler = new DownloadHandlerFile(imageSave) { removeFileOnAbort = true };
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                var sprite = File.ReadAllBytes(imageSave).ToSprite();
                if(!sprite)
                {
                    File.Delete(imageSave);
                    break;
                }
                onSprite?.Invoke(sprite);
                imageCounter++;
                File.WriteAllText(counterPath, imageCounter.ToString());
            }
            else
            {
                break;
            }
        }
    }
    public int ReadCounter(string counterPath)
    {
        int imageCounter = 0;
        if (!File.Exists(counterPath)
            || !int.TryParse(File.ReadAllText(counterPath).Trim(), out imageCounter))
        {
            File.WriteAllText(counterPath, imageCounter.ToString());
        }
        return imageCounter;
    }
}
