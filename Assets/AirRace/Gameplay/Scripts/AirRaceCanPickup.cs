using UnityEngine;

namespace AirRace.Gameplay
{
    public sealed class AirRaceCanPickup : MonoBehaviour
    {
        [SerializeField] private float bobHeight = 0.35f;
        [SerializeField] private float bobSpeed = 1.4f;
        [SerializeField] private float spinSpeed = 70f;
        [SerializeField] private Transform canVisual;
        private Vector3 startPosition;

        public void SetCanVisual(Transform visual) => canVisual = visual;

        private void Start() => startPosition = transform.position;

        private void Update()
        {
            if (canVisual != null) canVisual.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
            transform.position = startPosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed + startPosition.x) * bobHeight);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.GetComponentInParent<AircraftFlightController>()) return;
            if (AirRaceRunManager.Instance == null) return;
            AirRaceRunManager.Instance.CollectCan();
            Destroy(gameObject);
        }
    }
}
