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
        private const string PylonPath = Root + "/Art/Models/Pylons/AirRace_Pylon_SwimRaft.prefab";
        private const string AircraftPath = Root + "/Gameplay/Prefabs/Aircraft_Player.prefab";
        private const string CanPath = Root + "/Gameplay/Prefabs/Energy_Can_Blockout.prefab";

        [MenuItem("Air Race/Build Level 01 Map")]
        public static void BuildMap()
        {
            EnsureFolders();
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

        private static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "AirRace_Level01_Map";
            const string waterPath = Root + "/ThirdParty/AURW/AURW/Resources/Prefabs/Water100x100.prefab";
            var waterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(waterPath);
            if (waterPrefab != null)
            {
                var sea = (GameObject)PrefabUtility.InstantiatePrefab(waterPrefab, scene);
                sea.name = "AURW_Ocean_Play_Area_4800m";
                sea.transform.position = new Vector3(0f, -4.5f, 0f);
                // 4.8 km square ocean; the invisible play boundary is at 900 m.
                // The plane's far edge remains beyond the camera's far clip distance.
                sea.transform.localScale = new Vector3(48f, 1f, 48f);
            }
            else
            {
                var sea = GameObject.CreatePrimitive(PrimitiveType.Plane);
                sea.name = "Ocean_Surface_Blockout";
                sea.transform.position = new Vector3(0f, -4.5f, 0f);
                sea.transform.localScale = new Vector3(480f, 1f, 480f);
                sea.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Ocean_Teal_Blockout", new Color(0.025f, 0.34f, 0.47f), 0.72f);
            }
            const string skyPath = Root + "/ThirdParty/SkyboxSampler/SkyboxSampler/StylizedSky/Materials/StylizedSky_02_GoldenHour.mat";
            var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if (sky != null) RenderSettings.skybox = sky;

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
            if (pylonPrefab == null) throw new InvalidOperationException("Approved pylon + swim raft prefab not found.");
            var aircraftPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AircraftPath);
            if (aircraftPrefab == null) throw new InvalidOperationException("Build the aircraft prefab first from Air Race > Build Aircraft And Energy Can Prefabs.");
            var canPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CanPath);
            var gatePositions = new Vector3[] {
                new Vector3(0f, 14f, 200f), new Vector3(180f, 19f, 100f),
                new Vector3(180f, 15f, -100f), new Vector3(0f, 20f, -200f),
                new Vector3(-180f, 16f, -100f), new Vector3(-180f, 19f, 100f)
            };
            for (int i = 0; i < gatePositions.Length; i++)
            {
                var gate = new GameObject("Checkpoint_" + (i + 1).ToString("00"));
                gate.transform.SetParent(gateRoot.transform, false);
                gate.transform.position = gatePositions[i];
                Vector3 previous = gatePositions[(i + gatePositions.Length - 1) % gatePositions.Length];
                Vector3 next = gatePositions[(i + 1) % gatePositions.Length];
                Vector3 courseForward = Vector3.ProjectOnPlane(next - previous, Vector3.up).normalized;
                gate.transform.rotation = Quaternion.LookRotation(courseForward, Vector3.up);
                gate.tag = "Untagged";
                var checkpoint = gate.AddComponent<AirRace.Gameplay.AirRaceCheckpoint>();
                checkpoint.Initialize(i);
                var gateTrigger = gate.AddComponent<BoxCollider>();
                gateTrigger.isTrigger = true;
                gateTrigger.size = new Vector3(24f, 20f, 12f);
                for (int side = -1; side <= 1; side += 2)
                {
                    GameObject pylon = (GameObject)PrefabUtility.InstantiatePrefab(pylonPrefab, scene);
                    pylon.name = "Pylon_" + (i + 1).ToString("00") + (side < 0 ? "_Left" : "_Right");
                    pylon.transform.SetParent(gate.transform, false);
                    pylon.transform.localPosition = new Vector3(side * 12f, -gatePositions[i].y - 0.2f, 0f);
                    pylon.transform.localRotation = Quaternion.identity;
                }
            }

            var finish = new GameObject("Finish_Marker_07");
            finish.transform.SetParent(courseRoot.transform, false);
            Vector3 startDirection = Vector3.ProjectOnPlane(gatePositions[1] - gatePositions[gatePositions.Length - 1], Vector3.up).normalized;
            finish.transform.position = gatePositions[0] + startDirection * 48f;
            finish.transform.position = new Vector3(finish.transform.position.x, gatePositions[0].y, finish.transform.position.z);
            finish.transform.rotation = Quaternion.LookRotation(startDirection, Vector3.up);
            finish.AddComponent<AirRace.Gameplay.AirRaceFinishGate>();
            var finishTrigger = finish.AddComponent<BoxCollider>();
            finishTrigger.isTrigger = true;
            finishTrigger.size = new Vector3(28f, 20f, 12f);
            var finishLeft = (GameObject)PrefabUtility.InstantiatePrefab(pylonPrefab, scene);
            finishLeft.name = "Finish_Pylon_Left";
            finishLeft.transform.SetParent(finish.transform, false);
            finishLeft.transform.localPosition = new Vector3(-14f, -14.2f, 0f);
            var finishRight = (GameObject)PrefabUtility.InstantiatePrefab(pylonPrefab, scene);
            finishRight.name = "Finish_Pylon_Right";
            finishRight.transform.SetParent(finish.transform, false);
            finishRight.transform.localPosition = new Vector3(14f, -14.2f, 0f);
            var finishGold = MakeMaterial("Finish_Pylon_Gold", new Color(1f, 0.68f, 0.04f));
            ColorizeFinishPylon(finishLeft, finishGold);
            ColorizeFinishPylon(finishRight, finishGold);

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

            var manager = new GameObject("AirRace_Run_Manager");
            manager.transform.SetParent(courseRoot.transform, false);
            manager.AddComponent<AirRace.Gameplay.AirRaceRunManager>();

            var plane = (GameObject)PrefabUtility.InstantiatePrefab(aircraftPrefab, scene);
            plane.name = "Aircraft_Player";
            plane.transform.position = gatePositions[0] - startDirection * 72f;
            plane.transform.position = new Vector3(plane.transform.position.x, gatePositions[0].y, plane.transform.position.z);
            plane.transform.rotation = Quaternion.LookRotation(startDirection, Vector3.up);

            if (canPrefab != null)
            {
                var pickupRoot = new GameObject("Energy_Can_Pickups");
                pickupRoot.transform.SetParent(courseRoot.transform, false);
                for (int i = 0; i < gatePositions.Length; i++)
                {
                    Vector3 next = gatePositions[(i + 1) % gatePositions.Length];
                    Vector3 pickupDirection = Vector3.ProjectOnPlane(next - gatePositions[i], Vector3.up).normalized;
                    Vector3 pos = Vector3.Lerp(gatePositions[i], next, 0.5f) + Vector3.up * 7f;
                    var pickup = (GameObject)PrefabUtility.InstantiatePrefab(canPrefab, scene);
                    pickup.name = "Energy_Can_" + (i + 1).ToString("00");
                    pickup.transform.SetParent(pickupRoot.transform, true);
                    pickup.transform.position = pos;
                    pickup.transform.rotation = Quaternion.LookRotation(pickupDirection, Vector3.up);
                }
            }

            var chaseCamObject = new GameObject("AirRace_Chase_Camera");
            chaseCamObject.tag = "MainCamera";
            var chaseCamera = chaseCamObject.AddComponent<Camera>();
            chaseCamera.fieldOfView = 58f;
            chaseCamera.nearClipPlane = 0.1f;
            // The ocean spans 4.8 km, but keep its edge outside the camera range
            // and fog horizon so the playable world never reveals a square cutoff.
            chaseCamera.farClipPlane = 1400f;
            var cameraRig = chaseCamObject.AddComponent<AirRace.Gameplay.AirRaceCameraRig>();
            cameraRig.SetTarget(plane.transform);
            var chaseOffset = new Vector3(-5f, 4.5f, -16f);
            var alternateOffset = new Vector3(-2.5f, 3f, -11f);
            cameraRig.SetFraming(chaseOffset, alternateOffset, 3.8f, 0.65f);
            chaseCamObject.transform.position = plane.transform.TransformPoint(chaseOffset);
            chaseCamObject.transform.LookAt(plane.transform.position + plane.transform.forward * 7f + Vector3.up * 0.65f);

            var mapCam = new GameObject("Map_Preview_Camera").AddComponent<Camera>();
            mapCam.name = "Map_Preview_Camera";
            // Keep the overhead authoring camera out of runtime rendering. If both
            // cameras target Display 1, this later camera overlays the chase view
            // and can make the player aircraft appear to vanish.
            mapCam.enabled = false;
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

        private static void ColorizeFinishPylon(GameObject pylon, Material gold)
        {
            foreach (var renderer in pylon.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.gameObject.name.Contains("Pylon")) continue;
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++) materials[i] = gold;
                renderer.sharedMaterials = materials;
            }
        }
    }
}
