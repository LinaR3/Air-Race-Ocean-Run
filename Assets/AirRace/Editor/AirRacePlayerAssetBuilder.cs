using System;
using UnityEditor;
using UnityEngine;
using AirRace.Gameplay;

namespace AirRace.EditorTools
{
    public static class AirRacePlayerAssetBuilder
    {
        private const string ModelPath = "Assets/AirRace/ThirdParty/Fab/LowPolyAirfieldFreeSample/fbx/aircraft/plane_prop.fbx";
        private const string OutputFolder = "Assets/AirRace/Gameplay/Prefabs";
        private const string PrefabPath = OutputFolder + "/Aircraft_Player.prefab";
        private const string CanPath = OutputFolder + "/Energy_Can_Blockout.prefab";

        [MenuItem("Air Race/Build Aircraft And Energy Can Prefabs")]
        public static void Build()
        {
            EnsureFolder(OutputFolder);
            EnsureFolder("Assets/AirRace/Art/Models/Aircraft/Generated");
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }
            BuildAircraft();
            BuildCan();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Built playable aircraft and energy-can prefabs.");
        }

        private static void BuildAircraft()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (!source) throw new InvalidOperationException("Approved free aircraft FBX missing: " + ModelPath);
            var root = new GameObject("Aircraft_Player");
            root.transform.localScale = Vector3.one;
            var body = root.AddComponent<Rigidbody>();
            body.mass = 6f;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(4.4f, 1.65f, 7f);

            var visuals = new GameObject("Aircraft_Visual_Rig");
            visuals.transform.SetParent(root.transform, false);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source);
            model.name = "Aircraft_Visuals_Fab_Free_Sample";
            model.transform.SetParent(visuals.transform, false);
            model.transform.localScale = Vector3.one * (8.2f / 11.24f);
            model.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            // Keep the source FBX mesh bound to its imported asset. The previous
            // mesh-splitting pass produced a broken Mesh reference in Unity, which
            // made the whole aircraft invisible in the playable scene. We can add
            // a separate propeller visual after the base aircraft mesh is stable.
            Colorize(model);
            var flight = root.AddComponent<AircraftFlightController>();
            flight.SetVisualRoot(visuals.transform);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void Colorize(GameObject model)
        {
            var materials = new System.Collections.Generic.Dictionary<string, Material>();
            var red = LoadOrCreate("Aircraft_Red", new Color(0.87f, 0.025f, 0.10f));
            var blue = LoadOrCreate("Aircraft_Blue", new Color(0.015f, 0.19f, 0.56f));
            var yellow = LoadOrCreate("Aircraft_Yellow", new Color(1f, 0.70f, 0.02f));
            var navy = LoadOrCreate("Aircraft_Navy", new Color(0.015f, 0.045f, 0.12f));
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var source = renderer.sharedMaterials;
                var target = new Material[source.Length];
                for (int i = 0; i < source.Length; i++)
                {
                    string key = source[i] ? source[i].name.ToLowerInvariant() : "";
                    target[i] = key.Contains("white") ? blue : key.Contains("blue") ? red :
                        key.Contains("prop") || key.Contains("alu") || key.Contains("beacon") || key.Contains("navgreen") ? yellow :
                        key.Contains("glass") ? navy : blue;
                }
                renderer.sharedMaterials = target;
            }
        }

        private static Material LoadOrCreate(string name, Color color)
        {
            string path = "Assets/AirRace/Art/Materials/Blockout/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat) return mat;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader) { name = name, color = color };
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.18f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.28f);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static void BuildCan()
        {
            var root = new GameObject("Energy_Can_Blockout");
            var bodyMat = CreateMat("Can_Blue", new Color(0.01f, 0.28f, 0.72f));
            var redMat = CreateMat("Can_Red", new Color(0.9f, 0.025f, 0.1f));
            var yellowMat = CreateMat("Can_Yellow", new Color(1f, 0.72f, 0.01f));
            var glowMat = CreateEmissiveMat("Can_Ring_Glow", new Color(1f, 0.78f, 0.025f));
            EnsureFolder("Assets/AirRace/Art/Models/Aircraft/Generated");

            var canVisual = new GameObject("Can_Visual");
            canVisual.transform.SetParent(root.transform, false);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "LowPoly_Blue_Can";
            body.transform.SetParent(canVisual.transform, false);
            body.transform.localScale = new Vector3(1.5f, 2.5f, 1.5f);
            body.GetComponent<Renderer>().sharedMaterial = bodyMat;
            UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
            var stripe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stripe.name = "Red_Energy_Band";
            stripe.transform.SetParent(canVisual.transform, false);
            stripe.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            stripe.transform.localScale = new Vector3(1.52f, 0.32f, 1.52f);
            stripe.GetComponent<Renderer>().sharedMaterial = redMat;
            UnityEngine.Object.DestroyImmediate(stripe.GetComponent<Collider>());
            var top = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            top.name = "Yellow_Can_Top";
            top.transform.SetParent(canVisual.transform, false);
            top.transform.localPosition = new Vector3(0f, 2.56f, 0f);
            top.transform.localScale = new Vector3(1.05f, 0.14f, 1.05f);
            top.GetComponent<Renderer>().sharedMaterial = yellowMat;
            UnityEngine.Object.DestroyImmediate(top.GetComponent<Collider>());

            var ringMesh = CreateCanRingMesh();
            const string ringMeshPath = "Assets/AirRace/Art/Models/Aircraft/Generated/EnergyCan_Visibility_Ring.asset";
            AssetDatabase.DeleteAsset(ringMeshPath);
            AssetDatabase.CreateAsset(ringMesh, ringMeshPath);
            var ring = new GameObject("Energy_Visibility_Ring");
            ring.transform.SetParent(root.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0f, 0f);
            ring.AddComponent<MeshFilter>().sharedMesh = ringMesh;
            ring.AddComponent<MeshRenderer>().sharedMaterial = glowMat;

            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 6f;
            root.AddComponent<AirRaceCanPickup>().SetCanVisual(canVisual.transform);
            PrefabUtility.SaveAsPrefabAsset(root, CanPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static Mesh CreateCanRingMesh()
        {
            const int segments = 48;
            const int sides = 10;
            const float radius = 5.2f;
            const float tubeRadius = 0.36f;
            var vertices = new Vector3[segments * sides];
            var triangles = new int[segments * sides * 6];
            for (int i = 0; i < segments; i++)
            {
                float around = i * Mathf.PI * 2f / segments;
                Vector3 radial = new Vector3(Mathf.Cos(around), Mathf.Sin(around), 0f);
                Vector3 center = radial * radius;
                for (int j = 0; j < sides; j++)
                {
                    float tube = j * Mathf.PI * 2f / sides;
                    vertices[i * sides + j] = center + radial * (Mathf.Cos(tube) * tubeRadius) + Vector3.forward * (Mathf.Sin(tube) * tubeRadius);
                    int nextI = (i + 1) % segments;
                    int nextJ = (j + 1) % sides;
                    int index = (i * sides + j) * 6;
                    int a = i * sides + j;
                    int b = nextI * sides + j;
                    int c = nextI * sides + nextJ;
                    int d = i * sides + nextJ;
                    triangles[index] = a;
                    triangles[index + 1] = b;
                    triangles[index + 2] = c;
                    triangles[index + 3] = a;
                    triangles[index + 4] = c;
                    triangles[index + 5] = d;
                }
            }
            var mesh = new Mesh { name = "EnergyCan_Visibility_Ring" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Material CreateEmissiveMat(string name, Color color)
        {
            string path = "Assets/AirRace/Art/Materials/Blockout/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader glowShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (!material)
            {
                material = new Material(glowShader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (glowShader != null && material.shader != glowShader) material.shader = glowShader;
            material.color = color;
            material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color * 3.5f);
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.65f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateMat(string name, Color color)
        {
            string path = "Assets/AirRace/Art/Materials/Blockout/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { name = name, color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            var name = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
