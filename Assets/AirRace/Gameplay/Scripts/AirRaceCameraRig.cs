using UnityEngine;

namespace AirRace.Gameplay
{
    /// <summary>Stable rear chase framing with a visible horizon; no overhead/camera toggle.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class AirRaceCameraRig : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 chaseOffset = new Vector3(0f, 4.2f, -14f);
        [SerializeField] private float followSmoothing = 4.5f;
        [SerializeField] private float lookAhead = 30f;
        [SerializeField] private float aimHeight = 2.2f;
        private Camera targetCamera;
        private float defaultFov;

        public void SetTarget(Transform followTarget) => target = followTarget;

        public void SetFraming(Vector3 offset, float smoothing, float targetHeight, float forwardLook)
        {
            chaseOffset = offset;
            followSmoothing = smoothing;
            aimHeight = targetHeight;
            lookAhead = forwardLook;
        }

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
            defaultFov = targetCamera.fieldOfView;
        }

        private void Update()
        {
            bool boosting = AirRaceRunManager.Instance != null && AirRaceRunManager.Instance.IsBoosting;
            targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, boosting ? defaultFov + 7f : defaultFov, 3f * Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Keep the horizon level and let pitch show within the frame instead of rotating the whole camera.
            Quaternion headingRotation = Quaternion.Euler(0f, target.eulerAngles.y, 0f);
            Vector3 desired = target.position + headingRotation * chaseOffset;
            transform.position = Vector3.Lerp(transform.position, desired, followSmoothing * Time.deltaTime);
            Vector3 lookPoint = target.position + headingRotation * Vector3.forward * lookAhead + Vector3.up * aimHeight;
            Quaternion aim = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, aim, followSmoothing * Time.deltaTime);
        }
    }
}
