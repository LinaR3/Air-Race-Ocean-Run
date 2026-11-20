using System.IO;
using AirRace.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AirRace.EditorTools
{
    public static class AirRaceLevel01RouteUpdater
    {
        private const string ScenePath = "Assets/AirRace/Scenes/Map/AirRace_Level01_Map.unity";
        private const string RoutePath = "Assets/AirRace/Gameplay/Route/RouteLayout.json";
        private const float WaterSurfaceY = -4.5f;
        private const float StartToFirstGateMeters = 120f;
        private static readonly Vector3[] GatePositions =
        {
            new Vector3(0f, 14f, 270f),
            new Vector3(243f, 19f, 135f),
            new Vector3(243f, 15f, -135f),
            new Vector3(0f, 20f, -270f),
            new Vector3(-243f, 16f, -135f),
            new Vector3(-243f, 19f, 135f)
        };

        [MenuItem("Air Race/Update Level 01 Route And Start Platform")]
        public static void UpdateRouteAndStartPlatform()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            UpdateGates();
            UpdateFinish();
            UpdateCanPickups();
            CreateStartPlatform();
            UpdateAircraftStart();
            UpdateChaseCameraStart();
            WriteRouteLayout();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.Refresh();
            Debug.Log("Updated Level 01 to six wider-spaced gates and a floating start platform.");
        }

        private static void UpdateGates()
        {
            for (int i = 0; i < GatePositions.Length; i++)
            {
                GameObject gate = GameObject.Find("Checkpoint_" + (i + 1).ToString("00"));
                if (gate == null) throw new MissingReferenceException("Missing checkpoint: " + (i + 1));
                Vector3 previous = GatePositions[(i + GatePositions.Length - 1) % GatePositions.Length];
                Vector3 next = GatePositions[(i + 1) % GatePositions.Length];
                Vector3 heading = Vector3.ProjectOnPlane(next - previous, Vector3.up).normalized;
                gate.transform.SetPositionAndRotation(GatePositions[i], Quaternion.LookRotation(heading, Vector3.up));
                EditorUtility.SetDirty(gate.transform);
            }
        }

        private static void UpdateFinish()
        {
            GameObject finish = GameObject.Find("Finish_Marker_07");
            if (finish == null) throw new MissingReferenceException("Missing the seventh finish marker.");
            Vector3 heading = Vector3.ProjectOnPlane(GatePositions[1] - GatePositions[GatePositions.Length - 1], Vector3.up).normalized;
            Vector3 position = GatePositions[0] + heading * 48f;
            finish.transform.SetPositionAndRotation(position, Quaternion.LookRotation(heading, Vector3.up));
            EditorUtility.SetDirty(finish.transform);
        }

        private static void UpdateCanPickups()
        {
            for (int i = 0; i < GatePositions.Length; i++)
            {
                GameObject pickup = GameObject.Find("Energy_Can_" + (i + 1).ToString("00"));
                if (pickup == null) continue;
                Vector3 next = GatePositions[(i + 1) % GatePositions.Length];
                pickup.transform.position = Vector3.Lerp(GatePositions[i], next, 0.52f) + Vector3.up * 2.5f;
                EditorUtility.SetDirty(pickup.transform);
            }
        }

        private static void UpdateAircraftStart()
        {
            GameObject aircraft = GameObject.Find("Aircraft_Player");
            if (aircraft == null) throw new MissingReferenceException("Missing Aircraft_Player in the route scene.");
            Vector3 heading = Vector3.ProjectOnPlane(GatePositions[1] - GatePositions[GatePositions.Length - 1], Vector3.up).normalized;
            Vector3 start = GatePositions[0] - heading * StartToFirstGateMeters;
            start.y = WaterSurfaceY + 2.1f;
            aircraft.transform.SetPositionAndRotation(start, Quaternion.LookRotation(heading, Vector3.up));
            Rigidbody body = aircraft.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.position = start;
                body.rotation = aircraft.transform.rotation;
                EditorUtility.SetDirty(body);
            }
            EditorUtility.SetDirty(aircraft.transform);
        }

        private static void CreateStartPlatform()
        {
            const string rootName = "Start_Floating_Panel";
            GameObject oldRoot = GameObject.Find(rootName);
            if (oldRoot != null) Object.DestroyImmediate(oldRoot);

            Material navy = LoadMaterial("Pylon_Float_Navy.mat", new Color(0.015f, 0.045f, 0.12f));
            Material yellow = LoadMaterial("Aircraft_Yellow.mat", new Color(1f, 0.70f, 0.02f));
            Material red = LoadMaterial("Aircraft_Red.mat", new Color(0.87f, 0.025f, 0.10f));
            Material blue = LoadMaterial("Aircraft_Blue.mat", new Color(0.015f, 0.19f, 0.56f));

            Vector3 heading = Vector3.ProjectOnPlane(GatePositions[1] - GatePositions[GatePositions.Length - 1], Vector3.up).normalized;
            Vector3 center = GatePositions[0] - heading * StartToFirstGateMeters;
            center.y = WaterSurfaceY;
            GameObject root = new GameObject(rootName);
            root.transform.SetPositionAndRotation(center, Quaternion.LookRotation(heading, Vector3.up));

            Part("Deck", root.transform, PrimitiveType.Cube, new Vector3(0f, 0.88f, 0f), new Vector3(25f, 0.7f, 82f), navy);
            Part("Runway_Centerline", root.transform, PrimitiveType.Cube, new Vector3(0f, 1.245f, 0f), new Vector3(0.45f, 0.06f, 66f), yellow);
            Part("Left_Edge_Marking", root.transform, PrimitiveType.Cube, new Vector3(-11.9f, 1.245f, 0f), new Vector3(0.35f, 0.06f, 78f), yellow);
            Part("Right_Edge_Marking", root.transform, PrimitiveType.Cube, new Vector3(11.9f, 1.245f, 0f), new Vector3(0.35f, 0.06f, 78f), yellow);
            Part("Nose_Marking", root.transform, PrimitiveType.Cube, new Vector3(0f, 1.25f, 31f), new Vector3(21f, 0.08f, 9f), red);
            Part("Nose_Blue_Inset", root.transform, PrimitiveType.Cube, new Vector3(0f, 1.30f, 24f), new Vector3(4.2f, 0.12f, 2.3f), blue);

            for (int side = -1; side <= 1; side += 2)
            {
                Part("Floating_Pontoon_" + side, root.transform, PrimitiveType.Cylinder,
                    new Vector3(side * 9.4f, -0.25f, 0f), new Vector3(3.2f, 39f, 3.2f), blue,
                    Quaternion.Euler(90f, 0f, 0f));
            }
            for (int beam = -1; beam <= 1; beam++)
                Part("Crossbeam_" + beam, root.transform, PrimitiveType.Cube,
                    new Vector3(0f, 0.18f, beam * 26f), new Vector3(7f, 0.8f, 2.2f), red);

            // These are visual set dressing: collision remains on the pylons, not on the start deck.
            foreach (Collider collider in root.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
            EditorUtility.SetDirty(root);
        }

        private static void UpdateChaseCameraStart()
        {
            GameObject cameraObject = GameObject.Find("AirRace_Chase_Camera");
            GameObject aircraft = GameObject.Find("Aircraft_Player");
            if (cameraObject == null || aircraft == null) return;

            Quaternion heading = Quaternion.Euler(0f, aircraft.transform.eulerAngles.y, 0f);
            Vector3 offset = heading * new Vector3(0f, 4.2f, -14f);
            cameraObject.transform.position = aircraft.transform.position + offset;
            Vector3 lookPoint = aircraft.transform.position + heading * Vector3.forward * 30f + Vector3.up * 2.2f;
            cameraObject.transform.rotation = Quaternion.LookRotation(lookPoint - cameraObject.transform.position, Vector3.up);
            EditorUtility.SetDirty(cameraObject.transform);
        }

        private static void Part(string name, Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Quaternion? rotation = null)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localRotation = rotation ?? Quaternion.identity;
            part.transform.localScale = scale;
            Renderer renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            Object.DestroyImmediate(part.GetComponent<Collider>());
        }

        private static Material LoadMaterial(string fileName, Color fallback)
        {
            string path = "Assets/AirRace/Art/Materials/Blockout/" + fileName;
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            return new Material(shader) { name = "Start_" + Path.GetFileNameWithoutExtension(fileName), color = fallback };
        }

        private static void WriteRouteLayout()
        {
            float[] distances = new float[GatePositions.Length];
            for (int i = 0; i < GatePositions.Length; i++)
                distances[i] = Vector3.Distance(GatePositions[i], GatePositions[(i + 1) % GatePositions.Length]);
            string positions = string.Join(", ", System.Array.ConvertAll(GatePositions,
                p => $"[{p.x:0}, {p.y:0}, {p.z:0}]"));
            string lengths = string.Join(", ", System.Array.ConvertAll(distances, d => d.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)));
            string json = "{\n" +
                "  \"level\": \"Level 01\",\n" +
                "  \"durationSeconds\": 60,\n" +
                "  \"route\": \"Closed broad oval loop over open ocean\",\n" +
                "  \"checkpoints\": 6,\n" +
                "  \"finish\": 1,\n" +
                "  \"gatePairSpacingMeters\": 24,\n" +
                "  \"startPlatform\": \"Floating runway panel; aircraft starts 120 m before checkpoint 1\",\n" +
                "  \"checkpointCenterPositionsMeters\": [" + positions + "],\n" +
                "  \"checkpointSegmentDistancesMeters\": [" + lengths + "],\n" +
                "  \"checkpointCenterSpacingMeters\": \"270-278; about 7.9-8.2 seconds at 34 m/s cruise speed\",\n" +
                "  \"oceanSizeMeters\": \"4800 x 4800 square\",\n" +
                "  \"invisibleFlightBoundaryMeters\": \"900 from world origin on X and Z\",\n" +
                "  \"pylonHeightMeters\": 25,\n" +
                "  \"finishMarker\": \"Gold paired pylons near checkpoint 1\",\n" +
                "  \"energyCanVisibility\": \"5 m tall can inside a 10.4 m diameter double-sided bright yellow-orange emissive ring; pickup trigger radius 10 m\",\n" +
                "  \"designNote\": \"Six ordered paired pylon gates form a broad closed 1.65 km oval. A floating start panel sits before checkpoint 1; the total nominal distance from start to finish is about 1.77 km, leaving a small handling margin within the existing 60 second limit.\"\n" +
                "}\n";
            File.WriteAllText(RoutePath, json);
        }
    }
}
