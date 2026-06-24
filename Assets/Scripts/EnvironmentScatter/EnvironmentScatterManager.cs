using System.Collections.Generic;
using UnityEngine;

namespace EnvironmentScatter
{
    /// <summary>
    /// Place this on a single manager GameObject in your scene.
    /// Assign your SplineZone components and terrain references,
    /// then hit Play — assets are scattered once on Start.
    /// </summary>
    public class EnvironmentScatterManager : MonoBehaviour
    {
        [Header("Zones")]
        [Tooltip("All SplineZone components in the scene. Order doesn't matter — exclusion zones always win.")]
        public List<SplineZone> zones = new();

        [Header("Scatter Bounds")]
        [Tooltip("World-space XZ rect that defines the overall scatterable area.")]
        public Rect scatterBounds = new(-100f, -100f, 200f, 200f);

        [Header("Terrain Sources")]
        [Tooltip("Optional Unity Terrain objects. Height is sampled from these if the point falls within them.")]
        public List<Terrain> unityTerrains = new();

        [Tooltip("Optional mesh terrain colliders (your FBX chunks). Height is raycasted from these.")]
        public List<Collider> meshTerrainColliders = new();

        [Tooltip("LayerMask used when raycasting mesh terrain for height.")]
        public LayerMask meshTerrainLayer = ~0;

        [Header("Scatter Settings")]
        [Tooltip("Global minimum spacing fallback if a zone doesn't specify one.")]
        public float globalMinSpacing = 1.5f;

        [Tooltip("Y offset applied to all spawned assets (tweak if assets float or sink).")]
        public float yOffset = 0f;

        [Tooltip("Random Y rotation range in degrees.")]
        public Vector2 yRotationRange = new(0f, 360f);

        [Header("Output")]
        [Tooltip("Spawned assets are parented here to keep the hierarchy tidy.")]
        public Transform scatterRoot;

        // ---------------------------------------------------------------

        private void Start()
        {
            if (scatterRoot == null)
            {
                GameObject go = new("ScatterRoot");
                scatterRoot = go.transform;
            }

            Scatter();
        }

        public void Scatter()
        {
            // Bake all zone polygons
            foreach (SplineZone zone in zones)
            {
                zone.Bake();
            }

            // Separate exclusion zones for fast lookup
            List<SplineZone> exclusionZones = new();
            List<SplineZone> spawnZones = new();

            foreach (SplineZone zone in zones)
            {
                if (zone.zoneType == ZoneType.Exclusion)
                {
                    exclusionZones.Add(zone);
                }
                else
                {
                    spawnZones.Add(zone);
                }
            }

            // Generate candidate points across the full scatter bounds
            float minSpacing = globalMinSpacing;
            // use the smallest zone spacing so no zone is under-sampled
            foreach (SplineZone zone in spawnZones)
            {
                if (zone.minSpacing < minSpacing)
                {
                    minSpacing = zone.minSpacing;
                }
            }

            List<Vector2> candidates = PoissonDiskSampler.Sample(scatterBounds, minSpacing);
            int spawned = 0;

            foreach (Vector2 xz in candidates)
            {
                // 1. Exclusion check — bail immediately if inside any exclusion zone
                bool excluded = false;
                foreach (SplineZone ex in exclusionZones)
                {
                    if (ex.Contains(xz)) { excluded = true; break; }
                }
                if (excluded)
                {
                    continue;
                }

                // 2. Find which spawn zone this point belongs to
                SplineZone ownerZone = null;
                foreach (SplineZone zone in spawnZones)
                {
                    if (zone.Contains(xz)) { ownerZone = zone; break; }
                }
                if (ownerZone == null)
                {
                    continue;
                }

                // 3. Sample terrain height at this XZ position
                if (!TryGetHeight(xz, out float y))
                {
                    continue;
                }

                // 4. Pick a prefab from the zone's weighted list
                WeightedPrefab? pick = ownerZone.PickRandom();
                if (pick == null || pick.Value.prefab == null)
                {
                    continue;
                }

                // 5. Spawn
                Vector3 worldPos = new(xz.x, y + yOffset, xz.y);
                float scale = Random.Range(pick.Value.minScaleMultiplier, pick.Value.maxScaleMultiplier);
                if (scale <= 0f)
                {
                    scale = 1f;
                }

                float yRot = Random.Range(yRotationRange.x, yRotationRange.y);
                GameObject go = Instantiate(
                    pick.Value.prefab,
                    worldPos,
                    Quaternion.Euler(0f, yRot, 0f),
                    scatterRoot);

                go.transform.localScale *= scale;
                spawned++;
            }

            Debug.Log($"[EnvironmentScatter] Spawned {spawned} assets from {candidates.Count} candidates.");
        }

        // ---------------------------------------------------------------
        // Height sampling — tries Unity Terrain first, then mesh raycast

        private bool TryGetHeight(Vector2 xz, out float height)
        {
            height = 0f;

            // Try Unity Terrain components
            foreach (Terrain terrain in unityTerrains)
            {
                if (terrain == null)
                {
                    continue;
                }

                TerrainData data = terrain.terrainData;
                Vector3 origin = terrain.transform.position;
                Rect tRect = new(origin.x, origin.z, data.size.x, data.size.z);

                if (tRect.Contains(xz))
                {
                    height = terrain.SampleHeight(new Vector3(xz.x, 0f, xz.y)) + origin.y;
                    return true;
                }
            }

            // Try mesh terrain via raycast
            if (meshTerrainColliders.Count > 0)
            {
                Ray ray = new(new Vector3(xz.x, 5000f, xz.y), Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit hit, 10000f, meshTerrainLayer))
                {
                    // Make sure we only hit one of our registered mesh colliders
                    foreach (Collider col in meshTerrainColliders)
                    {
                        if (col == hit.collider)
                        {
                            height = hit.point.y;
                            return true;
                        }
                    }
                }
            }

            return false; // point not over any terrain — skip it
        }

        // Draw scatter bounds in editor
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Vector3 center = new(
                scatterBounds.center.x, 0f, scatterBounds.center.y);
            Vector3 size = new(
                scatterBounds.width, 0.1f, scatterBounds.height);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
