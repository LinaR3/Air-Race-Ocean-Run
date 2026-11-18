using UnityEngine;

namespace AirRace.Gameplay
{
    public sealed class AirRaceCheckpoint : MonoBehaviour
    {
        [SerializeField] private int checkpointIndex;
        private bool completed;

        public void Initialize(int index) => checkpointIndex = index;

        private void OnTriggerEnter(Collider other)
        {
            if (completed || !other.GetComponentInParent<AircraftFlightController>()) return;
            if (AirRaceRunManager.Instance != null && AirRaceRunManager.Instance.PassCheckpoint(checkpointIndex))
                completed = true;
        }
    }

}
