using UnityEngine;
using UnityEngine.InputSystem;

namespace AirRace.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class AircraftFlightController : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float cruiseSpeed = 32f;
        [SerializeField] private float boostSpeedMultiplier = 1.35f;
        [SerializeField] private float yawRate = 34f;
        [SerializeField] private float climbSpeed = 8f;
        [SerializeField] private float verticalAcceleration = 18f;
        [SerializeField] private float visualPitchAngle = 16f;
        [SerializeField] private float maximumAltitude = 70f;
        [SerializeField] private float waterLine = -4.5f;
        [SerializeField] private float worldBoundaryHalfExtent = 900f;

        private Rigidbody body;
        private float pitchInput;
        private float yawInput;
        private float bankInput;
        private float verticalSpeed;
        private float visualRoll;
        private float rollProgress;
        private bool rolling;
        private float rollStartAngle;
        private float rollTargetAngle;
        private float visualBank;
        private bool crashed;
        private int queuedRolls;
        private float stuntLiftVelocity;

        public void SetVisualRoot(Transform root) => visualRoot = root;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.None;
            if (visualRoot == null && transform.childCount > 0) visualRoot = transform.GetChild(0);
        }

        private void Update()
        {
            if (AirRaceRunManager.Instance != null && AirRaceRunManager.Instance.State != RaceState.Racing)
            {
                if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) AirRaceRunManager.Instance.Restart();
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null || crashed) return;
            pitchInput = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            yawInput = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            bankInput = yawInput;

            if (keyboard.spaceKey.wasPressedThisFrame && AirRaceRunManager.Instance != null)
            {
                if (AirRaceRunManager.Instance.TryActivateBoost()) StartBarrelRoll();
            }

            if (body.position.y <= waterLine + 1.5f && AirRaceRunManager.Instance != null)
            {
                crashed = true;
                AirRaceRunManager.Instance.Fail("El avión tocó el agua");
            }
        }

        private void FixedUpdate()
        {
            if (crashed || (AirRaceRunManager.Instance != null && AirRaceRunManager.Instance.State != RaceState.Racing))
            {
                body.linearVelocity = Vector3.zero;
                return;
            }

            float speed = cruiseSpeed;
            if (AirRaceRunManager.Instance != null && AirRaceRunManager.Instance.IsBoosting) speed *= boostSpeedMultiplier;

            Quaternion yaw = Quaternion.AngleAxis(yawInput * yawRate * Time.fixedDeltaTime, Vector3.up);
            body.MoveRotation(yaw * body.rotation);
            verticalSpeed = Mathf.MoveTowards(verticalSpeed, pitchInput * climbSpeed, verticalAcceleration * Time.fixedDeltaTime);
            stuntLiftVelocity = Mathf.MoveTowards(stuntLiftVelocity, 0f, 1.1f * Time.fixedDeltaTime);
            Vector3 horizontalForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            body.linearVelocity = horizontalForward * speed + Vector3.up * (verticalSpeed + stuntLiftVelocity);

            Vector3 nextPosition = body.position + body.linearVelocity * Time.fixedDeltaTime;
            bool hitBoundary = Mathf.Abs(nextPosition.x) > worldBoundaryHalfExtent || Mathf.Abs(nextPosition.z) > worldBoundaryHalfExtent;
            if (hitBoundary)
            {
                nextPosition.x = Mathf.Clamp(nextPosition.x, -worldBoundaryHalfExtent, worldBoundaryHalfExtent);
                nextPosition.z = Mathf.Clamp(nextPosition.z, -worldBoundaryHalfExtent, worldBoundaryHalfExtent);
                body.position = nextPosition;
                Vector3 inward = new Vector3(-body.position.x, 0f, -body.position.z).normalized;
                if (inward.sqrMagnitude > 0.01f) body.MoveRotation(Quaternion.RotateTowards(body.rotation, Quaternion.LookRotation(inward, Vector3.up), yawRate * 2f * Time.fixedDeltaTime));
            }

            if (transform.position.y > maximumAltitude)
            {
                body.position = new Vector3(body.position.x, maximumAltitude, body.position.z);
                verticalSpeed = Mathf.Min(0f, verticalSpeed);
                body.linearVelocity = horizontalForward * speed + Vector3.up * Mathf.Min(0f, stuntLiftVelocity);
            }
        }

        private void LateUpdate()
        {
            if (visualRoot == null) return;

            float bankTarget = -bankInput * 36f;
            visualBank = Mathf.Lerp(visualBank, bankTarget, 7f * Time.deltaTime);

            if (rolling)
            {
                rollProgress += Time.deltaTime / 0.58f;
                float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(rollProgress));
                visualRoll = Mathf.Lerp(rollStartAngle, rollTargetAngle, eased);
                if (rollProgress >= 1f)
                {
                    rolling = false;
                    visualRoll = 0f;
                    if (AirRaceRunManager.Instance != null) AirRaceRunManager.Instance.ScoreCompletedRoll();
                    stuntLiftVelocity = Mathf.Min(13f, stuntLiftVelocity + 2.2f * (AirRaceRunManager.Instance != null ? AirRaceRunManager.Instance.Combo : 1));
                    if (queuedRolls > 0)
                    {
                        queuedRolls--;
                        BeginBarrelRoll();
                    }
                }
            }
            visualRoot.localRotation = Quaternion.Euler(-pitchInput * visualPitchAngle, 0f, visualBank + visualRoll);
        }

        private void StartBarrelRoll()
        {
            if (visualRoot == null) return;
            if (rolling)
            {
                queuedRolls++;
                return;
            }
            BeginBarrelRoll();
        }

        private void BeginBarrelRoll()
        {
            rollStartAngle = visualRoll;
            rollTargetAngle = rollStartAngle + 360f;
            rollProgress = 0f;
            rolling = true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (crashed) return;
            Vector3 normal = collision.GetContact(0).normal;
            body.linearVelocity = Vector3.Reflect(body.linearVelocity, normal) * 0.55f - transform.forward * 4f;
            if (AirRaceRunManager.Instance != null) AirRaceRunManager.Instance.HitPylon();
        }
    }
}
