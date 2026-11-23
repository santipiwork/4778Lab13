using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class WebImageCache : MonoBehaviour
{
    readonly Dictionary<string, Texture2D> images = new Dictionary<string, Texture2D>();
    readonly Dictionary<string, List<Action<Texture2D>>> pending = new Dictionary<string, List<Action<Texture2D>>>();
    readonly Dictionary<string, string> failures = new Dictionary<string, string>();
    public int DownloadCount { get; private set; }
    public int CacheHits { get; private set; }
    public int CachedCount => images.Count;
    public int PendingCount => pending.Count;
    public string Status => failures.Count > 0
        ? string.Join("\n", failures.Values) + "\nUse Retry images to try failed downloads again."
        : pending.Count > 0 ? "Downloading " + pending.Count + " image(s)..."
        : images.Count > 0 ? "All requested images are cached." : "Ready";

    public void GetWebImage(string url, Action<Texture2D> callback)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http"))
        {
            failures[url ?? ""] = "Invalid image URL.";
            Notify(callback, null);
            return;
        }
        if (images.TryGetValue(url, out var texture) && texture != null) { CacheHits++; Notify(callback, texture); return; }
        if (pending.TryGetValue(url, out var callbacks)) { CacheHits++; callbacks.Add(callback); return; }
        failures.Remove(url);
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
                try
                {
                    texture = DownloadHandlerTexture.GetContent(request);
                    if (texture == null || texture.width <= 0 || texture.height <= 0)
                        throw new InvalidOperationException("Empty image");
                    images[url] = texture;
                    failures.Remove(url);
                }
                catch (Exception)
                {
                    if (texture != null) Destroy(texture);
                    texture = null;
                    failures[url] = new Uri(url).Host + ": response could not be decoded as an image.";
                }
            }
            else
            {
                string detail = request.responseCode == 429 ? "HTTP 429: server rate limit; wait before retrying."
                    : "HTTP " + request.responseCode + ": " + request.error;
                failures[url] = new Uri(url).Host + ": " + detail;
            }
        }
        var callbacks = pending[url];
        pending.Remove(url); // Failed requests may be retried.
        foreach (var callback in callbacks) Notify(callback, texture);
    }

    static void Notify(Action<Texture2D> callback, Texture2D texture)
    {
        try { callback?.Invoke(texture); }
        catch (Exception exception) { Debug.LogException(exception); }
    }

    void OnDisable()
    {
        // Unity stops coroutines when the object is disabled; don't leave stale in-flight entries.
        StopAllCoroutines();
        var interrupted = new List<List<Action<Texture2D>>>(pending.Values);
        pending.Clear();
        foreach (var callbacks in interrupted)
            foreach (var callback in callbacks) Notify(callback, null);
    }

    void OnDestroy()
    {
        StopAllCoroutines();
        foreach (var texture in images.Values) if (texture != null) Destroy(texture);
        images.Clear();
        pending.Clear();
    }
}
