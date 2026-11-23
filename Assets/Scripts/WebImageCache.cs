using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class WebImageCache : MonoBehaviour
{
    readonly Dictionary<string, Texture2D> images = new Dictionary<string, Texture2D>();
    readonly Dictionary<string, List<Action<Texture2D>>> pending = new Dictionary<string, List<Action<Texture2D>>>();
    public int DownloadCount { get; private set; }
    public int CacheHits { get; private set; }
    public int CachedCount => images.Count;
    public string Status { get; private set; } = "Ready";

    public void GetWebImage(string url, Action<Texture2D> callback)
    {
        if (images.TryGetValue(url, out var texture)) { CacheHits++; callback(texture); return; }
        if (pending.TryGetValue(url, out var callbacks)) { CacheHits++; callbacks.Add(callback); return; }
        pending[url] = new List<Action<Texture2D>> { callback };
        StartCoroutine(DownloadImage(url));
    }

    IEnumerator DownloadImage(string url)
    {
        DownloadCount++;
        Texture2D texture = null;
        using (var request = UnityWebRequestTexture.GetTexture(url))
        {
            request.timeout = 30;
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                texture = DownloadHandlerTexture.GetContent(request);
                images[url] = texture;
                Status = "Image downloaded";
            }
            else Status = "Image download failed: " + new Uri(url).Host + " (" + request.responseCode + "). Use Retry images.";
        }
        var callbacks = pending[url];
        pending.Remove(url); // Failed requests may be retried.
        foreach (var callback in callbacks)
        {
            try { callback?.Invoke(texture); }
            catch (Exception exception) { Debug.LogException(exception); }
        }
    }

    void OnDestroy()
    {
        StopAllCoroutines();
        foreach (var texture in images.Values) if (texture != null) Destroy(texture);
        images.Clear();
        pending.Clear();
    }
}
