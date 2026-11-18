using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace AirRace.EditorTools
{
    public static class AirRaceSceneBuilder
    {
        private const string Root = "Assets/AirRace";
        private const string ScenePath = Root + "/Scenes/Map/AirRace_Level01_Map.unity";
        private const string PylonPath = Root + "/Art/Models/Pylons/AirRace_Pylon_Blockout.prefab";
        private const string FloatPath = Root + "/Art/Models/Floats/SwimRaft/swim-raft-6aefb5.fbx";

        [MenuItem("Air Race/Build Level 01 Map")]
        public static void BuildMap()
        {
            EnsureFolders();
            BuildPylonPrefab();
            BuildScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Air Race Level 01 map scene generated.");
        }

        private static void EnsureFolders()
        {
            string[] folders = {
                Root + "/Editor",
                Root + "/Scenes/Map",
                Root + "/Scenes/Presentation",
                Root + "/Art/Materials/Blockout",
                Root + "/Art/Models/Pylons",
                Root + "/Gameplay/Route"
            };
            foreach (string folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
                    string name = Path.GetFileName(folder);
                    if (AssetDatabase.IsValidFolder(parent)) AssetDatabase.CreateFolder(parent, name);
                }
            }
        }

        private static Material MakeMaterial(string name, Color color, float smoothness = 0.1f)
        {
            string path = Root + "/Art/Materials/Blockout/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void BuildPylonPrefab()
        {
            Material red = MakeMaterial("Pylon_Race_Red", new Color(0.86f, 0.08f, 0.06f));
            Material white = MakeMaterial("Pylon_Race_White", new Color(0.91f, 0.94f, 0.92f));
            Material yellow = MakeMaterial("Pylon_Race_Yellow", new Color(1f, 0.66f, 0.05f));
            Material floatMat = MakeMaterial("Pylon_Float_Navy", new Color(0.02f, 0.13f, 0.25f));

            var root = new GameObject("AirRace_Pylon_Blockout");
            var floatBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floatBase.name = "Inflatable_Base_Blockout";
            floatBase.transform.SetParent(root.transform, false);
            floatBase.transform.localPosition = new Vector3(0f, 0.24f, 0f);
            floatBase.transform.localScale = new Vector3(3.1f, 0.24f, 2.1f);
            floatBase.GetComponent<Renderer>().sharedMaterial = floatMat;

            GameObject lower = CreateConeSegment(root.transform, "Lower_White_Segment", 0.75f, 0.72f, 1.7f, white);
            GameObject upper = CreateConeSegment(root.transform, "Upper_Red_Segment", 0.5f, 0.4f, 1.8f, red);
            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cap.name = "Yellow_Cap";
            cap.transform.SetParent(root.transform, false);
            cap.transform.localPosition = new Vector3(0f, 5.25f, 0f);
            cap.transform.localScale = new Vector3(0.48f, 0.12f, 0.48f);
            cap.GetComponent<Renderer>().sharedMaterial = yellow;

            var band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            band.name = "Red_White_Seam";
            band.transform.SetParent(root.transform, false);
            band.transform.localPosition = new Vector3(0f, 3.5f, 0f);
            band.transform.localScale = new Vector3(1.14f, 0.06f, 1.14f);
            band.GetComponent<Renderer>().sharedMaterial = red;

            foreach (Collider c in root.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PylonPath);
            UnityEngine.Object.DestroyImmediate(root);
            if (prefab == null) throw new InvalidOperationException("Could not save pylon prefab.");
        }

        private static GameObject CreateConeSegment(Transform parent, string name, float bottomRadius, float topRadius, float height, Material material)
        {
            var cone = new GameObject(name);
            cone.transform.SetParent(parent, false);
            cone.transform.localPosition = new Vector3(0f, 0.48f + height * 0.5f, 0f);
            var mesh = BuildFrustum(bottomRadius, topRadius, height, 24);
            var filter = cone.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = cone.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return cone;
        }

        private static Mesh BuildFrustum(float bottomRadius, float topRadius, float height, int segments)
        {
            var vertices = new Vector3[segments * 2 + 2];
            var triangles = new int[segments * 12];
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                float x = Mathf.Cos(a), z = Mathf.Sin(a);
                vertices[i] = new Vector3(x * bottomRadius, 0f, z * bottomRadius);
                vertices[segments + i] = new Vector3(x * topRadius, height, z * topRadius);
            }
            int bottomCenter = segments * 2, topCenter = bottomCenter + 1;
            vertices[bottomCenter] = Vector3.zero;
            vertices[topCenter] = new Vector3(0f, height, 0f);
            int t = 0;
            for (int i = 0; i < segments; i++)
            {
                int n = (i + 1) % segments;
                triangles[t++] = i; triangles[t++] = n; triangles[t++] = segments + i;
                triangles[t++] = n; triangles[t++] = segments + n; triangles[t++] = segments + i;
                triangles[t++] = bottomCenter; triangles[t++] = n; triangles[t++] = i;
                triangles[t++] = topCenter; triangles[t++] = segments + i; triangles[t++] = segments + n;
            }
            var mesh = new Mesh { name = "Smooth_Tapered_Pylon_Blockout" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "AirRace_Level01_Map";
            var oceanMat = MakeMaterial("Ocean_Teal_Blockout", new Color(0.025f, 0.34f, 0.47f), 0.72f);
            var sea = GameObject.CreatePrimitive(PrimitiveType.Plane);
            sea.name = "Ocean_Surface_Blockout";
            sea.transform.position = new Vector3(0f, -4.5f, 0f);
            sea.transform.localScale = new Vector3(220f, 1f, 220f);
            sea.GetComponent<Renderer>().sharedMaterial = oceanMat;
            sea.layer = LayerMask.NameToLayer("Water");

            var lighting = new GameObject("Lighting");
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(lighting.transform, false);
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.88f, 0.72f);
            sun.intensity = 1.35f;
            sun.transform.rotation = Quaternion.Euler(38f, -28f, 0f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.37f, 0.65f, 0.82f);
            RenderSettings.ambientEquatorColor = new Color(0.22f, 0.46f, 0.53f);
            RenderSettings.ambientGroundColor = new Color(0.08f, 0.21f, 0.25f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.39f, 0.71f, 0.84f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 220f;
            RenderSettings.fogEndDistance = 1100f;

            var courseRoot = new GameObject("Race_Course");
            var gateRoot = new GameObject("Checkpoint_Gates_01_to_06");
            gateRoot.transform.SetParent(courseRoot.transform, false);
            var pylonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PylonPath);
            if (pylonPrefab == null) throw new InvalidOperationException("Pylon prefab not found.");
            var gatePositions = new Vector3[] {
                new Vector3(0f, 14f, 82f), new Vector3(96f, 22f, 66f),
                new Vector3(142f, 16f, 0f), new Vector3(84f, 26f, -84f),
                new Vector3(-20f, 18f, -112f), new Vector3(-130f, 24f, -24f)
            };
            var forwardDirs = new Vector3[] { Vector3.right, Vector3.forward, Vector3.back, Vector3.left, Vector3.left, Vector3.forward };
            for (int i = 0; i < gatePositions.Length; i++)
            {
                var gate = new GameObject("Checkpoint_" + (i + 1).ToString("00"));
                gate.transform.SetParent(gateRoot.transform, false);
                gate.transform.position = gatePositions[i];
                gate.transform.rotation = Quaternion.LookRotation(forwardDirs[i], Vector3.up);
                gate.tag = "Untagged";
                gate.AddComponent<BoxCollider>().isTrigger = true;
                gate.GetComponent<BoxCollider>().size = new Vector3(18f, 8f, 4f);
                gate.GetComponent<BoxCollider>().enabled = false;
                for (int side = -1; side <= 1; side += 2)
                {
                    GameObject pylon = (GameObject)PrefabUtility.InstantiatePrefab(pylonPrefab, scene);
                    pylon.name = "Pylon_" + (i + 1).ToString("00") + (side < 0 ? "_Left" : "_Right");
                    pylon.transform.SetParent(gate.transform, false);
                    pylon.transform.localPosition = new Vector3(side * 9f, -9f, 0f);
                    pylon.transform.localRotation = Quaternion.identity;
                }
            }

            var finish = new GameObject("Finish_Marker_07");
            finish.transform.SetParent(courseRoot.transform, false);
            finish.transform.position = new Vector3(8f, 15f, 88f);
            finish.transform.rotation = Quaternion.LookRotation(Vector3.right);
            var finishLeft = (GameObject)PrefabUtility.InstantiatePrefab(pylonPrefab, scene);
            finishLeft.name = "Finish_Pylon_Left";
            finishLeft.transform.SetParent(finish.transform, false);
            finishLeft.transform.localPosition = new Vector3(-11f, -9f, 0f);
            var finishRight = (GameObject)PrefabUtility.InstantiatePrefab(pylonPrefab, scene);
            finishRight.name = "Finish_Pylon_Right";
            finishRight.transform.SetParent(finish.transform, false);
            finishRight.transform.localPosition = new Vector3(11f, -9f, 0f);
            finishLeft.transform.GetChild(1).GetComponent<Renderer>().sharedMaterial = MakeMaterial("Finish_Gold", new Color(1f, 0.72f, 0.05f));
            finishRight.transform.GetChild(1).GetComponent<Renderer>().sharedMaterial = MakeMaterial("Finish_Gold", new Color(1f, 0.72f, 0.05f));

            var routeLine = new GameObject("Route_Layout_Gizmos");
            routeLine.transform.SetParent(courseRoot.transform, false);
            var line = routeLine.AddComponent<LineRenderer>();
            line.positionCount = gatePositions.Length + 1;
            for (int i = 0; i < gatePositions.Length; i++) line.SetPosition(i, gatePositions[i] + Vector3.up * 2f);
            line.SetPosition(gatePositions.Length, gatePositions[0] + Vector3.up * 2f);
            line.widthMultiplier = 0.45f;
            line.useWorldSpace = true;
            line.sharedMaterial = MakeMaterial("Route_Layout_Gold", new Color(1f, 0.62f, 0.07f));
            line.enabled = false;

            var mapCam = new GameObject("Map_Preview_Camera").AddComponent<Camera>();
            mapCam.name = "Map_Preview_Camera";
            mapCam.transform.position = new Vector3(15f, 260f, -5f);
            mapCam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            mapCam.orthographic = true;
            mapCam.orthographicSize = 195f;
            mapCam.enabled = false;
            mapCam.clearFlags = CameraClearFlags.SolidColor;
            mapCam.backgroundColor = RenderSettings.fogColor;

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Selection.activeGameObject = courseRoot;
        }
    }
}
