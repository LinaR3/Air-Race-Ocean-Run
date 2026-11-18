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

    [MenuItem("Air Race/Build Aircraft Scale Review")]
    public static void Build()
    {
        var aircraftImporter = AssetImporter.GetAtPath(AircraftPath) as ModelImporter;
        if (aircraftImporter)
        {
            if (!aircraftImporter.bakeAxisConversion)
            {
                aircraftImporter.bakeAxisConversion = true;
                aircraftImporter.SaveAndReimport();
            }
            AssetDatabase.ImportAsset(AircraftPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }

        if (!AssetDatabase.LoadAssetAtPath<GameObject>(AircraftPath))
        {
            Debug.LogError("Aircraft FBX is not imported yet: " + AircraftPath);
            return;
        }

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

        var raftPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RaftPath);
        var raft = raftPrefab ? (GameObject)PrefabUtility.InstantiatePrefab(raftPrefab) : null;
        if (raft)
        {
            raft.name = "Swim Raft · scaled to 10 m";
            raft.transform.position = new Vector3(-8f, 0f, 0f);
            FitHorizontalWidth(raft, 10f);
            AlignBoundsBottom(raft, 0.05f);
            ColorizeRaft(raft);
        }

        // Real Air Race pylons are approximately 25 m tall, 5 m wide at the base,
        // and 0.75 m at the tip. The imported Fab traffic marker is shown separately
        // at left to make its scale and silhouette limitations clear.
        var pylon = CreateRacePylon("Air Race pylon · 25 m reference", Vector3.zero, 25f, 5f, 0.75f);
        pylon.transform.position = new Vector3(-8f, raft ? BoundsOf(raft).max.y : 0.35f, 0f);

        if (raftPrefab)
        {
            var secondRaft = (GameObject)PrefabUtility.InstantiatePrefab(raftPrefab);
            secondRaft.name = "Gate raft · 10 m reference";
            secondRaft.transform.position = new Vector3(8f, 0f, 0f);
            FitHorizontalWidth(secondRaft, 10f);
            AlignBoundsBottom(secondRaft, 0.05f);
            ColorizeRaft(secondRaft);
            CreateRacePylon("Gate pylon · 25 m reference", new Vector3(8f, BoundsOf(secondRaft).max.y, 0f), 25f, 5f, 0.75f);
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

    private static GameObject CreateRacePylon(string name, Vector3 position, float height, float baseDiameter, float tipDiameter)
    {
        var root = new GameObject(name);
        root.transform.position = position;
        var bands = new[]
        {
            new Band(0f, 14f, Color.white),
            new Band(14f, 6f, new Color(0.89f, 0.055f, 0.12f)),
            new Band(20f, 3.5f, Color.white),
            new Band(23.5f, 1.5f, new Color(0.89f, 0.055f, 0.12f))
        };
        var radiusAtBase = baseDiameter * 0.5f;
        var radiusAtTip = tipDiameter * 0.5f;
        foreach (var band in bands)
        {
            var segment = new GameObject("Inflatable band");
            segment.transform.SetParent(root.transform, false);
            var filter = segment.AddComponent<MeshFilter>();
            var renderer = segment.AddComponent<MeshRenderer>();
            filter.sharedMesh = MakeTaperedBand(band.start, band.start + band.height, height,
                radiusAtBase, radiusAtTip, 20);
            renderer.sharedMaterial = MakeMaterial("Air Race review · " + band.color, band.color);
        }
        return root;
    }

    private struct Band
    {
        public readonly float start, height;
        public readonly Color color;
        public Band(float start, float height, Color color) { this.start = start; this.height = height; this.color = color; }
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
        var renderers = aircraft.GetComponentsInChildren<Renderer>(true);
        var palette = new[]
        {
            new Color(0.91f, 0.93f, 0.96f),
            new Color(0.02f, 0.22f, 0.64f),
            new Color(0.88f, 0.035f, 0.12f),
            new Color(0.98f, 0.71f, 0.08f)
        };
        int index = 0;
        foreach (var renderer in renderers)
        {
            var source = renderer.sharedMaterials;
            var result = new Material[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                var sourceMat = source[i];
                var clone = MakeMaterial("Aircraft review · " + (sourceMat ? sourceMat.name : "palette"), palette[index++ % palette.Length]);
                result[i] = clone;
            }
            renderer.sharedMaterials = result;
        }
    }

    private static void ColorizeRaft(GameObject raft)
    {
        foreach (var renderer in raft.GetComponentsInChildren<Renderer>(true))
        {
            var source = renderer.sharedMaterials;
            var result = new Material[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                var material = MakeMaterial("Aircraft review · navy float", new Color(0.025f, 0.09f, 0.19f));
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.35f);
                result[i] = material;
            }
            renderer.sharedMaterials = result;
        }
    }

    private static void LogBounds(GameObject go)
    {
        var bounds = BoundsOf(go);
        Debug.Log($"Fab aircraft imported bounds (Unity units): {bounds.size.x:F2} x {bounds.size.y:F2} x {bounds.size.z:F2}; min {bounds.min}, max {bounds.max}");
    }
}
