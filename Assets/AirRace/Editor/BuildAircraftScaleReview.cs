using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using System.IO;

/// <summary>Builds a disposable, isolated scale review scene for the selected free aircraft asset.</summary>
public static class BuildAircraftScaleReview
{
    private const string ScenePath = "Assets/AirRace/Scenes/Presentation/AircraftScaleReview.unity";
    private const string AircraftPath = "Assets/AirRace/ThirdParty/Fab/LowPolyAirfieldFreeSample/fbx/aircraft/plane_prop.fbx";
    private const string RaftPath = "Assets/AirRace/ThirdParty/Polyfork/swim-raft-6aefb5.fbx";
    private const string WaterPath = "Assets/AirRace/ThirdParty/AURW/AURW/Resources/Prefabs/Water100x100.prefab";
    private const string PreviewPath = "Assets/AirRace/Documentation/Previews/aircraft_scale_review.png";
    private const string PylonPrefabPath = "Assets/AirRace/Art/Models/Pylons/AirRace_Pylon_SwimRaft.prefab";
    private const string PylonAssetsFolder = "Assets/AirRace/Art/Models/Pylons/AirRace_Pylon_SwimRaft_Assets";

    [MenuItem("Air Race/Build Aircraft Scale Review")]
    public static void Build()
    {
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(AircraftPath))
        {
            Debug.LogError("Aircraft FBX is not imported yet: " + AircraftPath);
            return;
        }

        EnsureReusablePylonPrefab();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "AircraftScaleReview";

        // Keep the imported stylized sky package in the project, but use a clear
        // procedural daytime sky for this scale test so the silhouette reads like the reference.
        var daytimeSky = new Material(Shader.Find("Skybox/Procedural")) { name = "Aircraft review · clear blue sky" };
        daytimeSky.SetFloat("_AtmosphereThickness", 0.8f);
        daytimeSky.SetFloat("_Exposure", 1.15f);
        daytimeSky.SetFloat("_SunSize", 0.035f);
        daytimeSky.SetColor("_SkyTint", new Color(0.58f, 0.62f, 0.68f));
        daytimeSky.SetColor("_GroundColor", new Color(0.66f, 0.69f, 0.72f));
        RenderSettings.skybox = daytimeSky;
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1.1f;
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.57f, 0.73f, 0.82f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 180;
        RenderSettings.fogEndDistance = 700;

        var sun = new GameObject("Sun · review lighting");
        var light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.25f;
        light.color = new Color(1f, 0.91f, 0.76f);
        sun.transform.rotation = Quaternion.Euler(38f, -32f, 0f);

        var waterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WaterPath);
        if (waterPrefab)
        {
            var water = (GameObject)PrefabUtility.InstantiatePrefab(waterPrefab);
            water.name = "AURW Ocean · 600 m review area";
            water.transform.position = new Vector3(0, -0.25f, 0);
            water.transform.localScale = new Vector3(6f, 1f, 6f);
            var waterRenderer = water.GetComponent<Renderer>();
            if (waterRenderer && waterRenderer.sharedMaterial)
            {
                var waterMaterial = new Material(waterRenderer.sharedMaterial) { name = "Aircraft review · turquoise AURW" };
                if (waterMaterial.HasProperty("_Color")) waterMaterial.SetColor("_Color", new Color(0.16f, 0.42f, 0.58f));
                if (waterMaterial.HasProperty("_Depth_Color")) waterMaterial.SetColor("_Depth_Color", new Color(0.04f, 0.27f, 0.46f, 0.95f));
                if (waterMaterial.HasProperty("_Water_Near_Color")) waterMaterial.SetColor("_Water_Near_Color", new Color(0.05f, 0.53f, 0.72f, 0.95f));
                waterRenderer.sharedMaterial = waterMaterial;
            }
        }

        var pylonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PylonPrefabPath);
        if (pylonPrefab)
        {
            var leftPylon = (GameObject)PrefabUtility.InstantiatePrefab(pylonPrefab);
            leftPylon.name = "Air Race pylon left · 25 m";
            leftPylon.transform.position = new Vector3(-8f, 0f, 0f);
            var rightPylon = (GameObject)PrefabUtility.InstantiatePrefab(pylonPrefab);
            rightPylon.name = "Air Race pylon right · 25 m";
            rightPylon.transform.position = new Vector3(8f, 0f, 0f);
        }

        var aircraft = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(AircraftPath));
        aircraft.name = "Fab plane_prop · 8.2 m / 842 tris";
        foreach (var filter in aircraft.GetComponentsInChildren<MeshFilter>(true))
        {
            var mesh = filter.sharedMesh;
            if (mesh) Debug.Log($"Aircraft mesh {filter.name}: local bounds {mesh.bounds.size}; transform rotation {filter.transform.localEulerAngles}, scale {filter.transform.localScale}");
        }
        aircraft.transform.position = new Vector3(4f, 18f, -11f);
        // The imported mesh's longest dimension is 11.24 units; normalize it to
        // the listing's stated 8.2 m and show a broad three-quarter view to camera.
        aircraft.transform.localScale = Vector3.one * (8.2f / 11.24f);
        aircraft.transform.rotation = Quaternion.Euler(0f, -20f, 0f);
        ColorizeAircraft(aircraft);
        LogBounds(aircraft);

        var cameraObject = new GameObject("Review Camera");
        var camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        camera.fieldOfView = 42f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 1000f;
        cameraObject.transform.position = new Vector3(20f, 23f, -55f);
        cameraObject.transform.LookAt(new Vector3(0f, 11f, 1f));

        EditorSceneManager.SaveScene(scene, ScenePath);
        Selection.activeGameObject = aircraft;
        SceneView.lastActiveSceneView?.FrameSelected();
        Capture(camera);
        Debug.Log("Built isolated aircraft scale review scene: " + ScenePath);
        Debug.Log("Reference dimensions: airplane listing 8.2 m; Air Race pylon 25 m high, 5 m base, 0.75 m tip.");
    }

    private static void EnsureReusablePylonPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PylonPrefabPath)) return;

        EnsureFolder(PylonAssetsFolder);
        var white = LoadOrCreateMaterial(PylonAssetsFolder + "/Pylon_White.mat", "Pylon · white", Color.white);
        var red = LoadOrCreateMaterial(PylonAssetsFolder + "/Pylon_Red.mat", "Pylon · race red", new Color(0.89f, 0.055f, 0.12f));
        var navy = LoadOrCreateMaterial(PylonAssetsFolder + "/SwimRaft_Navy.mat", "Swim Raft · navy", new Color(0.025f, 0.09f, 0.19f));

        var root = new GameObject("AirRace_Pylon_SwimRaft");
        var raftSource = AssetDatabase.LoadAssetAtPath<GameObject>(RaftPath);
        if (!raftSource)
        {
            Debug.LogError("Swim Raft FBX is not imported: " + RaftPath);
            Object.DestroyImmediate(root);
            return;
        }

        var raft = (GameObject)PrefabUtility.InstantiatePrefab(raftSource);
        raft.name = "SwimRaft_Platform_10m";
        raft.transform.SetParent(root.transform, false);
        raft.transform.localPosition = Vector3.zero;
        FitHorizontalWidth(raft, 10f);
        AlignBoundsBottom(raft, 0.05f);
        ColorizeRaft(raft, navy);
        float bodyBase = BoundsOf(raft).max.y;

        var body = new GameObject("Inflatable_Pylon_25m");
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = Vector3.up * bodyBase;
        var bands = new[]
        {
            new Band(0f, 14f, white),
            new Band(14f, 6f, red),
            new Band(20f, 3.5f, white),
            new Band(23.5f, 1.5f, red)
        };
        for (int i = 0; i < bands.Length; i++)
        {
            var band = bands[i];
            string meshPath = PylonAssetsFolder + $"/Pylon_Band_{i + 1:00}.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (!mesh)
            {
                mesh = MakeTaperedBand(band.start, band.start + band.height, 25f, 2.5f, 0.375f, 20);
                mesh.name = $"Pylon_Band_{i + 1:00}";
                AssetDatabase.CreateAsset(mesh, meshPath);
            }

            var segment = new GameObject($"Pylon_Band_{i + 1:00}");
            segment.transform.SetParent(body.transform, false);
            segment.AddComponent<MeshFilter>().sharedMesh = mesh;
            segment.AddComponent<MeshRenderer>().sharedMaterial = band.material;
            segment.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        AssetDatabase.SaveAssets();
        PrefabUtility.SaveAsPrefabAsset(root, PylonPrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string folder = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }

    private static Material LoadOrCreateMaterial(string path, string name, Color color)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material)
        {
            material = MakeMaterial(name, color);
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.color = color;
            EditorUtility.SetDirty(material);
        }
        return material;
    }

    private static void Capture(Camera camera)
    {
        var directory = Path.GetDirectoryName(PreviewPath);
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        var target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        camera.aspect = 16f / 9f;
        camera.targetTexture = target;
        camera.Render();
        RenderTexture.active = target;
        var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
        image.Apply();
        File.WriteAllBytes(PreviewPath, image.EncodeToPNG());
        Object.DestroyImmediate(image);
        camera.targetTexture = previousTarget;
        RenderTexture.active = previousActive;
        target.Release();
        Object.DestroyImmediate(target);
        AssetDatabase.ImportAsset(PreviewPath, ImportAssetOptions.ForceUpdate);
    }

    private static void FitHorizontalWidth(GameObject go, float width)
    {
        var b = BoundsOf(go);
        var maxHorizontal = Mathf.Max(b.size.x, b.size.z);
        if (maxHorizontal > 0.001f) go.transform.localScale *= width / maxHorizontal;
    }

    private static void AlignBoundsBottom(GameObject go, float y)
    {
        var b = BoundsOf(go);
        go.transform.position += Vector3.up * (y - b.min.y);
    }

    private static Bounds BoundsOf(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private struct Band
    {
        public readonly float start, height;
        public readonly Material material;
        public Band(float start, float height, Material material) { this.start = start; this.height = height; this.material = material; }
    }

    private static Mesh MakeTaperedBand(float y0, float y1, float totalHeight, float r0, float r1, int sides)
    {
        var mesh = new Mesh { name = "Air Race tapered pylon band" };
        var vertices = new Vector3[sides * 2 + 2];
        var triangles = new int[sides * 12];
        float lowRadius = Mathf.Lerp(r0, r1, y0 / totalHeight);
        float highRadius = Mathf.Lerp(r0, r1, y1 / totalHeight);
        for (int i = 0; i < sides; i++)
        {
            float angle = (i / (float)sides) * Mathf.PI * 2f;
            float x = Mathf.Cos(angle), z = Mathf.Sin(angle);
            vertices[i] = new Vector3(x * lowRadius, y0, z * lowRadius);
            vertices[sides + i] = new Vector3(x * highRadius, y1, z * highRadius);
        }
        vertices[sides * 2] = new Vector3(0, y0, 0);
        vertices[sides * 2 + 1] = new Vector3(0, y1, 0);
        int t = 0;
        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides;
            triangles[t++] = i; triangles[t++] = sides + i; triangles[t++] = next;
            triangles[t++] = next; triangles[t++] = sides + i; triangles[t++] = sides + next;
            triangles[t++] = sides * 2; triangles[t++] = next; triangles[t++] = i;
            triangles[t++] = sides * 2 + 1; triangles[t++] = sides + i; triangles[t++] = sides + next;
        }
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Material MakeMaterial(string name, Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader) { name = name, color = color };
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.18f);
        return material;
    }

    private static void ColorizeAircraft(GameObject aircraft)
    {
        var red = new Color(0.9f, 0.025f, 0.12f);
        var blue = new Color(0.02f, 0.20f, 0.70f);
        var deepBlue = new Color(0.015f, 0.07f, 0.21f);
        var yellow = new Color(1f, 0.72f, 0.015f);
        foreach (var renderer in aircraft.GetComponentsInChildren<Renderer>(true))
        {
            var original = renderer.sharedMaterials;
            if (original.Length == 0) continue;
            var recolored = new Material[original.Length];
            for (int i = 0; i < original.Length; i++)
            {
                string sourceName = original[i] ? original[i].name.ToLowerInvariant() : string.Empty;
                Color color;
                string colorName;
                if (sourceName.Contains("white")) { color = blue; colorName = "blue body"; }
                else if (sourceName.Contains("blue")) { color = red; colorName = "red trim"; }
                else if (sourceName.Contains("glass")) { color = deepBlue; colorName = "deep blue canopy"; }
                else if (sourceName.Contains("aluminium") || sourceName.Contains("alu") || sourceName.Contains("prop") || sourceName.Contains("beacon") || sourceName.Contains("navgreen"))
                { color = yellow; colorName = "yellow detail"; }
                else { color = deepBlue; colorName = "deep blue detail"; }
                recolored[i] = MakeMaterial($"Aircraft livery · {colorName}", color);
            }
            renderer.sharedMaterials = recolored;
        }
    }

    private static void ColorizeRaft(GameObject raft, Material navy)
    {
        foreach (var renderer in raft.GetComponentsInChildren<Renderer>(true))
        {
            var result = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < result.Length; i++) result[i] = navy;
            renderer.sharedMaterials = result;
        }
    }

    private static void LogBounds(GameObject go)
    {
        var bounds = BoundsOf(go);
        Debug.Log($"Fab aircraft imported bounds (Unity units): {bounds.size.x:F2} x {bounds.size.y:F2} x {bounds.size.z:F2}; min {bounds.min}, max {bounds.max}");
    }
}
