using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WeatherLabSetup
{
    const string ScenePath = "Assets/Scenes/WeatherLab.unity";
    [MenuItem("Weather Lab/Create scene")]
    public static void CreateScene()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory("Assets/WeatherLabMaterials");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("Weather Lab Managers");
        var weather = root.AddComponent<WeatherManager>();
        var cache = root.AddComponent<WebImageCache>();
        var playFab = root.AddComponent<PlayFabConnection>();
        var ui = root.AddComponent<WeatherLabUI>();
        ui.weather = weather; ui.images = cache; ui.playFab = playFab;
        var light = new GameObject("Sun").AddComponent<Light>();
        light.type = LightType.Directional;
        light.shadows = LightShadows.Soft;
        light.transform.rotation = Quaternion.Euler(45, -30, 0);
        weather.sun = light;
        RenderSettings.sun = light;
        Color[] colors = { new Color(.45f,.65f,.9f), new Color(.025f,.035f,.1f), new Color(.25f,.6f,1),
            new Color(.38f,.43f,.5f), new Color(.23f,.28f,.35f), new Color(.68f,.75f,.8f),
            new Color(.95f,.5f,.3f), new Color(.85f,.3f,.24f) };
        for (int i = 0; i < 8; i++)
        {
            string path = "Assets/WeatherLabMaterials/Preview_" + (WeatherManager.Sky)i + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Skybox/Procedural"));
                mat.SetColor("_SkyTint", colors[i]);
                mat.SetColor("_GroundColor", colors[i] * .4f);
                mat.SetFloat("_Exposure", i == 1 ? .15f : 1.1f);
                AssetDatabase.CreateAsset(mat, path);
            }
            weather.skyboxes[i] = mat;
        }
        RenderSettings.skybox = weather.skyboxes[0];
        var camera = new GameObject("Main Camera").AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.gameObject.AddComponent<AudioListener>();
        camera.transform.position = new Vector3(-3.5f, 5, -18);
        camera.transform.LookAt(new Vector3(-3.5f, 2.8f, 3));
        camera.fieldOfView = 52;
        camera.clearFlags = CameraClearFlags.Skybox;
        var groundMat = MakeMaterial("Ground", new Color(.14f,.23f,.23f), false);
        var frameMat = MakeMaterial("Frame", new Color(.055f,.085f,.12f), false);
        var imageMat = MakeMaterial("Image", new Color(.18f,.28f,.34f), true);
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(10, 1, 10);
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;
        string[] urls = {
            "https://upload.wikimedia.org/wikipedia/commons/9/97/The_Earth_seen_from_Apollo_17.jpg",
            "https://nssdc.gsfc.nasa.gov/image/planetary/earth/apollo16_earth_northamerica.jpg",
            "https://images-assets.nasa.gov/image/PIA01492/PIA01492~orig.jpg" };
        string[] labels = { "01 / BLUE MARBLE · NASA", "02 / APOLLO 16 · NASA", "03 / NEPTUNE · NASA/JPL" };
        for (int row = 0; row < 2; row++)
        for (int column = 0; column < 3; column++)
        {
            float x = (column - 1) * 4.8f + 2;
            float y = row == 0 ? 5.5f : 2.2f;
            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Billboard frame " + row + "-" + column;
            frame.transform.position = new Vector3(x, y, 3.15f);
            frame.transform.localScale = new Vector3(4.4f, 2.9f, .25f);
            frame.GetComponent<Renderer>().sharedMaterial = frameMat;
            var face = GameObject.CreatePrimitive(PrimitiveType.Quad);
            face.name = "Web billboard " + column + (row == 0 ? " original" : " cached copy");
            face.transform.position = new Vector3(x, y, 2.99f);
            face.transform.localScale = new Vector3(4.1f, 2.55f, 1);
            face.GetComponent<Renderer>().sharedMaterial = imageMat;
            var billboard = face.AddComponent<WebBillboard>();
            billboard.cache = cache;
            billboard.imageUrl = urls[column];
            var label = new GameObject("Image credit").AddComponent<TextMesh>();
            label.text = labels[column] + (row == 1 ? " / COPY" : "");
            label.fontSize = 40; label.characterSize = .06f;
            label.anchor = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.transform.position = new Vector3(x, y - 1.25f, 2.97f);
        }
        PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("WEATHER_LAB_SETUP_OK");
    }

    static Material MakeMaterial(string name, Color color, bool unlit)
    {
        string path = "Assets/WeatherLabMaterials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
        material.color = color;
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    [MenuItem("Weather Lab/Assign imported Fantasy skyboxes")]
    public static void AssignFantasySkyboxes()
    {
        var weather = UnityEngine.Object.FindFirstObjectByType<WeatherManager>();
        if (weather == null) throw new InvalidOperationException("Open WeatherLab first.");
        Undo.RecordObject(weather, "Assign Fantasy skyboxes");
        int count = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Material"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("WeatherLabMaterials") || path.IndexOf("Fantasy", StringComparison.OrdinalIgnoreCase) < 0) continue;
            string name = Path.GetFileNameWithoutExtension(path);
            for (int i = 0; i < 8; i++)
                if (name.Equals(((WeatherManager.Sky)i).ToString(), StringComparison.OrdinalIgnoreCase))
                { weather.skyboxes[i] = AssetDatabase.LoadAssetAtPath<Material>(path); count++; }
        }
        EditorUtility.SetDirty(weather);
        EditorSceneManager.MarkSceneDirty(weather.gameObject.scene);
        Debug.Log("Assigned " + count + " Fantasy materials. Check all eight Inspector slots; assign variants manually if needed.");
    }

    [MenuItem("Weather Lab/Validate scene and weather mapping")]
    public static void Validate()
    {
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
        Check(WeatherManager.SelectSky(800, 40000, 21600, 64800) == WeatherManager.Sky.Sunny, "Clear day");
        Check(WeatherManager.SelectSky(501, 40000, 21600, 64800) == WeatherManager.Sky.Rainy, "Rain");
        Check(WeatherManager.SelectSky(601, 40000, 21600, 64800) == WeatherManager.Sky.Snowy, "Snow");
        Check(WeatherManager.SelectSky(804, 40000, 21600, 64800) == WeatherManager.Sky.Sunless, "Overcast");
        Check(WeatherManager.SelectSky(801, 40000, 21600, 64800) == WeatherManager.Sky.Day, "Clouds");
        Check(WeatherManager.SelectSky(800, 10000, 21600, 64800) == WeatherManager.Sky.Night, "Night");
        Check(WeatherManager.SelectSky(800, 21600, 21600, 64800) == WeatherManager.Sky.Sunrise, "Sunrise");
        Check(WeatherManager.SelectSky(800, 64800, 21600, 64800) == WeatherManager.Sky.Sunset, "Sunset");
        var weather = UnityEngine.Object.FindFirstObjectByType<WeatherManager>();
        Check(weather != null && weather.sun != null, "Weather manager and sun");
        foreach (var material in weather.skyboxes) Check(material != null, "Missing skybox");
        Check(UnityEngine.Object.FindObjectsByType<WebBillboard>(FindObjectsSortMode.None).Length == 6, "Six billboards");
        Check(PlayerSettings.insecureHttpOption == InsecureHttpOption.AlwaysAllowed, "HTTP setting");
        Debug.Log("WEATHER_LAB_VALIDATION_OK: eight sky cases and scene wiring.");
    }
}


