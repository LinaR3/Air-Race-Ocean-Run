using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AirRace.Gameplay
{
    /// <summary>Arcade flight: pitch and bank determine the actual travel direction.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class AircraftFlightController : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float cruiseSpeed = 34f;
        [SerializeField] private float boostSpeedMultiplier = 1.35f;
        [SerializeField] private float maximumPitch = 24f;
        [SerializeField] private float pitchResponse = 58f;
        [SerializeField] private float maximumBank = 52f;
        [SerializeField] private float bankResponse = 105f;
        [SerializeField] private float turnRate = 52f;
        [SerializeField] private float maximumAltitude = 110f;
        [SerializeField] private float waterLine = -4.5f;
        [SerializeField] private float worldBoundaryHalfExtent = 900f;
        [SerializeField] private float brakeSpeedMultiplier = 0.68f;
        [SerializeField] private float brakeTurnMultiplier = 1.65f;
        [SerializeField] private float impactRecoilSpeed = 14f;
        [SerializeField] private float impactRecoveryDuration = 0.32f;
        [SerializeField] private float impactDamageCooldown = 0.65f;

        private Rigidbody body;
        private float pitchInput;
        private float turnInput;
        private bool braking;
        private float pitchAngle;
        private float bankAngle;
        private float heading;
        private float impactRecoveryRemaining;
        private float impactDamageCooldownRemaining;
        private float stuntLiftVelocity;
        private float rollProgress;
        private float rollStartAngle;
        private float rollTargetAngle;
        private float visualRoll;
        private bool rolling;
        private bool crashed;
        private int queuedRolls;

        public bool IsRecoveringFromImpact => impactRecoveryRemaining > 0f;
        public float AirSpeed => body != null ? body.linearVelocity.magnitude : cruiseSpeed;
        public float Altitude => transform.position.y - waterLine;
        public float Heading => heading;

        public void SetVisualRoot(Transform root) => visualRoot = root;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.None;
            heading = transform.eulerAngles.y;
            if (visualRoot == null && transform.childCount > 0) visualRoot = transform.GetChild(0);
        }

        private void Update()
        {
            if (AirRaceRunManager.Instance != null && AirRaceRunManager.Instance.State != RaceState.Racing)
            {
                if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) AirRaceRunManager.Instance.Restart();
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || crashed) return;
            pitchInput = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            turnInput = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            braking = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;

            if (keyboard.spaceKey.wasPressedThisFrame && AirRaceRunManager.Instance != null && AirRaceRunManager.Instance.TryActivateBoost())
                StartBarrelRoll();

            if (body.position.y <= waterLine + 1.5f && AirRaceRunManager.Instance != null)
            {
                crashed = true;
                AirRaceRunManager.Instance.Fail("El avión tocó el agua");
            }
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (crashed || (AirRaceRunManager.Instance != null && AirRaceRunManager.Instance.State != RaceState.Racing))
            {
                body.linearVelocity = Vector3.zero;
                return;
            }

            impactDamageCooldownRemaining = Mathf.Max(0f, impactDamageCooldownRemaining - dt);
            if (impactRecoveryRemaining > 0f)
            {
                impactRecoveryRemaining = Mathf.Max(0f, impactRecoveryRemaining - dt);
                // Keep the contact impulse free to move the aircraft away from the pylon.
                body.angularVelocity = Vector3.zero;
                return;
            }

            float speed = cruiseSpeed;
            if (braking) speed *= brakeSpeedMultiplier;
            if (AirRaceRunManager.Instance != null && AirRaceRunManager.Instance.IsBoosting) speed *= boostSpeedMultiplier;

            pitchAngle = Mathf.MoveTowards(pitchAngle, pitchInput * maximumPitch, pitchResponse * dt);
            float targetBank = -turnInput * maximumBank;
            bankAngle = Mathf.MoveTowards(bankAngle, targetBank, bankResponse * dt);
            float bankTurn = Mathf.Sin(Mathf.Abs(bankAngle) * Mathf.Deg2Rad);
            float turnScale = braking ? brakeTurnMultiplier : 1f;
            heading += turnInput * turnRate * bankTurn * turnScale * dt;

            Quaternion orientation = Quaternion.Euler(-pitchAngle, heading, bankAngle);
            body.MoveRotation(orientation);
            stuntLiftVelocity = Mathf.MoveTowards(stuntLiftVelocity, 0f, 3f * dt);
            Vector3 flightVelocity = orientation * Vector3.forward * speed + Vector3.up * stuntLiftVelocity;
            body.linearVelocity = flightVelocity;

            Vector3 nextPosition = body.position + flightVelocity * dt;
            bool hitBoundary = Mathf.Abs(nextPosition.x) > worldBoundaryHalfExtent || Mathf.Abs(nextPosition.z) > worldBoundaryHalfExtent;
            if (hitBoundary)
            {
                nextPosition.x = Mathf.Clamp(nextPosition.x, -worldBoundaryHalfExtent, worldBoundaryHalfExtent);
                nextPosition.z = Mathf.Clamp(nextPosition.z, -worldBoundaryHalfExtent, worldBoundaryHalfExtent);
                body.position = nextPosition;
                heading = Mathf.Atan2(-body.position.x, -body.position.z) * Mathf.Rad2Deg;
                bankAngle = 0f;
            }

            if (body.position.y > maximumAltitude)
            {
                body.position = new Vector3(body.position.x, maximumAltitude, body.position.z);
                pitchAngle = Mathf.Min(0f, pitchAngle);
            }
        }

        private void LateUpdate()
        {
            if (visualRoot == null) return;
            if (rolling)
            {
                rollProgress += Time.deltaTime / 0.58f;
                visualRoll = Mathf.Lerp(rollStartAngle, rollTargetAngle, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(rollProgress)));
                if (rollProgress >= 1f)
                {
                    rolling = false;
                    visualRoll = 0f;
                    if (AirRaceRunManager.Instance != null) AirRaceRunManager.Instance.ScoreCompletedRoll();
                    stuntLiftVelocity = Mathf.Min(8f, stuntLiftVelocity + 1.2f);
                    if (queuedRolls > 0) { queuedRolls--; BeginBarrelRoll(); }
                }
            }
            // The aircraft root now owns real pitch and bank. Child rotation is reserved for the stunt.
            visualRoot.localRotation = Quaternion.Euler(0f, 0f, visualRoll);
        }

        private void StartBarrelRoll()
        {
            if (visualRoot == null) return;
            if (rolling) { queuedRolls = Mathf.Min(queuedRolls + 1, 3); return; }
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
            if (crashed || collision.contactCount == 0 || impactRecoveryRemaining > 0f) return;
            ContactPoint contact = collision.GetContact(0);
            Vector3 away = contact.normal.normalized;
            if (away.sqrMagnitude < 0.01f) away = -transform.forward;
            // Recoil follows the collision normal; no artificial yaw that can make the plane orbit the pylon.
            away = (away + Vector3.up * 0.24f).normalized;
            body.linearVelocity = away * impactRecoilSpeed;
            body.angularVelocity = Vector3.zero;
            impactRecoveryRemaining = impactRecoveryDuration;

            bool thisIsAircraft = IsAircraftCollider(contact.thisCollider);
            Collider ownCollider = thisIsAircraft ? contact.thisCollider : contact.otherCollider;
            Collider obstacleCollider = thisIsAircraft ? contact.otherCollider : contact.thisCollider;
            AirRacePylonCollisionProxy pylon = FindPylonProxy(contact);
            if (pylon != null)
            {
                Collider[] pylonColliders = pylon.GetComponents<Collider>();
                bool[] enabledStates = new bool[pylonColliders.Length];
                for (int i = 0; i < pylonColliders.Length; i++)
                {
                    enabledStates[i] = pylonColliders[i] != null && pylonColliders[i].enabled;
                    if (pylonColliders[i] != null) pylonColliders[i].enabled = false;
                }
                StartCoroutine(RestorePylonCollision(pylonColliders, enabledStates));
            }
            else if (ownCollider != null && obstacleCollider != null)
            {
                Physics.IgnoreCollision(ownCollider, obstacleCollider, true);
                StartCoroutine(RestoreCollision(ownCollider, obstacleCollider));
            }

            if (impactDamageCooldownRemaining <= 0f)
            {
                if (AirRaceRunManager.Instance != null) AirRaceRunManager.Instance.HitPylon();
                impactDamageCooldownRemaining = impactDamageCooldown;
            }
        }

        private IEnumerator RestoreCollision(Collider aircraftCollider, Collider obstacleCollider)
        {
            yield return new WaitForSeconds(0.62f);
            if (aircraftCollider != null && obstacleCollider != null)
                Physics.IgnoreCollision(aircraftCollider, obstacleCollider, false);
        }

        private IEnumerator RestorePylonCollision(Collider[] pylonColliders, bool[] enabledStates)
        {
            yield return new WaitForSeconds(0.82f);
            for (int i = 0; i < pylonColliders.Length; i++)
                if (pylonColliders[i] != null) pylonColliders[i].enabled = enabledStates[i];
        }

        private static AirRacePylonCollisionProxy FindPylonProxy(ContactPoint contact)
        {
            AirRacePylonCollisionProxy pylon = contact.thisCollider != null
                ? contact.thisCollider.GetComponentInParent<AirRacePylonCollisionProxy>()
                : null;
            return pylon != null || contact.otherCollider == null
                ? pylon
                : contact.otherCollider.GetComponentInParent<AirRacePylonCollisionProxy>();
        }

        private bool IsAircraftCollider(Collider candidate)
        {
            return candidate != null && (candidate.transform == transform || candidate.transform.IsChildOf(transform));
        }
    }
}
