using UnityEngine;

namespace AirRace.Gameplay
{
    public sealed class AirRaceFinishGate : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (!other.GetComponentInParent<AircraftFlightController>()) return;
            if (AirRaceRunManager.Instance != null) AirRaceRunManager.Instance.TryFinish();
        }
    }
}
