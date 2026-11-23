using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class WeatherManager : MonoBehaviour
{
    public enum Sky { Day, Night, Sunny, Sunless, Rainy, Snowy, Sunrise, Sunset }
    public string apiKey = "";
    public bool previewMode = true;
    [Range(0, 4)] public int cityIndex;
    public Light sun;
    [Tooltip("Day, Night, Sunny, Sunless, Rainy, Snowy, Sunrise, Sunset")]
    public Material[] skyboxes = new Material[8];
    public static readonly string[] Cities = { "Orlando,US", "London,GB", "Dubai,AE", "Tokyo,JP", "Sydney,AU" };
    public string Status { get; private set; } = "Ready";
    public WeatherData Current { get; private set; }
    public Sky CurrentSky { get; private set; }
    int previousCity = -1, generation;
    bool previousPreview;
    float nextRefresh, nextLighting;

    [Serializable] class LocalSettings { public string openWeatherApiKey; }
    void Awake()
    {
#if UNITY_EDITOR
        string path = System.IO.Path.Combine(Application.dataPath, "../UserSettings/WeatherLabCredentials.json");
        if (System.IO.File.Exists(path))
        {
            try
            {
                var settings = JsonUtility.FromJson<LocalSettings>(System.IO.File.ReadAllText(path));
                if (!string.IsNullOrWhiteSpace(settings?.openWeatherApiKey))
                { apiKey = settings.openWeatherApiKey; previewMode = false; }
            }
            catch (Exception) { Debug.LogWarning("Could not read local weather credentials. Enter the key in the game UI."); }
        }
#endif
    }

    [Serializable] public class WeatherData
    {
        public string name;
        public int timezone;
        public long dt;
        public Main main;
        public Condition[] weather;
        public SunTimes sys;
    }
    [Serializable] public class Main { public float temp; }
    [Serializable] public class Condition { public int id; public string description; }
    [Serializable] public class SunTimes { public long sunrise, sunset; }

    void Update()
    {
        cityIndex = Mathf.Clamp(cityIndex, 0, Cities.Length - 1);
        if (previousCity != cityIndex || previousPreview != previewMode || Time.unscaledTime >= nextRefresh) Refresh();
        if (Current != null && Time.unscaledTime >= nextLighting)
        {
            Apply(Current);
            nextLighting = Time.unscaledTime + 30;
        }
    }

    public void Refresh()
    {
        previousCity = cityIndex;
        previousPreview = previewMode;
        nextRefresh = Time.unscaledTime + 600;
        int ticket = ++generation;
        Current = null;
        if (previewMode)
        {
            float[] temperatures = { 28, 9, 35, 16, 22 };
            int[] offsets = { -18000, 0, 14400, 32400, 39600 };
            int[] conditions = { 800, 501, 800, 600, 803 };
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var midnight = now - (now + offsets[cityIndex]) % 86400;
            Current = new WeatherData { name = Cities[cityIndex], timezone = offsets[cityIndex], dt = now,
                main = new Main { temp = temperatures[cityIndex] },
                weather = new[] { new Condition { id = conditions[cityIndex], description = "Sample conditions" } },
                sys = new SunTimes { sunrise = midnight + 21600, sunset = midnight + 64800 } };
            Status = "PREVIEW — sample weather, not live data";
            Apply(Current);
        }
        else if (string.IsNullOrWhiteSpace(apiKey)) Status = "Enter an OpenWeather key, then Refresh.";
        else StartCoroutine(GetWeatherJSON(data =>
        {
            if (ticket != generation) return;
            try
            {
                var parsed = JsonUtility.FromJson<WeatherData>(data);
                if (parsed?.main == null || parsed.weather == null || parsed.weather.Length == 0 || parsed.sys == null)
                    throw new FormatException();
                Current = parsed;
                Apply(Current);
                Status = "LIVE — " + parsed.weather[0].description;
            }
            catch (Exception) { Status = "Weather response could not be parsed."; }
        }, error => { if (ticket == generation) Status = error; }));
    }

    public IEnumerator GetWeatherJSON(Action<string> callback, Action<string> error)
    {
        string url = "https://api.openweathermap.org/data/2.5/weather?q=" + UnityWebRequest.EscapeURL(Cities[cityIndex])
            + "&units=metric&appid=" + UnityWebRequest.EscapeURL(apiKey.Trim());
        using (var request = UnityWebRequest.Get(url))
        {
            request.timeout = 20;
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success) callback(request.downloadHandler.text);
            else error(request.responseCode == 401 ? "Key rejected (401). New keys may need a few hours to activate."
                : "Weather request failed (HTTP " + request.responseCode + "). Check connection, key, or quota.");
        }
    }

    public static Sky SelectSky(int code, long now, long sunrise, long sunset)
    {
        if (sunrise > 0 && sunset > sunrise)
        {
            if (Math.Abs(now - sunrise) <= 1800) return Sky.Sunrise;
            if (Math.Abs(now - sunset) <= 1800) return Sky.Sunset;
            if (now < sunrise || now > sunset) return Sky.Night;
        }
        if (code >= 600 && code < 700) return Sky.Snowy;
        if (code >= 200 && code < 600) return Sky.Rainy;
        if (code >= 700 && code < 800 || code >= 803) return Sky.Sunless;
        return code == 800 ? Sky.Sunny : Sky.Day;
    }

    void Apply(WeatherData data)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        CurrentSky = SelectSky(data.weather[0].id, now, data.sys.sunrise, data.sys.sunset);
        int index = (int)CurrentSky;
        if (skyboxes != null && index < skyboxes.Length && skyboxes[index] != null && RenderSettings.skybox != skyboxes[index])
        {
            RenderSettings.skybox = skyboxes[index];
            DynamicGI.UpdateEnvironment();
        }
        bool night = CurrentSky == Sky.Night;
        bool twilight = CurrentSky == Sky.Sunrise || CurrentSky == Sky.Sunset;
        bool overcast = CurrentSky == Sky.Rainy || CurrentSky == Sky.Sunless || CurrentSky == Sky.Snowy;
        if (sun != null)
        {
            sun.intensity = night ? 0.12f : twilight ? 0.65f : overcast ? 0.55f : 1.4f;
            sun.color = night ? new Color(0.4f, 0.55f, 1) : twilight ? new Color(1, 0.5f, 0.25f)
                : Color.Lerp(new Color(0.75f, 0.85f, 1), new Color(1, 0.92f, 0.75f), Mathf.InverseLerp(-10, 35, data.main.temp));
            double hour = DateTimeOffset.UtcNow.AddSeconds(data.timezone).TimeOfDay.TotalHours;
            sun.transform.rotation = Quaternion.Euler(night ? 25 : (float)Math.Sin((hour - 6) / 12 * Math.PI) * 70, -30, 0);
        }
        RenderSettings.ambientIntensity = night ? 0.25f : 1;
    }
}
