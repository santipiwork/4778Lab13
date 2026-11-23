using System;
using UnityEngine;

public class WeatherLabUI : MonoBehaviour
{
    public WeatherManager weather;
    public WebImageCache images;
    public PlayFabConnection playFab;
    string username = "", password = "";
    Vector2 scroll;
    void OnGUI()
    {
        float scale = Mathf.Clamp(Screen.height / 800f, 0.65f, 1.5f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        GUILayout.BeginArea(new Rect(16, 16, 330, Screen.height / scale - 32), GUI.skin.box);
        scroll = GUILayout.BeginScrollView(scroll);
        GUILayout.Label("WEATHER / WORLD", new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold });
        GUILayout.Label("Web data lab  •  Five cities / three images");
        GUILayout.Space(12);
        weather.cityIndex = GUILayout.SelectionGrid(weather.cityIndex, WeatherManager.Cities, 1, GUILayout.Height(150));
        weather.previewMode = GUILayout.Toggle(weather.previewMode, "Preview sample weather");
        GUILayout.Label("OpenWeather API key (session only)");
        weather.apiKey = GUILayout.PasswordField(weather.apiKey, '*');
        if (GUILayout.Button("Refresh weather")) weather.Refresh();
        GUILayout.Label(weather.Status, new GUIStyle(GUI.skin.label) { wordWrap = true });
        if (weather.Current != null)
        {
            GUILayout.Label(weather.Current.main.temp.ToString("F1") + " °C   /   " + weather.CurrentSky);
            GUILayout.Label("City time: " + DateTimeOffset.UtcNow.AddSeconds(weather.Current.timezone).ToString("ddd HH:mm"));
        }
        GUILayout.Space(12);
        GUILayout.Label("BILLBOARD CACHE", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
        GUILayout.Label($"Requests: {images.DownloadCount}  Shared: {images.CacheHits}  Stored: {images.CachedCount}");
        GUILayout.Label(images.Status, new GUIStyle(GUI.skin.label) { wordWrap = true });
        if (GUILayout.Button("Retry images / demonstrate cached reuse"))
            foreach (var board in FindObjectsByType<WebBillboard>(FindObjectsSortMode.None)) board.Reload();
        GUILayout.Space(12);
        GUILayout.Label("PLAYFAB / TEST PLAYER", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
        GUILayout.Label("Title ID");
        playFab.titleId = GUILayout.TextField(playFab.titleId);
        GUILayout.Label("Test player username (not developer login)");
        username = GUILayout.TextField(username);
        GUILayout.Label("Test player password (not saved)");
        password = GUILayout.PasswordField(password, '*');
        GUI.enabled = !playFab.Busy;
        if (GUILayout.Button("Register test player")) { playFab.Authenticate(username, password, true); password = ""; }
        if (GUILayout.Button("Log in test player")) { playFab.Authenticate(username, password, false); password = ""; }
        GUI.enabled = true;
        GUILayout.Label(playFab.Status, new GUIStyle(GUI.skin.label) { wordWrap = true });
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
}
