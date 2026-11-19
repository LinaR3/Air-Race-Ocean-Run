using UnityEngine;
using UnityEngine.InputSystem;

namespace AirRace.Gameplay
{
    [RequireComponent(typeof(Camera))]
    public sealed class AirRaceCameraRig : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 chaseOffset = new Vector3(0f, 22f, -17f);
        [SerializeField] private Vector3 closeOffset = new Vector3(0f, 13f, -11f);
        [SerializeField] private float followSmoothing = 6f;
        [SerializeField] private float aimHeight = 1f;
        private Camera targetCamera;
        private bool closeCamera;
        private float defaultFov;

        public void SetTarget(Transform followTarget) => target = followTarget;

        public void SetFraming(Vector3 defaultOffset, Vector3 alternateOffset, float smoothing, float targetHeight)
        {
            chaseOffset = defaultOffset;
            closeOffset = alternateOffset;
            followSmoothing = smoothing;
            aimHeight = targetHeight;
        }

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
            defaultFov = targetCamera.fieldOfView;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame) closeCamera = !closeCamera;
            bool boosting = AirRaceRunManager.Instance != null && AirRaceRunManager.Instance.IsBoosting;
            targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, boosting ? defaultFov + 8f : defaultFov, 3f * Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 offset = closeCamera ? closeOffset : chaseOffset;
            Vector3 desired = target.position + target.rotation * offset;
            transform.position = Vector3.Lerp(transform.position, desired, followSmoothing * Time.deltaTime);
            Vector3 lookPoint = target.position + target.forward * 12f + Vector3.up * aimHeight;
            Quaternion aim = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, aim, followSmoothing * Time.deltaTime);
        }
    }
}
