using UnityEngine;

namespace AirRace.Gameplay
{
    /// <summary>Flight feedback built from soft particle smoke and short impact bursts.</summary>
    public sealed class AirRaceVFXController : MonoBehaviour
    {
        public static AirRaceVFXController Instance { get; private set; }

        private ParticleSystem leftWingTrail;
        private ParticleSystem rightWingTrail;
        private AircraftFlightController aircraft;
        private AirRaceRunManager run;
        private float wingEmission;
        private static Material smokeMaterial;
        private static Material burstMaterial;
        private static Texture2D smokeTexture;
        public string LastEffectName { get; private set; }

        public void Initialize(AirRaceRunManager manager)
        {
            run = manager;
            aircraft = FindFirstObjectByType<AircraftFlightController>();
            if (aircraft == null) return;

            leftWingTrail = CreateSmokeTrail("VFX_Smoke_Wingtip_Left", aircraft.transform, new Vector3(-7.2f, 0f, -0.2f));
            rightWingTrail = CreateSmokeTrail("VFX_Smoke_Wingtip_Right", aircraft.transform, new Vector3(7.2f, 0f, -0.2f));
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
            float targetRate = run.IsBoosting ? 32f : aircraft.IsTurning ? 12f : 0f;
            wingEmission = Mathf.MoveTowards(wingEmission, targetRate, 72f * Time.deltaTime);
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

        private static ParticleSystem CreateSmokeTrail(string name, Transform parent, Vector3 localPosition)
        {
            var go = new GameObject(name, typeof(ParticleSystem));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            ParticleSystem system = go.GetComponent<ParticleSystem>();
            var main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(1.85f, 2.9f);
            main.startColor = new Color(0.84f, 0.89f, 0.94f, 0.5f);
            main.gravityModifier = -0.015f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 240;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 13f;
            shape.radius = 0.28f;
            var noise = system.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(0.2f, 0.48f);
            noise.frequency = 0.34f;
            noise.scrollSpeed = new ParticleSystem.MinMaxCurve(0.08f);
            noise.damping = true;
            noise.quality = ParticleSystemNoiseQuality.Low;
            var color = system.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.84f, 0.87f, 0.91f), 0.74f), new GradientColorKey(new Color(0.72f, 0.76f, 0.81f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.5f, 0.1f), new GradientAlphaKey(0.34f, 0.58f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;
            var size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.56f), new Keyframe(0.28f, 1f), new Keyframe(1f, 1.85f)));
            ConfigureSmokeRenderer(go.GetComponent<ParticleSystemRenderer>(), system);
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
            ConfigureBurstRenderer(go.GetComponent<ParticleSystemRenderer>());
            system.Play();
            Destroy(go, 1.5f);
        }

        private static void ConfigureSmokeRenderer(ParticleSystemRenderer renderer, ParticleSystem system)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortMode = ParticleSystemSortMode.OldestInFront;
            var sheet = system.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Grid;
            sheet.numTilesX = 4;
            sheet.numTilesY = 4;
            sheet.animation = ParticleSystemAnimationType.WholeSheet;
            sheet.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 1f));
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
            if (shader == null) return;
            if (smokeTexture == null) smokeTexture = CreateSmokeTexture();
            if (smokeMaterial == null)
            {
                smokeMaterial = new Material(shader) { name = "AirRace_Runtime_SoftSmoke_Material", hideFlags = HideFlags.DontSave };
                if (smokeMaterial.HasProperty("_BaseMap")) smokeMaterial.SetTexture("_BaseMap", smokeTexture);
                if (smokeMaterial.HasProperty("_MainTex")) smokeMaterial.SetTexture("_MainTex", smokeTexture);
                if (smokeMaterial.HasProperty("_Surface")) smokeMaterial.SetFloat("_Surface", 1f);
                smokeMaterial.color = Color.white;
            }
            renderer.sharedMaterial = smokeMaterial;
        }

        private static Texture2D CreateSmokeTexture()
        {
            const int tileSize = 64;
            const int tiles = 4;
            const int resolution = tileSize * tiles;
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, true)
            {
                name = "AirRace_Runtime_Animated_Smoke_Atlas",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };
            var pixels = new Color[resolution * resolution];
            var random = new System.Random(70421);
            for (int tileY = 0; tileY < tiles; tileY++)
            for (int tileX = 0; tileX < tiles; tileX++)
            {
                Vector2[] lobes = new Vector2[9];
                float[] radii = new float[lobes.Length];
                for (int i = 0; i < lobes.Length; i++)
                {
                    float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                    float radius = Mathf.Sqrt((float)random.NextDouble()) * 0.54f;
                    lobes[i] = new Vector2(0.5f + Mathf.Cos(angle) * radius, 0.5f + Mathf.Sin(angle) * radius);
                    radii[i] = Mathf.Lerp(0.13f, 0.34f, (float)random.NextDouble());
                }

                for (int y = 0; y < tileSize; y++)
                for (int x = 0; x < tileSize; x++)
                {
                    float u = (x + 0.5f) / tileSize;
                    float v = (y + 0.5f) / tileSize;
                    float density = 0f;
                    for (int i = 0; i < lobes.Length; i++)
                    {
                        Vector2 delta = new Vector2((u - lobes[i].x) * 0.92f, v - lobes[i].y);
                        float d = delta.magnitude / radii[i];
                        float lobe = 1f - Mathf.SmoothStep(0.56f, 1.12f, d);
                        density = Mathf.Max(density, lobe);
                    }

                    float broad = Mathf.PerlinNoise(u * 5.2f + tileX * 7.31f, v * 5.2f + tileY * 9.17f);
                    float detail = Mathf.PerlinNoise(u * 13.5f + 11f + tileY, v * 13.5f + 3f + tileX);
                    float edgeNoise = 0.73f + broad * 0.32f + detail * 0.12f;
                    float alpha = Mathf.Clamp01((density - 0.13f) * 1.27f * edgeNoise);
                    int px = tileX * tileSize + x;
                    int py = tileY * tileSize + y;
                    pixels[py * resolution + px] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static void ConfigureBurstRenderer(ParticleSystemRenderer renderer)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
            if (shader == null) return;
            if (burstMaterial == null)
                burstMaterial = new Material(shader) { name = "AirRace_Runtime_Burst_Material", hideFlags = HideFlags.DontSave, color = Color.white };
            renderer.sharedMaterial = burstMaterial;
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
            if (smokeMaterial != null)
            {
                Destroy(smokeMaterial);
                smokeMaterial = null;
            }
            if (smokeTexture != null)
            {
                Destroy(smokeTexture);
                smokeTexture = null;
            }
            if (burstMaterial != null)
            {
                Destroy(burstMaterial);
                burstMaterial = null;
            }
        }
    }
}
