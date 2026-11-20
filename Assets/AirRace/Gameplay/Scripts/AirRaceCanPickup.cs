using UnityEngine;

namespace AirRace.Gameplay
{
    public sealed class AirRaceCanPickup : MonoBehaviour
    {
        [SerializeField] private float bobHeight = 0.35f;
        [SerializeField] private float bobSpeed = 1.4f;
        [SerializeField] private float spinSpeed = 70f;
        [SerializeField] private float revealDistance = 260f;
        [SerializeField] private Transform canVisual;
        private Vector3 startPosition;
        private Transform ringVisual;
        private Renderer ringRenderer;
        private Light revealLight;
        private AircraftFlightController aircraft;
        private MaterialPropertyBlock glowProperties;

        public void SetCanVisual(Transform visual) => canVisual = visual;

        private void Start()
        {
            startPosition = transform.position;
            ringVisual = transform.Find("Energy_Visibility_Ring");
            if (ringVisual != null) ringRenderer = ringVisual.GetComponent<Renderer>();
            revealLight = GetComponentInChildren<Light>();
            if (revealLight != null) revealLight.enabled = false;
            aircraft = FindFirstObjectByType<AircraftFlightController>();
            glowProperties = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (canVisual != null) canVisual.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
            transform.position = startPosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed + startPosition.x) * bobHeight);

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 3.2f);
            if (ringVisual != null)
            {
                float ringScale = 0.96f + pulse * 0.08f;
                ringVisual.localScale = Vector3.one * ringScale;
            }

            float proximity = 0f;
            if (aircraft != null)
            {
                float distance = Vector3.Distance(aircraft.transform.position, transform.position);
                proximity = Mathf.Clamp01(1f - distance / revealDistance);
            }
            if (revealLight != null)
            {
                revealLight.enabled = proximity > 0f;
                revealLight.intensity = 2f + pulse * 2f + proximity * 5f;
                revealLight.range = 32f + proximity * 38f;
            }

            if (ringRenderer != null)
            {
                ringRenderer.GetPropertyBlock(glowProperties);
                glowProperties.SetColor("_EmissionColor", new Color(1f, 0.68f, 0.025f) * (2.4f + pulse * 3f + proximity * 4f));
                ringRenderer.SetPropertyBlock(glowProperties);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.GetComponentInParent<AircraftFlightController>()) return;
            if (AirRaceRunManager.Instance == null) return;
            AirRaceRunManager.Instance.CollectCan();
            if (AirRaceVFXController.Instance != null) AirRaceVFXController.Instance.CanCollected(transform.position);
            Destroy(gameObject);
        }
    }
}
