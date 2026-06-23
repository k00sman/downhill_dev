using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace EnvironmentScatter
{
    public enum ZoneType
    {
        Exclusion,  // no spawns ever
        Forest,     // general scatter zone
        Clearing,
        Dense,
        Custom      // use only the asset list below
    }

    [System.Serializable]
    public struct WeightedPrefab
    {
        public GameObject prefab;
        [Range(0f, 100f)] public float weight;
        [Range(0f, 1f)]   public float minScaleMultiplier;
        [Range(0f, 1f)]   public float maxScaleMultiplier;
    }

    /// <summary>
    /// Place this on any GameObject that also has a SplineContainer.
    /// The spline should be closed and laid out in world XZ space.
    /// </summary>
    [RequireComponent(typeof(SplineContainer))]
    public class SplineZone : MonoBehaviour
    {
        [Header("Zone Settings")]
        public ZoneType zoneType = ZoneType.Forest;

        [Tooltip("Minimum spacing between spawned assets in this zone (metres).")]
        public float minSpacing = 1.5f;

        [Tooltip("Assets to spawn. Weights are relative — no need to sum to 100.")]
        public List<WeightedPrefab> assets = new List<WeightedPrefab>();

        // cached spline points in world XZ (built once at scatter time)
        private Vector2[] _polygon;
        private Rect       _bounds;
        private bool       _ready;

        public Rect   Bounds  => _bounds;
        public bool   IsReady => _ready;

        /// <summary>Call this once before any point-in-zone tests.</summary>
        public void Bake(int sampleCount = 64)
        {
            var container = GetComponent<SplineContainer>();
            var spline    = container.Spline;

            _polygon = new Vector2[sampleCount];
            float xMin = float.MaxValue, xMax = float.MinValue;
            float yMin = float.MaxValue, yMax = float.MinValue;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleCount;
                Vector3 localPt  = spline.EvaluatePosition(t);
                Vector3 worldPt  = transform.TransformPoint(localPt);

                _polygon[i] = new Vector2(worldPt.x, worldPt.z);

                if (worldPt.x < xMin) xMin = worldPt.x;
                if (worldPt.x > xMax) xMax = worldPt.x;
                if (worldPt.z < yMin) yMin = worldPt.z;
                if (worldPt.z > yMax) yMax = worldPt.z;
            }

            _bounds = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
            _ready  = true;
        }

        /// <summary>Even-odd ray crossing test — works for any concave polygon.</summary>
        public bool Contains(Vector2 point)
        {
            if (!_ready) return false;
            if (!_bounds.Contains(point)) return false;

            bool inside = false;
            int  j      = _polygon.Length - 1;

            for (int i = 0; i < _polygon.Length; i++)
            {
                float xi = _polygon[i].x, yi = _polygon[i].y;
                float xj = _polygon[j].x, yj = _polygon[j].y;

                bool intersect = ((yi > point.y) != (yj > point.y)) &&
                                 (point.x < (xj - xi) * (point.y - yi) / (yj - yi) + xi);
                if (intersect) inside = !inside;
                j = i;
            }

            return inside;
        }

        /// <summary>Picks a random prefab from the weighted list.</summary>
        public WeightedPrefab? PickRandom()
        {
            if (assets == null || assets.Count == 0) return null;

            float total = 0f;
            foreach (var a in assets) total += a.weight;
            if (total <= 0f) return null;

            float roll = Random.Range(0f, total);
            float acc  = 0f;

            foreach (var a in assets)
            {
                acc += a.weight;
                if (roll <= acc) return a;
            }

            return assets[assets.Count - 1];
        }

        // Draw the zone outline in the editor
        void OnDrawGizmos()
        {
            var container = GetComponent<SplineContainer>();
            if (container == null) return;

            Gizmos.color = zoneType == ZoneType.Exclusion
                ? new Color(1f, 0.2f, 0.2f, 0.6f)
                : new Color(0.2f, 1f, 0.4f, 0.4f);

            var spline = container.Spline;
            int steps  = 48;
            Vector3 prev = transform.TransformPoint(spline.EvaluatePosition(0f));

            for (int i = 1; i <= steps; i++)
            {
                float   t    = i / (float)steps;
                Vector3 curr = transform.TransformPoint(spline.EvaluatePosition(t));
                Gizmos.DrawLine(prev, curr);
                prev = curr;
            }
        }
    }
}
