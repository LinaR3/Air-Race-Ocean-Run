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
        [SerializeField] private float impactSeparationDuration = 0.28f;
        [SerializeField] private float impactRecoveryDuration = 0.22f;
        [SerializeField] private float impactSeparationSpeed = 17f;
        [SerializeField] private float impactDamageCooldown = 0.65f;
        [SerializeField] private float impactAvoidanceAngle = 30f;

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
        private Vector3 impactSeparationVelocity;
        private float impactRecoveryRemaining;
        private float impactRecoveryTotal;
        private float impactDamageCooldownRemaining;
        private Quaternion impactStartRotation;
        private float impactEscapeSign;

        public bool IsRecoveringFromImpact => impactRecoveryRemaining > 0f;

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

            impactDamageCooldownRemaining = Mathf.Max(0f, impactDamageCooldownRemaining - Time.fixedDeltaTime);

            if (impactRecoveryRemaining > 0f)
            {
                impactRecoveryRemaining = Mathf.Max(0f, impactRecoveryRemaining - Time.fixedDeltaTime);
                float turnProgress = 1f - impactRecoveryRemaining / Mathf.Max(0.01f, impactRecoveryTotal);
                float turnAngle = impactEscapeSign * impactAvoidanceAngle * Mathf.SmoothStep(0f, 1f, turnProgress);
                body.MoveRotation(Quaternion.AngleAxis(turnAngle, Vector3.up) * impactStartRotation);
                if (impactRecoveryRemaining > impactRecoveryTotal - impactSeparationDuration)
                {
                    // Hold the outward impulse briefly. Normal cruise velocity is
                    // otherwise reassigned every physics tick and pins the plane
                    // against the collider before it can separate.
                    body.linearVelocity = impactSeparationVelocity;
                }
                else
                {
                    float blend = 1f - impactRecoveryRemaining / Mathf.Max(0.01f, impactRecoveryTotal - impactSeparationDuration);
                    float recoveryCruiseSpeed = cruiseSpeed;
                    if (AirRaceRunManager.Instance != null && AirRaceRunManager.Instance.IsBoosting) recoveryCruiseSpeed *= boostSpeedMultiplier;
                    Vector3 cruiseVelocity = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized * recoveryCruiseSpeed + Vector3.up * verticalSpeed;
                    body.linearVelocity = Vector3.Lerp(impactSeparationVelocity, cruiseVelocity, Mathf.SmoothStep(0f, 1f, blend));
                }
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
            if (collision.contactCount == 0) return;
            if (impactRecoveryRemaining > 0f) return;
            ContactPoint contact = collision.GetContact(0);
            BeginImpactRecovery(contact.normal, contact.point);
        }

        private void BeginImpactRecovery(Vector3 contactNormal, Vector3 contactPoint)
        {
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 normal = Vector3.ProjectOnPlane(contactNormal, Vector3.up).normalized;
            if (normal.sqrMagnitude < 0.01f) normal = -forward;

            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 contactOffset = Vector3.ProjectOnPlane(contactPoint - transform.position, Vector3.up);
            float contactSide = Vector3.Dot(contactOffset, right);
            impactEscapeSign = Mathf.Abs(contactSide) < 0.1f ? -1f : -Mathf.Sign(contactSide);
            impactStartRotation = body.rotation;

            // Always bias the response away and slightly backwards, even for a
            // grazing hit whose contact normal points along the flight direction.
            // Add a lateral component so cruise resumes on a clear line past the pylon.
            Vector3 retreatDirection = (normal - forward * 0.65f + right * impactEscapeSign * 0.55f).normalized;
            impactSeparationVelocity = retreatDirection * impactSeparationSpeed + Vector3.up * Mathf.Max(2f, verticalSpeed * 0.25f);
            impactRecoveryTotal = impactSeparationDuration + impactRecoveryDuration;
            impactRecoveryRemaining = impactRecoveryTotal;

            if (impactDamageCooldownRemaining <= 0f)
            {
                if (AirRaceRunManager.Instance != null) AirRaceRunManager.Instance.HitPylon();
                impactDamageCooldownRemaining = impactDamageCooldown;
            }
        }
    }
}
