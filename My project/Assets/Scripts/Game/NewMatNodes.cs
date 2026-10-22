using UnityEngine;

// THE SIX NEWER MATERIALS (Sand, Bronze, Jade, Pearl, Bamboo, Silk).
// They aren't in the map file - they're put down by code when the game starts, always in the same spots
// (same random seed), so everyone online gets the same map and NetNodes matches them up.
public static class NewMatNodes
{
    const int PerMaterial = 12;

    public static void Spawn()
    {
        if (!MLGame.ArenaPresent) return;
        var root = new GameObject("New Material Spots").transform;
        Vector3 c = MLGame.ArenaCenter;
        float r = MLGame.ArenaRadius * 0.92f;
        var rng = new System.Random(4242);
        Mat[] mats = { Mat.Sand, Mat.Bronze, Mat.Jade, Mat.Pearl, Mat.Bamboo, Mat.Silk };
        foreach (Mat m in mats)
        {
            int placed = 0;
            for (int tries = 0; tries < 200 && placed < PerMaterial; tries++)
            {
                double a = rng.NextDouble() * Mathf.PI * 2.0;
                double d = Mathf.Sqrt((float)rng.NextDouble()) * r;
                Vector3 p = c + new Vector3((float)System.Math.Cos(a) * (float)d, 0f, (float)System.Math.Sin(a) * (float)d);
                if (!Ground(ref p)) continue;
                bool nearLab = false;
                foreach (Laboratory l in Laboratory.All) if ((l.transform.position - p).sqrMagnitude < 14f * 14f) { nearLab = true; break; }
                if (nearLab) continue;
                Make(root, m, p, (float)rng.NextDouble() * 360f);
                placed++;
            }
        }
    }

    // Find the ground under p. Skips roofs, water and the inside of buildings.
    static bool Ground(ref Vector3 p)
    {
        Terrain t = Terrain.activeTerrain;
        float ty = t != null ? t.SampleHeight(p) + t.transform.position.y : 0f;
        if (Physics.Raycast(p + Vector3.up * 200f, Vector3.down, out RaycastHit hit, 400f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (t != null && hit.point.y > ty + 0.6f) return false;   // something's in the way (a building, a rock...)
            p.y = hit.point.y;
            return Vector3.Angle(hit.normal, Vector3.up) < 30f;
        }
        if (t == null) return false;
        p.y = ty;
        return true;
    }

    static void Make(Transform root, Mat m, Vector3 pos, float yaw)
    {
        const PrimitiveType Box = PrimitiveType.Cube, Ball = PrimitiveType.Sphere, Cyl = PrimitiveType.Cylinder;
        var go = new GameObject("Node_" + m);
        go.transform.SetParent(root, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        Transform t = go.transform;
        Color col = Items.MatColors[(int)m];
        Material main = Gfx.Lit(col, 0.4f), glow = Gfx.Glow(col, 1.6f);
        switch (m)
        {
            case Mat.Sand:          // a little dune with a glittering top
                Gfx.Part(t, Ball, new Vector3(0f, 0.05f, 0f), new Vector3(1.4f, 0.45f, 1.1f), main);
                Gfx.Part(t, Ball, new Vector3(0.3f, 0.1f, 0.2f), new Vector3(0.7f, 0.35f, 0.6f), main);
                Gfx.Part(t, Ball, new Vector3(0f, 0.25f, 0f), new Vector3(0.25f, 0.08f, 0.25f), glow);
                break;
            case Mat.Bronze:        // an old half-buried bronze statue piece
                Gfx.Part(t, Box, new Vector3(0f, 0.25f, 0f), new Vector3(0.6f, 0.5f, 0.5f), Gfx.Lit(col, 0.7f, 0.8f), new Vector3(0f, 0f, 12f));
                Gfx.Part(t, Ball, new Vector3(0.1f, 0.6f, 0f), Vector3.one * 0.35f, Gfx.Lit(col, 0.7f, 0.8f));
                Gfx.Part(t, Box, new Vector3(0f, 0.52f, 0f), new Vector3(0.62f, 0.05f, 0.52f), glow);
                break;
            case Mat.Jade:          // green stone shards
                for (int i = 0; i < 4; i++)
                    Gfx.Part(t, Box, new Vector3(Mathf.Cos(i * 1.6f) * 0.25f, 0.3f, Mathf.Sin(i * 1.6f) * 0.25f), new Vector3(0.18f, 0.6f + i * 0.08f, 0.18f), i == 0 ? glow : main, new Vector3(i * 9f - 12f, i * 40f, 15f - i * 8f));
                break;
            case Mat.Pearl:         // an open clam with a shiny pearl
                Gfx.Part(t, Ball, new Vector3(0f, 0.08f, 0f), new Vector3(0.8f, 0.15f, 0.7f), Gfx.Lit(new Color(0.8f, 0.75f, 0.85f), 0.5f));
                Gfx.Part(t, Ball, new Vector3(0f, 0.32f, -0.2f), new Vector3(0.8f, 0.15f, 0.7f), Gfx.Lit(new Color(0.8f, 0.75f, 0.85f), 0.5f), new Vector3(-50f, 0f, 0f));
                Gfx.Part(t, Ball, new Vector3(0f, 0.22f, 0.05f), Vector3.one * 0.22f, glow);
                break;
            case Mat.Bamboo:        // a clump of bamboo stalks
                for (int i = 0; i < 5; i++)
                {
                    float h = 1.2f + (i % 3) * 0.4f;
                    Vector3 b = new Vector3(Mathf.Cos(i * 2.4f) * 0.25f, h * 0.5f, Mathf.Sin(i * 2.4f) * 0.25f);
                    Gfx.Part(t, Cyl, b, new Vector3(0.09f, h * 0.5f, 0.09f), main);
                    Gfx.Part(t, Cyl, b + Vector3.up * 0.2f, new Vector3(0.1f, 0.02f, 0.1f), glow);
                }
                break;
            default:                // Silk: a big cocoon hanging off a twig frame
                Gfx.Part(t, Cyl, new Vector3(0f, 0.5f, 0f), new Vector3(0.05f, 0.5f, 0.05f), Gfx.Lit(new Color(0.45f, 0.32f, 0.2f), 0.2f));
                Gfx.Part(t, Ball, new Vector3(0.18f, 0.7f, 0f), new Vector3(0.32f, 0.5f, 0.32f), main);
                Gfx.Part(t, Ball, new Vector3(0.18f, 0.7f, 0f), new Vector3(0.2f, 0.3f, 0.2f), glow);
                break;
        }
        Gfx.AddLight(t, new Vector3(0f, 0.6f, 0f), col, 4f, 0.8f);
        var node = go.AddComponent<MaterialNode>();
        node.mat = m;
        node.range = 2.8f;
    }
}
