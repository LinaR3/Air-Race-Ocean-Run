using UnityEngine;

namespace AirRace.Gameplay
{
    /// <summary>Subtle, inexpensive flight feedback made from short particle bursts and wingtip trails.</summary>
    public sealed class AirRaceVFXController : MonoBehaviour
    {
        public static AirRaceVFXController Instance { get; private set; }

        private ParticleSystem leftWingTrail;
        private ParticleSystem rightWingTrail;
        private AircraftFlightController aircraft;
        private AirRaceRunManager run;
        private float wingEmission;
        private static Material particleMaterial;
        public string LastEffectName { get; private set; }

        public void Initialize(AirRaceRunManager manager)
        {
            run = manager;
            aircraft = FindFirstObjectByType<AircraftFlightController>();
            if (aircraft == null) return;

            leftWingTrail = CreateTrail("VFX_Wingtip_Left", aircraft.transform, new Vector3(-7.2f, 0f, -0.2f));
            rightWingTrail = CreateTrail("VFX_Wingtip_Right", aircraft.transform, new Vector3(7.2f, 0f, -0.2f));
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (aircraft == null || run == null) return;
            float targetRate = run.IsBoosting ? 26f : aircraft.IsTurning ? 9f : 0f;
            wingEmission = Mathf.MoveTowards(wingEmission, targetRate, 100f * Time.deltaTime);
            SetRate(leftWingTrail, wingEmission);
            SetRate(rightWingTrail, wingEmission);
        }

        public void CheckpointPassed(Vector3 position)
        {
            Burst("VFX_Checkpoint_Pulse", position, new Color(1f, 0.79f, 0.12f), 26, 7.5f, 0.16f);
        }

        public void CanCollected(Vector3 position)
        {
            Burst("VFX_Energy_Collect", position, new Color(0.25f, 0.78f, 1f), 34, 9f, 0.2f);
        }

        public void PylonImpact(Vector3 position)
        {
            Burst("VFX_Pylon_Impact", position, new Color(1f, 0.39f, 0.12f), 20, 5f, 0.13f);
        }

        public void WaterImpact(Vector3 position)
        {
            Burst("VFX_Water_Splash", position, new Color(0.28f, 0.8f, 1f), 42, 7f, 0.2f);
        }

        private static ParticleSystem CreateTrail(string name, Transform parent, Vector3 localPosition)
        {
            var go = new GameObject(name, typeof(ParticleSystem));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            ParticleSystem system = go.GetComponent<ParticleSystem>();
            var main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = 0.62f;
            main.startSpeed = 4f;
            main.startSize = 0.14f;
            main.startColor = new Color(0.62f, 0.89f, 1f, 0.62f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 96;
            var emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 4f;
            shape.radius = 0.08f;
            var color = system.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(0.78f, 0.94f, 1f), 0f), new GradientColorKey(new Color(0.28f, 0.65f, 1f), 1f) },
                new[] { new GradientAlphaKey(0.65f, 0f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;
            ConfigureParticleRenderer(go.GetComponent<ParticleSystemRenderer>(), new Color(0.55f, 0.84f, 1f));
            return system;
        }

        private void Burst(string name, Vector3 position, Color tint, int count, float speed, float size)
        {
            LastEffectName = name;
            var go = new GameObject(name, typeof(ParticleSystem));
            go.transform.position = position;
            ParticleSystem system = go.GetComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.42f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.24f, 0.64f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.55f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.55f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(tint, Color.white);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = count + 2;
            var emission = system.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.35f;
            var color = system.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(tint, 0f), new GradientColorKey(Color.white, 0.28f), new GradientColorKey(tint, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;
            ConfigureParticleRenderer(go.GetComponent<ParticleSystemRenderer>(), tint);
            system.Play();
            Destroy(go, 1.5f);
        }

        private static void ConfigureParticleRenderer(ParticleSystemRenderer renderer, Color tint)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
            if (shader == null) return;
            if (particleMaterial == null)
                particleMaterial = new Material(shader) { name = "AirRace_Runtime_VFX_Material", color = Color.white };
            renderer.sharedMaterial = particleMaterial;
        }

        private static void SetRate(ParticleSystem system, float rate)
        {
            if (system == null) return;
            var emission = system.emission;
            emission.rateOverTime = rate;
            if (rate > 0f && !system.isPlaying) system.Play();
            else if (rate <= 0f && system.isPlaying) system.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            if (particleMaterial != null)
            {
                Destroy(particleMaterial);
                particleMaterial = null;
            }
        }
    }
}
