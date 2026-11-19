using UnityEngine;

namespace AirRace.Gameplay
{
    /// <summary>
    /// Replaces the imported segmented mesh collision on the visual pylon with
    /// one smooth capsule and one simple raft box at runtime.
    /// </summary>
    public sealed class AirRacePylonCollisionProxy : MonoBehaviour
    {
        [SerializeField, Range(0.25f, 0.5f)] private float coneRadiusFactor = 0.34f;
        [SerializeField] private float minimumRaftThickness = 0.8f;
        [SerializeField] private float raftPadding = 1.04f;
        private static PhysicsMaterial lowFrictionMaterial;

        private void Awake()
        {
            Transform inflatable = transform.Find("Inflatable_Pylon_25m");
            Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);

            Bounds coneBounds;
            if (inflatable != null && TryGetRootLocalBounds(inflatable.GetComponentsInChildren<Renderer>(true), out coneBounds))
            {
                var cone = gameObject.AddComponent<CapsuleCollider>();
                cone.direction = 1;
                cone.center = coneBounds.center;
                cone.radius = Mathf.Max(0.65f, Mathf.Max(coneBounds.size.x, coneBounds.size.z) * coneRadiusFactor);
                cone.height = Mathf.Max(coneBounds.size.y, cone.radius * 2f);
                cone.sharedMaterial = GetLowFrictionMaterial();
            }

            var raftRenderers = new System.Collections.Generic.List<Renderer>();
            foreach (Renderer renderer in allRenderers)
            {
                if (inflatable == null || !renderer.transform.IsChildOf(inflatable)) raftRenderers.Add(renderer);
            }

            Bounds raftBounds;
            if (raftRenderers.Count > 0 && TryGetRootLocalBounds(raftRenderers.ToArray(), out raftBounds))
            {
                var raft = gameObject.AddComponent<BoxCollider>();
                raft.center = raftBounds.center;
                raft.size = new Vector3(
                    Mathf.Max(1f, raftBounds.size.x * raftPadding),
                    Mathf.Max(minimumRaftThickness, raftBounds.size.y),
                    Mathf.Max(1f, raftBounds.size.z * raftPadding));
                raft.sharedMaterial = GetLowFrictionMaterial();
            }

            foreach (MeshCollider meshCollider in GetComponentsInChildren<MeshCollider>(true))
                meshCollider.enabled = false;
        }

        private static PhysicsMaterial GetLowFrictionMaterial()
        {
            if (lowFrictionMaterial == null)
            {
                lowFrictionMaterial = new PhysicsMaterial("AirRace_Pylon_LowFriction")
                {
                    dynamicFriction = 0f,
                    staticFriction = 0f,
                    bounciness = 0.05f,
                    frictionCombine = PhysicsMaterialCombine.Minimum,
                    bounceCombine = PhysicsMaterialCombine.Average
                };
            }

            return lowFrictionMaterial;
        }

        private bool TryGetRootLocalBounds(Renderer[] renderers, out Bounds bounds)
        {
            bounds = default;
            bool initialized = false;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;
                Bounds local = renderer.localBounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = new Vector3(
                        (corner & 1) == 0 ? local.min.x : local.max.x,
                        (corner & 2) == 0 ? local.min.y : local.max.y,
                        (corner & 4) == 0 ? local.min.z : local.max.z);
                    point = transform.InverseTransformPoint(renderer.transform.TransformPoint(point));
                    if (!initialized)
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        initialized = true;
                    }
                    else bounds.Encapsulate(point);
                }
            }
            return initialized;
        }
    }
}
