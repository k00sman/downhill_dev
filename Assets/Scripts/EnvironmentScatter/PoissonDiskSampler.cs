using System.Collections.Generic;
using UnityEngine;

namespace EnvironmentScatter
{
    /// <summary>
    /// Generates naturally distributed points using Poisson Disk Sampling.
    /// Avoids clumping by enforcing a minimum distance between all points.
    /// </summary>
    public static class PoissonDiskSampler
    {
        private const int K = 30; // candidates per active point before rejection

        /// <summary>
        /// Returns a list of 2D points (XZ) within the given bounds,
        /// with a minimum spacing of minDist between each point.
        /// </summary>
        public static List<Vector2> Sample(Rect bounds, float minDist)
        {
            float cellSize = minDist / Mathf.Sqrt(2f);
            int cols = Mathf.CeilToInt(bounds.width  / cellSize);
            int rows = Mathf.CeilToInt(bounds.height / cellSize);

            int[,] grid       = new int[cols, rows];
            var    points     = new List<Vector2>();
            var    active     = new List<Vector2>();

            // init grid to -1 (empty)
            for (int x = 0; x < cols; x++)
                for (int y = 0; y < rows; y++)
                    grid[x, y] = -1;

            // seed with a random point
            Vector2 seed = new Vector2(
                Random.Range(bounds.xMin, bounds.xMax),
                Random.Range(bounds.yMin, bounds.yMax));
            AddPoint(seed, bounds, cellSize, cols, rows, grid, points, active);

            while (active.Count > 0)
            {
                int idx    = Random.Range(0, active.Count);
                Vector2 origin = active[idx];
                bool found = false;

                for (int k = 0; k < K; k++)
                {
                    float   angle  = Random.Range(0f, Mathf.PI * 2f);
                    float   radius = Random.Range(minDist, minDist * 2f);
                    Vector2 candidate = origin + new Vector2(
                        Mathf.Cos(angle) * radius,
                        Mathf.Sin(angle) * radius);

                    if (!bounds.Contains(candidate)) continue;
                    if (!IsValid(candidate, bounds, cellSize, cols, rows, grid, points, minDist)) continue;

                    AddPoint(candidate, bounds, cellSize, cols, rows, grid, points, active);
                    found = true;
                    break;
                }

                if (!found)
                    active.RemoveAt(idx);
            }

            return points;
        }

        static bool IsValid(Vector2 p, Rect bounds, float cellSize,
                            int cols, int rows, int[,] grid,
                            List<Vector2> points, float minDist)
        {
            int gx = Mathf.FloorToInt((p.x - bounds.xMin) / cellSize);
            int gy = Mathf.FloorToInt((p.y - bounds.yMin) / cellSize);

            int x0 = Mathf.Max(0, gx - 2);
            int x1 = Mathf.Min(cols - 1, gx + 2);
            int y0 = Mathf.Max(0, gy - 2);
            int y1 = Mathf.Min(rows - 1, gy + 2);

            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    if (grid[x, y] >= 0 &&
                        Vector2.Distance(points[grid[x, y]], p) < minDist)
                        return false;

            return true;
        }

        static void AddPoint(Vector2 p, Rect bounds, float cellSize,
                             int cols, int rows, int[,] grid,
                             List<Vector2> points, List<Vector2> active)
        {
            int gx = Mathf.FloorToInt((p.x - bounds.xMin) / cellSize);
            int gy = Mathf.FloorToInt((p.y - bounds.yMin) / cellSize);
            gx = Mathf.Clamp(gx, 0, cols - 1);
            gy = Mathf.Clamp(gy, 0, rows - 1);

            grid[gx, gy] = points.Count;
            points.Add(p);
            active.Add(p);
        }
    }
}
