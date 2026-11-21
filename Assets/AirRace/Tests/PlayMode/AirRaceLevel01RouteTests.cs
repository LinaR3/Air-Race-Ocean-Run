using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AirRace.Tests
{
    public sealed class AirRaceLevel01RouteTests
    {
        private const string SceneName = "AirRace_Level01_Map";

        [UnityTest]
        public IEnumerator Route_HasSixOrderedGatesWithLongerSpacingAndASeparateFinish()
        {
            SceneManager.LoadScene(SceneName);
            yield return null;

            GameObject[] gates = new GameObject[6];
            for (int i = 0; i < gates.Length; i++)
            {
                gates[i] = GameObject.Find("Checkpoint_" + (i + 1).ToString("00"));
                Assert.That(gates[i], Is.Not.Null, $"Checkpoint {i + 1} must exist.");
                Component checkpoint = gates[i].GetComponent("AirRaceCheckpoint");
                Assert.That(checkpoint, Is.Not.Null);
                object index = checkpoint.GetType().GetProperty("Index").GetValue(checkpoint);
                Assert.That(index, Is.EqualTo(i));
            }

            for (int i = 0; i < gates.Length; i++)
            {
                float distance = Vector3.Distance(gates[i].transform.position, gates[(i + 1) % gates.Length].transform.position);
                Assert.That(distance, Is.InRange(260f, 281f), $"Leg {i + 1} spacing should feel longer than the old 200 m route.");
            }

            GameObject finish = GameObject.Find("Finish_Marker_07");
            Assert.That(finish, Is.Not.Null, "The finish remains separate from the six checkpoint gates.");
            Vector3 finalLeg = gates[0].transform.position - gates[5].transform.position;
            float finishProgress = Vector3.Dot(finish.transform.position - gates[5].transform.position, finalLeg) / finalLeg.sqrMagnitude;
            float finishCrossTrack = Vector3.ProjectOnPlane(finish.transform.position - gates[5].transform.position, finalLeg.normalized).magnitude;
            Assert.That(finishProgress, Is.InRange(0.65f, 0.75f), "The finish must sit on the final route leg after checkpoint 6.");
            Assert.That(finishCrossTrack, Is.LessThan(1f), "The aircraft should not have to leave the route to find the finish trigger.");
            Assert.That(Vector3.Dot(finish.transform.forward, finalLeg.normalized), Is.GreaterThan(0.99f), "The finish gate must face the final route direction.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Aircraft_StartsOnTheFloatingPanelBeforeCheckpointOne()
        {
            SceneManager.LoadScene(SceneName);
            yield return null;

            GameObject plane = GameObject.Find("Aircraft_Player");
            GameObject panel = GameObject.Find("Start_Floating_Panel");
            GameObject firstGate = GameObject.Find("Checkpoint_01");
            Assert.That(plane, Is.Not.Null);
            Assert.That(panel, Is.Not.Null);
            Assert.That(firstGate, Is.Not.Null);

            Transform deck = panel.transform.Find("Deck");
            Assert.That(deck, Is.Not.Null);
            float deckTop = deck.position.y + deck.localScale.y * 0.5f;
            Assert.That(deckTop, Is.GreaterThan(-4.5f), "The floating deck should sit above the ocean surface.");
            Assert.That(plane.transform.position.y, Is.GreaterThan(deckTop));
            Assert.That(plane.transform.position.y, Is.LessThan(deckTop + 1.5f), "The aircraft should start close above the panel, as if resting on it.");
            Assert.That(Vector3.Distance(plane.transform.position, firstGate.transform.position), Is.InRange(118f, 122f));
            yield return null;
        }
    }
}
