using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AirRace.Tests
{
    public sealed class AirRaceCollisionPlayModeTests
    {
        private const string SceneName = "AirRace_Level01_Map";

        [UnityTest]
        public IEnumerator Aircraft_HitsRealPylon_ReboundsAndReturnsToCruise()
        {
            SceneManager.LoadScene(SceneName);
            yield return null;
            yield return new WaitForFixedUpdate();

            GameObject aircraft = GameObject.Find("Aircraft_Player");
            GameObject pylon = GameObject.Find("Pylon_01_Left");
            Assert.That(aircraft, Is.Not.Null);
            Assert.That(pylon, Is.Not.Null);

            var body = aircraft.GetComponent<Rigidbody>();
            var aircraftCollider = aircraft.GetComponent<BoxCollider>();
            var flightController = aircraft.GetComponents<MonoBehaviour>()
                .First(component => component.GetType().Name == "AircraftFlightController");
            var impactProperty = flightController.GetType().GetProperty("IsRecoveringFromImpact");
            var capsule = pylon.GetComponent<CapsuleCollider>();

            Assert.That(capsule, Is.Not.Null, "The pylon should expose one smooth capsule for the cone.");
            Assert.That(pylon.GetComponentsInChildren<MeshCollider>(true).All(collider => !collider.enabled), Is.True,
                "Imported band mesh colliders should be disabled during play.");
            BoxCollider raftCollider = pylon.GetComponent<BoxCollider>();
            Assert.That(raftCollider, Is.Not.Null, "The raft should use one box collider.");
            Assert.That(capsule.sharedMaterial, Is.Not.Null, "The smooth cone collider should use the low-friction material.");
            Assert.That(raftCollider.sharedMaterial, Is.Not.Null, "The raft collider should use the low-friction material.");
            Assert.That(capsule.sharedMaterial.dynamicFriction, Is.EqualTo(0f).Within(0.001f));
            Assert.That(raftCollider.sharedMaterial.staticFriction, Is.EqualTo(0f).Within(0.001f));

            foreach (Collider collider in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (collider.gameObject.name.StartsWith("Pylon_") && !collider.transform.IsChildOf(pylon.transform))
                    collider.enabled = false;
                if (collider.isTrigger && collider.gameObject.name.StartsWith("Checkpoint_"))
                    collider.enabled = false;
            }

            GameObject managerObject = GameObject.Find("AirRace_Run_Manager");
            MonoBehaviour manager = managerObject.GetComponents<MonoBehaviour>()
                .First(component => component.GetType().Name == "AirRaceRunManager");
            manager.enabled = false;
            PropertyInfo energyProperty = manager.GetType().GetProperty("Energy");
            float energyBefore = (float)energyProperty.GetValue(manager);

            Vector3 approach = pylon.transform.forward;
            approach.y = 0f;
            approach.Normalize();
            Bounds pylonBounds = capsule.bounds;
            float pylonSupport = Mathf.Abs(approach.x) * pylonBounds.extents.x + Mathf.Abs(approach.z) * pylonBounds.extents.z;
            float aircraftSupport = aircraftCollider.size.z * 0.5f;
            Vector3 start = pylonBounds.center - approach * (pylonSupport + aircraftSupport + 1f);
            start.y = pylonBounds.center.y;
            aircraft.transform.SetPositionAndRotation(start, Quaternion.LookRotation(approach, Vector3.up));
            body.position = start;
            body.rotation = aircraft.transform.rotation;
            body.linearVelocity = approach * 32f;
            body.angularVelocity = Vector3.zero;
            Physics.SyncTransforms();

            bool hitObserved = false;
            float elapsedUntilImpact = 0f;
            for (int i = 0; i < 100; i++)
            {
                yield return new WaitForFixedUpdate();
                elapsedUntilImpact += Time.fixedDeltaTime;
                if ((bool)impactProperty.GetValue(flightController))
                {
                    hitObserved = true;
                    break;
                }
            }

            Assert.That(hitObserved, Is.True, "Play Mode should deliver OnCollisionEnter for the pylon hit.");
            yield return new WaitForFixedUpdate();
            Vector3 reboundVelocity = body.linearVelocity;
            Assert.That(Vector3.Dot(reboundVelocity, -approach), Is.GreaterThan(8f), "The plane should move away from the pylon.");
            Vector3 pylonCenter = pylonBounds.center;
            float distanceAtRebound = Vector3.Distance(aircraft.transform.position, pylonCenter);
            float energyAfterHit = (float)energyProperty.GetValue(manager);
            Assert.That(energyAfterHit, Is.EqualTo(energyBefore - 18f).Within(0.1f), "One impact should deduct energy once.");

            yield return new WaitForSeconds(1.2f);
            Assert.That((bool)impactProperty.GetValue(flightController), Is.False, "Recovery should finish.");
            Assert.That(body.linearVelocity.magnitude, Is.EqualTo(32f).Within(1.5f), "Cruise should resume after rebound.");
            float finalDistance = Vector3.Distance(aircraft.transform.position, pylonCenter);
            Assert.That(finalDistance, Is.GreaterThan(distanceAtRebound + 5f),
                "The aircraft should leave the pylon instead of staying stuck against it.");
            Debug.Log($"AIR_RACE_PLAYMODE_IMPACT: detected={elapsedUntilImpact:0.00}s, " +
                      $"escapeSpeed={Vector3.Dot(reboundVelocity, -approach):0.00}m/s, " +
                      $"energyCost={energyBefore - energyAfterHit:0.00}, " +
                      $"distance={distanceAtRebound:0.00}->{finalDistance:0.00}m, " +
                      $"cruise={body.linearVelocity.magnitude:0.00}m/s");
        }

        [UnityTest]
        public IEnumerator ChaseCamera_UsesCenteredElevatedAngleAndSeesTheNextGate()
        {
            SceneManager.LoadScene(SceneName);
            yield return new WaitForSeconds(0.5f);

            Camera camera = Camera.main;
            GameObject aircraft = GameObject.Find("Aircraft_Player");
            GameObject nextGate = GameObject.Find("Checkpoint_01");
            Assert.That(camera, Is.Not.Null);
            Assert.That(aircraft, Is.Not.Null);
            Assert.That(nextGate, Is.Not.Null);

            Vector3 offset = camera.transform.position - aircraft.transform.position;
            Assert.That(Vector3.Dot(offset, -aircraft.transform.forward), Is.GreaterThan(8f), "Camera should stay behind the aircraft.");
            Assert.That(offset.y, Is.GreaterThan(2f), "Camera should be elevated above the aircraft.");
            Assert.That(camera.transform.forward.y, Is.LessThan(0f), "Camera should look slightly down toward the route.");
            Assert.That(Mathf.Abs(Vector3.Dot(offset, aircraft.transform.right)), Is.LessThan(1.5f),
                "The main view should be centered over the aircraft, not off to one side.");
            Assert.That(camera.transform.forward.y, Is.LessThan(-0.25f), "The main camera should read as a clearly elevated view.");

            Vector3 gateOnScreen = camera.WorldToViewportPoint(nextGate.transform.position);
            Assert.That(gateOnScreen.z, Is.GreaterThan(0f), "The next gate should be in front of the camera.");
            Assert.That(gateOnScreen.x, Is.InRange(0.05f, 0.95f), "The next gate should stay inside the horizontal frame.");
            Assert.That(gateOnScreen.y, Is.InRange(0.05f, 0.95f), "The next gate should stay inside the vertical frame.");
            Debug.Log($"AIR_RACE_CAMERA_QA: next gate viewport=({gateOnScreen.x:0.00}, {gateOnScreen.y:0.00}), " +
                      $"cameraOffset={offset}, directionY={camera.transform.forward.y:0.00}");
        }

        [UnityTest]
        public IEnumerator RaceHud_ShowsAndAdvancesCheckpointNumber()
        {
            SceneManager.LoadScene(SceneName);
            yield return null;
            yield return null;

            GameObject hudCanvas = GameObject.Find("AirRace_HUD_Canvas");
            GameObject checkpointLabel = GameObject.Find("CHECKPOINT_VALUE");
            Assert.That(hudCanvas, Is.Not.Null, "The temporary OnGUI display should be replaced by a screen-space HUD.");
            Assert.That(checkpointLabel, Is.Not.Null);
            Text checkpointText = checkpointLabel.GetComponent<Text>();
            Assert.That(checkpointText.text, Is.EqualTo("01  /  06"));

            GameObject managerObject = GameObject.Find("AirRace_Run_Manager");
            MonoBehaviour manager = managerObject.GetComponents<MonoBehaviour>()
                .First(component => component.GetType().Name == "AirRaceRunManager");
            manager.GetType().GetMethod("PassCheckpoint").Invoke(manager, new object[] { 0 });
            yield return null;
            Assert.That(checkpointText.text, Is.EqualTo("02  /  06"), "The HUD should display the next active checkpoint.");
        }

        [UnityTest]
        public IEnumerator EnergyCan_IsReadableAndEasyToCollect()
        {
            SceneManager.LoadScene(SceneName);
            yield return null;
            yield return null;

            GameObject pickup = GameObject.Find("Energy_Can_01");
            Assert.That(pickup, Is.Not.Null);
            SphereCollider pickupRange = pickup.GetComponent<SphereCollider>();
            Assert.That(pickupRange, Is.Not.Null);
            Assert.That(pickupRange.radius, Is.GreaterThanOrEqualTo(10f), "The collection radius should be forgiving.");
            Transform canVisual = pickup.transform.Find("Can_Visual");
            Transform ring = pickup.transform.Find("Energy_Visibility_Ring");
            Light beacon = pickup.GetComponentInChildren<Light>(true);
            Assert.That(canVisual.localScale.x, Is.GreaterThanOrEqualTo(1.6f), "The can should read larger than before.");
            Assert.That(ring, Is.Not.Null);
            Assert.That(beacon, Is.Not.Null);
            GameObject aircraft = GameObject.Find("Aircraft_Player");
            MonoBehaviour flightController = aircraft.GetComponents<MonoBehaviour>()
                .First(component => component.GetType().Name == "AircraftFlightController");
            flightController.enabled = false;
            Vector3 nearPosition = pickup.transform.position + Vector3.right * 35f;
            aircraft.GetComponent<Rigidbody>().position = nearPosition;
            aircraft.transform.position = nearPosition;
            Physics.SyncTransforms();
            yield return null;
            Assert.That(beacon.enabled, Is.True, "The beacon should turn on as the player approaches.");
            Assert.That(beacon.range, Is.GreaterThan(20f));
        }
    }
}
