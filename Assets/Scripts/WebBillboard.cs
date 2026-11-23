using System;
using UnityEngine;

public class WebBillboard : MonoBehaviour
{
    public string imageUrl;
    public WebImageCache cache;
    Material material;
    Vector3 originalScale;
    void Awake()
    {
        material = GetComponent<Renderer>().material;
        originalScale = transform.localScale;
    }
    void Start() { Reload(); }
    public void GetWebImage(Action<Texture2D> callback) { cache.GetWebImage(imageUrl, callback); }
    public void Reload()
    {
        GetWebImage(texture =>
        {
            if (this == null || texture == null) return;
            material.mainTexture = texture;
            material.color = Color.white;
            float aspect = (float)texture.width / texture.height;
            transform.localScale = aspect > originalScale.x / originalScale.y
                ? new Vector3(originalScale.x, originalScale.x / aspect, 1)
                : new Vector3(originalScale.y * aspect, originalScale.y, 1);
        });
    }
    void OnDestroy() { if (material != null) Destroy(material); }
}
